using System.Text.Json.Serialization;

namespace Components.Entity;

public enum Role
{
	Admin = 1,
	GameServer = 2,
}

public record struct IdentityInfo
{
	public string AccessName { get; set; }
	public Role Role { get; set; } // Audiance
};

public record struct IdentityRolesReturnPayload
{
	public IdentityInfo Identity { get; set; }
	public string? RoleName { get; set; }
	public string? AuthToken { get; set; }
};

public record struct IdentityRolesPayload
{
	public IdentityInfo Identity { get; set; }
	public int? ExpireMinute { get; set; }
	public string? AuthToken { get; set; }
}
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true)] // Optional: Add desired options
[JsonSerializable(typeof(IdentityRolesPayload))]
[JsonSerializable(typeof(IdentityRolesReturnPayload))]
public sealed partial class IdentityRolesPayloadJsonSerializerContext : JsonSerializerContext;

