using System.Dynamic;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using Discord;
using DiscordMessageAPI.ServiceConnection.WebService;
using featureTest;
using ServiceConnection;
using ServiceConnection.Discord;
using ServiceConnection.Tools;
using ServiceConnection.WebService;
using Components.Entity;
using Discord.Interactions;
using Discord.Interactions.Builders;
using ExtensionComponents;
using ExtensionComponents.Entity;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

			var serviceProvider = services.BuildServiceProvider();

			ServiceStartup.InitConfiguration(
				(a, b) => Console.WriteLine($"\"{a}\" : {b}"),
				(a, b) => Console.WriteLine($"\"{a?.Message}\" \n\n:- ADDITIONAL -: {b}"),
				serviceProvider
			);

			const string jsonProfile = "Discord_Message_API/profiles/default.json";
			var json = await File.ReadAllTextAsync(jsonProfile, Encoding.UTF8);
			
			var profile = JsonConvert.DeserializeObject(json) as JObject;
			var configuration = profile["Configuration"]
				.ToObject<Dictionary<string,string>>();
			
			var serviceInteractions = serviceProvider.GetRequiredService<ServiceInteractions>();

			var isDifferent = false;
			var returnMessageId = "";
			serviceInteractions.ServiceAccessResult += returnPayload =>
			{
				var returnPayloadStrings = returnPayload.AdditionalPayload!
					.Trim('[', ']')
					.Split(',');
				
				returnMessageId = returnPayloadStrings[1].Trim('"', '"');
				if (!bool.TryParse(returnPayloadStrings.Last(), out isDifferent))
					throw new Exception("\"returnPayloadStrings\" `isDifferent` Parsing failed.");
			};

			/*ServiceStartup.Callback = (name, function, data) =>
			{
				var rptEnum = ((int)Arma3PayLoadType.ServiceRequest).ToString();
				if (rptEnum != function) return -1;
				
				var deserialize = JsonSerializer.Deserialize(data, Arma3PayloadJsonSerializerContext.Default.Arma3PayloadServiceRequest);
				var type = deserialize?.ActionType;

				serviceInteractions.WebSocketTrafficWriter(serviceInteractions.SendWebSocketMessage(data));
				Task task = null;
				switch (type)
				{
					case 1: //- Send Rpt lines
						task = serviceInteractions.SendWebSocketRptLines(ServiceStartup.RptFileDirectory, 50);
						break;
					case 2: //- RequestRpt
						task = serviceInteractions.SendWebSocketBinary(ServiceStartup.RptFileDirectory, ".temp");
						break;
				}

				serviceInteractions.WebSocketTrafficWriter(task);
				Console.WriteLine(type);
				
				// var output = new TestOutputBuilder();
				// var argsAction = new TestArgsAction(output, [".temp", deserialize.RequestGuildId], "SendWebSocketRPT");
				// var argsAction = new TestArgsAction(output, ["50", deserialize.RequestGuildId], "SendWebSocketRptLines");
				// argsAction.ExecuteAction();
				return 1;
			};*/

			serviceInteractions.WsClient.Connected += async () =>
			{
				if (!isDifferent) return;
				//- Send Templates
				var config = profile["Configuration"]
					.ToObject<Dictionary<string, string>>()
					.ToDictionary(
						v => $".profile/{v.Key}",
						v => v.Value
					);
				serviceInteractions.SendWebSocketBinaries(config);
					
				//- Update DB
				var jsonString = new JObject
				{
					["ProcessType"] = 3,
					["MessageId"] = returnMessageId,
					["Configuration"] = profile["Configuration"],
				}.ToString();
				
				var payload = new Arma3PayloadJson(jsonString);
				var message = JsonSerializer.Serialize(payload, Arma3PayloadJsonSerializerContext.Default.Arma3Payload);

				Console.WriteLine("DB data Updated !!");
				await ServiceStartup.serviceInteractions!.SendWebSocketMessage(message);
			};
			
			
			var profileDateOffsets = configuration
				.Select(x =>
					{
						var fileInfo = new FileInfo(x.Value);
						DateTimeOffset dateTimeOffset = fileInfo.LastWriteTime;

						return fileInfo.Exists ? 
							dateTimeOffset.ToUnixTimeSeconds().ToString() : 
							throw new FileNotFoundException($"File \"{x}\" not found");
					}
				).ToList();
			
			var profileIdentity = new JObject
			{
				["type"] = 2,
				["MessageId"] = profile["MessageId"]?.ToString(),
				["RPT_Directory"] = profile["RPT_Directory"],
				["Configuration"] = profile["Configuration"],
				["ProfileDateOffsets"] = JArray.FromObject(profileDateOffsets),
			}.ToString();
			
			
			await ServiceStartup.InitializeAsync(
				"FeatureTest", profileIdentity
			);
			
			/*var output = new TestOutputBuilder();
			var argsAction = new TestArgsAction(output, ["20"], "SendWebSocketRptLines");

			argsAction.ExecuteAction();*/
			
			//////////////////////////////
			// var readLines = File.ReadLinesAsync(
			// 	@"C:\Users\aaren_bb64lye\AppData\Local\Arma 3\Arma3_x64_2026-05-01_21-39-20.rpt", Encoding.UTF8);
			//
			// await foreach (var line in readLines.TakeLast(10))
			// {
			// 	Console.WriteLine(line);
			// }


			// const string jsonPath = "Discord_Message_API/Server_Info_msg.json";
			// json = await File.ReadAllTextAsync(jsonPath, Encoding.UTF8);
			
			/*var jsonObj = new JObject
			{
				["ProcessType"] = 2,
				["profileName"] = "Not Specified",
				["serverInfoMessageId"] = "1493570455041347664",
			};
			*/

			/*var array = "{ \"type\": 6, \"FlatJsonString\": { \"{MISSION_NAME}\": \"Nigga\" }}";
			var message = JsonSerializer.Deserialize(
				array, Arma3PayloadJsonSerializerContext.Default.Arma3Payload);*/
			
			/*var payload = new Arma3PayloadFlatJsonString(new Dictionary<string, string>
			{
				{ "{MISSION_NAME}", "Nigga" }
			});*/
			/*var payload = new Arma3PayloadJson(jsonObj.ToString());
			var message = JsonSerializer.Serialize(payload, Arma3PayloadJsonSerializerContext.Default.Arma3Payload);
			await ServiceStartup.serviceInteractions!.SendWebSocketMessage(message);*/
			
			
			// var payload = new Arma3PayloadText("msg");
			// var message = JsonSerializer.Serialize(payload, Arma3PayloadJsonSerializerContext.Default.Arma3Payload);
			// Console.WriteLine(message);
			
			
			
			// var RPT = Util.GetLastestFile(ServiceStartup.serviceInteractions.RPTDirectory);
			// await ServiceStartup.serviceInteractions.SendWebSocketBinary(RPT);
			
			Console.ReadKey();
			
			////////////////////////////

			/*var url = "http://localhost:5000/api/Arma/stream";
			using HttpClient client = new();
			*/ /*await foreach (var data in client.GetFromJsonAsAsyncEnumerable<string>(url))
			{
				Console.WriteLine(data);
			}*/ /*

			using (var form = new MultipartFormDataContent())
			{
				var file = ".env";
				await foreach (string item in File.ReadLinesAsync(file))
				{
					Console.WriteLine(item);
					//form.Add(new StringContent(item, Encoding.UTF8, "text/plain"), nameof(item));
					var res = await client.PostAsync(url, new StringContent(item, Encoding.UTF8, "text/plain"));
					Console.WriteLine(res.EnsureSuccessStatusCode());
				}

			}*/


			/*var (url, token) = (
				"ws://localhost:7172/api/ws/ingame",
				"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJOZXcgUHJvZHVjdCIsImp0aSI6IjMxYzgzYmIxLTMzNDktNDM3Ny04ZGVhLWVhMzM4NTIwMzA4ZSIsInVuaXF1ZV9uYW1lIjoiTmV3IFByb2R1Y3QiLCJyb2xlIjoiR2FtZS1TZXJ2ZXIiLCJuYW1laWQiOiJiYTNlZmQ3MC0zYmRjLTQ2NDMtOTYwZS02MzY2MGUxOWZhNmQiLCJuYmYiOjE3NzMxMzg1NDUsImV4cCI6MTc3MzE0MjE0NSwiaWF0IjoxNzczMTM4NTQ1LCJpc3MiOiJpc3N1ZXIiLCJhdWQiOiJHYW1lLVNlcnZlciJ9.SYCDhzU_CVsjlg0xYh3eXM4A1aUB6nmna846xiJoO8A"
			);

			var client = new WebSocketClient(url, token);

			// Subscribe to events
			client.Connected += () => Console.WriteLine("Event: Connected to server");
			client.Disconnected += () => Console.WriteLine("Event: Disconnected from server");
			client.MessageReceived += (message) => Console.WriteLine($"Event: Message received - {message}");

			// Connect to server
			await client.ConnectAsync();

			// Interactive message sending
			Console.WriteLine("Enter messages to send (type 'quit' to exit):");

			string? input;
			while ((input = Console.ReadLine()) != "quit")
			{
				if (!string.IsNullOrEmpty(input))
				{
					await client.SendMessageAsync(input);
				}
			}

			// Disconnect
			await client.DisconnectAsync();
			Console.WriteLine("Application ended.");*/
		}
	}
}
