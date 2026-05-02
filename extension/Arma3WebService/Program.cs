using System.Security.Claims;
using Arma3WebService.Configuration;
using Arma3WebService.DBContext;
using Arma3WebService.Entity;
using Arma3WebService.Extensions;
using Arma3WebService.Factory;
using Arma3WebService.Handler;
using Arma3WebService.Identities;
using Arma3WebService.Managers;
using Arma3WebService.Models;
using Components.Entity;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Net.Http.Headers;

namespace Arma3WebService
{
	public class Program
	{
		public static void Main(string[] args)
		{
			Env.Load();
			var builder = WebApplication.CreateBuilder(args);
			
			builder.Services.AddDbContext<ServiceDbContext>();
			
			// Add services to the container.
			builder.Services.AddHostedService<DiscordBotService>();
			//- Register Bot Service -//

			builder.Services.AddSingleton<WebSocketService>();
			builder.Services.AddHostedService<WebSocketService>();
			//- Register WebSocket Service -//

			//- Add controllers
			builder.Services.AddSingleton<IDiscordBotService, DiscordBotService>();
			builder.Services.AddSingleton<IWebSocketService, WebSocketService>();
			
			builder.Services.AddSingleton<WebSocketConnectionFactory.IConnectionFactory, WebSocketConnectionFactory.ConnectionFactory>();
			builder.Services.AddSingleton<WebSocketConnectionManager.IConnectionManager, WebSocketConnectionManager.ConnectionManager>();
			builder.Services.AddSingleton<IArma3ActionFactory, Arma3ActionFactory>();
			builder.Services.AddSingleton<IArma3ActionManager, Arma3ActionManager>();
			builder.Services.AddSingleton<WebsocketContextEntityFactory>();
			builder.Services.AddSingleton<ServiceAction>();
			builder.Services.AddSingleton<RemoteStateManager>();
			builder.Services.AddScoped<JwtHelpers>();
			
			builder.Services.AddControllers();

			// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
			//builder.Services.AddOpenApi();
			builder.Services.AddSwaggerGen();

			//builder.Services.AddControllersWithViews();

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
				.AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuth", null);
			builder.Services.ConfigureOptions<JwtConfigureOptions>();

			var app = builder.Build();

			// Configure the HTTP request pipeline.
			if (app.Environment.IsDevelopment())
			{
				//app.MapOpenApi();
				app.MapSwagger();
				app.UseSwaggerUI();
			}

			//app.UseHttpsRedirection();

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

			using (
				var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder
					.AddConsole())
			)
			{
				var factory = app.Services.GetRequiredService<IServiceScopeFactory>();
				var logger = loggerFactory.CreateLogger<Arma3PayloadExtended>();
					
				Arma3PayLoadExtension.Options(logger, factory); //- Setup Extension Methods
			}

			app.Run();
		}
	}
}
