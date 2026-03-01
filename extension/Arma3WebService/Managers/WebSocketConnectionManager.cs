using System.Collections.Concurrent;
using System.Data.Common;
using System.Net.WebSockets;
using Discord;

namespace Arma3WebService.Managers
{
	public class WebSocketConnectionManager
	{
		
		public interface IConnectionManager
		{
			Task HandleConnection(IConnection connection);
		}

		
		public class ConnectionManager : IConnectionManager
		{
			public async Task HandleConnection(IConnection connection)
			{
				await connection.KeepReceiving();
				await connection.Close();
			}
		}
	}
}
