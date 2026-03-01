using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;
using static Arma3WebService.Factory.WebSocketConnectionFactory;
using static Arma3WebService.Managers.WebSocketConnectionManager;

namespace Arma3WebService.Controllers
{
	[Route("/api/ws")]
	[ApiController]
	public class WebSocketApiController : ControllerBase
	{
		private readonly ILogger<WebSocketApiController> _logger;
		private readonly IConnectionFactory _connectionFactory;
		private readonly IConnectionManager _connectionManager;
		private static readonly ConcurrentDictionary<string, WebSocket> _connections = new ConcurrentDictionary<string, WebSocket>();


		public WebSocketApiController(
			ILogger<WebSocketApiController> logger)
		{
			_logger = logger;
			_connectionFactory = new ConnectionFactory();
			_connectionManager = new ConnectionManager();
		}

		[HttpGet]
		public async Task<IActionResult> Get()
		{
			var context = ControllerContext.HttpContext;

			if (context.WebSockets.IsWebSocketRequest)
			{
				WebSocket websocket;
				try
				{
					if (_connections.ContainsKey(context.Connection.Id))
						throw new Exception($"Connection already exist. '{context.Connection.Id}' from '{context.Connection.RemoteIpAddress}'");

					websocket = await context.WebSockets.AcceptWebSocketAsync();
					_connections.TryAdd(context.Connection.Id, websocket);

					_logger.LogInformation($"Accepted connection '{context.Connection.Id}' - '{context.Connection.RemoteIpAddress}'. Total connections: {_connections.Count}");

					var connection = _connectionFactory.CreateConnection(websocket);
					await _connectionManager.HandleConnection(connection);
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
			else
			{
				return new StatusCodeResult((int)HttpStatusCode.BadRequest);
			}
		}
	}
}
