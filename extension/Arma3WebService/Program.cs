using System.Collections.Concurrent;
using System.Security.Claims;
using System.Threading.Channels;
using Arma3WebService.Broker;
using Arma3WebService.Configuration;
using Arma3WebService.DBContext;
using Arma3WebService.DBContext.Repositories;
using Arma3WebService.Extensions;
using Arma3WebService.Factory;
using Arma3WebService.Handler;
using Arma3WebService.Identities;
using Arma3WebService.Managers;
using Arma3WebService.Models;
using Components.Entity;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Net.Http.Headers;
using static Arma3WebService.Managers.BinaryStreamManager;
using static Arma3WebService.Managers.WebsocketServer;

namespace Arma3WebService
{
	public class Program
	{
		public const string PersistDirectory = ".data";
		public static void Main(string[] args)
		{
			Env.Load();
			var builder = WebApplication.CreateBuilder(args);
			Arma3PayLoadExtension.Options();

			var provider = Environment.GetEnvironmentVariable("DB_PROVIDER") ?? builder.Configuration["DB_PROVIDER"] ?? "SQLite";
			builder.Services.AddDbContextFactory<ServiceDbContext>(options =>
			{
				var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ?? builder.Configuration["DB_CONNECTION_STRING"] ?? $"Data Source={Program.PersistDirectory}/data.db";

				var migrationAssembly = $"Arma3WebService.Migrations.{provider}";
				var optionsBuilder = (provider) switch
				{
					"MySQL" => options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), x => x.MigrationsAssembly(migrationAssembly)),
					"NpgSQL" => options.UseNpgsql(connectionString, x => x.MigrationsAssembly(migrationAssembly)),
					// Default to SQLite
					_ => options.UseSqlite(connectionString, x => x.MigrationsAssembly(migrationAssembly))
				};

				//- ignore last db setting warning (e.g. migrate from MySQL to Postgres)
				optionsBuilder.ConfigureWarnings(w =>
					w.Ignore(RelationalEventId.PendingModelChangesWarning)
				);
			});

			//- #LINK - `ServiceDbContextFactory.cs`
			//  : Design time for migration
			builder.Services.AddScoped(sp =>
				sp.GetRequiredService<IDbContextFactory<ServiceDbContext>>().CreateDbContext());

			builder.Services.AddSingleton<Channel<ActionPayload>>(_ => Channel.CreateBounded<ActionPayload>(new BoundedChannelOptions(1000)
			{
				SingleReader = true,
			}));
			builder.Services.AddSingleton<Channel<BinaryPayload>>(_ => Channel.CreateBounded<BinaryPayload>(new BoundedChannelOptions(100)
			{
				SingleReader = true,
			}));

			builder.Services.AddSingleton<Channel<Arma3PayloadBinaryContent>>(_ => Channel.CreateUnbounded<Arma3PayloadBinaryContent>(new UnboundedChannelOptions
			{
				SingleReader = true,
			}));
			builder.Services.AddSingleton<ConcurrentDictionary<string, Content>>(_ => new());
			builder.Services.AddSingleton<ConcurrentDictionary<string, Channel<Arma3PayloadBinaryContent>>>(_ => new());

			//- Add Services
			builder.Services.AddSingleton(new DiscordSocketClient(
				new DiscordSocketConfig
				{
					GatewayIntents = GatewayIntents.AllUnprivileged,
					LogLevel = LogSeverity.Info
				}
			));
			builder.Services.AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>(), new InteractionServiceConfig
			{
				LogLevel = LogSeverity.Info,
				DefaultRunMode = RunMode.Async
			}));

			//- Bot Background Services
			builder.Services.AddSingleton<IDiscordBotService, DiscordBotService>();
			builder.Services.AddSingleton<AdminConsoleManager>();

			//- Websocket Services
			builder.Services.AddScoped<WebsocketServer>();
			builder.Services.AddSingleton<DiscordBotRequestHandler>();
			builder.Services.AddSingleton<IWebSocketService, WebSocketService>();
			builder.Services.AddSingleton<BinaryStreamManager>();
			builder.Services.AddSingleton<UpdateDBActionBroker>();
			builder.Services.AddSingleton<BinaryPayloadBroker>();
			builder.Services.AddSingleton<IArma3ActionManager, Arma3ActionManager>();
			builder.Services.AddSingleton<WebsocketContextEntityFactory>();
			builder.Services.AddSingleton<ServiceActionManager>();
			builder.Services.AddSingleton<RemoteStateManager>();

			//- DB Repos
			builder.Services.AddScoped<IServerInfoTemplateRepository, ServerInfoTemplateRepository>();
			builder.Services.AddScoped<IServerIdentityRepository, ServerIdentityRepository>();

			//- Identity Services
			builder.Services.AddScoped<IdentityCheckService>();
			builder.Services.AddScoped<JwtHelpers>();

			// Add services to the container.
			builder.Services.AddHostedService<DiscordBotService>();
			builder.Services.AddHostedService<AdminConsoleManager>();
			//- Register Bot Service -//

			builder.Services.AddHostedService<WebSocketService>();
			builder.Services.AddHostedService<BinaryStreamManager>();
			builder.Services.AddHostedService<BinaryPayloadBroker>();
			builder.Services.AddHostedService<Arma3ActionManager>();
			//- Register Connection Services -//

			builder.Services.AddControllers();
			builder.Services.AddSwaggerGen();


			//- WebSocket
			builder.Services.AddCors(options =>
			{
				options.AddPolicy(
					"InternalCommunication",
					policy =>
						policy
							.AllowAnyMethod()
							// .AllowAnyOrigin()
							.WithHeaders(HeaderNames.ContentType, HeaderNames.Authorization)
					);
			});

			//- Auth Settings
			builder.Services
				.AddAuthorizationBuilder()
				.AddPolicy("GameRequest", policy =>
					policy.RequireClaim(
						ClaimTypes.NameIdentifier,
						IdentityRoles.GameServerGuid.ToString()
					)
				);

			builder.Services
				.AddAuthentication()
				.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme)
				.AddScheme<AuthenticationSchemeOptions, BearerAuthenticationHandler>("BasicAuth", null);
			builder.Services.ConfigureOptions<JwtConfigureOptions>();

			//- Resource monitor
			builder.Services.AddResourceMonitoring();

			var app = builder.Build();

			// Create a scope to resolve your DbContext safely
			using (var scope = app.Services.CreateScope())
			{
				var dbContext = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();

				var migrator = dbContext.GetService<IMigrator>();
				var targetMigration = dbContext.Database.GetMigrations().LastOrDefault()
					?? throw new InvalidOperationException($"No migrations found in <{nameof(ServiceDbContext)}>.");
				var pendingMigrations = dbContext.Database.GetPendingMigrations().ToArray();

				if (pendingMigrations.Length != 0)
					migrator.Migrate(targetMigration);
			}

			// Configure the HTTP request pipeline.
			if (app.Environment.IsDevelopment())
			{
				//app.MapOpenApi();
				app.MapSwagger();
				app.UseSwaggerUI();
			}

			//- Websocket
			app.UseWebSockets(new WebSocketOptions
			{
				KeepAliveInterval = TimeSpan.FromSeconds(30)
			});
			app.UseRouting();
			app.UseCors("InternalCommunication");

			app.UseAuthentication();
			app.UseAuthorization();

			app.MapControllers();

			app.Run();
		}
	}
}
