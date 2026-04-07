using System.Net.WebSockets;
using Arma3WebService.Entity;

namespace Arma3WebService.Factory;

public class WebSocketConnectionFactory
{
	public interface IConnectionFactory
	{
		IConnection CreateConnection(WebsocketContextEntity contextEntity);
	}

	public class ConnectionFactory : IConnectionFactory
	{
		public IConnection CreateConnection(WebsocketContextEntity contextEntity)
		{
			return new WebSocketConnection(contextEntity);
		}
	}
}
