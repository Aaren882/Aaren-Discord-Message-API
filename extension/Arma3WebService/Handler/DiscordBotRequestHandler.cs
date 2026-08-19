using Arma3WebService.Managers;
using Components.Entity;

namespace Arma3WebService.Handler;

public sealed class DiscordBotRequestHandler(
	BinaryStreamManager binaryStreamManager,
	ILogger<DiscordBotRequestHandler> logger
)
{
	private delegate Task ReceivedAction(WebsocketServer connection, Arma3PayloadServiceRequest payload);
	public Task OnReceived(WebsocketServer connection, Arma3PayloadServiceRequest payload)
	{
		ReceivedAction action = (payload) switch
		{
			{ ActionType: 1 } => ReceiveRptLineAction,
			{ ActionType: 2 } => BinaryAction
		};

		return action(connection, payload);
	}
	private async Task ReceiveRptLineAction(WebsocketServer connection, Arma3PayloadServiceRequest payload)
	{
		/* var content = "```ts\n";
		var readEnumerable = connection.ReceiveAndReadBinary().Reverse();
		await foreach (var line in readEnumerable)
		{
			content += line;
		}
		content += "```";
		logger.LogInformation("Receiving for Rpt Line '{TotalLength}'", content.Length);

		if (!DiscordBotAdminSubmitHelper.SubmittedModalSockets.TryRemove(payload.RequestGuildId,
					out var modalSocket)
		   ) throw new Exception($"No submitted print log modal socket found\n RequestGuildId : {payload.RequestGuildId}.");

		await modalSocket.RespondAsync(text: content, ephemeral: true); */
	}

	private async Task BinaryAction(WebsocketServer connection, Arma3PayloadServiceRequest request)
	{
		logger.LogInformation("Receiving metaData for binary file '{request}'", request);

		if (request.Payload is Arma3PayloadBinary binaryPayload)
		{
			var payloadId = binaryPayload.GetIdentifier(connection.websocketContext.GetIdentity());
			binaryStreamManager.TryAddBinaryValue(payloadId, binaryPayload, new MemoryStream());

			await binaryStreamManager.WaitUntilBinaryStreamFinished(payloadId);

			if (!DiscordBotAdminSubmitHelper.SubmittedModalSockets
					.TryGetValue(request.RequestGuildId, out var modalSocket))
			{
				throw new Exception($"No submitted print log modal socket found\n RequestGuildId : {request.RequestGuildId}.");
			}

			binaryStreamManager.TryRemoveBinaryValue(payloadId, out var metaData, out var writeStream);
			logger.LogDebug("Successfully processed binary file \"{FileName}\" for payload \"{PayloadId}\"", metaData.FileName, payloadId);

			await modalSocket.RespondWithFileAsync(
				fileStream: writeStream,
				fileName: binaryPayload.FileName,
				ephemeral: true
			);

			logger.LogInformation("Received Binary File \"{FileName}\"", metaData.FileName);
			DiscordBotAdminSubmitHelper.SubmittedModalSockets.Remove(request.RequestGuildId, out _);
			await writeStream.DisposeAsync();
		}
		else
		{
			throw new InvalidCastException("Invalid Respond payload format");
		}
	}
}
