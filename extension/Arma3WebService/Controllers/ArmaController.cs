using System.Runtime.CompilerServices;
using Arma3WebService.Models;
using Components.Entity;
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
		private readonly IWebSocketService _webSocketService;

		public ArmaController(ILogger<ArmaController> logger, IWebSocketService webSocketService, IServiceProvider serviceProvider)
		{
			_logger = logger;
			_serviceProvider = serviceProvider;
			_webSocketService = webSocketService;
		}

		[HttpPost(Name = "ArmaController")]
		public IActionResult PostLog(Arma3PayloadJson payload)
		{
			_logger.LogInformation($"Restful Received Log: {payload.JsonString}");
			return Ok(new { hello = "" });
		}
		
		[HttpPost("RemoteCommand")]
		public async Task<IActionResult> RemoteCommand(Arma3RemoteCommand command)
		{
			try
			{
				await _webSocketService.InvokeArmaCallBack(command);
				
				return Ok();
			}
			catch (Exception e)
			{
				return BadRequest(e.Message);
			}
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
