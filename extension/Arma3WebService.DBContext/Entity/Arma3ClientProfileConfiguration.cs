using System.Text;
using System.Text.Json;
using Arma3WebService.DBContext.Schema;
using Component.DiscordEntity;
using Components.Entity;

namespace Arma3WebService.DBContext.Entity;

public static class ClientProfileConfiguration
{
	public static ServerInfoTemplate CreateInfoTemplate(this Arma3ClientProfileConfiguration configuration, ulong messageId)
	{
		var (MessageTemplate, MessageOfflineTemplate, MessageActions) = configuration;
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
