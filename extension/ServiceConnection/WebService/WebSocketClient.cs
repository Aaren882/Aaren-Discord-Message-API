using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Components.Entity;
using static ServiceConnection.ServiceStartup;

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

			Logger(null ,$"Connecting to {serverUri}...");
			await _webSocket.ConnectAsync(new Uri(serverUri), _cancellationTokenSource.Token);

			Logger(null ,"Connected successfully!");
			Connected?.Invoke();

			// Start listening for messages
			_ = Task.Run(ReceiveMessages);
		}
		catch (Exception ex)
		{
			Logger(null ,$"Connection failed: {ex.Message}");
		}
	}
	public async Task SendBinaryAsync(string filePath, Arma3PayloadRPT payloadRpt, int chunkSize = 64 * 1024)
	{
		if (Status() == WebSocketState.Open)
		{
			await Task.Delay(500); //- # Wait for the RPT get written first.   
			
			// Send Chunks (as binary messages)
			await using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				for (var i = 1; i < payloadRpt.TotalChunks + 1; i++)
				{
					var buffer = new byte[chunkSize];			
					Tracer("SendBinaryAsync (Progress)", $"{i}/{payloadRpt.TotalChunks}");
					var bytesRead = await fileStream.ReadAsync(buffer, _cancellationTokenSource?.Token ?? CancellationToken.None);
					
					// If the last chunk is smaller than the buffer size
					var chunkBytes = new byte[bytesRead];
					Buffer.BlockCopy(buffer, 0, chunkBytes, 0, bytesRead);

					await _webSocket.SendAsync(new ArraySegment<byte>(chunkBytes), WebSocketMessageType.Binary, (i == payloadRpt.TotalChunks), CancellationToken.None);
				}
			}
			Logger(null ,$"Sent Binary: {filePath}");
		}
		else
		{
			Logger(null ,"WebSocket is not connected. Cannot send message.");
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
			while (Status() == WebSocketState.Open)
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

			Logger(null ,"Disconnected successfully.");
		}
		catch (Exception ex)
		{
			Logger(null ,$"Error during disconnect: {ex.Message}");
		}
	}
}
