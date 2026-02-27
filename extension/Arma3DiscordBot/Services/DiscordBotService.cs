using Discord;
using Discord.WebSocket;

namespace Arma3DiscordBot.Services
{
	public class DiscordBotService: IHostedService
	{
		private readonly ILogger<DiscordBotService> _logger;
		private readonly IServiceProvider _serviceProvider;

		static Dictionary<string, string> env = Program.env;
		static DiscordSocketClient? _client = Program.DiscordBotClient;

		// Inject IServiceProvider to manually scope and resolve other services
		public DiscordBotService(ILogger<DiscordBotService> logger, IServiceProvider serviceProvider)
		{
			_logger = logger;
			_serviceProvider = serviceProvider;

			_client.Log += Log;
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

		private async Task StartupBot()
		{
			await _client.LoginAsync(TokenType.Bot, env.GetValueOrDefault("BotToken", ""));
			await _client.StartAsync();
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
