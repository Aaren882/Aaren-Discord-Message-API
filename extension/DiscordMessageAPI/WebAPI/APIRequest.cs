using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using Arma3WebService;
namespace DiscordMessageAPI.WebAPI
{
	public class APIRequest
	{
		private static readonly HttpClientHandler handler = new HttpClientHandler
		{
			SslProtocols = SslProtocols.Tls12 |
				SslProtocols.Tls13
		};
		private static readonly HttpClient client = new(handler);

		public static async Task SendRequest(string uri, string text)
		{
			//package.Add(new StringContent(inputKey), "payload_json");
			Arma3Payload payload = new Arma3Payload { Log = text };

			string jsonPayload = JsonSerializer.Serialize(payload, Arma3Payload_JsonSerializerContext.Default.Arma3Payload);
			var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

			using HttpResponseMessage response = await client.PostAsync(uri, content);

			// Check if the request was successful
			if (response.IsSuccessStatusCode)
			{
				Console.WriteLine($"Product created with ID: {response.StatusCode}");
			}
		}
	}
}
