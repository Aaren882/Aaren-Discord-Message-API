using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Arma3WebService;
using DiscordMessageAPI.Tools;

namespace DiscordMessageAPI.WebService
{
	class WebSocketClient
	{
		private ClientWebSocket? _webSocket;
		private CancellationTokenSource? _cancellationTokenSource;
		private string _serverUri;

		public event Action<string>? MessageReceived;
		public event Action? Connected;
		public event Action? Disconnected;

		public WebSocketClient(string serverUri)
		{
			_serverUri = serverUri;
		}

		public async Task ConnectAsync()
		{
			try
			{
				_webSocket = new ClientWebSocket();
				_cancellationTokenSource = new CancellationTokenSource();

				Logger.Log(null ,$"Connecting to {_serverUri}...");
				await _webSocket.ConnectAsync(new Uri(_serverUri), _cancellationTokenSource.Token);

				Logger.Log(null ,"Connected successfully!");
				Connected?.Invoke();

				// Start listening for messages
				_ = Task.Run(ReceiveMessages);
			}
			catch (Exception ex)
			{
				Logger.Log(null ,$"Connection failed: {ex.Message}");
			}
		}

		public async Task SendMessageAsync(string message)
		{
			if (_webSocket?.State == WebSocketState.Open)
			{
				Arma3Payload messageObj = new Arma3Payload
				{
					Log = message,
					Timestamp = DateTime.Now
				};

				var messageJson = JsonSerializer.Serialize(messageObj, Arma3Payload_JsonSerializerContext.Default.Arma3Payload);
				var bytes = Encoding.UTF8.GetBytes(messageJson);

				await _webSocket.SendAsync(
					new ArraySegment<byte>(bytes),
					WebSocketMessageType.Text,
					true,
					_cancellationTokenSource?.Token ?? CancellationToken.None);

				Logger.Log(null ,$"Sent: {message}");
			}
			else
			{
				Logger.Log(null ,"WebSocket is not connected. Cannot send message.");
			}
		}

		private async Task ReceiveMessages()
		{
			var buffer = new byte[1024 * 4];

			try
			{
				while (_webSocket?.State == WebSocketState.Open)
				{
					var result = await _webSocket.ReceiveAsync(
						new ArraySegment<byte>(buffer),
						_cancellationTokenSource?.Token ?? CancellationToken.None);

					if (result.MessageType == WebSocketMessageType.Text)
					{
						var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
						Logger.Log(null ,$"Received: {message}");
						MessageReceived?.Invoke(message);
					}
					else if (result.MessageType == WebSocketMessageType.Close)
					{
						Logger.Log(null ,"Server closed the connection.");
						break;
					}
				}
			}
			catch (OperationCanceledException)
			{
				Logger.Log(null ,"Connection cancelled.");
			}
			catch (Exception ex)
			{
				Logger.Log(null ,$"Error receiving message: {ex.Message}");
			}
			finally
			{
				Disconnected?.Invoke();
			}
		}

		public async Task DisconnectAsync()
		{
			try
			{
				if (_webSocket?.State == WebSocketState.Open)
				{
					await _webSocket.CloseAsync(
						WebSocketCloseStatus.NormalClosure,
						"Client disconnect",
						CancellationToken.None);
				}

				_cancellationTokenSource?.Cancel();
				_webSocket?.Dispose();
				_cancellationTokenSource?.Dispose();

				Logger.Log(null ,"Disconnected successfully.");
			}
			catch (Exception ex)
			{
				Logger.Log(null ,$"Error during disconnect: {ex.Message}");
			}
		}
	}
}
