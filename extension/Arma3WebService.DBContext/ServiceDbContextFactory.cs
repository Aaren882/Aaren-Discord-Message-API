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

		DbContextOptionsBuilder<ServiceDbContext> optionsBuilder = new();

		switch (provider)
		{
			case "MySQL":
				var serverVersion = new MySqlServerVersion(new Version(11, 0));
				optionsBuilder.UseMySql(connectionString, serverVersion, x => x.MigrationsAssembly("Arma3WebService.Migrations.MySQL"));
				break;
			case "Npgsql":
				optionsBuilder.UseNpgsql(connectionString, x => x.MigrationsAssembly("Arma3WebService.Migrations.NpgSQL"));
				break;
			default:
				optionsBuilder.UseSqlite(connectionString, x => x.MigrationsAssembly("Arma3WebService.Migrations.SQLite"));
				break;
		}

		optionsBuilder.ConfigureWarnings(w =>
			w.Ignore(RelationalEventId.PendingModelChangesWarning)
		);

		return new ServiceDbContext(optionsBuilder.Options);
	}
}
