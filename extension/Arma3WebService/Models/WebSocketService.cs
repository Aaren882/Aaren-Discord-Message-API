using System.Collections.Concurrent;
using System.Net.WebSockets;
using Arma3WebService.Entity;
using Arma3WebService.Factory;
using Arma3WebService.Managers;
using static Arma3WebService.Factory.WebSocketConnectionFactory;
using static Arma3WebService.Managers.WebSocketConnectionManager;

namespace Arma3WebService.Models
{
	public interface IWebSocketService
	{
		IConnection GetConnection(string connectionIdentity);
		Task InvokeArmaCallBack(Arma3RemoteCommand command);
		Task CreateConnection(HttpContext context);
		event Action<WebsocketContextEntity, IConnection> OnConnected;
		event Action<WebsocketContextEntity, IConnection> OnDisconnected;
	}

	public sealed class WebSocketService(
		ILogger<WebSocketService> logger,
		ServiceActionManager serviceActionManager,
		RemoteStateManager remoteStateManager,
		WebsocketContextEntityFactory contextEntityFactory,
		IConnectionFactory connectionFactory,
		IConnectionManager connectionManager
	) : IWebSocketService, IHostedService, IDisposable
	{
		private readonly ILogger _logger = logger;
		private readonly CancellationTokenSource _stoppingCts = new();
		private readonly ConcurrentDictionary<string, IConnection> _connections = new();
		public event Action<WebsocketContextEntity, IConnection> OnConnected = (entity, connection) =>
		{
			var profileName = entity.GetIdentity();
			_ = remoteStateManager.GetServerInfoTemplateAsync(profileName);
			_ = remoteStateManager.UpdateGameSessionCacheAsync(profileName, connection);
		};

		public event Action<WebsocketContextEntity, IConnection> OnDisconnected = (entity, connection) =>
		{
			_ = remoteStateManager.UpdateGameSessionCacheAsync(entity.GetIdentity());
		};

		public IConnection GetConnection(string connectionIdentity)
		{
			return _connections.TryGetValue(connectionIdentity, out var session)
				? session
				: throw new NullReferenceException($"No \"{connectionIdentity}\" is not found.");
		}
		public Task InvokeArmaCallBack(Arma3RemoteCommand command)
			=> serviceActionManager.CallBackAction(
				GetConnection(command.gameId),
				command.payload
			);
		
		public Task StartAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("WebSocket is Listening now");

			return Task.CompletedTask;
		}
		public async Task StopAsync(CancellationToken cancellationToken)
		{
			try
			{
				// Signal cancellation to the executing method
				await _stoppingCts.CancelAsync();
			}
			finally
			{
				// Wait until the task completes or the stop token triggers
				var connections = _connections.Values
					.ToAsyncEnumerable()
					.WithCancellation(cancellationToken);
				
				await foreach (var connection in connections)
				{
					await connection.Close();
				}
			}

			_logger.LogInformation("WebSocket Has Stopped Listening...");
		}
		
		public async Task CreateConnection(HttpContext context)
		{
			var contextEntity = contextEntityFactory.CreateJsonStringContext(context);
			var connectionIdentity = contextEntity.GetIdentity();

			if (_connections.ContainsKey(connectionIdentity))
			{
				_logger.LogError(
					"Refuse Request. Connection already exist. Name : '{Identity}'/'{ContextId}'", 
					connectionIdentity, 
					contextEntity.Id
				);
				return;
			}

			IConnection connection;
			try
			{
				connection = connectionFactory.CreateConnection(contextEntity);
				_connections.TryAdd(contextEntity.GetIdentity(), connection);

				_logger.LogInformation(
					"Accepted connection Name : '{Identity}'/'{ContextId}' - '{ClientIpAddress}'. Total connections: {Count}",
					connectionIdentity,
					contextEntity.Id, 
					contextEntity.ClientIpAddress,
					_connections.Count
				);

				OnConnected.Invoke(contextEntity, connection);
				await connectionManager.HandleConnection(connection);
			}
			catch (OperationCanceledException)
			{
				// This exception is expected if the token is cancelled
				_logger.LogInformation(
					"WebSocket '{Identity}'/'{ContextId}' - '{ClientIpAddress}' connection was cancelled. Total connections: {Counts}",
					connectionIdentity,
					contextEntity.Id, 
					contextEntity.ClientIpAddress,
					_connections.Count
				);
			}
			catch (WebSocketException e) when (e.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
			{
				// Handle unexpected client disconnects
				_logger.LogWarning(
					"Client '{Identity}'/'{ContextId}' - '{ClientIpAddress}' unexpectedly disconnected. Total connections: {Counts}",
					connectionIdentity,
					contextEntity.Id, 
					contextEntity.ClientIpAddress,
					_connections.Count
				);
			}
			catch (WebSocketException e)
			{
				_logger.LogError(
					e,
					"Client '{Identity}'/'{ContextId}' - '{ClientIpAddress}' \n disconnected. Total connections: {Counts}",
					connectionIdentity,
					contextEntity.Id, 
					contextEntity.ClientIpAddress,
					_connections.Count
				);
			}
			catch (Exception e)
			{
				_logger.LogError(
					e,
					"Client '{Identity}'/'{ContextId}' - '{ClientIpAddress}' \n disconnected. Total connections: {Counts}",
					connectionIdentity,
					contextEntity.Id, 
					contextEntity.ClientIpAddress,
					_connections.Count
				);
			}
			finally
			{
				if (_connections.TryRemove(connectionIdentity, out connection!))
				{
					OnDisconnected.Invoke(contextEntity, connection);
					_logger.LogInformation(
						"\"({Status})\" connection \"{ConnectionIdentity}\" - \"{ConnectionRemoteIpAddress}\". Total connections: {ConnectionsCount}", 
						connection.CloseStatusDescription(),
						contextEntity.GetIdentity(),
						contextEntity.ClientIpAddress,
						_connections.Count
					);
				}
				else
				{
					_logger.LogError("{connectionIdentity} was not found.", connectionIdentity);
				}
			}
		}


		public void Dispose()
		{
			_stoppingCts.Cancel();
		}
	}
}
