using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Arma3WebService.Models
{
	public interface IDiscordBotService
	{
		public DiscordSocketClient GetClient();
		public Task<IUserMessage> PostBotOnline(string text);
	}

	public sealed class DiscordBotService : IHostedService, IDiscordBotService
	{
		private readonly ILogger<DiscordBotService> _logger;
		private readonly IServiceProvider _serviceProvider;
		private static readonly DiscordSocketClient? _client = new();

		private static readonly string TestChannel = Environment.GetEnvironmentVariable("TestChannel")!;

		// Inject IServiceProvider to manually scope and resolve other services
		public DiscordBotService(ILogger<DiscordBotService> logger, IServiceProvider serviceProvider)
		{
			_logger = logger;
			_serviceProvider = serviceProvider;
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			using var scope = _serviceProvider.CreateScope();
			
			// Resolve the service containing the business logic
			var myBusinessLogicService = scope
				.ServiceProvider
				.GetRequiredService<DiscordBotService>();
			
			await myBusinessLogicService.StartupBot();
		}
		public Task StopAsync(CancellationToken cancellationToken)
		{
			// Return Task.CompletedTask if the work is synchronous
			return Task.CompletedTask;
		}

		public DiscordSocketClient GetClient()
		{
			return _client!;
		}
		private async Task StartupBot()
		{
			_client!.Log += Log;
			_client!.ButtonExecuted += MyButtonHandler;
			await _client!.LoginAsync(
				TokenType.Bot, 
				Environment.GetEnvironmentVariable("BotToken")
			);
			await _client.StartAsync();
		}

		public async Task<IUserMessage> PostBotOnline(string text)
		{
			var channel = await _client!
				.GetChannelAsync(Convert.ToUInt64(TestChannel)) as IMessageChannel;
			
			var requestURL= $"{Environment.GetEnvironmentVariable("ASPNETCORE_URLS")}/DiscordBot/File/.env";


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
					_logger.LogCritical(msg.Exception, template);
					break;
				case LogSeverity.Error:
					_logger.LogError(msg.Exception, template);
					break;
				case LogSeverity.Warning:
					_logger.LogWarning(template);
					break;
				case LogSeverity.Info:
					_logger.LogInformation(template);
					break;
				case LogSeverity.Verbose:
					_logger.LogInformation(template);
					break;
				case LogSeverity.Debug:
					_logger.LogDebug(template);
					break;
			}
			return Task.CompletedTask;
		}
	}
}
