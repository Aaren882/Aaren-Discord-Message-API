using System.Collections.Concurrent;
using System.Net.WebSockets;
using Arma3WebService.Entity;
using static Arma3WebService.Factory.WebSocketConnectionFactory;
using static Arma3WebService.Managers.WebSocketConnectionManager;

namespace Arma3WebService.Models
{
	public interface IWebSocketService
	{
		public Task CreateConnection(WebsocketContextEntity context);
	}

	public sealed class WebSocketService : IHostedService, IWebSocketService
	{
		private readonly ILogger _logger;
		private readonly IServiceProvider _serviceProvider;

		private readonly IConnectionFactory _connectionFactory;
		private readonly IConnectionManager _connectionManager;
		private static readonly ConcurrentDictionary<string, WebsocketEntity> _connections = new ConcurrentDictionary<string, WebsocketEntity>();

		public WebSocketService(ILogger<WebSocketService> logger, IServiceProvider serviceProvider)
		{
			_logger = logger;
			_serviceProvider = serviceProvider;

			_connectionFactory = new ConnectionFactory();
			_connectionManager = new ConnectionManager();
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			var service = _serviceProvider.GetRequiredService<WebSocketService>();

			_logger.LogInformation("WebSocket is Listening now");

			return Task.CompletedTask;
		}
		public Task StopAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("WebSocket Has Stopped Listening...");

			return Task.CompletedTask;
		}
		public async Task CreateConnection(WebsocketContextEntity contextEntity)
		{
			var connectionIdentity = contextEntity.Context.User.Identity?.Name ?? "Not Specified";

			if (_connections.ContainsKey(connectionIdentity))
			{
				_logger.LogError($"Refuse Request. Connection already exist. Name : '{connectionIdentity}'/'{contextEntity.Context.Connection.Id}'");
				return;
			}

			WebsocketEntity websocketEntity;
			try
			{
				var websocket = await contextEntity.Context.WebSockets.AcceptWebSocketAsync();
				websocketEntity = new WebsocketEntity(websocket, contextEntity);
				
				var connection = _connectionFactory.CreateConnection(websocketEntity);

				_connections.TryAdd(connectionIdentity, websocketEntity);

				_logger.LogInformation($"Accepted connection Name : '{connectionIdentity}'/'{contextEntity.Context.Connection.Id}' - '{contextEntity.Context.Connection.RemoteIpAddress}'. Total connections: {_connections.Count}");

				await _connectionManager.HandleConnection(connection);
			}
			catch (WebSocketException e)
			{
				_logger.LogWarning(e.Message);
			}
			catch (Exception e)
			{
				_logger.LogError(e.Message);
			}
			finally
			{
				_connections.TryRemove(connectionIdentity, out websocketEntity!);
				_logger.LogInformation($"Close connection '{contextEntity.Context.Connection.Id}' - '{contextEntity.Context.Connection.RemoteIpAddress}'. Total connections: {_connections.Count}");
			}
		}
	}
}
