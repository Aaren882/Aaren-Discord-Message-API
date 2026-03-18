using System.Text.Json;
using System.Text.Json.Serialization;

namespace Arma3WebService;

public enum Arma3PayLoadType
{
	Message = 1,
	Rpt = 2, //- in game *.rpt logs
}
public record struct Arma3Payload
{
	public required Arma3PayLoadType MessageType { get; set; }
	public DateTime Timestamp => DateTime.Now;
	public string? Message { get; set; }
	public Arma3PayloadRPT? Rpt { get; set; }
}

public record struct Arma3PayloadRPT(
	string FileName,
	long FileSize,
	DateTime CreatedTime,
	int TotalChunks
);

public record struct ServiceAuthenticationHeader
{
	public string Username { get; set; }
	public string Password { get; set; }
}

public record Arma3ServiceSecret(
	string ServiceUri,
	string WebSocketServiceUri,
	string RPT_Directory,
	ServiceAuthenticationHeader Secret
);

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true)] // Optional: Add desired options
[JsonSerializable(typeof(Arma3Payload))]
[JsonSerializable(typeof(Arma3PayloadRPT))]
[JsonSerializable(typeof(List<Arma3Payload>))] // Add all root types used
[JsonSerializable(typeof(Arma3ServiceSecret))]
internal sealed partial class Arma3PayloadJsonSerializerContext : JsonSerializerContext;
