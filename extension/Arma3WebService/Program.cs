using System.Security.Claims;
using Arma3WebService.Handler;
using Arma3WebService.Identities;
using Arma3WebService.Models;
using Components.Entity;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Arma3WebService
{
	public class Program
	{
		public static void Main(string[] args)
		{
			Env.Load();
			var builder = WebApplication.CreateBuilder(args);

			// Add services to the container.
			builder.Services.AddSingleton<DiscordBotService>();
			builder.Services.AddHostedService<DiscordBotService>();
			//- Regiester Bot Service -//

			builder.Services.AddSingleton<WebSocketService>();
			builder.Services.AddHostedService<WebSocketService>();
			//- Regiester WebSocket Service -//

			//- Add controllers
			builder.Services.AddScoped<IDiscordBotService, DiscordBotService>();
			builder.Services.AddTransient<IWebSocketService, WebSocketService>();
			builder.Services.AddSingleton<JwtHelpers>();
			builder.Services.AddControllers();

			// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
			//builder.Services.AddOpenApi();
			builder.Services.AddSwaggerGen();

			//builder.Services.AddControllersWithViews();

			//- WebSocket
			builder.Services.AddCors(options =>
			{
				options.AddPolicy("AllowAll",
					policy =>
					{
						policy.AllowAnyOrigin()
							  .AllowAnyHeader()
							  .AllowAnyMethod();
					});
			});

			builder.Services.AddAuthorization(options =>
			{
				options.AddPolicy("GameRequest", policy => 
					policy.RequireClaim(ClaimTypes.NameIdentifier, IdentityRoles.GameServerGuid.ToString())
				);
			});

			builder.Services
				.AddAuthentication()
				.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
				{
					options.IncludeErrorDetails = true; // Show exception details
					options.TokenValidationParameters =
						new JwtHelpers(builder.Configuration).GetValidationParameters();
				})
				.AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuth", null);


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
			app.UseCors("AllowAll");
			app.UseWebSockets(new WebSocketOptions
			{
				KeepAliveInterval = TimeSpan.FromSeconds(30)
			});
			app.UseRouting();

			app.UseAuthentication();
			app.UseAuthorization();


			app.MapControllers();

			app.Run();
		}
	}
}
