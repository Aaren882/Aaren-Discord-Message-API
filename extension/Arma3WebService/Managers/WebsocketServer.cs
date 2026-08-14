using System.Net.WebSockets;
using System.Text;
using Component.Websocket;

namespace Arma3WebService.Managers;

public sealed class WebsocketServer(ILogger<IWebsocketWorker> logger) : WebsocketWorker
{
	protected override ILogger<IWebsocketWorker> Logger => logger;
	public override void PostReceived(in Stream assembledStream, WebSocketMessageType messageType)
	{
		switch (messageType)
		{
			case (WebSocketMessageType.Text):
				using (StreamReader reader = new(assembledStream, Encoding.UTF8))
				{
					var str = reader.ReadToEnd();
					if (str is not null)
					{
						Console.WriteLine($"From Client : {str}");
					}
				}
				break;
		}
	}
	public async Task StartAsync(WebSocket context, CancellationToken ct)
	{
		if (WebSocketStateMachine is not null) throw new InvalidOperationException("Websocket Connection is already Established...");

		WebSocketStateMachine = new(this, Logger, ct);
		await WebSocketStateMachine.StartAsync(context);
	}
}
