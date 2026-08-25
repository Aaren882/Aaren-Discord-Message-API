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
		using StreamReader reader = new(assembledStream, Encoding.UTF8);
		var receivedMessage = reader.ReadToEnd();
		if (string.IsNullOrEmpty(receivedMessage))
		{
			Logger.LogWarning("Received empty \"{MessageType}\" Message.", messageType);
			return;
		}

		var payload = JsonSerializer.Deserialize(
			receivedMessage,
			Arma3PayloadJsonSerializerContext.Default.Arma3Payload
		)!;
		if (payload is Arma3PayloadServiceRequest request)
		{
			Task.Run(async () => await serviceRequestHandler.RespondRequest(request))
				.GetAwaiter().GetResult();
		}
		MessageReceived?.Invoke(payload);
	}
	public async ValueTask SendBinaryAsync(string accessName, string filePath, Arma3PayloadBinary payloadBinary, int chunkSize = 60 * 1024)
	{
		ArgumentNullException.ThrowIfNull(WebSocketStateMachine, nameof(WebSocketStateMachine));
		Logger.LogInformation("Sending Binary: \n File: {File} \n Header: {header}", filePath, payloadBinary);

		// Send Chunks (as binary messages)
		await using (FileStream fs = new(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, chunkSize))
		{
			var readBuffer = (new byte[chunkSize]).AsMemory<byte>();
			var identifier = payloadBinary.GetIdentifier(accessName);

			for (var i = 1; i < payloadBinary.TotalChunks + 1; i++)
			{
				int readLength = await fs.ReadAsync(readBuffer, CancellationToken.None);
				Arma3PayloadBinaryContent content = new(identifier, readBuffer[..readLength].ToArray(), i == payloadBinary.TotalChunks);

				Logger.LogDebug("SendBinaryAsync (Progress): {i}/{TotalChunks}", i, payloadBinary.TotalChunks);
				var payload = JsonSerializer.SerializeToUtf8Bytes(
					content,
					Arma3PayloadJsonSerializerContext.Default.Arma3Payload
				);
				await WebSocketStateMachine.SendMessageAsync(payload, WebSocketMessageType.Binary, true);
			}
		}

		Logger.LogInformation("Sent Binary: {File}", filePath);
	}
	public async ValueTask SendRptLinesAsync(string accessName, string filePath, Arma3PayloadBinary payloadBinary, int linesCount)
	{
		Logger.LogInformation("Sending RPT : {linesCount} lines", linesCount);

		if (HasConnection)
		{
			var sw = Stopwatch.StartNew();
			await using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

			var lastLines = await GetLastLinesAsync(fileStream, linesCount);
			lastLines.Reverse();

			var identifier = payloadBinary.GetIdentifier(accessName);
			var lineCount = lastLines.Count;
			var charCount = 0;

			Arma3PayloadBinaryContent content;
			byte[] bytes;
			foreach (var (line, i) in lastLines.Select((value, i) => (value, i)))
			{
				var wLine = line + "\n";
				charCount += wLine.Length;

				content = new(identifier, Encoding.UTF8.GetBytes(wLine), false);
				bytes = JsonSerializer.SerializeToUtf8Bytes(content, Arma3PayloadJsonSerializerContext.Default.Arma3Payload);
				await WebSocketStateMachine!.SendMessageAsync(bytes, WebSocketMessageType.Binary, true);

				if (charCount > 1980)
				{
					Logger.LogWarning("SendRptLines has reached limit: \"{line}\".", line);
					lineCount = i;
					break;
				}
			}
			Logger.LogInformation("SendRptLines [{lineCount}]: {filePath}", lineCount, filePath);

			content = new(identifier, [], true);
			bytes = JsonSerializer.SerializeToUtf8Bytes(content, Arma3PayloadJsonSerializerContext.Default.Arma3Payload);
			await WebSocketStateMachine!.SendMessageAsync(bytes, WebSocketMessageType.Binary, true);

			sw.Stop();
			Logger.LogInformation("{Function} Execution took: {sw.ElapsedMilliseconds} ms", nameof(SendRptLinesAsync), sw.ElapsedMilliseconds);
		}
		else
		{
			Logger.LogError("WebSocket is not connected. Cannot send message.");
		}

		return;

		static async Task<List<string>> GetLastLinesAsync(FileStream stream, int count)
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

		await webSocket.ConnectAsync(new(uri), CancellationToken.None);
		Logger.LogInformation("Connected to server.");
		Connected?.Invoke();

		WebSocketStateMachine = new(this, Logger);
		await WebSocketStateMachine.StartAsync(webSocket);
	}
	public override async Task CloseAsync()
	{
		await base.CloseAsync();
		Disconnected?.Invoke();
		Logger.LogInformation("Disconnected from server.");
	}
}
