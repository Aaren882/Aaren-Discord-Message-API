using Arma3WebService.Managers;
using Components.Entity;

namespace Arma3WebService.Entity;

public sealed record Arma3Action
{
	IConnection Connection { get; }
	Arma3Payload Payload { get; }
	IArma3ActionManager ActionManager { get; }
	public Task DoAction { get; }

	public Arma3Action(
		IConnection connection,
		Arma3Payload payload,
		IArma3ActionManager ActionManager
	)
	{
		Connection = connection;
		Payload = payload;
		DoAction = ActionManager.GetAction(this);
	}

	public void Deconstruct(out IConnection connection, out Arma3Payload payload)
	{
		connection = Connection;
		payload = Payload;
	}
}
