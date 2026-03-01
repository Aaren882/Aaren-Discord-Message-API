using System.Text.Json.Serialization;
using Arma3WebService;

namespace DiscordMessageAPI.WebAPI
{
	[JsonSourceGenerationOptions(WriteIndented = true)] // Optional: Add desired options
	[JsonSerializable(typeof(Arma3Payload))]
	[JsonSerializable(typeof(List<Arma3Payload>))] // Add all root types used
	internal sealed partial class Arma3Payload_JsonSerializerContext : JsonSerializerContext
	{
	}
}
