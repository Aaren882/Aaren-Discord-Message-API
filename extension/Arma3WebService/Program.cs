using Arma3WebService.Services;
using Discord.WebSocket;
using DotNetEnv;
using System.Collections;

namespace Arma3WebService
{
	public class Program
	{
		//internal static IQueryable env;
		//internal static DiscordSocketClient? DiscordBotClient;

		public static void Main(string[] args)
		{
			Env.Load();
			//env = Environment.GetEnvironmentVariables();

			var builder = WebApplication.CreateBuilder(args);

			// Add services to the container.
			builder.Services.AddSingleton<DiscordBotService>();
			builder.Services.AddHostedService<DiscordBotService>();
			//- Regiester Bot Service -//


			//- Add controller
			builder.Services.AddScoped<IDiscordBotService, DiscordBotService>();
			builder.Services.AddControllers();

			// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
			//builder.Services.AddOpenApi();
			builder.Services.AddSwaggerGen();


			//- WebSocket
			//builder.Services.AddSingleton<WebSocketConnectionManager>();
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

			var app = builder.Build();

			// Configure the HTTP request pipeline.
			if (app.Environment.IsDevelopment())
			{
				//app.MapOpenApi();
				app.MapSwagger();
				app.UseSwaggerUI();
			}

			app.UseHttpsRedirection();

			//- Websocket
			app.UseCors("AllowAll");
			app.UseWebSockets(new WebSocketOptions
			{
				KeepAliveInterval = TimeSpan.FromSeconds(30)
			});
			app.UseRouting();


			app.UseAuthorization();


			app.MapControllers();

			app.Run();
		}
	}
}
