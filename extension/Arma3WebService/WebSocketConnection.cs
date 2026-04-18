using System.Net.WebSockets;
using System.Text;
using Arma3WebService.Entity;
using Components.Entity;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Arma3WebService.Extensions;

namespace Arma3WebService;

public interface IConnection
{
	public WebsocketContextEntity websocketContext { get; init; }
	Task<WebSocketCloseStatus?> KeepReceiving();
	Task<WebSocketReceiveResult> ReceiveMessage(Stream memoryStream);
	Task<WebSocketReceiveResult> ReceiveBinary(FileStream fileStream);
	Task SendArmaCallBackMessage(Arma3Payload callBack);
	Task Send(string message);
	Task StartAsync();
	Task Close();
	string? CloseStatusDescription();
}

public sealed class WebSocketConnection(WebsocketContextEntity websocketContext) : IConnection
{
	private WebSocket _webSocket;
	private readonly CancellationToken _cts = websocketContext.CancellationToken;
	public WebsocketContextEntity websocketContext { get; init; } = websocketContext;

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
			
			var payload = JsonSerializer.Deserialize(
				receivedMessage,
				Arma3PayloadJsonSerializerContext.Default.Arma3Payload
			)!;
			
			//- Execute
			await websocketContext
				.CreateAction(this, payload)
				.DoAction;
			
		} while (message.MessageType != WebSocketMessageType.Close);

		return message.CloseStatus;
	}

	public async Task<WebSocketReceiveResult> ReceiveMessage(Stream memoryStream)
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
	public async Task<WebSocketReceiveResult> ReceiveBinary(FileStream fileStream)
	{
		var readBuffer = new ArraySegment<byte>(new byte[64 * 1024]);

		WebSocketReceiveResult result;
		do
		{
			result = await _webSocket.ReceiveAsync(readBuffer, _cts);
			await fileStream.WriteAsync(readBuffer.Array!, _cts);
		} while (!result.EndOfMessage);

		return result;
	}
	
	public async Task SendArmaCallBackMessage(Arma3Payload callBack)
	{
		await Send(callBack.ToJsonString());
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

	public async Task StartAsync()
	{
		_webSocket = await websocketContext.Context.WebSockets.AcceptWebSocketAsync();
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
