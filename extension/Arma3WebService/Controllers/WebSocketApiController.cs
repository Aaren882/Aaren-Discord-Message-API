using System.Net;
using Arma3WebService.Models;
using Discord.Interactions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Arma3WebService.Controllers
{
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
				return Problem(statusCode: 501);

			await _service.CreateConnection(context);
			return new EmptyResult();
		}
	}
}
