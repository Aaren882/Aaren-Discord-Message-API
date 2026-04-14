using System.Net;
using Arma3WebService.Factory;
using Components.Entity;

namespace Arma3WebService.Entity;

public sealed record WebsocketContextEntity(HttpContext Context, IArma3ActionFactory ActionFactory)
{
	private readonly string Identity = Context.User.Identity?.Name ?? "Not Specified";
	public readonly string Id = Context.Connection.Id;
	public readonly IPAddress? ClientIpAddress = Context.Connection.RemoteIpAddress;
	public readonly CancellationToken CancellationToken = Context.RequestAborted;

	public Arma3Action CreateAction(IConnection connection, Arma3Payload payload)
	{
		return ActionFactory.Create(connection, payload);
	}
	public string GetIndentity()
	{
		return Identity;
	}
};
