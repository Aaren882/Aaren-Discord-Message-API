using System.Collections.ObjectModel;
using Discord;
using Discord.WebSocket;
using DotNetEnv.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Arma3DiscordBot.Controllers
{

	[ApiController]
	[Route("[controller]")]
	public class DiscordBotController : ControllerBase
	{
		private static readonly string[] Summaries = new[]
		{
			"Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
		};

		private readonly ILogger<DiscordBotController> _logger;

		static Dictionary<string, string> env = Program.env;
		static DiscordSocketClient? _client = Program.DiscordBotClient;
		readonly static string TestChannel = env.GetValueOrDefault("TestChannel", "");

		public DiscordBotController(ILogger<DiscordBotController> logger)
		{
			_logger = logger;

			_client.Log += Log;
		}

		private Task Log(LogMessage msg)
		{
			// Use the appropriate ILogger method based on Discord's LogSeverity
			switch (msg.Severity)
			{
				case LogSeverity.Critical:
					_logger.LogCritical(msg.Exception, "{Message}", msg.Message);
					break;
				case LogSeverity.Error:
					_logger.LogError(msg.Exception, "{Message}", msg.Message);
					break;
				case LogSeverity.Warning:
					_logger.LogWarning("{Message}", msg.Message);
					break;
				case LogSeverity.Info:
					_logger.LogInformation("{Message}", msg.Message);
					break;
				case LogSeverity.Verbose:
					_logger.LogInformation("{Message}", msg.Message);
					break;
				case LogSeverity.Debug:
					_logger.LogDebug("{Message}", msg.Message);
					break;
			}
			return Task.CompletedTask;
		}

		[HttpPost(Name = "PostBotOnline")]
		public async Task<JsonResult> PostBotOnline(string text)
		{
			var channel = await _client
				.GetChannelAsync(Convert.ToUInt64(TestChannel)) as IMessageChannel;

			IUserMessage message = await channel.SendMessageAsync(text: text);

			return new JsonResult(message);
		}

		//////////////////////

		[HttpGet(Name = "GetDiscordBot")]
		public IEnumerable<WeatherForecast> Get()
		{
			var result = CreateWeatherForecast(1,5).ToArray();

			return result;
		}

		private static IEnumerable<WeatherForecast> CreateWeatherForecast(int start, int end)
		{
			for (int index = 0; index < end; index++)
			{
				yield return new WeatherForecast
				{
					Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
					TemperatureC = Random.Shared.Next(-20, 55),
					Summary = Summaries[Random.Shared.Next(Summaries.Length)]
				};
			}
		}
	}
}
