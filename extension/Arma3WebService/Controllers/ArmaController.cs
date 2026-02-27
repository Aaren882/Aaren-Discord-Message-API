using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Arma3WebService.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ArmaController : ControllerBase
	{
		private readonly ILogger<ArmaController> _logger;

		public ArmaController(ILogger<ArmaController> logger)
		{
			_logger = logger;
		}
	}
}
