using System.Text.Json.Serialization;

namespace Components.Entity;

public enum Role
{
	Admin,
	GameServer = 2,
}

public readonly record struct IdentityInfo(
	string AccessName,
	Role Role
);

public readonly record struct IdentityRolesReturnPayload(
	IdentityInfo Identity,
	string? RoleName,
	string? AuthToken,
	bool IsNewIdentity,
	bool IsDifferent
);

public readonly record struct IdentityRolesPayload(
	IdentityInfo Identity,
	int? ExpireMinute,
	string? AuthToken,
	ProfileConfigurationDateOffsets ProfileDateOffsets
);

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true)] // Optional: Add desired options
[JsonSerializable(typeof(IdentityRolesPayload))]
[JsonSerializable(typeof(IdentityRolesReturnPayload))]
public sealed partial class IdentityRolesPayloadJsonSerializerContext : JsonSerializerContext;
