using System.Text.Json.Serialization;

namespace Arma3WebService
{
	public class Arma3Payload
	{
		public string? Log { get; set; }
		public DateTime Timestamp { get; set; }
	}

	public class ServiceReturnPayload
	{
		public DateTime Date { get { return DateTime.Now; } }
	}

	[JsonSourceGenerationOptions(WriteIndented = true)] // Optional: Add desired options
	[JsonSerializable(typeof(Arma3Payload))]
	[JsonSerializable(typeof(List<Arma3Payload>))] // Add all root types used
	internal sealed partial class Arma3Payload_JsonSerializerContext : JsonSerializerContext
	{
	}
}
