using System.Collections.Concurrent;
using Arma3WebService.DBContext;
using Arma3WebService.Models;
using Microsoft.EntityFrameworkCore;

namespace Arma3WebService.Managers;

public sealed class RemoteStateManager(
	IServiceScopeFactory ServiceScopeFactory,
	IServiceProvider ServiceProvider
)
{
	private readonly ConcurrentDictionary<ulong, IConnection> _gameSessionsCache = [];
	private readonly ConcurrentDictionary<ulong, ServerInfoTemplate> _serverInfoTemplatesCache = [];
	private readonly ConcurrentDictionary<string, ulong> _serverInfoProfileNamesCache = [];

	internal async Task UpdateGameSessionCacheAsync(string profileName, IConnection? connection = null)
	{
		using var scope = ServiceScopeFactory.CreateScope();
		await using var dbContext = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
		
		var serverIdentity = await dbContext.GetServerIdentityFromProfileNameAsync(profileName);
		if (serverIdentity == null)
			throw new NullReferenceException($"\"serverIdentity : {serverIdentity}\" is not exist.");
		
		var messageId = serverIdentity.messageId;
		if (connection is not null)
			_gameSessionsCache[messageId] = connection;
		else
			_gameSessionsCache.TryRemove(messageId, out connection);
	}
	
	internal async Task<IConnection> GetGameSessionAsync(ulong messageId)
	{
		if (_gameSessionsCache.TryGetValue(messageId, out var session))
			return session;

		using var scope = ServiceScopeFactory.CreateScope();
		await using var dbContext = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
		
		var serverIdentity = await dbContext.GetServerIdentityFromMessageIdAsync(messageId);
		if (serverIdentity == null)
			throw new NullReferenceException($"\"serverIdentity : {serverIdentity}\" is not exist.");
		
		var webSocketService = ServiceProvider.GetRequiredService<IWebSocketService>();
		var connection = webSocketService.GetConnection(serverIdentity.profileName);
		_gameSessionsCache[messageId] = connection;

		return connection;
	}
	
	internal bool TryUpdateExistingServerInfoTemplateCache(ulong messageId, ServerInfoTemplate serverInfo)
	{
		if (!_serverInfoTemplatesCache.TryGetValue(messageId, out _)) return false;
		_serverInfoTemplatesCache[messageId] = serverInfo;
		return true;
	}

	internal async Task<ServerInfoTemplate> GetServerInfoTemplateAsync(ulong messageId)
	{
		if (_serverInfoTemplatesCache.TryGetValue(messageId, out var template))
			return template;
		
		using var scope = ServiceScopeFactory.CreateScope();
		await using var dbContext = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
		
		var infoTemplate = await dbContext.ServerInfoList
			.FirstOrDefaultAsync(o => o.messageId == messageId);

		if (infoTemplate == null) throw new NullReferenceException("\"infoTemplate\" does not exist.");
		
		_serverInfoTemplatesCache.TryAdd(messageId, infoTemplate);
		
		return infoTemplate;
	}
	internal async Task<ServerInfoTemplate> GetServerInfoTemplateAsync(string profileName)
	{
		if (_serverInfoProfileNamesCache.TryGetValue(profileName, out var messageId))
			return await GetServerInfoTemplateAsync(messageId);
		
		using var scope = ServiceScopeFactory.CreateScope();
		await using var dbContext = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();

		var serverIdentity = await dbContext.GetServerIdentityFromProfileNameAsync(profileName);
		if (serverIdentity is null)
			throw new NullReferenceException($"\"serverIdentity : {serverIdentity}\" is not exist.");

		_serverInfoProfileNamesCache.TryAdd(profileName, serverIdentity.messageId);
		
		return await GetServerInfoTemplateAsync(serverIdentity.messageId);
	}

	internal bool TryUpdateServerInfoMessageId(string profileName, ulong messageId)
	{
		if (!_serverInfoProfileNamesCache.TryGetValue(profileName, out _)) return false;
		_serverInfoProfileNamesCache[profileName] = messageId;
		return true;
	}
}
