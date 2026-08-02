using System.Net;

namespace Arma3WebService.Entity;

public sealed record WebsocketContextEntity(
	HttpContext Context
)
{
	private readonly string _identity = Context.User.Identity?.Name ?? "Not Specified";
	public readonly string Id = Context.Connection.Id;
	public readonly IPAddress? ClientIpAddress = Context.Connection.RemoteIpAddress;
	public readonly CancellationToken CancellationToken = Context.RequestAborted;

	public string GetIdentity() => _identity;
};
