using System.Runtime.CompilerServices;
using Arma3WebService.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Arma3WebService.Controllers
{
	/*[Authorize(
		Policy = "GameRequest",
		AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)
	]*/
	[Route("api/[controller]")]
	[ApiController]
	public class ArmaController : ControllerBase
	{
		private readonly ILogger<ArmaController> _logger;
		private readonly IServiceProvider _serviceProvider;

		public ArmaController(ILogger<ArmaController> logger, IServiceProvider serviceProvider)
		{
			_logger = logger;
			_serviceProvider = serviceProvider;
		}

		[HttpPost(Name = "ArmaController")]
		public IActionResult PostLog(Arma3PayloadMessage payload)
		{
			_logger.LogInformation($"Restful Received Log: {payload.Message}");
			return Ok(new { hello = "" });
		}
		
		[HttpPost("remote")]
		public async Task<IActionResult> Remote(string gameId, Arma3PayloadCallBack payload)
		{
			var service = _serviceProvider.GetRequiredService<WebSocketService>();
			await service.InvokeArmaCallBack(gameId, payload);
			return Ok();
		}

		[HttpGet("GetLogs")]
		public async IAsyncEnumerable<WeatherForecast> Get()
		{
			for (int index = 1; index <= 5; index++)
			{
				await Task.Delay(500);
				yield return new WeatherForecast
				{
					Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
					TemperatureC = Random.Shared.Next(-20, 55),
					Summary = "None"
				};
			}
		}

		// Returns data one item at a time asynchronously
		[HttpGet("stream")]
		public async IAsyncEnumerable<string> GetStreamingData([EnumeratorCancellation] CancellationToken ct)
		{
			for (int i = 0; i < 10; i++)
			{
				await Task.Delay(1000, ct); // Simulate asynchronous work (e.g., db call)
				yield return $"Data item {i} at {DateTime.Now}";
			}
		}
		[HttpPost("stream")]
		public IActionResult PostStreamingData(string item)
		{
			Console.WriteLine($"POST '/stream' Hit !! data = {item}");
			/*await foreach (var item in data)
			{
				Console.WriteLine(item);
			}*/
			return Ok();
		}
	}
}
