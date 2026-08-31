using System.Text.Json;
using Component.DiscordEntity;
using Microsoft.EntityFrameworkCore;

namespace Arma3WebService.DBContext.Schema;

[PrimaryKey(nameof(messageId))]
public class ServerInfoTemplate
{
	private string _messageTemplatePath = ".profile/MessageTemplate/default.json";

	public ulong messageId { get; set; }

	public string? messageTemplatePath
	{
		get => Path.GetFullPath(_messageTemplatePath);
		set => _messageTemplatePath = value ?? _messageTemplatePath;
	}

	private const string _messageOfflinePath = ".profile/MessageOfflineTemplate/default.json";
	private DiscordMessageDto _messageOffline =
		JsonSerializer.Deserialize(_messageOfflinePath, MsgPayload_JsonContext.Default.DiscordMessageDto) ??
		throw new NullReferenceException($"Default messageOfflinePath \"{nameof(_messageOfflinePath)}\" is not exist.");

	public DiscordMessageDto messageOffline
	{
		get => _messageOffline;
		set => _messageOffline = value;
	}
	public string? messageActionPath { get; set; }

	private DateTimeOffset _lastUpdate = DateTime.Now.ToUniversalTime();
	public DateTimeOffset lastUpdate
	{
		get => _lastUpdate;
		set => _lastUpdate = value.ToUniversalTime();
	}
}
