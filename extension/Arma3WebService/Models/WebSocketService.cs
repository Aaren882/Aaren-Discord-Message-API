using System.Collections.Concurrent;
using System.Net.WebSockets;
using Arma3WebService.Entity;
using Arma3WebService.Factory;
using static Arma3WebService.Factory.WebSocketConnectionFactory;
using static Arma3WebService.Managers.WebSocketConnectionManager;

namespace Arma3WebService.Models
{
	public interface IWebSocketService
	{
		IConnection GetConnection(string connectionIdentity);
		Task InvokeArmaCallBack(Arma3RemoteCommand command);
		Task CreateConnection(HttpContext context);
		event Action<WebsocketContextEntity>? OnConnected;
		event Action<WebsocketContextEntity>? OnDisconnected;
	}

	public sealed class WebSocketService(
		ILogger<WebSocketService> logger,
		ServiceActionManager serviceActionManager,
		WebsocketContextEntityFactory contextEntityFactory,
		IConnectionFactory connectionFactory,
		IConnectionManager connectionManager
	) : IWebSocketService, IHostedService, IDisposable
	{
		private readonly ILogger _logger = logger;
		private readonly CancellationTokenSource _stoppingCts = new();
		private readonly ConcurrentDictionary<string, IConnection> Connections = new();
		public event Action<WebsocketContextEntity>? OnConnected;
		public event Action<WebsocketContextEntity>? OnDisconnected;

		public IConnection GetConnection(string connectionIdentity)
		{
			return Connections.TryGetValue(connectionIdentity, out var session)
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
				var connections = Connections.Values
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

			if (Connections.ContainsKey(connectionIdentity))
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
				Connections.TryAdd(contextEntity.GetIdentity(), connection);

				_logger.LogInformation(
					"Accepted connection Name : '{Identity}'/'{ContextId}' - '{ClientIpAddress}'. Total connections: {Count}",
					connectionIdentity,
					contextEntity.Id, 
					contextEntity.ClientIpAddress,
					Connections.Count
				);

				OnConnected?.Invoke(contextEntity);
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
					Connections.Count
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
					Connections.Count
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
					Connections.Count
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
					Connections.Count
				);
			}
			finally
			{
				if (Connections.TryRemove(connectionIdentity, out connection!))
				{
					OnDisconnected?.Invoke(contextEntity);
					_logger.LogInformation(
						"\"({Status})\" connection \"{ConnectionIdentity}\" - \"{ConnectionRemoteIpAddress}\". Total connections: {ConnectionsCount}", 
						connection.CloseStatusDescription(),
						contextEntity.GetIdentity(),
						contextEntity.ClientIpAddress,
						Connections.Count
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
