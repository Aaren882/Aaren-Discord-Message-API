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
		var ActionType = payload.ActionType;
		ReceivedAction action = (ActionType) switch
		{
			1 => ReceiveRptLineAction,
			2 => RptFileAction,
			_ => throw new IndexOutOfRangeException($"Received unknown ActionType: {ActionType}")
		};

		return action(connection, payload);
	}
	private async Task ReceiveRptLineAction(WebsocketServer connection, Arma3PayloadServiceRequest request)
	{
		if (request.Payload is Arma3PayloadBinary binaryPayload)
		{
			var payloadId = binaryPayload.GetIdentifier(connection.websocketContext.GetIdentity());

			if (!DiscordBotAdminSubmitHelper.SubmittedModalSockets
					.TryGetValue(request.RequestGuildId, out var modalSocket))
			{
				throw new Exception($"No submitted print log modal socket found\n RequestGuildId : {request.RequestGuildId}.");
			}

			//- Wait for binary complete
			var (_, WrittenContent) = await binaryStreamManager.AddBinaryAsync(payloadId, binaryPayload, new MemoryStream());
			var (metaData, writeStream, _) = WrittenContent;

			try
			{
				using (WrittenContent)
				{
					var content = "```ts\n";
					using (StreamReader sr = new(writeStream))
					{
						while (!sr.EndOfStream)
						{
							content += (await sr.ReadLineAsync())?.Trim(' ', '\r', '\n');
							content += "\n";
						}
					}
					content += "```";

					if (content.Length >= 2000)
						throw new OverflowException($"\"{nameof(ReceiveRptLineAction)}\" Content length exceeds 2000 characters.");

					await modalSocket.RespondAsync(text: content, ephemeral: true);
				}

				logger.LogInformation("{Function} - \"{FileName}\" uploaded.", nameof(ReceiveRptLineAction), metaData.FileName);
				DiscordBotAdminSubmitHelper.SubmittedModalSockets.Remove(request.RequestGuildId, out _);
			}
			catch (TimeoutException)
			{
				logger.LogWarning("{Function} - SubmitModal Request timeout !!", nameof(ReceiveRptLineAction));
			}
			catch (OverflowException ex)
			{
				logger.LogWarning(ex.Message);
			}
		}
		else
		{
			throw new InvalidCastException("Invalid Respond payload format");
		}
	}

	private async Task RptFileAction(WebsocketServer connection, Arma3PayloadServiceRequest request)
	{
		if (request.Payload is Arma3PayloadBinary binaryPayload)
		{
			try
			{
				var payloadId = binaryPayload.GetIdentifier(connection.websocketContext.GetIdentity());

				if (!DiscordBotAdminSubmitHelper.SubmittedModalSockets
						.TryGetValue(request.RequestGuildId, out var modalSocket))
				{
					throw new Exception($"No submitted print log modal socket found\n RequestGuildId : {request.RequestGuildId}.");
				}

				//- Wait for binary complete
				var (_, WrittenContent) = await binaryStreamManager.AddBinaryAsync(payloadId, binaryPayload, new MemoryStream());
				var (metaData, writeStream, _) = WrittenContent;

				using (WrittenContent)
				{
					await modalSocket.RespondWithFileAsync(
						fileStream: writeStream,
						fileName: binaryPayload.FileName,
						ephemeral: true
					);
				}
				logger.LogInformation("{Function} - RPT File \"{FileName}\" Uploaded.", nameof(RptFileAction), metaData.FileName);
				DiscordBotAdminSubmitHelper.SubmittedModalSockets.Remove(request.RequestGuildId, out _);
			}
			catch (TimeoutException)
			{
				logger.LogWarning("{Function} - SubmitModal Request timeout !!", nameof(ReceiveRptLineAction));
			}
		}
		else
		{
			throw new InvalidCastException("Invalid Respond payload format");
		}
	}
}
