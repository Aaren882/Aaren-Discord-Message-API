using System.Collections.Concurrent;
using System.Collections.Immutable;
using Arma3WebService.DBContext.Repositories;
using Arma3WebService.Entity;
using Arma3WebService.Managers;
using Discord;

namespace Arma3WebService.Models
{
	public interface IWebSocketService
	{
		bool TryGetConnection(string connectionIdentity, out WebsocketServer? websocketServer);
		ValueTask InvokeArmaCallBack(Arma3RemoteCommand command);
		bool TryAddConnection(WebsocketContextEntity contextEntity, in WebsocketServer websocketWorker);
		void RemoveConnection(WebsocketContextEntity contextEntity);
		ImmutableArray<string> GetConnectionsNames();
		event Action<WebsocketContextEntity, WebsocketServer> OnConnected;
		event Action<WebsocketContextEntity, WebsocketServer> OnDisconnected;
	}

	public sealed class WebSocketService(
		ILogger<WebSocketService> logger,
		IDiscordBotService discordBotService,
		IServiceScopeFactory scopeFactory,
		ServiceActionManager serviceActionManager
	) : IWebSocketService, IHostedService, IDisposable
	{
		private readonly ILogger _logger = logger;
		private readonly CancellationTokenSource _stoppingCts = new();
		public event Action<WebsocketContextEntity, WebsocketServer> OnConnected = async (entity, connection) =>
		{
			var sessionIdentity = entity.GetIdentity();
			try
			{
				//- Logging
				var loggingChannel = discordBotService.GetPresetMessageChannelId(DiscordBotChannel.Logging);
				var channel = await discordBotService.GetMessageChannelAsync(loggingChannel);

				var embedBuilder = new EmbedBuilder()
					.WithTitle("🎮 Session Connected!")
					.WithDescription("A new Arma 3 session has successfully initialized and is ready for deployment.")
					.WithColor(3066993)
					.AddField("🖥️ Server Name", sessionIdentity)
					.WithFooter("System Logger")
					.WithCurrentTimestamp();
				await channel.SendMessageAsync(embed: embedBuilder.Build());
			}
			catch (Exception e)
			{
				logger.LogError(e, "GameSession Connected (Bot Logging):");
			}
		};

		public event Action<WebsocketContextEntity, WebsocketServer> OnDisconnected = async (entity, connection) =>
		{
			try
			{
				var profileName = entity.GetIdentity();
				await using var serviceScope = scopeFactory.CreateAsyncScope();

				var identityRepository = serviceScope.ServiceProvider.GetRequiredService<IServerIdentityRepository>();
				var templateRepository = serviceScope.ServiceProvider.GetRequiredService<IServerInfoTemplateRepository>();

				var identity = await identityRepository.GetByProfileNameAsync(profileName, tracked: false)
					?? throw new NullReferenceException();

				var template = await templateRepository.GetByMessageIdAsync(identity.messageId, tracked: false)
					?? throw new NullReferenceException();

				await discordBotService.ModifyMessageAsync(template.messageId, template.messageOffline);

				var loggingChannel = discordBotService.GetPresetMessageChannelId(DiscordBotChannel.Logging);
				var channel = await discordBotService.GetMessageChannelAsync(loggingChannel);
				var embedBuilder = new EmbedBuilder()
					.WithTitle("🛑 Session Disconnected")
					.WithDescription("The Arma 3 operations session has been terminated or the server has gone offline.")
					.WithColor(15158332)
					.AddField("🖥️ Server Name", entity.GetIdentity(), true)
					.AddField("⏱️ Session Status", "Offline / Hibernating", true)
					.WithFooter("System Logger")
					.WithCurrentTimestamp();
				await channel.SendMessageAsync(embed: embedBuilder.Build());
			}
			catch (Exception e)
			{
				logger.LogError(e, "GameSession Disconnected :");
			}
		};

		public bool TryGetConnection(string connectionIdentity, out WebsocketServer? session)
		{
			return _connectionWorkers.TryGetValue(connectionIdentity, out session);
		}

		public ImmutableArray<string> GetConnectionsNames() => _connectionWorkers.Keys.ToImmutableArray();

		public ValueTask InvokeArmaCallBack(Arma3RemoteCommand command)
		{
			if (TryGetConnection(command.gameId, out var session))
			{
				ArgumentNullException.ThrowIfNull(session);
				return serviceActionManager.CallBackAction(
					session,
					command.payload
				);
			}
			return ValueTask.CompletedTask;
		}

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
				var connections = _connectionWorkers.Values.ToAsyncEnumerable()
					.WithCancellation(cancellationToken);

				await foreach (var connection in connections)
				{
					await connection.CloseAsync();
				}
			}

			_logger.LogInformation("WebSocket Has Stopped Listening...");
		}

		/* public async Task CreateConnection(HttpContext context)
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
				// This exception is expected if the token is canceled
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
		} */
		private readonly ConcurrentDictionary<string, WebsocketServer> _connectionWorkers = new();
		public bool TryAddConnection(WebsocketContextEntity contextEntity, in WebsocketServer websocketServer)
		{
			var connectionIdentity = contextEntity.GetIdentity();

			if (_connectionWorkers.TryAdd(connectionIdentity, websocketServer))
			{
				_logger.LogInformation(
					"Accepted connection Name : '{Identity}'/'{ContextId}' - '{ClientIpAddress}'. Total connections: {Count}",
					connectionIdentity,
					contextEntity.Id,
					contextEntity.ClientIpAddress,
					_connectionWorkers.Count
				);
				OnConnected.Invoke(contextEntity, websocketServer);
				return true;
			}

			_logger.LogError(
				"Refuse Request. Connection already exist. Name : '{Identity}'/'{ContextId}'",
				connectionIdentity,
				contextEntity.Id
			);

			return false;
		}
		public void RemoveConnection(WebsocketContextEntity contextEntity)
		{
			var connectionIdentity = contextEntity.GetIdentity();

			if (_connectionWorkers.TryRemove(connectionIdentity, out var websocketServer))
			{
				_logger.LogInformation(
					"Removed connection Name : '{Identity}'/'{ContextId}' - '{ClientIpAddress}'. Total connections: {Count}",
					connectionIdentity,
					contextEntity.Id,
					contextEntity.ClientIpAddress,
					_connectionWorkers.Count
				);
				websocketServer.Dispose();
				OnDisconnected.Invoke(contextEntity, websocketServer);
				return;
			}
			_logger.LogError(
				"Refuse Remove. Connection is not exist. Name : '{Identity}'/'{ContextId}'. Total connections: {Count}",
				connectionIdentity,
				contextEntity.Id,
				_connectionWorkers.Count
			);
		}

		public void Dispose()
		{
			_stoppingCts.Cancel();
		}
	}
}
