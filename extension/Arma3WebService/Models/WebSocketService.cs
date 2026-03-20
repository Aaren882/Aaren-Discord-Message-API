using System.Collections.Concurrent;
using System.Net.WebSockets;
using Arma3WebService.Entity;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using static Arma3WebService.Factory.WebSocketConnectionFactory;
using static Arma3WebService.Managers.WebSocketConnectionManager;

namespace Arma3WebService.Models
{
	public interface IWebSocketService
	{
		public Task InvokeArmaCallBack(Arma3RemoteCommand command);
		public Task CreateConnection(WebsocketContextEntity context);
	}

	public sealed class WebSocketService : IHostedService, IWebSocketService
	{
		private readonly ILogger _logger;
		private readonly IServiceProvider _serviceProvider;

		private readonly IConnectionFactory _connectionFactory;
		private readonly IConnectionManager _connectionManager;
		private static readonly ConcurrentDictionary<string, IConnection> Connections = new();

		public WebSocketService(ILogger<WebSocketService> logger, IServiceProvider serviceProvider)
		{
			_logger = logger;
			_serviceProvider = serviceProvider;

			_connectionFactory = new ConnectionFactory();
			_connectionManager = new ConnectionManager();
		}

		public async Task InvokeArmaCallBack(Arma3RemoteCommand command)
		{
			if (!Connections.TryGetValue(command.gameId, out var session))
				throw new NullReferenceException($"No \"{command.gameId}\" is not found.");
			
			await session.SendArmaCallback(command.payload);
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

			if (Connections.ContainsKey(connectionIdentity))
			{
				_logger.LogError($"Refuse Request. Connection already exist. Name : '{connectionIdentity}'/'{contextEntity.Context.Connection.Id}'");
				return;
			}

			IConnection connection;
			try
			{
				var websocket = await contextEntity.Context.WebSockets.AcceptWebSocketAsync();
				var websocketEntity = new WebsocketEntity(websocket, contextEntity);
				
				connection = _connectionFactory.CreateConnection(websocketEntity);

				Connections.TryAdd(connectionIdentity, connection);

				_logger.LogInformation($"Accepted connection Name : '{connectionIdentity}'/'{contextEntity.Context.Connection.Id}' - '{contextEntity.Context.Connection.RemoteIpAddress}'. Total connections: {Connections.Count}");

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
				Connections.TryRemove(connectionIdentity, out connection!);
				await connection.Close();
				_logger.LogInformation($"Close connection '{contextEntity.Context.Connection.Id}' - '{contextEntity.Context.Connection.RemoteIpAddress}'. Total connections: {Connections.Count}");
			}
		}
	}
}
