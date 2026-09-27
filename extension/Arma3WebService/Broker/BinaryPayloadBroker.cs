using System.Threading.Channels;
using Arma3WebService.Managers;
using Components.Entity;
using static Arma3WebService.Managers.WebsocketServer;

namespace Arma3WebService.Broker;

public sealed class BinaryPayloadBroker(
	ILogger<BinaryPayloadBroker> Logger,
	BinaryStreamManager binaryStreamManager,
	Channel<BinaryPayload> _BinaryChannel
) : BackgroundService
{
	public ValueTask BinaryAction(WebsocketServer connection, Arma3PayloadBinary payload)
	{
		Logger.LogInformation("Receiving metaData for binary file '{Payload}'", payload);
		var (FileName, _, _, DirectoryPrefix) = payload;

		if (DirectoryPrefix != null && !Directory.Exists(payload.DirectoryPrefix))
			Directory.CreateDirectory(payload.DirectoryPrefix!);

		string? profileName = connection.websocketContext.GetIdentity();
		var payloadId = payload.GetIdentifier(profileName);
		FileStream fs = new(
			Path.Combine(DirectoryPrefix ?? ".temp", FileName),
			FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite
		);
		binaryStreamManager.TryAddBinaryValue(payloadId, payload, fs, async (WrittenContent) =>
		{
			var (_, writeStream, _) = WrittenContent;
			try
			{
			}
			catch (Exception ex)
			{
				Logger.LogWarning(ex, "[{profileName}] having trouble with \"{FileName}\".", profileName, FileName);
			}
		});

		return ValueTask.CompletedTask;
	}
	public ValueTask BinaryContentAction(WebsocketServer connection, Arma3PayloadBinaryContent payload)
	{
		try
		{
			return binaryStreamManager.PushBinaryContentAsync(payload);
		}
		catch (Exception e)
		{
			Logger.LogError(e, "\"{Action}\" threw an exception...", nameof(BinaryContentAction));
			throw;
		}
	}
	public bool TryEnqueueAction(WebsocketServer connection, Arma3Payload payload)
	{
		Logger.LogTrace("[Writer] Start writing Channel. Channel Hash: {Hash}", _BinaryChannel.GetHashCode());
		var success = _BinaryChannel.Writer.TryWrite(new(connection, payload));
		Logger.LogTrace("[Writer] TryWrite Result: {Success}。Item Counts: {Count}", success, _BinaryChannel.Reader.Count);
		return success;
	}
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		Logger.LogInformation("{Service} service started. HashCode : {HashCode}, Thread : {ThreadID}", nameof(BinaryPayloadBroker), _BinaryChannel.GetHashCode(), Environment.CurrentManagedThreadId);
		try
		{
			await foreach (var actionPayload in _BinaryChannel.Reader.ReadAllAsync(stoppingToken))
			{
				var (connection, payload) = actionPayload;
				try
				{
					var action = (payload) switch
					{
						Arma3PayloadBinary payloadBinary =>
							BinaryAction(connection, payloadBinary),
						Arma3PayloadBinaryContent payloadBinary =>
							BinaryContentAction(connection, payloadBinary),
						_ => throw new NotSupportedException($"Payload type {payload.GetType().Name} is not supported in BinaryPayloadBroker.")
					};
					await action;
				}
				catch (NotSupportedException ex)
				{
					Logger.LogWarning(ex, "Unhandled payload type in BinaryPayloadBroker.");
				}
			}
		}
		catch (OperationCanceledException)
		{
			Logger.LogInformation("{Service} shutdown gracefully.", nameof(BinaryPayloadBroker));
		}
		catch (Exception e)
		{
			Logger.LogError(e, "An unexpected error occurred in BinaryPayloadBroker.");
		}
	}
}
