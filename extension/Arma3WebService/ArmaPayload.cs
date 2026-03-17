using System.Text.Json;
using System.Text.Json.Serialization;

namespace Arma3WebService;

public enum Arma3PayLoadType
{
	Logging = 1,
	Rpt = 2, //- in game *.rpt logs
}
public record struct Arma3Payload
{
	public required Arma3PayLoadType MessageType { get; set; }
	public string? Message { get; set; }
	public DateTime Timestamp => DateTime.Now;
}
public record struct Arma3PayloadRPT
{
	public Arma3PayLoadType MessageType => Arma3PayLoadType.Rpt;
	public required string FileName { get; set; }
	// public byte[] FileBytes { get; set; }
	public long FileSize { get; set; }
	public int TotalChunks { get; set; }
	public DateTime Timestamp => DateTime.Now;
}
public record struct Arma3PayloadRPTChunks
{
	public required string FileName { get; set; }
	public byte[] FileBytes { get; set; }
	public int Index { get; set; }
	public DateTime Timestamp => DateTime.Now;
}

public record struct ServiceAuthenticationHeader
{
	public string Username { get; set; }
	public string Password { get; set; }
}

public record struct Arma3ServiceSecret
{
	public required string ServiceUri { get; set; }
	public required string WebSocketServiceUri { get; set; }
	public ServiceAuthenticationHeader Secret { get; set; }
}

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true)] // Optional: Add desired options
[JsonSerializable(typeof(Arma3Payload))]
[JsonSerializable(typeof(Arma3PayloadRPT))]
[JsonSerializable(typeof(Arma3PayloadRPTChunks))]
[JsonSerializable(typeof(List<Arma3Payload>))] // Add all root types used
[JsonSerializable(typeof(Arma3ServiceSecret))]
internal sealed partial class Arma3PayloadJsonSerializerContext : JsonSerializerContext;
