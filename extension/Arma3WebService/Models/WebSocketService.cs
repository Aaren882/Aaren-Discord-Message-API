using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc;
using static Arma3WebService.Factory.WebSocketConnectionFactory;
using static Arma3WebService.Managers.WebSocketConnectionManager;

namespace Arma3WebService.Models
{
	public interface IWebSocketService
	{
		public Task<IActionResult> CreateConnection(HttpContext context);
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

			_logger.LogInformation($"WebSocket is Start Listening now");

			return Task.CompletedTask;
		}
		public Task StopAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation($"WebSocket is Stop Listening now");

			return Task.CompletedTask;
		}


		public async Task<IActionResult> CreateConnection(HttpContext context)
		{
			WebSocket websocket;
			try
			{
				if (_connections.ContainsKey(context.Connection.Id))
					throw new Exception($"Refuse Request. Connection already exist. '{context.Connection.Id}'");

				websocket = await context.WebSockets.AcceptWebSocketAsync();
				var connection = _connectionFactory.CreateConnection(websocket);

				_connections.TryAdd(context.Connection.Id, websocket);

				_logger.LogInformation($"Accepted connection '{context.Connection.Id}' - '{context.Connection.RemoteIpAddress}'. Total connections: {_connections.Count}");

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
				_connections.TryRemove(context.Connection.Id, out websocket!);
				_logger.LogInformation($"Close connection '{context.Connection.Id}' - '{context.Connection.RemoteIpAddress}'. Total connections: {_connections.Count}");
			}

			return new EmptyResult();
		}
	}
}
