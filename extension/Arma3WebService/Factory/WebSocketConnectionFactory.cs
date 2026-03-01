using System.Net.WebSockets;

namespace Arma3WebService.Factory
{
	public class WebSocketConnectionFactory
	{
		public interface IConnectionFactory
		{
			IConnection CreateConnection(WebSocket webSocket);
		}

		public class ConnectionFactory : IConnectionFactory
		{
			public IConnection CreateConnection(WebSocket webSocket)
			{
				return new WebSocketConnection(webSocket);
			}
		}
	}
}
