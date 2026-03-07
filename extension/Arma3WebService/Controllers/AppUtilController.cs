using Arma3WebService.Identities;
using Microsoft.AspNetCore.Mvc;

namespace Arma3WebService.Controllers
{
	[Route("api/token")]
	[ApiController]
	public class AppUtilController : ControllerBase
	{
		private readonly ILogger _logger;
		private readonly JwtHelpers _jwtHelpers;

		public AppUtilController(ILogger<AppUtilController> logger, JwtHelpers jwtHelpers)
		{
			_logger = logger;
			_jwtHelpers = jwtHelpers;
		}

		[HttpPost]
		public IActionResult GenToken(IdentityRolesPayload payload)
		{
			if (payload == null)
			{
				return BadRequest();
			}
			//AnsiConsole.MarkupLine($"[yellow bold]\"{username}\"[/] [green]here is your token below:[/]");
			//AnsiConsole.MarkupLine($"[Chartreuse4]{token}[/]");

			//var panel = new Panel($"[dim]{token}[/]")
			//	.Header($"[yellow bold]\"{username}\"[/] [green]here is your token below:[/]")
			//	.BorderColor(Color.Chartreuse4)
			//	.Border(BoxBorder.Rounded);

			//AnsiConsole.Write(panel);
			//Console.WriteLine($"Token Vaildation : \"{VaildateToken(token)}\"");

			return Ok(_jwtHelpers.GenerateToken(payload));
		}

		[HttpGet]
		public IActionResult VaildateToken(IdentityRolesPayload payload)
		{
			var vaildation = _jwtHelpers.VaildateToken(payload)
				.GetAwaiter().GetResult();

			return Ok(vaildation.IsValid);
		}
	}
}
