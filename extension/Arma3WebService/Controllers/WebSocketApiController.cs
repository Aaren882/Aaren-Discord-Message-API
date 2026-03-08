using Arma3WebService.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Arma3WebService.Controllers
{
	[Authorize(
		Policy = "GameRequest",
		AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)
	]
	[Route("/api/ws")]
	[ApiController]
	public class WebSocketApiController : ControllerBase
	{
		private readonly IWebSocketService _service;

		public WebSocketApiController(IWebSocketService service)
		{
			_service = service;
		}

		[HttpGet("ingame")]
		public async Task<IActionResult> InGameWebSocket()
		{
			var context = ControllerContext.HttpContext;

			if (!context.WebSockets.IsWebSocketRequest)
				return Problem(statusCode: 501, detail: "Incorrect Request Context");

			await _service.CreateConnection(context);
			return new EmptyResult();
		}
	}
}
