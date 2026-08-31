using System.Text.Json;
using Arma3WebService.DBContext.Schema;
using Component.DiscordEntity;

namespace Arma3WebService.DBContext.Entity;

public record struct Arma3ClientProfileConfiguration()
{
	private FileInfo _messageTemplate = new(".profile/MessageTemplate/default.json");
	private FileInfo _messageOfflineTemplate = new(".profile/MessageOfflineTemplate/default.json");
	private FileInfo? _messageActions = null;

	public string MessageTemplate
	{
		readonly get => _messageTemplate.FullName;
		set =>
			_messageTemplate = new(
				Path.GetFullPath($".profile/MessageTemplate/{Path.GetFileName(value)}")
			);
	}

	public string MessageOfflineTemplate
	{
		readonly get => _messageOfflineTemplate.FullName;
		set => 
			_messageOfflineTemplate = new(
				Path.GetFullPath($".profile/MessageOfflineTemplate/{Path.GetFileName(value)}")
			);
	}

	public string? MessageActions
	{
		readonly get => _messageActions?.FullName;
		set => _messageActions = new FileInfo(
			Path.GetFullPath($".profile/MessageActions/{Path.GetFileName(value)}")
		);
	}

	public readonly ServerInfoTemplate CreateInfoTemplate(ulong messageId)
	{
		// ArgumentNullException.ThrowIfNull(MessageOfflineTemplate);
		return new()
		{
			messageId = messageId,
			messageTemplatePath = MessageTemplate,
			messageOffline = JsonSerializer.Deserialize(MessageOfflineTemplate, MsgPayload_JsonContext.Default.DiscordMessageDto)!,
			messageActionPath = MessageActions,
		};
	}
}
