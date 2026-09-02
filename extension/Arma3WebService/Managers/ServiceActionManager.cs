using System.Collections.Concurrent;
using System.Net.Mime;
using System.Net.WebSockets;
using System.Text.Json;
using Arma3WebService.Broker;
using Arma3WebService.DBContext.Repositories;
using Arma3WebService.Entity;
using Arma3WebService.Extensions;
using Arma3WebService.Handler;
using Arma3WebService.Models;
using Component.DiscordEntity;
using Components.Entity;
using Discord;
using Microsoft.Net.Http.Headers;

namespace Arma3WebService.Managers;

public sealed class ServiceActionManager(
	ILogger<ServiceActionManager> logger,
	IServiceProvider serviceProvider,
	IDiscordBotService discordBotService,
	DiscordBotRequestHandler requestHandler,
	BinaryStreamManager binaryStreamManager,
	BinaryPayloadBroker binaryPayloadBroker,
	IServerIdentityRepository identityRepository,
	IServerInfoTemplateRepository infoRepository
)
{
	public ValueTask CallBackAction(WebsocketServer connection, Arma3PayloadCallBack command)
	{
		return connection.SendAsync(command.ToJsonString(), WebSocketMessageType.Text, true);
	}
	public ValueTask TextAction(WebsocketServer connection, Arma3PayloadText payload)
	{
		return connection.SendAsync(payload.ToJsonString(), WebSocketMessageType.Text, true);
	}
	public ValueTask BinaryAction(WebsocketServer connection, Arma3PayloadBinary payload)
	{
		logger.LogInformation("Receiving metaData for binary file '{Payload}'", payload);
		var (FileName, _, _, _, DirectoryPrefix) = payload;

		if (DirectoryPrefix != null && !Directory.Exists(payload.DirectoryPrefix))
			Directory.CreateDirectory(payload.DirectoryPrefix!);

		string? profileName = connection.websocketContext.GetIdentity();
		var payloadId = payload.GetIdentifier(profileName);
		FileStream fs = new(
			Path.Combine(DirectoryPrefix ?? ".temp", FileName),
			FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite
		);
		binaryStreamManager.TryAddBinaryValue(payloadId, payload, fs, async () =>
		{
			try
			{
				await fs.DisposeAsync();
				binaryPayloadBroker.Publish(profileName + FileName); //- Invoke subscribed events
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "[{profileName}] having trouble with \"{FileName}\".", profileName, FileName);
			}
		});

		return ValueTask.CompletedTask;
	}
	public async ValueTask BinaryContentAction(WebsocketServer connection, Arma3PayloadBinaryContent payload)
	{
		try
		{
			await binaryStreamManager.PushBinaryContentAsync(payload);
		}
		catch (Exception e)
		{
			logger.LogError(e, "\"{Action}\" threw an exception...", nameof(BinaryContentAction));
			throw;
		}
	}

	public async ValueTask ServiceRequestAction(WebsocketServer connection, Arma3PayloadServiceRequest payload)
	{
		logger.LogInformation("Receiving RequestAction : '{RequestAction}'", payload);

		try
		{
			await requestHandler.OnReceived(connection, payload);
		}
		catch (Exception e)
		{
			logger.LogError(e, "\"{Action}\" threw an exception...", nameof(ServiceRequestAction));
			throw;
		}
	}

	public async ValueTask JsonStringAction(WebsocketServer connection, Arma3PayloadJson payload)
	{
		logger.LogDebug("Received message \"{PayloadJsonString}\"", payload.JsonString);

		try
		{
			var JsonStringAction = JsonSerializer.Deserialize(
				payload.JsonString,
				Arma3PayloadExtendedJsonSerializerContext.Default.Arma3PayloadExtended
			);

			ArgumentNullException.ThrowIfNull(JsonStringAction, nameof(JsonStringAction));
			await JsonStringAction.Invoke(connection, serviceProvider, identityRepository, infoRepository);
		}
		catch (Exception e)
		{
			logger.LogError(e, "JsonStringAction threw an exception...");
			throw;
		}
	}

	public async ValueTask FlatJsonStringAction(WebsocketServer connection, Arma3PayloadFlatJsonString payload)
	{
		var collection = payload.FlatJsonString;
		var identity = connection.websocketContext.GetIdentity();

		logger.LogDebug("\"{identity}\" received game info", identity);

		try
		{
			UpdateSSEGameInfo(identity, collection);
			await UpdateDiscordServerInfoMessageAsync(identity, collection);
		}
		catch (Exception e)
		{
			logger.LogError(e, "\"FlatJsonStringAction\" threw an exception...");
		}
	}

	private readonly List<string> ctxList = [];
	private readonly ConcurrentDictionary<string, Dictionary<string, string>?> _gameInfoSSEConcurrentDictionary = [];

	private void UpdateSSEGameInfo(string identity, Dictionary<string, string> collection)
	{
		if (ctxList.Count == 0) return;
		_gameInfoSSEConcurrentDictionary[identity] = collection;
	}
	private async ValueTask UpdateDiscordServerInfoMessageAsync(string sessionIdentity, Dictionary<string, string> logItem)
	{
		var serverIdentity = await identityRepository.GetByProfileNameAsync(sessionIdentity);

		//- If messageId not set  
		if (serverIdentity is null)
		{
			logger.LogError("\"{sessionIdentity}\" does not exist.", sessionIdentity);
			return;
		}
		if (serverIdentity.messageId is 0) return;

		var serverInfo = await infoRepository.GetByMessageIdAsync(serverIdentity.messageId);
		if (serverInfo is null) return;

		// var infoMessage = await File.ReadAllTextAsync(serverInfo.messageTemplatePath!);
		var infoMessage = serverInfo.messageTemplate;
		infoMessage = logItem.Aggregate(
			infoMessage,
			(current, item) => current.Replace(item.Key, item.Value)
		);

		var messageDto = JsonSerializer.Deserialize(
			infoMessage,
			MsgPayload_JsonContext.Default.DiscordMessageDto
		);

		//- Inject components
		var components = messageDto?.Components ?? [];
		if (serverIdentity.modListMessageId is not null)
		{
			var adminLoggingChannelId = discordBotService.GetPresetMessageChannelId(DiscordBotChannel.AdminLogging);
			var url = await discordBotService.GetPermanentUrlAsync(adminLoggingChannelId, (ulong)serverIdentity.modListMessageId);
			List<DiscordDto.ComponentBase> additionalComponents =
			[
				new DiscordDto.ButtonComponent(
					label: "MOD",
					emoji: new(0, "📦"),
					url: url,
					style: ButtonStyle.Link
				)
			];
			components.Add(new DiscordDto.ActionRowComponent(additionalComponents));
		}

		messageDto!.Components = components;
		await discordBotService.ModifyMessageAsync(serverIdentity.messageId, messageDto!);
	}
	public async ValueTask SSE_Logging(HttpContext ctx, string sessionIdentity)
	{
		var ctxID = ctx.TraceIdentifier;
		ctxList.Add(ctxID);

		ctx.Response.Headers.Append(HeaderNames.ContentType, MediaTypeNames.Text.EventStream);
		while (!ctx.RequestAborted.IsCancellationRequested)
		{
			await Task.Delay(1000);
			if (!_gameInfoSSEConcurrentDictionary.TryGetValue(sessionIdentity, out var logItem)) continue;
			if (logItem is null) continue;

			await JsonSerializer.SerializeAsync(ctx.Response.Body, logItem);
			await ctx.Response.WriteAsync("\n\n");
			await ctx.Response.Body.FlushAsync();
			_gameInfoSSEConcurrentDictionary[sessionIdentity] = null;
		}

		ctxList.Remove(ctxID);
	}
}
