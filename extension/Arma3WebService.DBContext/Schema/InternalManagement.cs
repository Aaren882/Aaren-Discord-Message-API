
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Arma3WebService.DBContext.Schema;

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
