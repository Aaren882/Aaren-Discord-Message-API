using System.Net.WebSockets;
using Arma3WebService.Entity;

namespace Arma3WebService.Factory
{
	public class WebSocketConnectionFactory
	{
		public interface IConnectionFactory
		{
			IConnection CreateConnection(WebsocketEntity webSocket);
		}

		public class ConnectionFactory : IConnectionFactory
		{
			public IConnection CreateConnection(WebsocketEntity webSocket)
			{
				return new WebSocketConnection(webSocket);
			}
		}
	}
}
