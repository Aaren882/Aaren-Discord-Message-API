using System.Net;
using Arma3WebService.Models;
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

		[HttpGet]
		public async Task<IActionResult> Get()
		{
			var context = ControllerContext.HttpContext;

			if (!context.WebSockets.IsWebSocketRequest)
				return BadRequest();

			return await _service.CreateConnection(context);
		}
	}
}
