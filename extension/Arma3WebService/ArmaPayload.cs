using System.Text.Json.Serialization;

namespace Arma3WebService;

public enum Arma3PayLoadType
{
	Logging = 1,
	PlayerConnectionChanged = 2, 
}
public class Arma3Payload
{
	public required Arma3PayLoadType MessageType { get; set; }
	public string? Message { get; set; }
	public static DateTime Timestamp => DateTime.Now;
}

public struct ServiceAuthenticationHeader
{
	public string Username { get; set; }
	public string Password { get; set; }
}

public class Arma3ServiceSecret
{
	public required string ServiceUri { get; set; }
	public required string WebSocketServiceUri { get; set; }
	public ServiceAuthenticationHeader Secret { get; set; }
}

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true)] // Optional: Add desired options
[JsonSerializable(typeof(Arma3Payload))]
[JsonSerializable(typeof(List<Arma3Payload>))] // Add all root types used
[JsonSerializable(typeof(Arma3ServiceSecret))]
internal sealed partial class Arma3Payload_JsonSerializerContext : JsonSerializerContext;
