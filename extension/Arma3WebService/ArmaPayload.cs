using System.Text.Json.Serialization;

namespace Arma3WebService;

public class Arma3Payload
{
	public string? Log { get; set; }
	public DateTime Timestamp { get; set; }
}

public class ServiceReturnPayload
{
	public DateTime Date { get { return DateTime.Now; } }
}

public struct ServiceAuthenticationHeader
{
	public string Username { get; set; }
	public string Password { get; set; }
}

public class Arma3ServiceSecret
{
	public ServiceAuthenticationHeader Secret { get; set; }
	public required string ServiceUri { get; set; }
}

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true)] // Optional: Add desired options
[JsonSerializable(typeof(Arma3Payload))]
[JsonSerializable(typeof(List<Arma3Payload>))] // Add all root types used
[JsonSerializable(typeof(Arma3ServiceSecret))]
internal sealed partial class Arma3Payload_JsonSerializerContext : JsonSerializerContext;
