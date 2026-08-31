using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Arma3WebService.DBContext;

public class ServiceDbContextFactory : IDesignTimeDbContextFactory<ServiceDbContext>
{
	public ServiceDbContext CreateDbContext(string[] args)
	{
		var provider = Environment.GetEnvironmentVariable("DB_PROVIDER") ?? "SQLite";
		var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ?? "Data Source=data.db";
		var migrationAssembly = $"Arma3WebService.Migrations.{provider}";

		DbContextOptionsBuilder<ServiceDbContext> optionsBuilder = new ();

		switch (provider)
		{
			case "MySQL":
				optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), x => x.MigrationsAssembly(migrationAssembly));
				break;
			case "Npgsql":
				optionsBuilder.UseNpgsql(connectionString, x => x.MigrationsAssembly(migrationAssembly));
				break;
			default:
				optionsBuilder.UseSqlite(connectionString, x => x.MigrationsAssembly(migrationAssembly));
				break;
		}

		optionsBuilder.ConfigureWarnings(w =>
			w.Ignore(RelationalEventId.PendingModelChangesWarning)
		);

		return new ServiceDbContext(optionsBuilder.Options);
	}
}
