using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Arma3WebService.Entity;

namespace Arma3WebService
{
	public interface IConnection
	{
		Task<WebSocketCloseStatus?> KeepReceiving();
		Task Send(string message);
		Task Close();
	}

	public class WebSocketConnection(WebsocketEntity websocketEntity) : IConnection
	{
		private readonly WebsocketEntity _websocketEntity = websocketEntity;
		private readonly WebsocketContextEntity _websocketContextEntity = websocketEntity.ContextEntity;
		private readonly WebSocket _webSocket = websocketEntity.WebSocket;

		public async Task<WebSocketCloseStatus?> KeepReceiving()
		{
			WebSocketReceiveResult message;
			do
			{
				using var memoryStream = new MemoryStream();

				message = (_websocketContextEntity.ConnectionType) switch
				{
					Arma3PayLoadType.Logging => await ReceiveMessage(memoryStream),
					Arma3PayLoadType.Rpt => await ReceiveBinary(),
				};

				if (memoryStream.Length <= 0) continue;
				
				//- Deserialize Payload
				var receivedMessage = Encoding.UTF8.GetString(memoryStream.ToArray());
				if (string.IsNullOrEmpty(receivedMessage)) continue;
				
				var deserialized = JsonSerializer.Deserialize(
					receivedMessage,
					Arma3PayloadJsonSerializerContext.Default.Arma3Payload
				)!;
						
				Console.WriteLine($"Received message '{deserialized.Message}'");
				await Send(receivedMessage);
			} while (message.MessageType != WebSocketMessageType.Close);

			return message.CloseStatus;
		}

		private async Task<WebSocketReceiveResult> ReceiveMessage(Stream memoryStream)
		{
			var readBuffer = new ArraySegment<byte>(new byte[2 * 1024]);
			WebSocketReceiveResult result;
			
			do
			{
				result = await _webSocket.ReceiveAsync(readBuffer, CancellationToken.None);
				await memoryStream.WriteAsync(readBuffer.Array!, readBuffer.Offset, result.Count,
					CancellationToken.None);
			} while (!result.EndOfMessage);

			return result;
		}
		private async Task<WebSocketReceiveResult> ReceiveBinary()
		{
			var readBuffer = new ArraySegment<byte>(new byte[2 * 1024]);
			WebSocketReceiveResult? result = null;

			FileStream? fileStream = null;
			while (_webSocket.State == WebSocketState.Open)
			{
				result = await _webSocket.ReceiveAsync(readBuffer, CancellationToken.None);

				Arma3PayloadRPT? metadata;
				if (result.MessageType == WebSocketMessageType.Text)
				{
					// Received metadata
					var metadataJson = Encoding.UTF8.GetString(readBuffer.Array!, 0, result.Count);
					metadata = JsonSerializer.Deserialize(
						metadataJson,
						Arma3PayloadJsonSerializerContext.Default.Arma3PayloadRPT
					);
					
					fileStream = new FileStream(
							metadata!.Value.FileName, FileMode.Create, FileAccess.Write);
					
					// Initialize file storage based on metadata
				}
				else if (result.MessageType == WebSocketMessageType.Binary)
				{
					// Received a file chunk
					await fileStream!.WriteAsync(readBuffer.Array!, 0, result.Count);

					if (result.EndOfMessage)
					{
						// All chunks for the current file have been received
						fileStream.Position = 0;
						// Process the complete file stream and metadata
                
						// Clean up
						metadata = null;
						// fileStream.SetLength(0); 
						await fileStream.DisposeAsync();
					}
				}
				else if (result.MessageType == WebSocketMessageType.Close)
				{
					await _webSocket.CloseAsync(
						WebSocketCloseStatus.NormalClosure,
						string.Empty,
						CancellationToken.None
					);
					break;
				}
			}

			return result;
		}

		public async Task Send(string message)
		{
			var bytes = Encoding.UTF8.GetBytes(message);
			await _webSocket.SendAsync(new ArraySegment<byte>(bytes, 0, bytes.Length), WebSocketMessageType.Text, true,
				CancellationToken.None);
		}

		public async Task Close()
		{
			await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
		}
	}
}
