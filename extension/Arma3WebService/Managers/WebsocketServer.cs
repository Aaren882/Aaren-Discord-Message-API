using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Arma3WebService.Entity;
using Arma3WebService.Factory;
using Arma3WebService.Models;
using Component.Websocket;
using Components.Entity;

namespace Arma3WebService.Managers;

public sealed class WebsocketServer(
	ILogger<IWebsocketWorker> logger,
	IArma3ActionManager arma3ActionManager,
	IWebSocketService service,
	WebsocketContextEntityFactory wsContextEntityFactory
) : WebsocketWorker
{
	protected override ILogger<IWebsocketWorker> Logger => logger;
	public required WebsocketContextEntity websocketContext;
	public override void PostReceived(in Stream assembledStream, WebSocketMessageType messageType)
	{
		using StreamReader reader = new(assembledStream, Encoding.UTF8);
		var receivedMessage = reader.ReadToEnd();
		if (string.IsNullOrEmpty(receivedMessage))
		{
			Logger.LogTrace("\"{Identity}\" : Received empty \"{MessageType}\" Message.", websocketContext.GetIdentity(), messageType.ToString());
			return;
		}

		var payload = JsonSerializer.Deserialize(
			receivedMessage,
			Arma3PayloadJsonSerializerContext.Default.Arma3Payload
		)!;
		Console.WriteLine($"From Client : {receivedMessage}");

		Task.Run(async () => await arma3ActionManager.GetAction(this, payload), websocketContext.CancellationToken)
			.GetAwaiter()
			.GetResult();
	}
	public ValueTask Send(string payload, WebSocketMessageType messageType, bool endOfMessage)
	{
		ArgumentNullException.ThrowIfNull(WebSocketStateMachine, nameof(WebSocketStateMachine));
		return WebSocketStateMachine.SendMessageAsync(Encoding.UTF8.GetBytes(payload), messageType, endOfMessage);
	}
	public async Task StartAsync(HttpContext context)
	{
		WebsocketContextEntity contextEntity = wsContextEntityFactory.CreateJsonStringContext(context);

		if (!service.TryAddConnection(contextEntity, this)) return;

		var webSocket = await context.WebSockets.AcceptWebSocketAsync(subProtocol: null);
		websocketContext = contextEntity;

		await StartAsync(webSocket, websocketContext.CancellationToken);
		service.RemoveConnection(contextEntity); //- Clean up
	}
}
