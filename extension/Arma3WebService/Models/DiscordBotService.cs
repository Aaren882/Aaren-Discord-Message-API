using System.Text.Json;
using Arma3WebService.Entity;
using Arma3WebService.Entity.DiscordBotAction;
using Discord;
using Discord.WebSocket;
using ServiceConnection.Discord;

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

	public sealed class DiscordBotService(ILogger<DiscordBotService> logger)
		: BackgroundService, IDiscordBotService
	{
		private static readonly DiscordSocketClient Client = new();
		private static readonly ulong TestChannel = ulong.Parse(Environment.GetEnvironmentVariable("TestChannel")!);

		public DiscordSocketClient GetClient() => Client;
		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			Client.Log += Log;
			Client.ButtonExecuted += async (component) =>
			{
				try
				{
					var json = await File.ReadAllTextAsync("testBot.json", stoppingToken);
					var deserialize = JsonSerializer.Deserialize(
						json,
						DiscordBotActionJsonSerializerContext.Default.DiscordBotInteraction
					);
					await deserialize!.Execute(component);
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
					throw;
				}
			};
			
			await Client.LoginAsync(
				TokenType.Bot, 
				Environment.GetEnvironmentVariable("BotToken")
			);
			await Client.StartAsync();
		}
		private async Task MyButtonHandler(SocketMessageComponent component)
		{
			// Check for the custom ID defined in step 1
			var stream = File.OpenRead(Path.GetFullPath(".env"));
			
			switch (component.Data.CustomId)
			{
				case "custom-id-1":
					// Respond to the interaction
					await component.RespondWithFileAsync(
						stream,
						".env",
						text: $"{component.User.Mention} has clicked the button!",
						ephemeral: true
					);
					
					// filePath: Path.GetFullPath(".env"),
					// fileName: ".env",
					// fileStream: SendLocalFile(filename: ".env"),
					// text: $"{component.User.Mention} has clicked the button!",
					// ephemeral: true
					break;
				// You can add more cases for different button custom IDs
				case "another-id":
					// ... other logic ...
					break;
			}
		}
		
		public async Task<IUserMessage> ModifyMessageAsync(ulong messageID, DiscordMessageDto message)
		{
			var channel = await GetMessageChannelAsync(TestChannel);

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
			var channel = await GetMessageChannelAsync(TestChannel);

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

		public async Task<IUserMessage> PostBotOnline(string text)
		{
			var channel = await GetMessageChannelAsync(TestChannel);
			
			var buttton = ButtonBuilder
				.CreatePrimaryButton("I'm button", "custom-id-1");
			// var buttton = ButtonBuilder
			// 	.CreateLinkButton("i'm Sec", requestURL);

			var component = new ComponentBuilder()
				.WithButton(buttton);
				// .WithSelectMenu(menuBuilder);
				
			var message = await channel.SendMessageAsync(text: text, components: component.Build());

			return message;
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
