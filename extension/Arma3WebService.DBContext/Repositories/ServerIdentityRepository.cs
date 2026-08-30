using Arma3WebService.DBContext.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Arma3WebService.DBContext.Repositories;

public interface IServerIdentityRepository
{
	ServiceDbContext DbContext { get; }
	Task<ServerIdentity?> GetByMessageIdAsync(ulong messageId);
	Task<ServerIdentity?> GetByProfileNameAsync(string profileName);

	Task<int> AddServerIdentityAsync(ServerIdentity identity);

	Task<int> UpdateServerIdentityAsync(ServerIdentity identity);

	Task<int> UpdateServerIdentityMessageIdAsync(string profileName, string serverInfoMessageId);

	Task<int> RemoveServerIdentityAsync(ServerIdentity identity);
}

public class ServerIdentityRepository(ILogger<IServerIdentityRepository> Logger, ServiceDbContext DbContext) : IServerIdentityRepository
{
	public ServiceDbContext DbContext { get; } = DbContext;

	public Task<ServerIdentity?> GetByMessageIdAsync(ulong messageId)
	{
		return DbContext.ServerIdentities
			.FirstOrDefaultAsync(o => o.messageId == messageId);
	}
	public Task<ServerIdentity?> GetByProfileNameAsync(string profileName)
	{
		// The repository is where the specific EF Core call lives
		return DbContext.ServerIdentities
			.FirstOrDefaultAsync(o => o.profileName == profileName);
	}

	public Task<int> AddServerIdentityAsync(ServerIdentity identity)
	{
		DbContext.ServerIdentities.Add(identity);
		return Task.FromResult(0);
	}

	public Task<int> UpdateServerIdentityAsync(ServerIdentity identity)
	{
		// EF Core tracks changes, so you often just attach/update and save.
		DbContext.ServerIdentities.Update(identity);
		return Task.FromResult(0);
	}

	public async Task<int> UpdateServerIdentityMessageIdAsync(string profileName, string serverInfoMessageId)
	{
		var exist = await DbContext.ServerIdentities.FirstOrDefaultAsync(
			o => o.profileName == profileName
		);

		if (exist == null)
		{
			Logger.LogError("\"{profileName}\" ServerIdentity  is not found !!", profileName);
			return -1;
		}

		exist.messageId = ulong.Parse(serverInfoMessageId);

		return 0;
	}

	public Task<int> RemoveServerIdentityAsync(ServerIdentity identity)
	{
		DbContext.ServerIdentities.Remove(identity);
		return Task.FromResult(0);
	}
}
