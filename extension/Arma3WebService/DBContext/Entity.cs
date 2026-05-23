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
	public string? modListUrl { get; set; }
	public DateTime lastUpdate { get; set; } = DateTime.Now;
}
[PrimaryKey(nameof(messageId))]
public class ServerInfoTemplate
{
	private string _messageTemplatePath = ".profile/MessageTemplate/default.json";
	private string _messageOfflinePath = ".profile/MessageOfflineTemplate/default.json";

	public ulong messageId { get; set; }

	public string? messageTemplatePath {
		get => Path.GetFullPath(_messageTemplatePath);
		set => _messageTemplatePath = value ?? _messageTemplatePath;
	}
	public string? messageOfflinePath { 
		get => Path.GetFullPath(_messageOfflinePath);
		set => _messageOfflinePath = value ?? _messageOfflinePath;
	}
	public string? messageActionPath { get; set; }
	public DateTime lastUpdate { get; set; } = DateTime.Now;
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
