using System.Collections.Concurrent;
using Arma3WebService.DBContext.Repositories;
using Arma3WebService.DBContext.Schema;

namespace Arma3WebService.Managers;

public sealed class RemoteStateManager(
	IServiceScopeFactory scopeFactory
)
{
	private readonly ConcurrentDictionary<ulong, WebsocketServer> _gameSessionsCache = [];
	private readonly ConcurrentDictionary<ulong, ServerInfoTemplate> _serverInfoTemplatesCache = [];
	private readonly ConcurrentDictionary<string, ulong> _serverInfoProfileNamesCache = [];

	internal async Task UpdateGameSessionCacheAsync(string profileName, WebsocketServer? connection = null)
	{
		await using var scope = scopeFactory.CreateAsyncScope();
		var identityRepository = scope.ServiceProvider.GetRequiredService<IServerIdentityRepository>();
		var serverIdentity = await identityRepository.GetByProfileNameAsync(profileName, tracked: false);

		if (serverIdentity == null)
			throw new NullReferenceException($"\"serverIdentity : {serverIdentity}\" is not exist.");

		var messageId = serverIdentity.messageId;
		if (connection is not null)
			_gameSessionsCache[messageId] = connection;
		else
			_gameSessionsCache.TryRemove(messageId, out connection);
	}

	internal async Task<ServerInfoTemplate> GetServerInfoTemplateAsync(ulong messageId)
	{
		if (_serverInfoTemplatesCache.TryGetValue(messageId, out var template))
			return template;

		await using var scope = scopeFactory.CreateAsyncScope();
		var infoTemplateRepository = scope.ServiceProvider.GetRequiredService<IServerInfoTemplateRepository>();
		var infoTemplate = await infoTemplateRepository.GetByMessageIdAsync(messageId, tracked: false);

		ArgumentNullException.ThrowIfNull(infoTemplate);
		_serverInfoTemplatesCache.TryAdd(messageId, infoTemplate);

		return infoTemplate;
	}
	internal async Task<ServerInfoTemplate> GetServerInfoTemplateAsync(string profileName)
	{
		if (_serverInfoProfileNamesCache.TryGetValue(profileName, out var messageId))
			return await GetServerInfoTemplateAsync(messageId);

		await using var scope = scopeFactory.CreateAsyncScope();
		var identityRepository = scope.ServiceProvider.GetRequiredService<IServerIdentityRepository>();
		var serverIdentity = await identityRepository.GetByProfileNameAsync(profileName, tracked: false);

		ArgumentNullException.ThrowIfNull(serverIdentity);
		_serverInfoProfileNamesCache.TryAdd(profileName, serverIdentity.messageId);

		return await GetServerInfoTemplateAsync(serverIdentity.messageId);
	}

	internal bool TryUpdateExistingServerInfoTemplateCache(ulong messageId, ServerInfoTemplate serverInfo)
	{
		if (!_serverInfoTemplatesCache.TryGetValue(messageId, out _)) return false;
		_serverInfoTemplatesCache[messageId] = serverInfo;
		return true;
	}

	internal bool TryUpdateServerInfoMessageId(string profileName, ulong messageId)
	{
		if (!_serverInfoProfileNamesCache.TryGetValue(profileName, out _)) return false;
		_serverInfoProfileNamesCache[profileName] = messageId;
		return true;
	}
}
