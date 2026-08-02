using Arma3WebService.Managers;
using Arma3WebService.Entity;

namespace Arma3WebService.Factory;

public class WebSocketConnectionFactory
{
	public interface IConnectionFactory
	{
		IConnection CreateConnection(WebsocketContextEntity contextEntity);
	}

	public class ConnectionFactory(IArma3ActionManager Arma3ActionManager) : IConnectionFactory
	{
		public IConnection CreateConnection(WebsocketContextEntity contextEntity)
		{
			return new WebSocketConnection(contextEntity, Arma3ActionManager);
		}
	}
}
