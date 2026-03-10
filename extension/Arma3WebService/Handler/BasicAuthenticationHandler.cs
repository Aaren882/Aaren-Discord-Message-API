using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Arma3WebService.Identities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Arma3WebService.Handler
{
	public class BasicAuthenticationHandler: AuthenticationHandler<AuthenticationSchemeOptions>
	{
		private readonly string _username = Environment.GetEnvironmentVariable("tokenManagerName") ?? "admin";
		private readonly string _HashedKey;
		private readonly ILogger _logger;

		public BasicAuthenticationHandler(
			IOptionsMonitor<AuthenticationSchemeOptions> options,
			ILoggerFactory logger,
			UrlEncoder encoder
		) : base(options, logger, encoder)
		{
			string password = Environment.GetEnvironmentVariable("tokenManagerPassword") ?? "password";
			string usernamePassword = String.Join(':', [_username, password]);
			_HashedKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(usernamePassword));
			
			_logger = logger.CreateLogger("BasicAuthenticationHandler");
		}

		protected override Task<AuthenticateResult> HandleAuthenticateAsync()
		{
			var authorizationHeader = Request.Headers.Authorization.ToString();

			if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Basic "))
			{
				return Task.FromResult(
					AuthenticateResult.Fail("Missing Basic Auth header")
				);
			}
			
			var encodedUsernamePassword = authorizationHeader.Substring("Basic ".Length).Trim();

			if (_HashedKey == encodedUsernamePassword)
			{
				_logger.LogInformation("\"Arma Token Manager Request is Authenticated.\"");

				// Generate the JWT token upon successful validation
				var claims = new[] {
					new Claim(ClaimTypes.Name, _username),
					//#NOTE : In game server request
					new Claim(ClaimTypes.NameIdentifier, IdentityRoles.GameServerGuid.ToString())
				};

				var claimsIdentity = new ClaimsIdentity(claims, Scheme.Name);
				var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
				var authenticationTicket = new AuthenticationTicket(claimsPrincipal, Scheme.Name);

				return Task.FromResult(
					AuthenticateResult.Success(authenticationTicket)
				);
			}

			return Task.FromResult(
				AuthenticateResult.Fail("Invalid credentials")
			);
		}
	}
}
