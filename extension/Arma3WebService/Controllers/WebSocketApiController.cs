using System.Net;
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
	[Route("/api/ws")]
	[ApiController]
	public class WebSocketApiController(IWebSocketService service) : ControllerBase
	{
		[HttpGet("ingame")]
		public async Task<IActionResult> InGameWebSocket()
		{
			var context = ControllerContext.HttpContext;

			if (!context.WebSockets.IsWebSocketRequest)
				return Problem(statusCode: (int)HttpStatusCode.MisdirectedRequest , detail: "Incorrect Request Context");
			
			if (context.User.Identity == null)
				return Unauthorized("No Identity is specified.");
			
			await service.CreateConnection(context);
			return new EmptyResult();
		}
		
		/*[HttpGet("file/rpt")]
		public async Task<IActionResult> RptWebSocket()
		{
			var context = ControllerContext.HttpContext;
			
			if (!context.WebSockets.IsWebSocketRequest)
				return Problem(statusCode: 501, detail: "Incorrect Request Context");
			
			if (context.User.Identity == null)
				return Unauthorized("No Identity is specified.");
			
			await service.CreateConnection(new WebsocketContextEntity(context, Arma3PayLoadType.Rpt));
			return new EmptyResult();
		}*/
	}
}
