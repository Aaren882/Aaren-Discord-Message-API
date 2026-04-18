using System.Runtime.CompilerServices;
using Arma3WebService.Entity;
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
	public class ArmaController(
		ILogger<ArmaController> logger,
		IWebSocketService webSocketService,
		ServiceAction serviceAction
	) : ControllerBase
	{
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

		[HttpGet("GetLogs/{sessionIdentity}")]
		public async Task Get(string sessionIdentity)
		{
			var ctx = ControllerContext.HttpContext;
			await serviceAction.SSE_Logging(ctx, sessionIdentity);
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
	}
}
