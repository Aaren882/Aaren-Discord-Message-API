using System.Collections.Concurrent;
using Arma3WebService.DBContext;
using Arma3WebService.DBContext.Repositories;
using Arma3WebService.DBContext.Schema;
using Microsoft.EntityFrameworkCore;

namespace Arma3WebService.Managers;

public sealed class RemoteStateManager(
	IServerIdentityRepository identityRepository,
	IServerInfoTemplateRepository infoTemplateRepository
)
{
	private readonly ConcurrentDictionary<ulong, WebsocketServer> _gameSessionsCache = [];
	private readonly ConcurrentDictionary<ulong, ServerInfoTemplate> _serverInfoTemplatesCache = [];
	private readonly ConcurrentDictionary<string, ulong> _serverInfoProfileNamesCache = [];

	internal async Task UpdateGameSessionCacheAsync(string profileName, WebsocketServer? connection = null)
	{
		var serverIdentity = await identityRepository.GetByProfileNameAsync(profileName);

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

		var infoTemplate = await infoTemplateRepository.GetByMessageIdAsync(messageId);

		ArgumentNullException.ThrowIfNull(infoTemplate);
		_serverInfoTemplatesCache.TryAdd(messageId, infoTemplate);

		return infoTemplate;
	}
	internal async Task<ServerInfoTemplate> GetServerInfoTemplateAsync(string profileName)
	{
		if (_serverInfoProfileNamesCache.TryGetValue(profileName, out var messageId))
			return await GetServerInfoTemplateAsync(messageId);

		var serverIdentity = await identityRepository.GetByProfileNameAsync(profileName);

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
