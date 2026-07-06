using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Components.Entity;
using static ExtensionComponents.ExtensionStartup;

namespace ServiceConnection.WebService;

public class WebSocketClient(string serverUri)
{
	private ClientWebSocket? _webSocket;
	private CancellationTokenSource? _cancellationTokenSource;

	public event Action<Arma3Payload>? MessageReceived;
	public event Action? Connected;
	public event Action? Disconnected;

	public WebSocketState? Status => _webSocket?.State;

	public async Task ConnectAsync(string? jwtToken)
	{
		try
		{
			if (Status == WebSocketState.Open)
				throw new Exception("WebSocket already connected."); 
			
			_webSocket = new ClientWebSocket();
			if (jwtToken != null)
				_webSocket.Options.SetRequestHeader("Authorization", "Bearer " + jwtToken);
			
			_cancellationTokenSource = new CancellationTokenSource();

			Logger(null ,$"Connecting to {serverUri}...");
			await _webSocket.ConnectAsync(new Uri(serverUri), _cancellationTokenSource.Token);

			Logger(null ,"Connected successfully!");
			Connected?.Invoke();

			// Start listening for messages
			_ = ReceiveMessages();
		}
		catch (Exception ex)
		{
			Logger(null ,$"Connection failed: {ex.Message}");
		}
	}
	public async Task SendBinaryAsync(string filePath, Arma3PayloadBinary payloadBinary, int chunkSize = 64 * 1024)
	{
		Logger(null, "INFO: Sending Binary");
		
		if (Status == WebSocketState.Open)
		{
			await Task.Delay(500); //- # Wait for the RPT get written first.   
			
			// Send Chunks (as binary messages)
			await using (FileStream fs = new (filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				for (var i = 1; i < payloadBinary.TotalChunks + 1; i++)
				{
					var buffer = new byte[chunkSize];			
					Tracer("SendBinaryAsync (Progress)", $"{i}/{payloadBinary.TotalChunks}");
					var bytesRead = await fs.ReadAsync(buffer, _cancellationTokenSource?.Token ?? CancellationToken.None);
					
					// If the last chunk is smaller than the buffer size
					var chunkBytes = new byte[bytesRead];
					Buffer.BlockCopy(buffer, 0, chunkBytes, 0, bytesRead);

					await _webSocket.SendAsync(new ArraySegment<byte>(chunkBytes), WebSocketMessageType.Binary, (i == payloadBinary.TotalChunks), CancellationToken.None);
				}
			}
			Logger(null ,$"Sent Binary: {filePath}");
		}
		else
		{
			Logger(null ,"WebSocket is not connected. Cannot send message.");
		}
	}
	public async Task SendRptLinesAsync(string filePath, int linesCount)
	{
		Logger(null, $"INFO: Sending RPT : {linesCount} lines");
		if (Status == WebSocketState.Open)
		{
			var sw = Stopwatch.StartNew();
			await using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

			var lastLines = await GetLastLinesAsync(fileStream, linesCount);
			lastLines.Reverse();
			
			var lineCount = lastLines.Count;
			var charCount = 0;
			foreach (var (line, i) in lastLines.Select((value, i) => ( value, i )))
			{
				var wLine = line + "\n";
				charCount += wLine.Length;
				if  (charCount > 1980)
				{
					Logger(null ,$"SendRptLines has reached limit: \"{line}\".");
					lineCount = i;
					break;
				}
				var bytes = Encoding.UTF8.GetBytes(wLine);
				await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Binary, false, CancellationToken.None);
			}
			Logger(null ,$"SendRptLines [{lineCount}]: {filePath}");
			await _webSocket.SendAsync(new ArraySegment<byte>([]), WebSocketMessageType.Binary, true, CancellationToken.None);
			sw.Stop();
			
			Logger(null, nameof(SendRptLinesAsync) + " Execution took: " + sw.ElapsedMilliseconds + "ms");
		}
		else
		{
			Logger(null ,"WebSocket is not connected. Cannot send message.");
		}
		
		return;
		
		async Task<List<string>> GetLastLinesAsync(FileStream stream, int count)
		{
			if (count <= 0) return [];
			using var reader = new StreamReader(stream, Encoding.UTF8);
			var queue = new Queue<string>(count);
			
			while (!reader.EndOfStream)
			{
				var line = await reader.ReadLineAsync();
				
				if (line == null) continue;
				
				if (queue.Count == count) queue.Dequeue();
				queue.Enqueue(line);
			}
			return queue.ToList();
		}
	}

	public async Task SendMessageAsync(string messagePayload)
	{
		if (Status == WebSocketState.Open)
		{
			var bytes = Encoding.UTF8.GetBytes(messagePayload);
			
			await _webSocket.SendAsync(
				new ArraySegment<byte>(bytes),
				WebSocketMessageType.Text,
				true,
				_cancellationTokenSource?.Token ?? CancellationToken.None);

			Logger(null ,$"Sent: {messagePayload}");
		}
		else
		{
			Logger(null ,"WebSocket is not connected. Cannot send message.");
		}
	}

	private async Task ReceiveMessages()
	{
		var buffer = new byte[1024 * 2];

		try
		{
			while (Status == WebSocketState.Open)
			{
				var result = await _webSocket.ReceiveAsync(
					new ArraySegment<byte>(buffer),
					_cancellationTokenSource?.Token ?? CancellationToken.None);

				if (result.MessageType == WebSocketMessageType.Text)
				{
					var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
					Logger(null ,$"Received: {message}");
					
					var payload = JsonSerializer.Deserialize(
						message, 
						Arma3PayloadJsonSerializerContext.Default.Arma3Payload
					);
					
					//- Invoke callback
					MessageReceived?.Invoke(payload);
					
					//- Respond request to the service 
					if (payload is Arma3PayloadServiceRequest request)
					{
						ServiceRequestHandler.RespondRequest(request, message);
					}
				}
				else if (result.MessageType == WebSocketMessageType.Close)
				{
					Logger(null ,"Server closed the connection.");
					break;
				}
			}
		}
		catch (OperationCanceledException)
		{
			Logger(null ,"Connection cancelled.");
		}
		catch (Exception ex)
		{
			Logger(null ,$"Error receiving message: \"{ex.Message}\"");
		}
		finally
		{
			Disconnected?.Invoke();
		}
	}

	public async Task DisconnectAsync(string description)
	{
		try
		{
			if (Status == WebSocketState.Open)
			{
				await _webSocket!.CloseAsync(
					WebSocketCloseStatus.NormalClosure,
					description,
					CancellationToken.None);
			}

			_cancellationTokenSource?.Cancel();
			_webSocket?.Dispose();
			_cancellationTokenSource?.Dispose();
			Disconnected?.Invoke();

			Logger(null ,"Disconnected successfully.");
		}
		catch (Exception ex)
		{
			Logger(null ,$"Error during disconnect: {ex.Message}");
		}
	}
}
