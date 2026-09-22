using System.Runtime.CompilerServices;
using Arma3WebService.Models;
using Microsoft.AspNetCore.Mvc;
using Arma3WebService.Managers;

namespace Arma3WebService.Controllers
{
	/*[Authorize(
		Policy = "GameRequest",
		AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)
	]*/
	[Route("api/[controller]")]
	[ApiController]
	public class ArmaController(
		IWebSocketService webSocketService,
		ServiceActionManager serviceAction
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
	}
}
