using Arma3WebService.Entity;
using Microsoft.EntityFrameworkCore;

namespace Arma3WebService.DBContext;

[PrimaryKey(nameof(name))]
public class ServerIdentity
{
	public string name { get; set; }
	public DateTime createTime { get; set; }
};
[PrimaryKey(nameof(name))]
public class ServerInfo
{
	public string name { get; set; }
	public UpdateServerInfoExtension infoExtension { get; set; }
};
