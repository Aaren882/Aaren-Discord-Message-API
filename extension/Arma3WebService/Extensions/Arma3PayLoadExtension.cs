using System.Net.WebSockets;
using System.Text.Json;
using Arma3WebService.DBContext.Repositories;
using Arma3WebService.Entity;
using Arma3WebService.Managers;
using Components.Entity;

namespace Arma3WebService.Extensions;

public static class Arma3PayLoadExtension
{
	private static ILogger logger;

	public static async Task Invoke(
		this Arma3PayloadExtended action,
		WebsocketServer connection,
		IServiceProvider serviceProvider,
		IServerIdentityRepository identityRepository,
		IServerInfoTemplateRepository infoRepository
	)
	{
		/* await using (var dbContext = await dbContextFactory.CreateDbContextAsync())
		{
		} */

		logger.LogInformation("Invoking : {Type}", action.Type);
		await action.Run(serviceProvider, identityRepository, infoRepository);
		logger.LogInformation("Invoked : {Type}", action.Type);

		//- Send back message to the client
		var msg = new Arma3PayloadText($"Invoked \"{action.Type}\"");
		await connection.SendAsync(msg.ToJsonBytes(), WebSocketMessageType.Text, true);
	}

	public static void Options()
	{
		var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder.AddConsole());
		logger = loggerFactory.CreateLogger<Arma3PayloadExtended>();
	}

	public static string ToJsonString(this Arma3Payload payload)
	{
		return JsonSerializer.Serialize(
			payload,
			Arma3PayloadJsonSerializerContext.Default.Arma3Payload
		);
	}
	public static byte[] ToJsonBytes(this Arma3Payload payload)
	{
		return JsonSerializer.SerializeToUtf8Bytes(
			payload,
			Arma3PayloadJsonSerializerContext.Default.Arma3Payload
		);
	}
}
