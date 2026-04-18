using Arma3WebService.Entity;
using Discord;
using Discord.WebSocket;
using ServiceConnection.Discord;

namespace Arma3WebService.Models
{
	public interface IDiscordBotService
	{
		public DiscordSocketClient GetClient();
		public Task<IUserMessage> PostBotOnline(string text);
		public Task<IUserMessage> ModifyMessageAsync(ulong messageID, DiscordMessageDto message);
		public Task<IUserMessage> SendMessageAsync(DiscordMessageDto message);
	}

	public sealed class DiscordBotService(ILogger<DiscordBotService> logger, IServiceProvider serviceProvider)
		: BackgroundService, IDiscordBotService
	{
		private static readonly DiscordSocketClient? _client = new();
		private static readonly string TestChannel = Environment.GetEnvironmentVariable("TestChannel")!;

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_client!.Log += Log;
			_client!.ButtonExecuted += MyButtonHandler;
			await _client!.LoginAsync(
				TokenType.Bot, 
				Environment.GetEnvironmentVariable("BotToken")
			);
			await _client.StartAsync();
		}

		public DiscordSocketClient GetClient()
		{
			return _client!;
		}
		
		public async Task<IUserMessage> ModifyMessageAsync(ulong messageID, DiscordMessageDto message)
		{
			var channel = await _client!
				.GetChannelAsync(Convert.ToUInt64(TestChannel)) as IMessageChannel;

			var modifyResult = await channel!.ModifyMessageAsync(messageID, msg =>
			{
				msg.Content = message.Content;
				msg.Embeds = ConvertEmbeds(message.Embeds)?.ToArray();
				msg.Components = new Optional<MessageComponent>();
			});
			
			return modifyResult;
		}
		public async Task<IUserMessage> SendMessageAsync(DiscordMessageDto message)
		{
			var channel = await _client!
				.GetChannelAsync(Convert.ToUInt64(TestChannel)) as IMessageChannel;

			MessageComponent? component = null;
			if (message.Components is not null)
			{
				component = new ComponentBuilderV2(ConvertComponents(message.Components))
					.Build();
			}

			var sentMessage = (message) switch
			{
				{ Attachments: not null } => channel!
					.SendFilesAsync(
						message.Attachments,
						text: message.Content,
						isTTS: message.Tts ?? false,
						embeds: ConvertEmbeds(message.Embeds)?.ToArray() ?? [],
						components: component
					),
				{ File: not null } => channel!
					.SendFileAsync(
						filePath: message.File,
						text: message.Content,
						isTTS: message.Tts ?? false,
						embeds: ConvertEmbeds(message.Embeds)?.ToArray() ?? [],
						components: component
					),
				_ => channel!
					.SendMessageAsync(
						text: message.Content,
						isTTS: message.Tts ?? false,
						embeds: ConvertEmbeds(message.Embeds)?.ToArray() ?? [],
						components: component
					)
			};

			return await sentMessage;
		}

		private IEnumerable<IMessageComponentBuilder> ConvertComponents(IReadOnlyCollection<DiscordDto.ComponentBase>? components)
		{
			return components?.Select(x => x.Convert()) ?? throw new InvalidOperationException();
		}

		public async Task<IUserMessage> PostBotOnline(string text)
		{
			var channel = await _client!
				.GetChannelAsync(Convert.ToUInt64(TestChannel)) as IMessageChannel;

			var buttton = ButtonBuilder
				.CreatePrimaryButton("I'm button", "custom-id-1");
			// var buttton = ButtonBuilder
			// 	.CreateLinkButton("i'm Sec", requestURL);

			var component = new ComponentBuilder()
				.WithButton(buttton);
				// .WithSelectMenu(menuBuilder);
				
			var message = await channel!.SendMessageAsync(text: text, components: component.Build());

			return message;
		}

		public async Task<byte[]> SendLocalFile(string filename)
		{
			// var channel = await _client!
			// 	.GetChannelAsync(Convert.ToUInt64(TestChannel)) as IMessageChannel;

			// var stream = File.OpenRead(Path.GetFullPath(filename));
			// await channel
			// 	.SendFileAsync(
			// 		stream: stream, 
			// 		filename: filename, 
			// 		"Here is the file!"
			// 	);
			
			var bytes = await File.ReadAllBytesAsync(Path.GetFullPath(filename));
			return bytes;
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

		private IEnumerable<Embed>? ConvertEmbeds(IEnumerable<EmbedData>? embeds)
		{
			return embeds?.Select(x => 
				new EmbedBuilder
				{
					Author = new EmbedAuthorBuilder
					{
						IconUrl	= x.author.icon_url,
						Name = x.author.name,
						Url = x.author.url
					},
					ThumbnailUrl = x.thumbnail.url,
					ImageUrl = x.image.url,
					Description = x.description,
					Fields = x.fields
						.Select(f => new EmbedFieldBuilder
						{
							IsInline = f.inline,
							Name = f.name,
							Value = f.value 
						})
						.ToList(),
					Footer = new EmbedFooterBuilder
					{
						IconUrl	= x.footer.icon_url,
						Text = x.footer.text
					}
				}.Build()
			);
		}
	}
}
