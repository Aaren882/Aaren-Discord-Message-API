using System.Text.Json;
using Arma3WebService.Entity;
using Arma3WebService.Entity.DiscordBotAction;
using Arma3WebService.Managers;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Arma3WebService.Models
{
	public interface IDiscordBotService
	{
		public Task<IMessageChannel> GetMessageChannelAsync(ulong channelID);
		public DiscordSocketClient GetClient();
		public Task<IUserMessage> PostBotOnline(string text);
		public Task<byte[]> SendLocalFile(string text);
		public Task<IUserMessage> ModifyMessageAsync(ulong messageID, DiscordMessageDto message);
		public Task<IUserMessage> SendMessageAsync(DiscordMessageDto message);
	}

	public sealed class DiscordBotService(
		ILogger<DiscordBotService> logger,
		IServiceProvider serviceProvider,
		RemoteStateManager remoteStateManager
	) : BackgroundService, IDiscordBotService
	{
		private static readonly DiscordSocketClient Client = new();
		public static readonly InteractionService DiscordInteractionService = new(Client);
		private static readonly ulong MonitorChannel = ulong.Parse(Environment.GetEnvironmentVariable("MonitorChannel")!);

		public DiscordSocketClient GetClient() => Client;
		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			Client.Log += Log;
			Client.ModalSubmitted += async (component) =>
			{
				try
				{
					var json = await File.ReadAllTextAsync("testBotModal.json", stoppingToken);
					var deserialize = JsonSerializer.Deserialize(
						json,
						DiscordBotActionJsonSerializerContext.Default.DiscordBotModalInteraction
					);
					await deserialize!.Execute(component);
				}
				catch (Exception e)
				{
					logger.LogError("ERROR ModalSubmitted : {Error}", e.Message);
				}
			};
			Client.ButtonExecuted += async (component) =>
			{
				try
				{
					var currentTemplate = await remoteStateManager.GetServerInfoTemplateAsync(component.Message.Id);
					if (currentTemplate.messageActionPath is null) return;
					
					var json = await File.ReadAllTextAsync(currentTemplate.messageActionPath, stoppingToken);
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
					var currentTemplate = await remoteStateManager.GetServerInfoTemplateAsync(component.Message.Id);
					if (currentTemplate.messageActionPath is null) return;
					
					var json = await File.ReadAllTextAsync(currentTemplate.messageActionPath, stoppingToken);
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
			webSocketService.OnDisconnected += async entity =>
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
			
			await Client.LoginAsync(
				TokenType.Bot, 
				Environment.GetEnvironmentVariable("BotToken")
			);
			await Client.StartAsync();
		}
		
		public async Task<IUserMessage> ModifyMessageAsync(ulong messageID, DiscordMessageDto message)
		{
			var channel = await GetMessageChannelAsync(MonitorChannel);

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
		
		public async Task<IUserMessage> SendMessageAsync(DiscordMessageDto message)
		{
			var channel = await GetMessageChannelAsync(MonitorChannel);

			var component = message.ConvertComponents();
			
			var sentMessage = (message) switch
			{
				{ Attachments: not null } => channel
					.SendFilesAsync(
						message.Attachments,
						text: message.Content,
						isTTS: message.Tts ?? false,
						embeds: message.ConvertEmbeds(),
						components: component,
						flags: message.Flags
					),
				{ File: not null } => channel
					.SendFileAsync(
						filePath: message.File,
						text: message.Content,
						isTTS: message.Tts ?? false,
						embeds: message.ConvertEmbeds(),
						components: component,
						flags: message.Flags
					),
				_ => channel
					.SendMessageAsync(
						text: message.Content,
						isTTS: message.Tts ?? false,
						embeds: message.ConvertEmbeds(),
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
}
