using System.Text.Json.Serialization;

namespace ServiceConnection.Entity;

public record struct CallContext(
	UInt64 steamId,
	string fileSource,
	string missionName,
	string serverName,
	Int16 remoteExecutedOwner
);

public delegate int ExtensionCallback(
	string name, 
	string function,
	string data
);

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true, AllowOutOfOrderMetadataProperties = true)] // Optional: Add desired options
[JsonSerializable(typeof(Dictionary<string, string>))]
public sealed partial class ExtensionSerializable : JsonSerializerContext;
