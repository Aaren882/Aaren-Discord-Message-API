using Microsoft.EntityFrameworkCore;

namespace Arma3WebService.DBContext.Schema;

[PrimaryKey(nameof(profileName))]
public class ServerIdentity
{
	public required string profileName { get; set; }
	public ulong messageId { get; set; }
	public long profileStateStamp { get; set; }
	public ulong? modListMessageId { get; set; }

	private DateTimeOffset _lastUpdate = DateTime.Now.ToUniversalTime();
	public DateTimeOffset lastUpdate
	{
		get => _lastUpdate;
		set => _lastUpdate = value.ToUniversalTime();
	}
}
