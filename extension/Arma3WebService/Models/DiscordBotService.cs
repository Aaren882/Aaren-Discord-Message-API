using Discord;
using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc;

namespace Arma3WebService.Services
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
		private static readonly DiscordSocketClient? _client = new DiscordSocketClient();

		private readonly static string TestChannel = Environment.GetEnvironmentVariable("TestChannel")!;

		// Inject IServiceProvider to manually scope and resolve other services
		public DiscordBotService(ILogger<DiscordBotService> logger, IServiceProvider serviceProvider)
		{
			_logger = logger;
			_serviceProvider = serviceProvider;
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			using (var scope = _serviceProvider.CreateScope())
			{
				// Resolve the service containing the business logic
				var myBusinessLogicService = scope.ServiceProvider.GetRequiredService<DiscordBotService>();
				myBusinessLogicService.StartupBot().GetAwaiter().GetResult();
			}
			// Return Task.CompletedTask if the work is synchronous
			return Task.CompletedTask;
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
			await _client!.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("BotToken"));
			await _client.StartAsync();
		}

		public async Task<IUserMessage> PostBotOnline(string text)
		{
			var channel = await _client!
				.GetChannelAsync(Convert.ToUInt64(TestChannel)) as IMessageChannel;

			IUserMessage message = await channel!.SendMessageAsync(text: text);
			return message;
		}

		public Task Log(LogMessage msg)
		{
			string Template = $"[{msg.Source}] {msg.Message}";

			// Use the appropriate ILogger method based on Discord's LogSeverity
			switch (msg.Severity)
			{
				case LogSeverity.Critical:
					_logger.LogCritical(msg.Exception, Template);
					break;
				case LogSeverity.Error:
					_logger.LogError(msg.Exception, Template);
					break;
				case LogSeverity.Warning:
					_logger.LogWarning(Template);
					break;
				case LogSeverity.Info:
					_logger.LogInformation(Template);
					break;
				case LogSeverity.Verbose:
					_logger.LogInformation(Template);
					break;
				case LogSeverity.Debug:
					_logger.LogDebug(Template);
					break;
			}
			return Task.CompletedTask;
		}
	}
}
