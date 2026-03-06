using Arma3WebService.Models;
using DotNetEnv;

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
			builder.Services.AddControllers();

			// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
			//builder.Services.AddOpenApi();
			builder.Services.AddSwaggerGen();


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
