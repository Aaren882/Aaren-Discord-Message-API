using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;
using static Arma3WebService.Factory.WebSocketConnectionFactory;
using static Arma3WebService.Managers.WebSocketConnectionManager;

namespace Arma3WebService.Models
{
	public interface IWebSocketService
	{
		public Task CreateConnection(HttpContext context);
	}

	public sealed class WebSocketService : IHostedService, IWebSocketService
	{
		private readonly ILogger _logger;
		private readonly IServiceProvider _serviceProvider;

		private readonly IConnectionFactory _connectionFactory;
		private readonly IConnectionManager _connectionManager;
		private static readonly ConcurrentDictionary<string, WebSocket> _connections = new ConcurrentDictionary<string, WebSocket>();

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


		public async Task CreateConnection(HttpContext context)
		{
			var connectionIdentity = context.User.Identity?.Name ?? "Not Specified";
			
			WebSocket websocket;
			try
			{
				
				if (_connections.ContainsKey(connectionIdentity))
					throw new Exception($"Refuse Request. Connection already exist. Name : '{connectionIdentity}'/'{context.Connection.Id}'");

				websocket = await context.WebSockets.AcceptWebSocketAsync();
				var connection = _connectionFactory.CreateConnection(websocket);

				_connections.TryAdd(connectionIdentity, websocket);

				_logger.LogInformation($"Accepted connection Name : '{connectionIdentity}'/'{context.Connection.Id}' - '{context.Connection.RemoteIpAddress}'. Total connections: {_connections.Count}");

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
				_connections.TryRemove(connectionIdentity, out websocket!);
				_logger.LogInformation($"Close connection '{context.Connection.Id}' - '{context.Connection.RemoteIpAddress}'. Total connections: {_connections.Count}");
			}
		}
	}
}
