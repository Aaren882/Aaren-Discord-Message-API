using System.Net;
using Arma3WebService.Factory;
using Arma3WebService.Managers;
using Arma3WebService.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Arma3WebService.Controllers;

[Authorize(
	Policy = "GameRequest",
	AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)
]
[Route("/api/ws")]
[ApiController]
public class WebSocketApiController(
	WebsocketServer websocketWorker
) : ControllerBase
{
	[HttpGet("ingame")]
	public async Task<IActionResult> InGameWebSocket()
	{
		var context = ControllerContext.HttpContext;

		if (!context.WebSockets.IsWebSocketRequest)
		{
			return Problem(
				detail: "Incorrect Request Context",
				statusCode: (int)HttpStatusCode.MisdirectedRequest
			);
		}

		if (context.User.Identity == null)
			return Unauthorized("No Identity is specified.");

		//- Implement new Framework
		await websocketWorker.StartAsync(context);

		return new EmptyResult();
	}
}
