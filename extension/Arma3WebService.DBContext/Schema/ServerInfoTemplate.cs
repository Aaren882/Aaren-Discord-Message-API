using System.Text.Json;
using Component.DiscordEntity;
using Microsoft.EntityFrameworkCore;

namespace Arma3WebService.DBContext.Schema;

[PrimaryKey(nameof(messageId))]
public class ServerInfoTemplate
{
	public ulong messageId { get; set; }

	private const string _messageTemplatePath = ".profile/MessageTemplate/default.json";
	private string? _messageTemplate = null;
	public string messageTemplate
	{
		get => _messageTemplate ?? File.ReadAllText(_messageTemplatePath);
		set
		{
			if (value != null)
				_messageTemplate = value;
		}
	}

	private const string _messageOfflinePath = ".profile/MessageOfflineTemplate/default.json";
	private DiscordMessageDto _messageOffline =
		JsonSerializer.Deserialize(File.ReadAllText(_messageOfflinePath), MsgPayload_JsonContext.Default.DiscordMessageDto)
		?? throw new NullReferenceException($"Default messageOfflinePath \"{nameof(_messageOfflinePath)}\" is not exist.");

	public DiscordMessageDto messageOffline
	{
		get => _messageOffline;
		set
		{
			if (value != null)
				_messageOffline = value;
		}
	}
	public string? messageActionPath { get; set; }

	private DateTimeOffset _lastUpdate = DateTime.Now.ToUniversalTime();
	public DateTimeOffset lastUpdate
	{
		get => _lastUpdate;
		set => _lastUpdate = value.ToUniversalTime();
	}
}
