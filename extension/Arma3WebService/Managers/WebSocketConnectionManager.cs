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
				await connection.StartAsync();
				await connection.KeepReceiving();
				await connection.Close();
			}
		}
	}
}
