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
	private readonly ConcurrentDictionary<ulong, IConnection> _gameSessionCache = [];
	private readonly ConcurrentDictionary<ulong, ServerInfoTemplate> _serverInfoCache = [];
	private readonly ConcurrentDictionary<string, ulong> _serverInfoProfileNameCache = [];

	internal async Task<IConnection> GetGameSessionAsync(ulong messageId)
	{
		if (_gameSessionCache.TryGetValue(messageId, out var session))
			return session;

		using var scope = ServiceScopeFactory.CreateScope();
		await using var dbContext = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
		
		var serverIdentity = await dbContext.GetServerIdentityFromMessageIdAsync(messageId);
		if (serverIdentity == null)
			throw new NullReferenceException($"\"serverIdentity : {serverIdentity}\" is not exist.");
		
		var webSocketService = ServiceProvider.GetRequiredService<IWebSocketService>();
		var connection = webSocketService.GetConnection(serverIdentity.profileName);
		_gameSessionCache[messageId] = connection;

		return connection;
	}
	
	internal bool TryUpdateExistingServerInfoCache(ulong messageId, ServerInfoTemplate serverInfo)
	{
		if (!_serverInfoCache.TryGetValue(messageId, out _)) return false;
		_serverInfoCache[messageId] = serverInfo;
		return true;
	}

	internal async Task<ServerInfoTemplate> GetServerInfoTemplateAsync(ulong messageId)
	{
		if (_serverInfoCache.TryGetValue(messageId, out var template))
			return template;
		
		using var scope = ServiceScopeFactory.CreateScope();
		await using var dbContext = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
		
		var infoTemplate = await dbContext.ServerInfoList
			.FirstOrDefaultAsync(o => o.messageId == messageId);

		if (infoTemplate == null) throw new NullReferenceException("\"infoTemplate\" does not exist.");
		
		_serverInfoCache.AddOrUpdate(messageId, infoTemplate, 
			(_, _) => infoTemplate);
		
		return infoTemplate;
	}
	internal async Task<ServerInfoTemplate> GetServerInfoTemplateAsync(string profileName)
	{
		if (_serverInfoProfileNameCache.TryGetValue(profileName, out var messageId))
			return await GetServerInfoTemplateAsync(messageId);
		
		using var scope = ServiceScopeFactory.CreateScope();
		await using var dbContext = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();

		var serverIdentity = await dbContext.GetServerIdentityFromProfileNameAsync(profileName);
		if (serverIdentity is null)
			throw new NullReferenceException($"\"serverIdentity : {serverIdentity}\" is not exist.");

		_serverInfoProfileNameCache.AddOrUpdate(profileName, serverIdentity.messageId, 
			(_, _) => serverIdentity.messageId);
		
		return await GetServerInfoTemplateAsync(serverIdentity.messageId);
	}
}
