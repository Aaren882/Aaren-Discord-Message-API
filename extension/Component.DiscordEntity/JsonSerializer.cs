using System.Text.Json.Serialization;

namespace Component.DiscordEntity;

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true, AllowOutOfOrderMetadataProperties = true)] // Optional: Add desired options
[
	JsonSerializable(typeof(MsgPayload)),
	JsonSerializable(typeof(DiscordMessage)),
	JsonSerializable(typeof(EmbedData)),
	JsonSerializable(typeof(List<EmbedData>)),
	JsonSerializable(typeof(DiscordMessageDto))
]
public partial class MsgPayload_JsonContext : JsonSerializerContext;
