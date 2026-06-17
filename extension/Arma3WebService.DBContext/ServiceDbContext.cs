using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Arma3WebService.DBContext;

public class ServiceDbContext: DbContext
{
	private readonly ILogger<ServiceDbContext> _logger;
	private readonly IConfiguration _configuration;
	
	public ServiceDbContext(
		DbContextOptions<ServiceDbContext> options,
		IConfiguration configuration,
		ILogger<ServiceDbContext> logger
	): base(options)
	{
		_logger = logger;
	}

	public DbSet<ServerIdentity> ServerIdentities { get; set; }
	public DbSet<ServerInfoTemplate> ServerInfoList { get; set; }
	public DbSet<InternalManagement> InternalManagement { get; set; }

	public async Task<bool> CreateServerIdentityAsync(string profileName, string messageId)
	{
		var exist = ServerIdentities.FirstOrDefault(o => o.profileName == profileName);
				
		if (exist != null) return false;
		
		await ServerIdentities.AddAsync(new ServerIdentity
		{
			profileName = profileName,
			messageId = ulong.Parse(messageId),
		});
		await SaveChangesAsync();
		_logger.LogInformation("Create \"{profileName}\" ServerIdentity.", profileName);
		return true;
	}

	public async Task UpdateServerIdentityMessageIdAsync(string profileName, string serverInfoMessageId)
	{
		var exist = ServerIdentities.FirstOrDefault(
			o => o.profileName == profileName
		);

		if (exist == null)
		{
			_logger.LogError("\"{profileName}\" ServerIdentity  is not found !!", profileName);
			return;
		}

		exist.messageId = ulong.Parse(serverInfoMessageId);
		await SaveChangesAsync();
	}

	public async Task UpsertServerInfoTemplateAsync(FileInfo fileInfo, string serverInfoMessageId)
	{
		var parsedId = ulong.Parse(serverInfoMessageId); 
		var exist = ServerInfoList.FirstOrDefault(o => o.messageId == parsedId);
		
		if (exist == null) {
			await ServerInfoList.AddAsync(new ServerInfoTemplate
			{
				messageId = parsedId,
				messageTemplatePath = fileInfo.FullName
			});
		} else {
			exist.messageTemplatePath = fileInfo.FullName;
			exist.lastUpdate = DateTime.Now;
		}
		
		await SaveChangesAsync();
	}
	
	public async Task<ServerIdentity?> GetServerIdentityFromProfileNameAsync(string profileName)
	{
		var exist = await ServerIdentities.FirstOrDefaultAsync(
			o => o.profileName == profileName
		);

		if (exist is null)
			_logger.LogError("\"{profileName}\" ServerIdentity  is not found !!", profileName);
		
		return exist;
	}
	public async Task<ServerIdentity?> GetServerIdentityFromMessageIdAsync(ulong messageId)
	{
		var exist = await ServerIdentities.FirstOrDefaultAsync(
			o => o.messageId == messageId
		);

		if (exist is null)
			_logger.LogError("\"{messageId}\" ServerIdentity  is not found !!", messageId);
		
		return exist;
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);
		
		//- Postgres work around
		var provider = Environment.GetEnvironmentVariable("DB_PROVIDER") ?? _configuration["DB_PROVIDER"] ?? "SQLite";
		if (provider == "NpgSQL")
		{
			//- it cannot take ulong :(
			modelBuilder.Entity<InternalManagement>()
				.Property(e => e.messageId)
				.HasConversion(
					v => (decimal)v,	// To database
					v => (ulong)v	// From database
				)
				.HasColumnType("numeric(20, 0)"); //- type in Postgres 
		}
	}
}
