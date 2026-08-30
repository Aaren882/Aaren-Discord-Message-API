using Arma3WebService.DBContext.Schema;

namespace Arma3WebService.DBContext.Entity;

public record struct Arma3ClientProfileConfiguration
{
	private FileInfo? _messageTemplate;
	private FileInfo? _messageOfflineTemplate;
	private FileInfo? _messageActions;

	public string? MessageTemplate
	{
		readonly get => _messageTemplate?.FullName;
		set
		{
			if (value is not null)
			{
				_messageTemplate = new (
					Path.GetFullPath($".profile/MessageTemplate/{Path.GetFileName(value)}")
				);
			}
		}
	}

	public string? MessageOfflineTemplate
	{
		readonly get => _messageOfflineTemplate?.FullName;
		set
		{
			if (value is not null)
			{
				_messageOfflineTemplate = new (
					Path.GetFullPath($".profile/MessageOfflineTemplate/{Path.GetFileName(value)}")
				);
			}
		}
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
		return new()
		{
			messageId = messageId,
			messageTemplatePath = MessageTemplate,
			messageOfflinePath = MessageOfflineTemplate,
			messageActionPath = MessageActions,
		};
	}
}
