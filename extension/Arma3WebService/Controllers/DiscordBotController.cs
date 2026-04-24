using Arma3WebService.Models;
using Discord;
using Microsoft.AspNetCore.Mvc;

namespace Arma3WebService.Controllers
{

	[ApiController]
	[Route("[controller]")]
	public class DiscordBotController(IDiscordBotService service) : ControllerBase
	{
		private static readonly string[] Summaries = new[]
		{
			"Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
		};

		[HttpPost(Name = "PostBotOnline")]
		public async Task<IActionResult> PostBotOnline(string text)
		{
			var result = await service.PostBotOnline(text);
			return Ok(result);
		}

		//////////////////////
		[HttpGet($"File/{{id}}")]
		public async Task<IActionResult> DownloadFile(string id)
		{
			var bytes = await service.SendLocalFile(id);
			return File(bytes,"application/octet-stream", id);
		}

		[HttpGet(Name = "GetDiscordBot")]
		public async Task<IEnumerable<WeatherForecast>> Get()
		{
			var result = await CreateWeatherForecast(1, 5).ToArrayAsync();

			return result;
		}

		private static async IAsyncEnumerable<WeatherForecast> CreateWeatherForecast(int start, int end)
		{
			for (var index = 0; index < end; index++)
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
			for (var index = 0; index < end; index++)
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
