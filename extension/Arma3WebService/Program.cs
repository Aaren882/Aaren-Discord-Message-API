using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Arma3WebService.Identities;
using Arma3WebService.Models;
using Discord;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

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
				options.AddPolicy("ElevatedRights", policy =>
					policy.RequireRole("Administrator", "PowerUser", "BackupAdministrator"));
			});

			builder.Services
				.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
				.AddJwtBearer(options =>
				{
					options.IncludeErrorDetails = true; // Show exception details
					options.TokenValidationParameters = 
						new JwtHelpers(builder.Configuration).GetValidationParameters(Role.Admin);

					/*options.TokenValidationParameters = new TokenValidationParameters
					{
						// 簽發者
						ValidateIssuer = true,
						ValidIssuer = builder.Configuration["Jwt:Issuer"],
						// 接收者
						ValidateAudience = false,
						ValidAudience = builder.Configuration["Jwt:Audience"],
						// Token 的有效期間
						ValidateLifetime = true,
						// 如果 Token 中包含 key 才需要驗證，一般都只有簽章而已
						ValidateIssuerSigningKey = false,
						// key
						IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtHelpers.GenerateHashSecret(builder.Configuration["Jwt:Key"]!)))
					};*/
				});

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
