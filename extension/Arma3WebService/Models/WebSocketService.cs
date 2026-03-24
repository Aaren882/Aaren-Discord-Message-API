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

	public sealed class WebSocketService(
		ILogger<WebSocketService> logger,
		IServiceProvider serviceProvider,
		IConnectionFactory connectionFactory,
		IConnectionManager connectionManager
	) : IHostedService, IWebSocketService
	{
		private readonly ILogger _logger = logger;

		private static readonly ConcurrentDictionary<string, IConnection> Connections = new();

		public async Task InvokeArmaCallBack(Arma3RemoteCommand command)
		{
			if (!Connections.TryGetValue(command.gameId, out var session))
				throw new NullReferenceException($"No \"{command.gameId}\" is not found.");
			
			await session.SendArmaCallback(command.payload);
		}
		
		public Task StartAsync(CancellationToken cancellationToken)
		{
			var service = serviceProvider.GetRequiredService<WebSocketService>();

			_logger.LogInformation("WebSocket is Listening now");

			return Task.CompletedTask;
		}
		public Task StopAsync(CancellationToken cancellationToken)
		{
			_ = Connections.Values.Select( //- Disconnect all client 
				async c => await c.Close());

			_logger.LogInformation("WebSocket Has Stopped Listening...");

			return Task.CompletedTask;
		}
		
		public async Task CreateConnection(WebsocketContextEntity contextEntity)
		{
			var connectionIdentity = contextEntity.Identity;

			if (Connections.ContainsKey(connectionIdentity))
			{
				_logger.LogError($"Refuse Request. Connection already exist. Name : '{connectionIdentity}'/'{contextEntity.Context.Connection.Id}'");
				return;
			}

			IConnection connection;
			try
			{
				var websocketEntity = new WebsocketEntity(contextEntity);
				connection = connectionFactory.CreateConnection(websocketEntity);

				Connections.TryAdd(contextEntity.Identity, connection);

				_logger.LogInformation($"Accepted connection Name : '{connectionIdentity}'/'{contextEntity.Id}' - '{contextEntity.ClientIpAddress}'. Total connections: {Connections.Count}");

				await connectionManager.HandleConnection(connection);
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
				_logger.LogInformation(
					"\"({Status})\" connection \"{ConnectionIdentity}\" - \"{ConnectionRemoteIpAddress}\". Total connections: {ConnectionsCount}", 
					connection.CloseStatusDescription(),
					contextEntity.Identity,
					contextEntity.ClientIpAddress,
					Connections.Count
				);
			}
		}
	}
}
