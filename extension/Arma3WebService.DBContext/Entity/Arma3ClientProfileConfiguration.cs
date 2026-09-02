using System.Text;
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
	public readonly List<FileInfo> GetTemplateFileList()
	{
		List<FileInfo> fileInfoList = [_messageTemplate, _messageOfflineTemplate];
		if (_messageActions != null) fileInfoList.Add(_messageActions);

		return fileInfoList;
	}


	public readonly ServerInfoTemplate CreateInfoTemplate(ulong messageId)
	{
		ServerInfoTemplate template = new()
		{
			messageId = messageId,
			messageActionPath = MessageActions,
		};
		if (File.Exists(MessageOfflineTemplate))
		{
			var deserializedMsg = JsonSerializer.Deserialize(
				ReadAllTextShared(MessageOfflineTemplate),
				MsgPayload_JsonContext.Default.DiscordMessageDto
			);
			template.messageOffline = deserializedMsg ?? throw new NullReferenceException("Invalid MessageOfflineTemplate = \"Null\".");
		}
		if (File.Exists(MessageTemplate))
		{
			template.messageTemplate = ReadAllTextShared(MessageTemplate);
		}

		return template;
	}

	private static string ReadAllTextShared(string path)
	{
		using FileStream fs = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		using StreamReader sr = new(fs, Encoding.UTF8);
		return sr.ReadToEnd();
	}
}
