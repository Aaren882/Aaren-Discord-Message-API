using Arma3WebService.Entity;
using Arma3WebService.Managers;
using Components.Entity;

namespace Arma3WebService.Factory;

public interface IArma3ActionFactory
{
	Arma3Action Create(IConnection connection, Arma3Payload payload);
}

public sealed class Arma3ActionFactory(IArma3ActionManager actionManager) : IArma3ActionFactory
{
	public Arma3Action Create(IConnection connection, Arma3Payload payload)
	{
		return new Arma3Action(connection, payload, actionManager);
	}
}
