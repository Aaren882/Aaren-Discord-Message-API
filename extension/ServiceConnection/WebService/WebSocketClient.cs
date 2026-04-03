using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Components.Entity;

namespace ServiceConnection.WebService;

public class WebSocketClient(string serverUri)
{
	private ClientWebSocket? _webSocket;
	private CancellationTokenSource? _cancellationTokenSource;

	public event Action<Arma3Payload>? MessageReceived;
	public event Action? Connected;
	public event Action? Disconnected;

	public WebSocketState? Status()
	{
		return _webSocket?.State;
	}

	public async Task ConnectAsync(string? jwtToken)
	{
		try
		{
			if (Status() == WebSocketState.Open)
				throw new Exception("WebSocket already connected."); 
			
			_webSocket = new ClientWebSocket();
			if (jwtToken != null)
				_webSocket.Options.SetRequestHeader("Authorization", "Bearer " + jwtToken);
			
			_cancellationTokenSource = new CancellationTokenSource();

			ServiceStartup.Logger(null ,$"Connecting to {serverUri}...");
			await _webSocket.ConnectAsync(new Uri(serverUri), _cancellationTokenSource.Token);

			ServiceStartup.Logger(null ,"Connected successfully!");
			Connected?.Invoke();

			// Start listening for messages
			_ = Task.Run(ReceiveMessages);
		}
		catch (Exception ex)
		{
			ServiceStartup.Logger(null ,$"Connection failed: {ex.Message}");
		}
	}
	public async Task SendBinaryAsync(string filePath, int chunkSize = 64 * 1024)
	{
		if (Status() == WebSocketState.Open)
		{
			var fileInfo = new FileInfo(filePath);
			var totalChunks = (int)Math.Ceiling((double)fileInfo.Length / chunkSize);

			// Send Metadata (as text message)
			var metadata = new Arma3PayloadRPT
			(
				fileInfo.Name,
				fileInfo.Length,
				fileInfo.CreationTime,
				totalChunks
				// ChunkIndex
			);

			var metadataJson = JsonSerializer.Serialize(
				metadata,
				Arma3PayloadJsonSerializerContext.Default.Arma3Payload
			);
			var metadataBytes = Encoding.UTF8.GetBytes(metadataJson);
			
			await _webSocket.SendAsync(new ArraySegment<byte>(metadataBytes), WebSocketMessageType.Text, true, CancellationToken.None);

			// Send Chunks (as binary messages)
			await using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
			{
				for (var i = 0; i < totalChunks; i++)
				{
					var buffer = new byte[chunkSize];
					var bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length);

					// If the last chunk is smaller than the buffer size
					var chunkBytes = new byte[bytesRead];
					Buffer.BlockCopy(buffer, 0, chunkBytes, 0, bytesRead);

					await _webSocket.SendAsync(new ArraySegment<byte>(chunkBytes), WebSocketMessageType.Binary, (i == totalChunks - 1), CancellationToken.None);
				}
			}
			ServiceStartup.Logger(null ,$"Sent Binary: {filePath}");
		}
		else
		{
			ServiceStartup.Logger(null ,"WebSocket is not connected. Cannot send message.");
		}
	}

	public async Task SendMessageAsync(string messagePayload)
	{
		if (Status() == WebSocketState.Open)
		{
			var bytes = Encoding.UTF8.GetBytes(messagePayload);
			
			await _webSocket.SendAsync(
				new ArraySegment<byte>(bytes),
				WebSocketMessageType.Text,
				true,
				_cancellationTokenSource?.Token ?? CancellationToken.None);

			ServiceStartup.Logger(null ,$"Sent: {messagePayload}");
		}
		else
		{
			ServiceStartup.Logger(null ,"WebSocket is not connected. Cannot send message.");
		}
	}

	private async Task ReceiveMessages()
	{
		var buffer = new byte[1024 * 2];

		try
		{
			while (Status() == WebSocketState.Open)
			{
				var result = await _webSocket.ReceiveAsync(
					new ArraySegment<byte>(buffer),
					_cancellationTokenSource?.Token ?? CancellationToken.None);

				if (result.MessageType == WebSocketMessageType.Text)
				{
					var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
					ServiceStartup.Logger(null ,$"Received: {message}");
					
					var payload = JsonSerializer.Deserialize(
						message, 
						Arma3PayloadJsonSerializerContext.Default.Arma3Payload
					);
					
					//- Invoke callback
					MessageReceived?.Invoke(payload);
				}
				else if (result.MessageType == WebSocketMessageType.Close)
				{
					ServiceStartup.Logger(null ,"Server closed the connection.");
					break;
				}
			}
		}
		catch (OperationCanceledException)
		{
			ServiceStartup.Logger(null ,"Connection cancelled.");
		}
		catch (Exception ex)
		{
			ServiceStartup.Logger(null ,$"Error receiving message: \"{ex.Message}\"");
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
			if (Status() == WebSocketState.Open)
			{
				await _webSocket!.CloseAsync(
					WebSocketCloseStatus.NormalClosure,
					description,
					CancellationToken.None);
			}

			_cancellationTokenSource?.Cancel();
			_webSocket?.Dispose();
			_cancellationTokenSource?.Dispose();

			ServiceStartup.Logger(null ,"Disconnected successfully.");
		}
		catch (Exception ex)
		{
			ServiceStartup.Logger(null ,$"Error during disconnect: {ex.Message}");
		}
	}
}
