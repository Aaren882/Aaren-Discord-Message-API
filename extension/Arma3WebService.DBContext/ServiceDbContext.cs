using System.Text.Json;
using Arma3WebService.DBContext.Schema;
using Component.DiscordEntity;
using Microsoft.EntityFrameworkCore;

namespace Arma3WebService.DBContext;

public class ServiceDbContext : DbContext
{
	public ServiceDbContext(
		DbContextOptions<ServiceDbContext> options
	): base(options)
	{
	}

	public DbSet<ServerIdentity> ServerIdentities { get; set; }
	public DbSet<ServerInfoTemplate> ServerInfoList { get; set; }
	public DbSet<InternalManagement> InternalManagement { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		// Prevents EF Core from treating external types as DB Entities
		modelBuilder.Entity<ServerInfoTemplate>(builder  =>
		{
			var propertyBuilder = builder.Property(c => c.messageOffline)
				.HasColumnName("MessageOffline")
				.HasConversion(
					// To: DB
					v => JsonSerializer.Serialize(v, MsgPayload_JsonContext.Default.DiscordMessageDto),
					// From: DB
					v => JsonSerializer.Deserialize(v, MsgPayload_JsonContext.Default.DiscordMessageDto) 
					     ?? new DiscordMessageDto()
				);
			if (Database.IsNpgsql()) // PostgreSQL
			{
				propertyBuilder.HasColumnType("jsonb");
			}
			else if (Database.IsMySql()) // MySQL (Pomelo)
			{
				propertyBuilder.HasColumnType("json");
			}
			else if (Database.IsSqlite()) // SQLite
			{
				propertyBuilder.HasColumnType("TEXT");
			}
		});

		//- Postgres work around
		if (Database.IsNpgsql())
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
