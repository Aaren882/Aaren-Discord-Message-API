using System.Text.Json.Serialization;
using Components.Entity;

namespace Arma3WebService.Entity;

public enum Arma3PayLoadTypeExtension
{
	DiscrodSend = 1,
	ServerInfo = 2,
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "ProcessType")]
[JsonDerivedType(typeof(DiscordJsonExtension), (int)Arma3PayLoadTypeExtension.DiscrodSend)]
[JsonDerivedType(typeof(ServerInfoExtension), (int)Arma3PayLoadTypeExtension.ServerInfo)]
public abstract record Arma3PayloadExtension
{
	public abstract Arma3PayLoadTypeExtension Type { get; }
	public static DateTime Timestamp => DateTime.Now;
}

public record DiscordJsonExtension
(
	DiscordMessageDto DiscordMessage
) : Arma3PayloadExtension
{
	[JsonIgnore]
	public override Arma3PayLoadTypeExtension Type => Arma3PayLoadTypeExtension.DiscrodSend;
};
public record ServerInfoExtension
(
	IEnumerable<string> Infos
) : Arma3PayloadExtension
{
	[JsonIgnore]
	public override Arma3PayLoadTypeExtension Type => Arma3PayLoadTypeExtension.ServerInfo;
};

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true, AllowOutOfOrderMetadataProperties = true)] // Optional: Add desired options
[
	JsonSerializable(typeof(Arma3PayloadExtension))
]
public sealed partial class Arma3PayloadExtensionJsonSerializerContext : JsonSerializerContext;
