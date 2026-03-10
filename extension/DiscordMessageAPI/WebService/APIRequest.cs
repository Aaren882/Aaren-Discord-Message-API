using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using Arma3WebService;
namespace DiscordMessageAPI.WebService
{
	public class APIRequest
	{
		private static readonly HttpClientHandler Handler = new HttpClientHandler
		{
			SslProtocols = SslProtocols.Tls12 |
				SslProtocols.Tls13
		};
		private static readonly HttpClient Client = new(Handler);

		public static async Task SendRequest(string uri, string text)
		{
			var payload = new Arma3Payload { Log = text };

			var jsonPayload = JsonSerializer.Serialize(payload, Arma3Payload_JsonSerializerContext.Default.Arma3Payload);
			var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

			using HttpResponseMessage response = await PostRequest(uri, content);

			// Check if the request was successful
			if (response.IsSuccessStatusCode)
			{
				Console.WriteLine($"Product created with ID: {response.StatusCode}");
			}
		}
		public static async Task<HttpResponseMessage> PostRequest(
			string uri,
			StringContent content,
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
	}
}
