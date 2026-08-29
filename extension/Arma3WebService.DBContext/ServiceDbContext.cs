using Arma3WebService.DBContext.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Arma3WebService.DBContext;

public class ServiceDbContext(
	DbContextOptions<ServiceDbContext> options,
	IConfiguration configuration
) : DbContext(options)
{
	public DbSet<ServerIdentity> ServerIdentities { get; set; }
	public DbSet<ServerInfoTemplate> ServerInfoList { get; set; }
	public DbSet<InternalManagement> InternalManagement { get; set; }

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
