using System.Text;
using Components.Entity;
using ExtensionComponents;
using ExtensionComponents.Entity;
using ExtensionComponents.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceConnection;
using ServiceConnection.WebService;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace DiscordMessageAPI
{
	class Program
	{
		static async Task Main(string[] args)
		{
			var services = new ServiceCollection();
			services.AddSingleton<ServiceInteractions>();
			services.AddSingleton<ServiceRequestHandler>();
			services.AddSingleton<ILocalServices, LocalServices>();
			services.AddSingleton<EntryDelegatesBase, EntryDelegates>();
			services.AddSingleton<WebsocketClient>();

			services.AddLogging(builder =>
			{
				builder
					.AddConsole()
					// .UseDefaultFileLogger()
					.SetMinimumLevel(LogLevel.Trace);
			});

			await using var serviceProvider = services.BuildServiceProvider();

			serviceProvider.InitConfiguration(
				LoggerBase.Trace,
				LoggerBase.Log
			);

			var serviceInteractions = serviceProvider.GetRequiredService<ServiceInteractions>();

			await ServiceStartup.InitializeAsync(
				"FeatureTest", "default"
			);

			Arma3PayloadFlatJsonString payload = new(new Dictionary<string, string>
			{
				{ "{MISSION_NAME}", "Mission Name" }
			});
			var message = payload.ToJsonBytes();
			await serviceInteractions.SendWebSocketMessage(message);

			Console.ReadKey();
		}
	}
}
