using Arma3WebService.Entity;
using Arma3WebService.Models;
using Components.Entity;

namespace Arma3WebService.Handler;

public sealed class DiscordBotRequestHandler(
	ILogger<DiscordBotRequestHandler> logger,
	IDiscordBotService discordBotService
)
{
	public async Task ReceiveRptLineAction(IConnection connection, Arma3PayloadServiceRequest payload)
	{
		var content = "```ts\n";
		var readEnumerable = connection.ReceiveAndReadBinary();
		await foreach (var line in readEnumerable)
		{
			content += line;
		}
		content += "```";
		logger.LogInformation("Receiving for Rpt Line '{TotalLength}'", content.Length);
		
		var channelId = discordBotService.GetPresetMessageChannelId(DiscordBotChannel.AdminConsole);
		
		if (payload.RequestGuildId is null)
		{
			var message = new DiscordMessageDto { Content = content };
			await discordBotService.SendMessageAsync(channelId, message);
		}
		else
		{
			if (!DiscordBotAdminSubmitHelper.
				    SubmittedPrintLogModalSockets.
				    TryRemove(payload.RequestGuildId,
					    out var modalSocket)
			   ) throw new Exception($"No submitted print log modal socket found\n RequestGuildId : {payload.RequestGuildId}.");
			
			await modalSocket.RespondAsync(text: content, ephemeral: true);
		}
	}
	
	public async Task BinaryAction(IConnection connection, Arma3PayloadServiceRequest payload)
	{
		logger.LogInformation("Receiving metaData for binary file '{Arma3PayloadRpt}'", payload);
		
		if (payload.Payload is Arma3PayloadBinary binaryPayload)
		{
			if (!Directory.Exists(binaryPayload.DirectoryPrefix)) Directory.CreateDirectory(binaryPayload.DirectoryPrefix);

			var path = Path.GetFullPath(Path.Combine(binaryPayload.DirectoryPrefix, binaryPayload.FileName));
			await using (FileStream fileStream = new(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
			{
				await connection.ReceiveBinary(fileStream);
			};

			
			var channelId = discordBotService.GetPresetMessageChannelId(DiscordBotChannel.AdminConsole);
			
			if (payload.RequestGuildId is null)
			{
				var message = new DiscordMessageDto { FileName = binaryPayload.FileName, File = path };
				await discordBotService.SendMessageAsync(channelId, message);
			}
			else
			{
				if (!DiscordBotAdminSubmitHelper.
					    SubmittedExportLogModalSockets.
					    TryRemove(payload.RequestGuildId,
						    out var modalSocket)
				   ) throw new Exception($"No submitted print log modal socket found\n RequestGuildId : {payload.RequestGuildId}.");
			
				await modalSocket.RespondWithFileAsync(
					fileName: binaryPayload.FileName,
					filePath: path,
					ephemeral: true
				);
			}
		}
		else
		{
			throw new InvalidCastException("Invalid Respond payload format");
		}
	}
}
