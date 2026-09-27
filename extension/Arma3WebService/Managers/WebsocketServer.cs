using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Arma3WebService.Broker;
using Arma3WebService.Entity;
using Arma3WebService.Factory;
using Arma3WebService.Models;
using Component.Websocket;
using Components.Entity;

namespace Arma3WebService.Managers;

public sealed class WebsocketServer(
	ILogger<IWebsocketWorker> logger,
	IArma3ActionManager arma3ActionManager,
	BinaryPayloadBroker binaryPayloadBroker,
	IWebSocketService service,
	WebsocketContextEntityFactory wsContextEntityFactory
) : WebsocketWorker
{
	public record ActionPayload(WebsocketServer Connection, Arma3Payload Payload);
	public record BinaryPayload(WebsocketServer Connection, Arma3Payload Payload);
	protected override ILogger<IWebsocketWorker> Logger => logger;
	public required WebsocketContextEntity websocketContext;
	public override void PostReceived(in Stream assembledStream, WebSocketMessageType messageType)
	{
		try
		{
			if (assembledStream.Length == 0)
			{
				Logger.LogTrace("\"{Identity}\" : Received empty \"{MessageType}\" Message.", websocketContext.GetIdentity(), messageType.ToString());
				return;
			}

			var payload = JsonSerializer.Deserialize(
				assembledStream,
				Arma3PayloadJsonSerializerContext.Default.Arma3Payload
			)!;

			var enqueued = (messageType) switch
			{
				WebSocketMessageType.Text => arma3ActionManager.TryEnqueueAction(this, payload),
				WebSocketMessageType.Binary => binaryPayloadBroker.TryEnqueueAction(this, payload),
				_ => throw new NotSupportedException($"Unsupported WebSocketMessageType: {messageType}")
			};
			if (!enqueued)
				throw new InvalidOperationException($"Enqueue failed on {websocketContext.GetIdentity()}: \"{payload}\"");
		}
		catch (Exception ex) when (ex is InvalidOperationException || ex is NotSupportedException)
		{
			Logger.LogWarning(ex, "Failed to process message due to invalid operation or unsupported type.");
		}
		catch (JsonException e)
		{
			Logger.LogWarning(e, "JsonException: ");
		}
		catch (Exception e)
		{
			Logger.LogError(e, "Fatal Exception: ");
		}
	}
	public async Task StartAsync(HttpContext context, params CancellationToken[] cts)
	{
		WebsocketContextEntity contextEntity = wsContextEntityFactory.CreateJsonStringContext(context);

		if (!service.TryAddConnection(contextEntity, this)) return;

		var webSocket = await context.WebSockets.AcceptWebSocketAsync(subProtocol: null);
		websocketContext = contextEntity;

		await StartAsync(webSocket, cts);
		service.RemoveConnection(websocketContext);
	}
}
