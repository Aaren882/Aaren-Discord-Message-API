using Components.Entity;

namespace Arma3WebService.Handler;

public sealed class DiscordBotRequestHandler(
	ILogger<DiscordBotRequestHandler> logger
)
{
	private delegate Task ReceivedAction(IConnection connection, Arma3PayloadServiceRequest payload);
	public async Task OnReceived(IConnection connection, Arma3PayloadServiceRequest payload)
	{
		ReceivedAction action = (payload) switch
		{
			{ ActionType : 1 } => ReceiveRptLineAction,
			{ ActionType : 2 } => BinaryAction
		};

		await action(connection, payload);
	}
	private async Task ReceiveRptLineAction(IConnection connection, Arma3PayloadServiceRequest payload)
	{
		var content = "```ts\n";
		var readEnumerable = connection.ReceiveAndReadBinary().Reverse();
		await foreach (var line in readEnumerable)
		{
			content += line;
		}
		content += "```";
		logger.LogInformation("Receiving for Rpt Line '{TotalLength}'", content.Length);
		
		if (!DiscordBotAdminSubmitHelper.
			    SubmittedPrintLogModalSockets.
			    TryRemove(payload.RequestGuildId,
				    out var modalSocket)
		   ) throw new Exception($"No submitted print log modal socket found\n RequestGuildId : {payload.RequestGuildId}.");
			
		await modalSocket.RespondAsync(text: content, ephemeral: true);
	}
	
	private async Task BinaryAction(IConnection connection, Arma3PayloadServiceRequest payload)
	{
		logger.LogInformation("Receiving metaData for binary file '{Arma3PayloadRpt}'", payload);
		
		if (payload.Payload is Arma3PayloadBinary binaryPayload)
		{
			await using Stream memoryStream = new MemoryStream(); 
			await connection.ReceiveBinary(memoryStream);
			
			if (!DiscordBotAdminSubmitHelper.
				    SubmittedExportLogModalSockets.
				    TryRemove(payload.RequestGuildId,
					    out var modalSocket)
			   ) throw new Exception($"No submitted print log modal socket found\n RequestGuildId : {payload.RequestGuildId}.");

			await modalSocket.RespondWithFileAsync(
				fileStream: memoryStream,
				fileName: binaryPayload.FileName,
				ephemeral: true
			);
		}
		else
		{
			throw new InvalidCastException("Invalid Respond payload format");
		}
	}
}
