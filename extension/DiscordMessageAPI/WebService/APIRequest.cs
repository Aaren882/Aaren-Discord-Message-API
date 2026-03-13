using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using Arma3WebService;
namespace DiscordMessageAPI.WebService
{
	public static class APIRequest
	{
		private static readonly HttpClientHandler Handler = new HttpClientHandler
		{
			SslProtocols = SslProtocols.Tls12 |
				SslProtocols.Tls13
		};
		private static readonly HttpClient Client = new(Handler);

		public static async Task<HttpResponseMessage> PostRequest(
			string uri,
			HttpContent content,
			AuthenticationHeaderValue? authHeader = null
		)
		{
			if (authHeader != null)
			{
				Client.DefaultRequestHeaders.Authorization = authHeader;
			}
			
			var response = await Client.PostAsync(uri, content);
			return response;
		}
		
		public static async Task<HttpResponseMessage> PatchRequest(
			string uri,
			HttpContent content,
			AuthenticationHeaderValue? authHeader = null
		)
		{
			if (authHeader != null)
			{
				Client.DefaultRequestHeaders.Authorization = authHeader;
			}
			
			var response = await Client.PatchAsync(uri, content);
			return response;
		}
	}
}
