using System.Text.Json;
using Arma3WebService.DBContext;
using Arma3WebService.Entity;
using Arma3WebService.Entity.DiscordBotAction;
using Arma3WebService.Managers;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Arma3WebService.Models;

public enum DiscordBotChannel
{
	Monitor,
	AdminConsole
}

public interface IDiscordBotService
{
	public ulong GetPresetMessageChannelId(DiscordBotChannel channelType);
	public Task<IMessageChannel> GetMessageChannelAsync(ulong channelID);
	public DiscordSocketClient GetClient();
	public Task<byte[]> SendLocalFile(string text);
	public Task<IUserMessage> ModifyMessageAsync(ulong messageID, DiscordMessageDto message);
	public Task<IUserMessage> SendMessageAsync(ulong channelId, DiscordMessageDto message);
}

public sealed class DiscordBotService(
	ILogger<DiscordBotService> logger,
	IServiceProvider serviceProvider,
	RemoteStateManager remoteStateManager
) : BackgroundService, IDiscordBotService
{
	private static readonly DiscordSocketClient Client = new();
	private readonly ulong _monitorChannel = ulong.Parse(Environment.GetEnvironmentVariable("MonitorChannel")!);

	public DiscordSocketClient GetClient() => Client;
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await Client.LoginAsync(
			TokenType.Bot, 
			Environment.GetEnvironmentVariable("BotToken")
		);
		await Client.StartAsync();
		
		var adminConsoleService = serviceProvider.GetRequiredService<AdminConsoleManager>();
		await adminConsoleService.CreateAdminConsole();
		
		Client.Log += Log;
		Client.ModalSubmitted += async (socketModal) =>
		{
			try
			{
				string json;
				if (socketModal.Message.Id == adminConsoleService.AdminMessageId)
					json = await adminConsoleService.GetActionJson(AdminConsoleManager.ActionType.Modal);
				else 
					json = await File.ReadAllTextAsync("testBotModal.json", stoppingToken);
				
				var deserialize = JsonSerializer.Deserialize(
					json,
					DiscordBotActionJsonSerializerContext.Default.DiscordBotModalInteraction
				);
				await deserialize!.Execute(socketModal);
			}
			catch (Exception e)
			{
				logger.LogError("ModalSubmitted : {Error}", e.Message);
				await socketModal.RespondAsync(text: $"Exception : {e.Message}", ephemeral: true);
			}
		};
		Client.ButtonExecuted += async (component) =>
		{
			try
			{
				string json;
				if (component.Message.Id == adminConsoleService.AdminMessageId)
					json = await adminConsoleService.GetActionJson(AdminConsoleManager.ActionType.Button);
				else
				{
					var currentTemplate = await remoteStateManager.GetServerInfoTemplateAsync(component.Message.Id);
					if (currentTemplate.messageActionPath is null) throw new NullReferenceException("\"ActionTemplate\" for this message is not exist.");
					json = await File.ReadAllTextAsync(currentTemplate.messageActionPath, stoppingToken);
				}
				
				var deserialize = JsonSerializer.Deserialize(
					json,
					DiscordBotActionJsonSerializerContext.Default.DiscordBotInteraction
				);
				await deserialize!.Execute(component);
			}
			catch (Exception e)
			{
				logger.LogError("ButtonExecuted : {Error}", e.Message);
				await component.RespondAsync(text: $"Exception : {e.Message}", ephemeral: true);
			}
		};
		Client.SelectMenuExecuted += async (component) =>
		{
			try
			{
				string json;
				if (component.Message.Id == adminConsoleService.AdminMessageId)
					json = await adminConsoleService.GetActionJson(AdminConsoleManager.ActionType.SelectMenu);
				else 
				{
					var currentTemplate = await remoteStateManager.GetServerInfoTemplateAsync(component.Message.Id);
					if (currentTemplate.messageActionPath is null) throw new NullReferenceException("\"ActionTemplate\" for this message is not exist.");
					json = await File.ReadAllTextAsync(currentTemplate.messageActionPath, stoppingToken);
				}
				
				var deserialize = JsonSerializer.Deserialize(
					json,
					DiscordBotActionJsonSerializerContext.Default.DiscordBotInteraction
				);
				await deserialize!.Execute(component);
			}
			catch (Exception e)
			{
				logger.LogError("SelectMenuExecuted : {Error}", e.Message);
				await component.RespondAsync(text: $"Exception : {e.Message}", ephemeral: true);
			}
		};

		var webSocketService = serviceProvider.GetRequiredService<IWebSocketService>();
		webSocketService.OnDisconnected += async (entity, connection) =>
		{
			try
			{
				var profileName = entity.GetIdentity();
				var currentTemplate = await remoteStateManager.GetServerInfoTemplateAsync(profileName);

				var json = await File.ReadAllTextAsync(currentTemplate.messageOfflinePath, stoppingToken);
				var deserialize = JsonSerializer.Deserialize(
					json,
					MsgPayload_JsonContext.Default.DiscordMessageDto
				);
				await ModifyMessageAsync(currentTemplate.messageId, deserialize!);
			}
			catch(Exception e)
			{
				logger.LogError("GameSession Disconnected : {Error}", e.Message);
			}
		};
	}
	
	public async Task<IUserMessage> ModifyMessageAsync(ulong messageID, DiscordMessageDto message)
	{
		var channel = await GetMessageChannelAsync(_monitorChannel);

		var modifyResult = await channel!.ModifyMessageAsync(messageID, msg =>
		{
			msg.Content = message.Content;
			msg.Embeds = message.ConvertEmbeds();
			msg.Components = message.ConvertComponents();
			msg.Flags = message.Flags;
		});
		
		return modifyResult;
	}

	public async Task<IMessageChannel> GetMessageChannelAsync(ulong channelID)
	{
		var channel = await Client.GetChannelAsync(channelID) as IMessageChannel;
		return channel ?? throw new NullReferenceException($"Channel {channelID} not found");
	}
	public ulong GetPresetMessageChannelId(DiscordBotChannel channelType)
	{
		var channelId = channelType switch
		{
			DiscordBotChannel.Monitor => _monitorChannel,
			// DiscordBotChannel.AdminConsole => AdminChannel,
			_ => throw new ArgumentOutOfRangeException(nameof(channelType), channelType, null)
		};
		return channelId;
	}
	
	public async Task<IUserMessage> SendMessageAsync(ulong channelId, DiscordMessageDto message)
	{
		var channel = await GetMessageChannelAsync(channelId);

		var component = message.ConvertComponents();
		var embeds = message.ConvertEmbeds();
		
		var sentMessage = (message) switch
		{
			{ Attachments: not null } => channel
				.SendFilesAsync(
					message.Attachments,
					text: message.Content,
					isTTS: message.Tts ?? false,
					embeds: embeds,
					components: component,
					flags: message.Flags
				),
			{ File: not null } => channel
				.SendFileAsync(
					filePath: message.File,
					text: message.Content,
					isTTS: message.Tts ?? false,
					embeds: embeds,
					components: component,
					flags: message.Flags
				),
			_ => channel
				.SendMessageAsync(
					text: message.Content,
					isTTS: message.Tts ?? false,
					embeds: embeds,
					components: component,
					flags: message.Flags
				)
		};

		return await sentMessage;
	}

	

	public Task<byte[]> SendLocalFile(string filename)
		=> File.ReadAllBytesAsync(Path.GetFullPath(filename));

	
	private Task Log(LogMessage msg)
	{
		var template = $"[{msg.Source}] {msg.Message}";

		// Use the appropriate ILogger method based on Discord's LogSeverity
		switch (msg.Severity)
		{
			case LogSeverity.Critical:
				logger.LogCritical(msg.Exception, template);
				break;
			case LogSeverity.Error:
				logger.LogError(msg.Exception, template);
				break;
			case LogSeverity.Warning:
				logger.LogWarning(template);
				break;
			case LogSeverity.Info:
				logger.LogInformation(template);
				break;
			case LogSeverity.Verbose:
				logger.LogInformation(template);
				break;
			case LogSeverity.Debug:
				logger.LogDebug(template);
				break;
		}
		return Task.CompletedTask;
	}
}
