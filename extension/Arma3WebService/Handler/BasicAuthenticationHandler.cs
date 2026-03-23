using System.Security.Claims;
using System.Text.Encodings.Web;
using Components.Entity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Arma3WebService.Handler
{
	public class BasicAuthenticationHandler: AuthenticationHandler<AuthenticationSchemeOptions>
	{
		private readonly ServiceAuthenticationHeader _header;
		private readonly string _HashedKey;
		private readonly ILogger _logger;

		public BasicAuthenticationHandler(
			IOptionsMonitor<AuthenticationSchemeOptions> options,
			ILoggerFactory logger,
			UrlEncoder encoder
		) : base(options, logger, encoder)
		{
			var username = Environment.GetEnvironmentVariable("tokenManagerName") ?? "admin";
            var password = Environment.GetEnvironmentVariable("tokenManagerPassword") ?? "password";

            _header = new ServiceAuthenticationHeader(username, password);
			_HashedKey = _header.ToString();
			_logger = logger.CreateLogger("BasicAuthenticationHandler");
		}

		protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
		{
			var authorizationHeader = Request.Headers.Authorization.ToString();

			if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Basic "))
			{
				return AuthenticateResult.Fail("Missing Basic Auth header");
			}
			
			var encodedUsernamePassword = authorizationHeader.Substring("Basic ".Length).Trim();
			
			//- Check Authentication
			if (_HashedKey != encodedUsernamePassword)
				return AuthenticateResult.Fail("Invalid credentials");
			
			_logger.LogInformation("\"Arma Token Manager Request is Authenticated.\"");

			// Generate the JWT token upon successful validation
			var claims = new[] {
				new Claim(ClaimTypes.Name, _header.Username),
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
