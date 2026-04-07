using System.Runtime.CompilerServices;
using System.Text.Json;
using Arma3WebService.Entity;
using Arma3WebService.Models;
using Components.Entity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;

namespace Arma3WebService.Controllers
{
	/*[Authorize(
		Policy = "GameRequest",
		AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)
	]*/
	[Route("api/[controller]")]
	[ApiController]
	public class ArmaController(
		ILogger<ArmaController> logger,
		IWebSocketService webSocketService,
		ServiceAction serviceAction
	) : ControllerBase
	{
		[HttpPost(Name = "ArmaController")]
		public IActionResult PostLog(Arma3PayloadJson payload)
		{
			logger.LogInformation($"Restful Received Log: {payload.JsonString}");
			return Ok(new { hello = "" });
		}
		
		[HttpPost("RemoteCommand")]
		public async Task<IActionResult> RemoteCommand(Arma3RemoteCommand command)
		{
			try
			{
				await webSocketService.InvokeArmaCallBack(command);
				
				return Ok();
			}
			catch (Exception e)
			{
				return BadRequest(e.Message);
			}
		}

		[HttpGet("GetLogs")]
		public async Task Get()
		{
			var ctx = ControllerContext.HttpContext;
			ctx.Response.Headers.Append(HeaderNames.ContentType, "text/event-stream");
			await serviceAction.SSE_Logging(ctx);
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
