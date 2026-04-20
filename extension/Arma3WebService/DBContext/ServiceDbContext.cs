using Arma3WebService.Entity;
using Microsoft.EntityFrameworkCore;

namespace Arma3WebService.DBContext;


public sealed class ServiceDbContext: DbContext
{
	private ILogger<ServiceDbContext> logger;

	public ServiceDbContext(
		ILogger<ServiceDbContext> logger, IWebHostEnvironment env
	)
	{
		this.logger = logger;
		Database.EnsureCreated();
	}
	
	public DbSet<ServerIdentity> Identifier { get; set; }
	public DbSet<ServerInfoTemplate> UpdateServerInfo { get; set; }

	public async Task CreateServerIdentityAsync(WebsocketContextEntity websocketContextEntity)
	{
		var profileName = websocketContextEntity.GetIndentity();
		var exist = Identifier.FirstOrDefault(o => o.profileName == profileName);
				
		if (exist != null) return;
		
		Identifier.Add(new ServerIdentity
		{
			profileName = profileName
		});
		await SaveChangesAsync();
		logger.LogInformation("Create \"{profileName}\" ServerIdentity.", profileName);
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

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		optionsBuilder.UseSqlite("Data Source=Test.db");
	}
}
