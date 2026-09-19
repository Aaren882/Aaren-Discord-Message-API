using Arma3WebService.DBContext.Repositories;
using Arma3WebService.Managers;
using Components.Entity;

namespace Arma3WebService.Broker;

public class UpdateDBActionBroker(
	BinaryStreamManager binaryStreamManager,
	ILogger<UpdateDBActionBroker> Logger,
	IServiceScopeFactory scopeFactory
)
{
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
	private async Task UpdateAndSaveProfile(WebsocketServer connection, UpdateAndSaveProfile ActionPayload)
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

			var contentsAsyncEnumerable = metaDataList
				.Select((binaryPayload, i) =>
				{
					var payloadId = binaryPayload.GetIdentifier(profileName);
					var (FileName, _, _, _) = binaryPayload;

					return binaryStreamManager.AddBinaryAsync(
						payloadId,
						binaryPayload,
						new FileStream(
							nativeFileDirectories[i],
							FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite
						),
						TimeSpan.FromMinutes(3)
					);
				});

			Logger.LogInformation("Waiting for Profile's binary content to be written.");
			await foreach (var item in Task.WhenEach(contentsAsyncEnumerable))
			{
				var (identifier, writtenContent) = await item;
				writtenContent.Dispose();
				Logger.LogInformation("BinaryAction finished DB Request for {profileName} : ID = {identifier}", profileName, identifier);
			}

			using var serviceScope = scopeFactory.CreateScope();
			var identityRepository = serviceScope.ServiceProvider.GetRequiredService<IServerIdentityRepository>();
			var identity = await identityRepository.GetByProfileNameAsync(profileName);

			ArgumentNullException.ThrowIfNull(identity);

			var infoRepository = serviceScope.ServiceProvider.GetRequiredService<IServerInfoTemplateRepository>();
			var infoTemplate = await infoRepository.GetByMessageIdAsync(identity.messageId);

			//- Create/Update Database value
			if (infoTemplate is null)
				await infoRepository.AddTemplateAsync(identity.messageId, newConfiguration);
			else
				await infoRepository.UpdateTemplateAsync(infoTemplate, newConfiguration);

			await infoRepository.DbContext.SaveChangesAsync();
		}
		catch (ArgumentNullException ex)
		{
			Logger.LogError(ex, "Argument null exception occurred during UpdateAndSaveProfile.");
		}
		catch (Exception)
		{
			throw;
		}
	}
}
