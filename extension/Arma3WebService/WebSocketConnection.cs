using System.ComponentModel;
using System.Net.WebSockets;
using System.Text;
using Arma3WebService.Entity;
using Arma3WebService.Models;
using Components.Entity;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Arma3WebService
{
	public interface IConnection 
	{
		Task<WebSocketCloseStatus?> KeepReceiving();
		Task SendArmaCallback(Arma3PayloadCallBack callBack);
		Task Send(string message);
		Task Close();
		string? CloseStatusDescription();
	}

	public class WebSocketConnection(WebsocketEntity websocketEntity) : IConnection
	{
		private readonly WebSocket _webSocket = websocketEntity.AcceptConnection();
		private readonly CancellationToken _cts = websocketEntity.ContextEntity.Context.RequestAborted;
		private readonly IWebSocketService _service = websocketEntity.ContextEntity.WebSocketService;

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
				
				switch (deserialized.Type) 
				{
					case Arma3PayLoadType.Text :
					{
						var messagePayload = deserialized as Arma3PayloadText;
						Console.WriteLine($"Received Text '{messagePayload.Message}'");
						
						await Send(receivedMessage);
						break;
					}
					case Arma3PayLoadType.ArrayString :
					{
						var messagePayload = deserialized as Arma3PayloadArrayString;

						try
						{
							var collection = messagePayload?.ArrayString
								.Select(x =>
									JsonSerializer.Deserialize<List<string>>(x)
								)
								.ToDictionary(
									value => value![0],
									value => value![1]
								);
						
							Console.WriteLine(collection);
						}
						catch (ArgumentException e)
						{
							Console.WriteLine(e);
						}
						
						break;
					}
					case Arma3PayLoadType.JsonString :
					{
						var messagePayload = deserialized as Arma3PayloadJson;
						Console.WriteLine($"Received message '{messagePayload.JsonString}'");

						var dto = JsonSerializer.Deserialize(messagePayload.JsonString, MsgPayload_JsonContext.Default.DiscordMessageDto);

						if (dto != null) await _service!.InvokeDiscordBotMessage(dto);

						break;
					}
					case Arma3PayLoadType.Rpt : //- Must use metaData first
					{
						var metadata = deserialized as Arma3PayloadRPT;
						Console.WriteLine($"Receiving metaData for binary file '{metadata}'");
						
						await using var fileStream = new FileStream(
							metadata.FileName, FileMode.Create, FileAccess.Write);
						
						await ReceiveBinary(fileStream);
						
						Console.WriteLine($"Stored binary file '{metadata.FileName}'");
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
				result = await _webSocket.ReceiveAsync(readBuffer, _cts);
				await memoryStream.WriteAsync(readBuffer.Array!, readBuffer.Offset, result.Count,
					_cts);
			} while (!result.EndOfMessage);

			return result;
		}
		private async Task<WebSocketReceiveResult> ReceiveBinary(Stream fileStream)
		{
			var readBuffer = new ArraySegment<byte>(new byte[64 * 1024]);

			WebSocketReceiveResult result;
			do
			{
				result = await _webSocket.ReceiveAsync(readBuffer, _cts);
				await fileStream.WriteAsync(readBuffer.Array!, 0, result.Count, _cts);
			} while (!result.EndOfMessage);

			return result;
		}
		
		public async Task SendArmaCallback(Arma3PayloadCallBack callBack)
		{
			// var payload = new Arma3PayloadCallBack();
			var package = JsonSerializer.Serialize(
				callBack, 
				Arma3PayloadJsonSerializerContext.Default.Arma3Payload
			);
			var bytes = Encoding.UTF8.GetBytes(package);
			await _webSocket.SendAsync(new ArraySegment<byte>(bytes, 0, bytes.Length), WebSocketMessageType.Text, true,
				_cts);
		}
		public async Task Send(string message)
		{
			var bytes = Encoding.UTF8.GetBytes(message);
			await _webSocket.SendAsync(
				new ArraySegment<byte>(bytes, 0, bytes.Length), 
				WebSocketMessageType.Text, 
				true,
				_cts);
		}

		public async Task Close()
		{
			await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
		}
		
		public string? CloseStatusDescription()
		{
			return _webSocket.CloseStatusDescription ?? _cts.ToString();
		}
	}
}
