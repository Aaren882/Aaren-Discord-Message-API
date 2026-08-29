using Arma3WebService.DBContext.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Arma3WebService.DBContext.Repositories;

public interface IServerIdentityRepository
{
	ServiceDbContext DbContext { get; }
	Task<ServerIdentity?> GetByProfileNameAsync(string profileName);
	Task AddServerIdentityAsync(ServerIdentity identity);
	Task UpdateServerIdentityAsync(ServerIdentity identity);
	Task UpdateServerIdentityMessageIdAsync(string profileName, string serverInfoMessageId);
}

public class ServerIdentityRepository(ILogger<IServerIdentityRepository> Logger, ServiceDbContext DbContext) : IServerIdentityRepository
{
	public ServiceDbContext DbContext { get; } = DbContext;

	public Task<ServerIdentity?> GetByProfileNameAsync(string profileName)
	{
		// The repository is where the specific EF Core call lives
		return DbContext.ServerIdentities
			.FirstOrDefaultAsync(o => o.profileName == profileName);
	}

	public Task AddServerIdentityAsync(ServerIdentity identity)
	{
		DbContext.ServerIdentities.Add(identity);
		return DbContext.SaveChangesAsync();
	}

	public Task UpdateServerIdentityAsync(ServerIdentity identity)
	{
		// EF Core tracks changes, so you often just attach/update and save.
		DbContext.ServerIdentities.Update(identity);
		return DbContext.SaveChangesAsync();
	}

	public async Task UpdateServerIdentityMessageIdAsync(string profileName, string serverInfoMessageId)
	{
		var exist = await DbContext.ServerIdentities.FirstOrDefaultAsync(
			o => o.profileName == profileName
		);

		if (exist == null)
		{
			Logger.LogError("\"{profileName}\" ServerIdentity  is not found !!", profileName);
			return;
		}

		exist.messageId = ulong.Parse(serverInfoMessageId);
		await DbContext.SaveChangesAsync();
	}
}
