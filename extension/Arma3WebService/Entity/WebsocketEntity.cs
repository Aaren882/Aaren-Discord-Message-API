using System.Net;
using System.Net.WebSockets;
using Components.Entity;

namespace Arma3WebService.Entity;

public sealed record WebsocketContextEntity(HttpContext Context, Arma3PayLoadType ConnectionType)
{
	public readonly string Identity = Context.User.Identity?.Name ?? "Not Specified";
	public readonly string Id = Context.Connection.Id;
	public readonly IPAddress? ClientIpAddress = Context.Connection.RemoteIpAddress;
};

public sealed record WebsocketEntity(WebsocketContextEntity ContextEntity)
{
	public WebSocket AcceptConnection()
	{
		return ContextEntity.Context.WebSockets.AcceptWebSocketAsync()
			.GetAwaiter().GetResult();
	}
};
