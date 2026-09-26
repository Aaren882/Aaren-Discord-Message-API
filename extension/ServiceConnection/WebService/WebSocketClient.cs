using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Component.Websocket;
using Components.Entity;
using Microsoft.Extensions.Logging;

namespace ServiceConnection.WebService;

public sealed class WebsocketClient(
	ILogger<IWebsocketWorker> logger,
	ServiceRequestHandler serviceRequestHandler
) : WebsocketWorker
{
	protected override ILogger<IWebsocketWorker> Logger => logger;
	public event Action<Arma3Payload>? MessageReceived;
	public event Action? Connected;
	public event Action? Disconnected;

	public override void PostReceived(in Stream assembledStream, WebSocketMessageType messageType)
	{
		try
		{
			if (assembledStream.Length == 0)
			{
				Logger.LogWarning("Received empty \"{MessageType}\" Message.", messageType);
				return;
			}

			var payload = JsonSerializer.Deserialize(
				assembledStream,
				Arma3PayloadJsonSerializerContext.Default.Arma3Payload
			)!;

			//- Processing Requests
			if (payload is Arma3PayloadServiceRequest request)
			{
				if (!serviceRequestHandler.TryAddRequest(request))
					throw new OverflowException("Service request limit exceeded. Cannot process new requests.");
			}
			MessageReceived?.Invoke(payload);
		}
		catch (Exception ex) when (ex is InvalidOperationException || ex is NotSupportedException)
		{
			Logger.LogWarning(ex, "Failed to process message due to invalid operation or unsupported type.");
		}
		catch (Exception e) when (e is JsonException || e is OverflowException)
		{
			Logger.LogWarning(e, "Something went wrong during/after parsing incoming payload.");
		}
		catch (Exception e)
		{
			Logger.LogError(e, "Fatal Exception: ");
		}
	}
	public async ValueTask SendBinaryAsync(string accessName, string filePath, Arma3PayloadBinary payloadBinary)
	{
		ArgumentNullException.ThrowIfNull(WebSocketStateMachine, nameof(WebSocketStateMachine));

		if (!HasConnection)
		{
			Logger.LogError("WebSocket is not connected. Cannot send message.");
			return;
		}

		Logger.LogInformation("Sending Binary: \n File: {File} \n Header: {header}", filePath, payloadBinary);
		var totalChunks = payloadBinary.TotalChunks;
		if (totalChunks < 1)
		{
			FileInfo fileInfo = new(filePath);
			totalChunks = payloadBinary.TotalChunks = (int)Math.Ceiling((double)fileInfo.Length / BufferSize);
		}
		// Send Chunks (as binary messages)
		await using (FileStream fs = new(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, BufferSize))
		{
			var readBuffer = (new byte[BufferSize]).AsMemory<byte>();
			var identifier = payloadBinary.GetIdentifier(accessName);

			for (var i = 1; i < totalChunks + 1; i++)
			{
				int readLength = await fs.ReadAsync(readBuffer, CancellationToken);
				Arma3PayloadBinaryContent content = new(identifier, readBuffer[..readLength].ToArray(), i == totalChunks);

				Logger.LogDebug("SendBinaryAsync (Progress): {i}/{TotalChunks}", i, totalChunks);
				var payload = content.ToJsonBytes();
				await WebSocketStateMachine.SendMessageAsync(payload, WebSocketMessageType.Binary, true);
			}
		}

		Logger.LogInformation("Sent Binary: {File}", filePath);
	}
	public async ValueTask SendRptLinesAsync(string accessName, string filePath, Arma3PayloadBinary payloadBinary, int linesCount)
	{
		ArgumentNullException.ThrowIfNull(WebSocketStateMachine, nameof(WebSocketStateMachine));

		if (!HasConnection)
		{
			Logger.LogError("WebSocket is not connected. Cannot send message.");
			return;
		}

		Logger.LogInformation("Sending RPT : {linesCount} lines", linesCount);

		var sw = Stopwatch.StartNew();
		await using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

		var lastLines = await GetLastLinesAsync(fileStream, linesCount);
		lastLines.Reverse();

		var identifier = payloadBinary.GetIdentifier(accessName);
		var lineCount = lastLines.Count;
		var charCount = 0;

		Arma3PayloadBinaryContent content;
		ReadOnlyMemory<byte> bytes;
		foreach (var (line, i) in lastLines.Select((value, i) => (value, i)))
		{
			var wLine = line + "\n";
			charCount += wLine.Length;

			if (charCount > 1980)
			{
				Logger.LogWarning("SendRptLines has reached limit: \"{line}\".", line);
				lineCount = i;
				break;
			}

			content = new(identifier, Encoding.UTF8.GetBytes(wLine), false);
			bytes = content.ToJsonBytes();
			await WebSocketStateMachine.SendMessageAsync(bytes, WebSocketMessageType.Binary, true);
		}
		Logger.LogInformation("SendRptLines [{lineCount}]: {filePath}", lineCount, filePath);

		content = new(identifier, [], true);
		bytes = content.ToJsonBytes();
		await WebSocketStateMachine.SendMessageAsync(bytes, WebSocketMessageType.Binary, true);

		sw.Stop();
		Logger.LogInformation("{Function} Execution took: {Milliseconds} ms", nameof(SendRptLinesAsync), sw.ElapsedMilliseconds);

		static async ValueTask<List<string>> GetLastLinesAsync(FileStream stream, int count)
		{
			if (count <= 0) return [];
			using StreamReader reader = new(stream, Encoding.UTF8);
			Queue<string> queue = new(count);

			while (!reader.EndOfStream)
			{
				var line = await reader.ReadLineAsync();

				if (line is null) continue;

				if (queue.Count == count) queue.Dequeue();
				queue.Enqueue(line);
			}
			return queue.ToList();
		}
	}
	public async Task StartAsync(string uri, string? authToken)
	{
		if (WebSocketStateMachine is not null) throw new InvalidOperationException("Websocket Connection is already Established...");

		var webSocket = new ClientWebSocket();
		if (authToken != null)
			webSocket.Options.SetRequestHeader("Authorization", "Bearer " + authToken);

		await webSocket.ConnectAsync(new(uri), CancellationToken);
		Logger.LogInformation("Connected to server.");
		Connected?.Invoke();

		WebSocketStateMachine = new(this, Logger);
		_ = WebSocketStateMachine.StartAsync(webSocket); //- Don't block the thread (Client-Side)
	}
	public override async Task CloseAsync()
	{
		await base.CloseAsync();
		Disconnected?.Invoke();
		Logger.LogInformation("Disconnected from server.");
	}
}
