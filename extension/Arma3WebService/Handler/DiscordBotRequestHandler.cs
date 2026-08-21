using System.Text;
using Arma3WebService.Managers;
using Components.Entity;

namespace Arma3WebService.Handler;

public sealed class DiscordBotRequestHandler(
	BinaryStreamManager binaryStreamManager,
	ILogger<DiscordBotRequestHandler> logger
)
{
	private delegate ValueTask ReceivedAction(WebsocketServer connection, Arma3PayloadServiceRequest payload);
	public ValueTask OnReceived(WebsocketServer connection, Arma3PayloadServiceRequest payload)
	{
		ReceivedAction action = (payload) switch
		{
			{ ActionType: 1 } => ReceiveRptLineAction,
			{ ActionType: 2 } => BinaryAction
		};

		return action(connection, payload);
	}
	private async ValueTask ReceiveRptLineAction(WebsocketServer connection, Arma3PayloadServiceRequest request)
	{
		if (request.Payload is Arma3PayloadBinary binaryPayload)
		{
			var payloadId = binaryPayload.GetIdentifier(connection.websocketContext.GetIdentity());

			if (!DiscordBotAdminSubmitHelper.SubmittedModalSockets
					.TryGetValue(request.RequestGuildId, out var modalSocket))
			{
				throw new Exception($"No submitted print log modal socket found\n RequestGuildId : {request.RequestGuildId}.");
			}

			var path = $".temp/{payloadId}.rpt";
			binaryStreamManager.TryAddBinaryValue(
				payloadId,
				binaryPayload,
				new MemoryStream(),
				async () =>
				{
					try
					{
						// await binaryStreamManager.WaitUntilBinaryStreamFinished(payloadId);
						binaryStreamManager.TryRemoveBinaryValue(payloadId, out var metaData, out var writeStream);
						logger.LogDebug("Successfully processed binary file \"{FileName}\" for payload \"{PayloadId}\"", metaData.FileName, payloadId);

						using StreamReader sr = new(writeStream, leaveOpen: true);
						var content = "```ts\n";
						while (!sr.EndOfStream)
						{
							content += (await sr.ReadLineAsync())?.Trim(' ', '\r', '\n');
							content += "\n";
						}
						content += "```";

						if (content.Length >= 2000)
							throw new OverflowException("Content length exceeds 2000 characters.");

						await modalSocket.RespondAsync(text: content, ephemeral: true);

						logger.LogInformation("Received Binary File \"{FileName}\"", metaData.FileName);
						DiscordBotAdminSubmitHelper.SubmittedModalSockets.Remove(request.RequestGuildId, out _);
						await writeStream.DisposeAsync();
					}
					catch (TimeoutException ex)
					{
						logger.LogDebug(ex, "Timeout occurred while processing the print log.");
					}
				}
			);
		}
		else
		{
			throw new InvalidCastException("Invalid Respond payload format");
		}
	}

	private async ValueTask BinaryAction(WebsocketServer connection, Arma3PayloadServiceRequest request)
	{
		logger.LogInformation("Receiving metaData for binary file '{request}'", request);

		if (request.Payload is Arma3PayloadBinary binaryPayload)
		{
			var payloadId = binaryPayload.GetIdentifier(connection.websocketContext.GetIdentity());

			if (!DiscordBotAdminSubmitHelper.SubmittedModalSockets
					.TryGetValue(request.RequestGuildId, out var modalSocket))
			{
				throw new Exception($"No submitted print log modal socket found\n RequestGuildId : {request.RequestGuildId}.");
			}
			binaryStreamManager.TryAddBinaryValue(
				payloadId,
				binaryPayload,
				new MemoryStream(),
				async () =>
				{
					// await binaryStreamManager.WaitUntilBinaryStreamFinished(payloadId);
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
			);
		}
		else
		{
			throw new InvalidCastException("Invalid Respond payload format");
		}
	}
}
