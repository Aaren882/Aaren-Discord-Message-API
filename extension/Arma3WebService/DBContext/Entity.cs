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
	public string filePath  { get; set; }
	public DateTime lastUpdate { get; set; } = DateTime.Now;
	public DateTime fileCreateTime  { get; set; }
}
