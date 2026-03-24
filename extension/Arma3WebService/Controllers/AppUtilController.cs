using Arma3WebService.Identities;
using Components.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Arma3WebService.Controllers
{
	[Authorize(AuthenticationSchemes = "BasicAuth")]
	[EnableCors("InternalCommunication")]
	[Route("api/token")]
	[ApiController]
	public class AppUtilController(ILogger<AppUtilController> logger, JwtHelpers jwtHelpers) : ControllerBase
	{
		private readonly ILogger _logger = logger;

		[HttpPost]
		public IActionResult GenToken(IdentityRolesPayload payload)
		{
			if (payload == null)
			{
				return BadRequest();
			}

			return Ok(jwtHelpers.GenerateToken(payload));
		}

		[HttpGet]
		public async Task<IActionResult> ValidateToken(IdentityRolesPayload payload)
		{
			var vaildation = await jwtHelpers.VaildateToken(payload);
			
			return Ok(new {
				Vaild = vaildation.IsValid
			});
		}
	}
}
