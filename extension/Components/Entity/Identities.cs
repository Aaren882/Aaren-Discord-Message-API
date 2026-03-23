namespace Components.Entity;

public static class IdentityRoles
{
	public static readonly Guid AdminGuid = new("84fe05e0-82b8-41f1-97be-65801e1230e4");
	public static readonly Guid GameServerGuid = new("ba3efd70-3bdc-4643-960e-63660e19fa6d");

	public const string Admin = "Admin";
	public const string GameServer = "Game-Server";
}
