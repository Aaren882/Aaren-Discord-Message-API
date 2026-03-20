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
		Task SendArmaCallback(Arma3PayloadCallBack callBack);
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
				message = await ReceiveMessage(memoryStream);

				if (memoryStream.Length <= 0) continue;
				
				//- Deserialize Payload
				var receivedMessage = Encoding.UTF8.GetString(memoryStream.ToArray());
				if (string.IsNullOrEmpty(receivedMessage)) continue;
				
				var deserialized = JsonSerializer.Deserialize(
					receivedMessage,
					Arma3PayloadJsonSerializerContext.Default.Arma3Payload
				)!;
				
				switch (deserialized.MessageType) 
				{
					case Arma3PayLoadType.Message :
					{
						Console.WriteLine($"Received message '{deserialized.Message}'");
						
						await Send(receivedMessage);
						break;
					}
					case Arma3PayLoadType.Rpt : //- Must use metaData first
					{
						var metadata = deserialized.Rpt;
						Console.WriteLine($"Received metaData for binary file '{metadata}'");
						
						await using var fileStream = new FileStream(
							metadata.FileName, FileMode.Create, FileAccess.Write);
						
						await ReceiveBinary(fileStream);
						break;
					}
					default:
						throw new ArgumentOutOfRangeException("No Connection is found.");
				}
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
		private async Task<WebSocketReceiveResult> ReceiveBinary(Stream fileStream)
		{
			var readBuffer = new ArraySegment<byte>(new byte[64 * 1024]);

			WebSocketReceiveResult result;
			do
			{
				result = await _webSocket.ReceiveAsync(readBuffer, CancellationToken.None);
				await fileStream!.WriteAsync(readBuffer.Array!, 0, result.Count);
			} while (!result.EndOfMessage);

			return result;
		}
		
		public async Task SendArmaCallback(Arma3PayloadCallBack callBack)
		{
			var payload = new Arma3Payload(Arma3PayLoadType.Command)
			{
				CallBack = callBack
			};
			var package = JsonSerializer.Serialize(
				payload, 
				Arma3PayloadJsonSerializerContext.Default.Arma3Payload
			);
			var bytes = Encoding.UTF8.GetBytes(package);
			await _webSocket.SendAsync(new ArraySegment<byte>(bytes, 0, bytes.Length), WebSocketMessageType.Text, true,
				CancellationToken.None);
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
