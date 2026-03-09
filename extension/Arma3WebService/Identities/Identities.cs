using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Arma3WebService
{
	public enum Role
	{
		Admin = 1,
		GameServer = 2,
	}

	public static class IdentityRoles
	{
		public static readonly Guid AdminGuid = new("84fe05e0-82b8-41f1-97be-65801e1230e4");
		public static readonly Guid GameServerGuid = new("ba3efd70-3bdc-4643-960e-63660e19fa6d");

		public const string Admin = "Admin";
		public const string GameServer = "Game-Server";
	}

	public abstract class IdentityBase
	{
		public string? AuthToken { get; set; }
	}
	public class IdentityRolesReturnPayload: IdentityBase
	{
		public required string RoleName { get; set; }
	}
	public class IdentityRolesPayload: IdentityBase
	{
		public required string Name { get; set; }
		public Role Role { get; set; } // Audiance
		public int? ExpireMinute { get; set; }
	}
	

	[JsonSourceGenerationOptions(WriteIndented = true)] // Optional: Add desired options
	[JsonSerializable(typeof(IdentityRolesPayload))]
	[JsonSerializable(typeof(List<IdentityRolesPayload>))] // Add all root types used
	internal sealed partial class IdentityRolesPayload_JsonSerializerContext : JsonSerializerContext
	{
	}
}
