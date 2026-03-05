using Arma3WebService.Models;
using Discord;
using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc;

namespace Arma3WebService.Controllers
{

	[ApiController]
	[Route("[controller]")]
	public class DiscordBotController: ControllerBase
	{
		private static readonly string[] Summaries = new[]
		{
			"Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
		};

		//private readonly ILogger<DiscordBotController> _logger;
		private readonly IDiscordBotService _service;

		public DiscordBotController(IDiscordBotService service)
		{
			_service = service;
			//_logger = logger;
		}

		[HttpPost(Name = "PostBotOnline")]
		public async Task<OkObjectResult> PostBotOnline(string text)
		{
			IUserMessage result = await _service.PostBotOnline(text);
			return new OkObjectResult(result);
		}

		//////////////////////

		[HttpGet(Name = "GetDiscordBot")]
		public async Task<IEnumerable<WeatherForecast>> Get()
		{
			var result = await CreateWeatherForecast(1, 5).ToArrayAsync();

			return result;
		}

		private async static IAsyncEnumerable<WeatherForecast> CreateWeatherForecast(int start, int end)
		{
			for (int index = 0; index < end; index++)
			{
				await Task.Delay(500);
				yield return new WeatherForecast
				{
					Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
					TemperatureC = Random.Shared.Next(-20, 55),
					Summary = Summaries[Random.Shared.Next(Summaries.Length)]
				};
			}
		}

		[HttpGet("Async")]
		public async IAsyncEnumerable<WeatherForecast> AsyncWeatherForecast(int start = 0, int end = 5)
		{
			for (int index = 0; index < end; index++)
			{
				await Task.Delay(500);
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
