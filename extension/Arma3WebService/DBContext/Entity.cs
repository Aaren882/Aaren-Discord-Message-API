using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Arma3WebService.DBContext;

[PrimaryKey(nameof(profileName))]
public class ServerIdentity
{
	public string profileName { get; set; }
	public ulong messageId { get; set; }
	public long profileStateStamp { get; set; }
	public DateTime lastUpdate { get; set; } = DateTime.Now;
}
[PrimaryKey(nameof(messageId))]
public class ServerInfoTemplate
{
	public ulong messageId { get; set; }
	public string messageTemplatePath { get; set; }
	public string? messageActionPath { get; set; }
	public string messageOfflinePath { get; set; } = Path.GetFullPath(".profile/MessageOfflineTemplate/default.json");
	public DateTime lastUpdate { get; set; } = DateTime.Now;
	public DateTime fileCreateTime  { get; set; }
}

public enum InternalManagementType
{
	AdminConsole
}

[PrimaryKey(nameof(managementType))]
public class InternalManagement
{
	[Key]
	[Column(Order = 0)]
	public InternalManagementType managementType { get; set; }
	[Key]
	[Column(Order = 1)]
	public ulong messageId { get; set; }
	public string? description { get; set; }
}
