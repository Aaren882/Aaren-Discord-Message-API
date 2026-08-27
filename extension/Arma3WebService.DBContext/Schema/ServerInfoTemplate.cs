using Microsoft.EntityFrameworkCore;

namespace Arma3WebService.DBContext.Schema;

[PrimaryKey(nameof(messageId))]
public class ServerInfoTemplate
{
	private string _messageTemplatePath = ".profile/MessageTemplate/default.json";
	private string _messageOfflinePath = ".profile/MessageOfflineTemplate/default.json";

	public ulong messageId { get; set; }

	public string? messageTemplatePath
	{
		get => Path.GetFullPath(_messageTemplatePath);
		set => _messageTemplatePath = value ?? _messageTemplatePath;
	}
	public string? messageOfflinePath
	{
		get => Path.GetFullPath(_messageOfflinePath);
		set => _messageOfflinePath = value ?? _messageOfflinePath;
	}
	public string? messageActionPath { get; set; }

	private DateTimeOffset _lastUpdate = DateTime.Now.ToUniversalTime();
	public DateTimeOffset lastUpdate
	{
		get => _lastUpdate;
		set => _lastUpdate = value.ToUniversalTime();
	}
}
