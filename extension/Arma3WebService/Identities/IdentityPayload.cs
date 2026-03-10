using System.Text.Json.Serialization;

namespace Arma3WebService.Identities;

public enum Role
{
	Admin = 1,
	GameServer = 2,
}

public abstract class IdentityBase
{
	public string? AuthToken { get; set; }
}
public class IdentityRolesReturnPayload: IdentityBase
{
	public string RoleName { get; set; }
}
public class IdentityRolesPayload: IdentityBase
{
	public required string Name { get; set; }
	public Role Role { get; set; } // Audiance
	public int? ExpireMinute { get; set; }
}

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true)] // Optional: Add desired options
[JsonSerializable(typeof(IdentityRolesPayload))]
[JsonSerializable(typeof(IdentityRolesReturnPayload))]
internal sealed partial class IdentityRolesPayload_JsonSerializerContext : JsonSerializerContext
{
}

