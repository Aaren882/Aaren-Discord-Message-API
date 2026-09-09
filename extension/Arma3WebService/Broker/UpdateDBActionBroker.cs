using Arma3WebService.DBContext.Repositories;
using Arma3WebService.Managers;
using Components.Entity;

namespace Arma3WebService.Broker;

public class UpdateDBActionBroker(
	BinaryStreamManager binaryStreamManager,
	ILogger<UpdateDBActionBroker> Logger,
	IServerIdentityRepository identityRepository,
	IServerInfoTemplateRepository infoRepository
)
{
	public record RequestContent(WebsocketServer connection, Arma3PayloadUpdateDB PayloadUpdate);
	// private readonly Channel<RequestContent> _channel = Channel.CreateBounded<RequestContent>(100);
	public async Task AddAsync(WebsocketServer connection, Arma3PayloadUpdateDB PayloadUpdate)
	{
		var DBConfigAction = PayloadUpdate.DBConfigAction;
		var task = (DBConfigAction) switch
		{
			UpdateAndSaveProfile ActionPayload => UpdateAndSaveProfile(connection, ActionPayload),
			_ => throw new IndexOutOfRangeException(nameof(DBConfigAction))
		};
		await task;
	}
	/* protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		Logger.LogInformation("\"{Service}\" start Serving.", nameof(UpdateDBActionBroker));
		try
		{
			while (await _channel.Reader.WaitToReadAsync(stoppingToken))
			{
				while (_channel.Reader.TryRead(out var item))
				{
					var (connection, PayloadUpdate) = item;
					var DBConfigAction = PayloadUpdate.DBConfigAction;
					var task = (DBConfigAction) switch
					{
						UpdateAndSaveProfile ActionPayload => UpdateAndSaveProfile(connection, ActionPayload),
						_ => throw new IndexOutOfRangeException(nameof(DBConfigAction))
					};
					await task;
				}
			}
		}
		catch (OperationCanceledException) { }
		catch (Exception ex)
		{
			Logger.LogError(ex, "An unexpected error occurred while processing the DB update action.");
		}
		finally
		{
			Logger.LogCritical("DB update action processing terminated.");
		}
	} */
	private Task UpdateAndSaveProfile(WebsocketServer connection, UpdateAndSaveProfile ActionPayload)
	{
		try
		{
			var (metaDataList, configuration) = ActionPayload;
			var profileName = connection.websocketContext.GetIdentity();

			const string DirectoryPrefix = ".profile";

			if (!Directory.Exists(DirectoryPrefix))
				Directory.CreateDirectory(DirectoryPrefix);

			string[] propertyNames = [.. typeof(Arma3ClientProfileConfiguration).GetProperties().Select(x => x.Name)];
			List<string> nativeFileDirectories = [.. metaDataList.Select((metaData, i) => Path.Combine(DirectoryPrefix, propertyNames[i], metaData.FileName))];

			var newConfiguration = configuration with
			{
				MessageTemplate = nativeFileDirectories[0],
				MessageOfflineTemplate = nativeFileDirectories[1],
				MessageActions = nativeFileDirectories[2]
			};

			/* var contentsAsyncEnumerable = metaDataList
				.Select(binaryPayload =>
				{
					var payloadId = binaryPayload.GetIdentifier(profileName);
					var (FileName, _, _, _, _) = binaryPayload;

					const string DirectoryPrefix = ".profile";

					if (!Directory.Exists(DirectoryPrefix))
						Directory.CreateDirectory(DirectoryPrefix);

					return binaryStreamManager.AddBinaryAsync(
						payloadId,
						binaryPayload,
						new FileStream(
							Path.Combine(DirectoryPrefix, FileName),
							FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite
						)
					);
				}); */

			var Last = metaDataList.Last();
			foreach (var (binaryPayload, index) in metaDataList.Select((v, i) => (v, i)))
			{
				var payloadId = binaryPayload.GetIdentifier(profileName);
				var (FileName, _, _, _, _) = binaryPayload;

				binaryStreamManager.TryAddBinaryValue(
					payloadId,
					binaryPayload,
					new FileStream(
						nativeFileDirectories[index],
						FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite
					),
					async (writtenContent) =>
					{
						if (Last != binaryPayload) return;
						Logger.LogInformation("BinaryAction finished DB Request for {profileName} : ID = {payloadId}", profileName, payloadId);

						var identity = await identityRepository.GetByProfileNameAsync(profileName, tracked: false);
						ArgumentNullException.ThrowIfNull(identity);

						var infoTemplate = await infoRepository.GetByMessageIdAsync(identity.messageId);

						//- Create/Update Database value
						if (infoTemplate is null)
							await infoRepository.AddTemplateAsync(identity.messageId, newConfiguration);
						else
							await infoRepository.UpdateTemplateAsync(infoTemplate, newConfiguration);

						await infoRepository.DbContext.SaveChangesAsync();
					}
				);
			}
			return Task.CompletedTask;
		}
		catch (ArgumentNullException ex)
		{
			Logger.LogError(ex, "Argument null exception occurred during UpdateAndSaveProfile.");
			return Task.CompletedTask;
		}
		catch (Exception)
		{
			throw;
		}
	}
}
