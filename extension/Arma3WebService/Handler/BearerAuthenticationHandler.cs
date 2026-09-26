using System.Security.Claims;
using System.Text.Encodings.Web;
using Components.Entity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Arma3WebService.Handler
{
	public class BearerAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
	{
		private readonly string ApiKey;
		private readonly ILogger _logger;

		public BearerAuthenticationHandler(
			IOptionsMonitor<AuthenticationSchemeOptions> options,
			ILoggerFactory logger,
			UrlEncoder encoder,
			IConfiguration configuration
		) : base(options, logger, encoder)
		{
			ApiKey = Environment.GetEnvironmentVariable("APIKey") ?? configuration["APIKey"]
				?? throw new AuthenticationFailureException("API Key not configured");

			_logger = logger.CreateLogger("BasicAuthenticationHandler");
		}

		protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
		{
			var authorizationHeader = Request.Headers.Authorization.ToString();

			if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
			{
				return AuthenticateResult.Fail("Missing Bearer Auth header");
			}

			var requestKey = authorizationHeader.Substring("Bearer ".Length).Trim();

			//- Check Authentication
			if (requestKey != ApiKey)
				return AuthenticateResult.Fail("Invalid credentials");

			_logger.LogInformation("\"Arma Token Manager Request is Authenticated.\"");

			// Generate the JWT token upon successful validation
			var claims = new[] {
				//#NOTE : In game server request
				new Claim(ClaimTypes.NameIdentifier, IdentityRoles.GameServerGuid.ToString())
			};

			var claimsIdentity = new ClaimsIdentity(claims, Scheme.Name);
			var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
			var authenticationTicket = new AuthenticationTicket(claimsPrincipal, Scheme.Name);

			return AuthenticateResult.Success(authenticationTicket);
		}
	}
}
