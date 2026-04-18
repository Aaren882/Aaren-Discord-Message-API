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
		
		//- Remove Database for local sqlite testing only
		// if (env.IsDevelopment()) Database.EnsureDeleted();
		Database.EnsureCreated();
	}
	
	public DbSet<ServerIdentity> Identifier { get; set; }
	public DbSet<ServerInfo> UpdateServerInfo { get; set; }

	public void UpsertServerIdentity(WebsocketContextEntity websocketContextEntity)
	{
		var name = websocketContextEntity.GetIndentity();
		var exist = Identifier.FirstOrDefault(o => o.profileName == name);
				
		if (exist == null) {
			Identifier.Add(new ServerIdentity { profileName = name , createTime = DateTime.Now });
		} else {
			exist.profileName = name;
		}
		
		SaveChanges();
		logger.LogInformation("Update \"{name}\" ServerIdentity .", name);
	}

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		optionsBuilder.UseSqlite("Data Source=Test.db");
	}

	/*public async Task UpdateUpdateServerInfoAsync(string name, UpdateServerInfoExtension infoExtension)
	{
		var rows = await UpdateServerInfo.ExecuteUpdateAsync(setters =>
			setters.SetProperty(extension =>
				extension, new ServerInfo(name, infoExtension))
		);
		logger.LogInformation("UpdateServerInfo update {rows} rows.",rows);
	}*/

	/*public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		// 1. Detect changes in the ChangeTracker
		ChangeTracker.DetectChanges();
		var auditEntries = new List<AuditEntry>(); // Assuming AuditEntry maps to AuditLog
        
		// 2. Iterate through changes and capture data (simplified)
		foreach (var entry in ChangeTracker.Entries())
		{
			if (entry.Entity is AuditLog || entry.State == EntityState.Unchanged) continue;
            
			// ... (Logic to capture OldValues/NewValues as JSON)
		}

		// 3. Save main changes
		var result = await base.SaveChangesAsync(cancellationToken);
        
		// 4. Save audit logs
		await SaveAuditLogs(auditEntries); 
		return result;
	}*/
}
