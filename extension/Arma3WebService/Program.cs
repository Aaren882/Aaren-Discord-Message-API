
using Arma3WebService;
using Arma3WebService.Controllers;
using Arma3WebService.Services;
using Discord;
using Discord.WebSocket;
using DotNetEnv.Extensions;

namespace Arma3WebService
{
	public class Program
	{
		internal static readonly Dictionary<string, string> env = DotNetEnv.Env.Load().ToDotEnvDictionary();
		internal static DiscordSocketClient? DiscordBotClient;

		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);
			DiscordBotClient = new DiscordSocketClient();

			// Add services to the container.

			builder.Services.AddControllers();
			// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
			//builder.Services.AddOpenApi();
			builder.Services.AddSwaggerGen();

			//- Regiester Bot Service
			builder.Services.AddScoped<DiscordBotService>();
			builder.Services.AddHostedService<DiscordBotService>();

			var app = builder.Build();

			// Configure the HTTP request pipeline.
			if (app.Environment.IsDevelopment())
			{
				//app.MapOpenApi();
				app.MapSwagger();
				app.UseSwaggerUI();
			}

			app.UseHttpsRedirection();

			app.UseAuthorization();


			app.MapControllers();

			app.Run();
		}
	}
}
