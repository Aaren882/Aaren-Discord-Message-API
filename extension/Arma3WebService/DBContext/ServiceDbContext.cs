using Arma3WebService.Entity;
using Microsoft.EntityFrameworkCore;

namespace Arma3WebService.DBContext;


public sealed class ServiceDbContext: DbContext
{
	private ILogger<ServiceDbContext> logger;

	public ServiceDbContext(
		ILogger<ServiceDbContext> logger,IWebHostEnvironment env
	)
	{
		this.logger = logger;
		Database.EnsureCreated();
	}
	
	public DbSet<ServerIdentity> Identifier { get; set; }
	public DbSet<ServerInfoTemplate> UpdateServerInfo { get; set; }

	public async Task<bool> CreateServerIdentityAsync(string profileName, string messageId)
	{
		var exist = Identifier.FirstOrDefault(o => o.profileName == profileName);
				
		if (exist != null) return false;
		
		Identifier.Add(new ServerIdentity
		{
			profileName = profileName,
			messageId = ulong.Parse(messageId),
		});
		await SaveChangesAsync();
		logger.LogInformation("Create \"{profileName}\" ServerIdentity.", profileName);
		return true;
	}

	public async Task UpdateServerIdentityMessageIdAsync(string profileName, string serverInfoMessageId)
	{
		var exist = Identifier.FirstOrDefault(
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
	
	public async Task<ServerIdentity?> GetServerIdentityMessageIdAsync(string profileName)
	{
		var exist = await Identifier.FirstOrDefaultAsync(
			o => o.profileName == profileName
		);

		if (exist != null) return exist;
		
		logger.LogError("\"{profileName}\" ServerIdentity  is not found !!", profileName);
		return null;
	}

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		optionsBuilder.UseSqlite("Data Source=Test.db");
	}
}
