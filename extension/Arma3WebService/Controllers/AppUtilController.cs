using Arma3WebService.Identities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Arma3WebService.Controllers
{
	[Authorize(AuthenticationSchemes = "BasicAuth")]
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

			return Ok(_jwtHelpers.GenerateToken(payload));
		}

		[HttpGet]
		public IActionResult VaildateToken(IdentityRolesPayload payload)
		{
			var vaildation = _jwtHelpers.VaildateToken(payload)
				.GetAwaiter().GetResult();
			
			return Ok(new {
				Vaild = vaildation.IsValid
			});
		}
	}
}
