using Arma3WebService.Entity;
using Microsoft.EntityFrameworkCore;

namespace Arma3WebService.DBContext;

[PrimaryKey(nameof(profileName))]
public class ServerIdentity
{
	public string profileName { get; set; }
	public ServerInfo? serverInfo { get; set; }
	public DateTime createTime { get; set; } = DateTime.Now;
}
[PrimaryKey(nameof(messageId))]
public class ServerInfo
{
	public string messageId { get; set; }
	public string filePath  { get; set; }
	public DateTime lastUpdate { get; set; } = DateTime.Now;
	public DateTime createTime  { get; set; }
}
