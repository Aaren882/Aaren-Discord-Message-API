using Arma3WebService.DBContext.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Arma3WebService.DBContext;

public class ServiceDbContext(
	DbContextOptions<ServiceDbContext> options,
	IConfiguration configuration,
	ILogger<ServiceDbContext> logger
) : DbContext(options)
{
	public DbSet<ServerIdentity> ServerIdentities { get; set; }
	public DbSet<ServerInfoTemplate> ServerInfoList { get; set; }
	public DbSet<InternalManagement> InternalManagement { get; set; }

	public async Task UpdateServerIdentityMessageIdAsync(string profileName, string serverInfoMessageId)
	{
		var exist = ServerIdentities.FirstOrDefault(
			o => o.profileName == profileName
		);

		if (exist == null)
		{
			logger.LogError("\"{profileName}\" ServerIdentity  is not found !!", profileName);
			return;
		}

		exist.messageId = ulong.Parse(serverInfoMessageId);
		await SaveChangesAsync();
	}
	public async Task<ServerIdentity?> GetServerIdentityFromProfileNameAsync(string profileName)
	{
		var exist = await ServerIdentities.FirstOrDefaultAsync(
			o => o.profileName == profileName
		);

		if (exist is null)
			logger.LogError("\"{profileName}\" ServerIdentity  is not found !!", profileName);

		return exist;
	}
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		//- Postgres work around
		var provider = Environment.GetEnvironmentVariable("DB_PROVIDER") ?? configuration["DB_PROVIDER"] ?? "SQLite";
		if (provider == "NpgSQL")
		{
			//- it cannot take ulong :(
			modelBuilder.Entity<InternalManagement>()
				.Property(e => e.messageId)
				.HasConversion(
					v => (decimal)v,    // To database
					v => (ulong)v   // From database
				)
				.HasColumnType("numeric(20, 0)"); //- type in Postgres 
		}
	}
}
