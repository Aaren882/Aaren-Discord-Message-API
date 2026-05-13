using System.Collections.Concurrent;
using System.Net.Mime;
using Arma3WebService.DBContext;
using Arma3WebService.Entity;
using Arma3WebService.Entity.DiscordBotAction;
using Arma3WebService.Models;
using Components.Entity;
using Discord;
using Discord.WebSocket;

namespace Arma3WebService.Handler;

using RespondHelper = DiscordBotAdminModalRespondHelper;
internal static class DiscordBotAdminSubmitHelper
{
	private delegate Task SubmitAction(SocketModal component, DiscordBotAdminSimpleAction simpleAction, IServiceProvider serviceProvider);

	public static async Task Extension(this DiscordBotAdminSimpleAction simpleAction, SocketModal component,
		IServiceProvider serviceProvider)
	{
		SubmitAction content = (simpleAction.ModalType) switch
		{
			DiscordBotAdminModalType.upload_list => UploadList,
			DiscordBotAdminModalType.print_log => PrintLog,
			DiscordBotAdminModalType.export_log => ExportLog,
			DiscordBotAdminModalType.admin_restart_mission => AdminRestartMission,
			DiscordBotAdminModalType.admin_broadcast => AdminBroadcast,
			_ => throw new ArgumentOutOfRangeException(nameof(simpleAction), "\"ModalType\" does not exist in the options.")
		};
		await content(component, simpleAction, serviceProvider);
	}
	private static HttpClient _httpClient = new ();
	private static async Task UploadList(SocketModal component, DiscordBotAdminSimpleAction simpleAction, IServiceProvider serviceProvider)
	{
		var attachments = component.Data.Attachments.ToList();
		var attachment = attachments[0];
		if (!attachment.ContentType.Contains(MediaTypeNames.Text.Html)) throw new Exception("Invalid content type.");

		//- Saving Url
		var serviceScopeFactory= serviceProvider.GetRequiredService<IServiceScopeFactory>();
		await using var scoped = serviceScopeFactory.CreateAsyncScope();
		await using var dbContext = scoped.ServiceProvider.GetRequiredService<ServiceDbContext>();
		//- Get correct server info
		var sessionName = GetSelectedSession(component);
		var serverIdentity = await dbContext.GetServerIdentityFromProfileNameAsync(sessionName);
		if (serverIdentity is null)
			throw new NullReferenceException($"\"serverIdentity : {serverIdentity}\" is not exist.");
		
		//- http://cdn.discordapp.com/attachments/1315253136511991818/1386731812268540054/TFOX_2025.html?ex=6a049aa4&is=6a034924&hm=e88ebbf4e32e3edfe24fb4078a23545ec5ab1a9d2e2ce2aa6b5d9f866bc3e46f&
		//https://cdn.discordapp.com/ephemeral-attachments/1502234846066376807/1502236089145098350/Arma_3_Preset_tfox_greensea.html?ex=69fef9e1&is=69fda861&hm=645634eced13e56caf4cf41039a5f735542f2efacc9c3d40e540cb8152214ac0&
		var url = attachment.Url;

		//- Respond with a message
		var discordBotService = serviceProvider.GetRequiredService<IDiscordBotService>();
		await using (var content = await _httpClient.GetStreamAsync(url))
		{
		
			//- Send to logging channel
			var channelId = discordBotService.GetPresetMessageChannelId(DiscordBotChannel.Logging);
			var channel = await discordBotService.GetMessageChannelAsync(channelId);

			var embedBuilder = new EmbedBuilder()
				.WithAuthor(component.User.GlobalName)
				.WithThumbnailUrl(component.User.GetAvatarUrl(size: 64))
				.WithTitle("📂 File Upload Log")
				.WithColor(3447003)
				.AddField("Filename", attachment.Filename, true)
				.AddField("Size", $"{attachment.Size:##,###} Bytes", true)
				.AddField("Channel", $"https://discord.com/channels/{component.GuildId}/{component.Channel.Id}")
				.WithFooter("System Logger")
				.WithCurrentTimestamp();
		
			await component.RespondAsync($"`Mod List update Completed !` \n\n {url}", ephemeral: true);
			var message = await channel.SendFileAsync(
				content,
				attachment.Filename,
				embed: embedBuilder.Build()
			);

			var sentAttachment = message.Attachments.First();
			serverIdentity.modListUrl = sentAttachment.Url;
		}
		
		await dbContext.SaveChangesAsync();
	}
	
	internal static ConcurrentDictionary<string, SocketModal> SubmittedPrintLogModalSockets = new();
	private static async Task PrintLog(SocketModal component, DiscordBotAdminSimpleAction simpleAction, IServiceProvider serviceProvider)
	{
		var webSocketService = serviceProvider.GetRequiredService<IWebSocketService>();
		var session = GetSelectedSession(component);
		var connection = webSocketService.GetConnection(session);

		//component.GuildId
		var guildId = $"{component.GuildId}";
		/*if (!SubmittedPrintLogModalSockets.TryAdd(guildId, component))
		{
			await component.RespondAsync("Your request is already submitted (Still waiting for a response).");
			return;
		}*/
		SubmittedPrintLogModalSockets[guildId] = component;
		var command = new Arma3PayloadServiceRequest(1, guildId);
		await connection.SendArmaCallBackMessage(command);
	}

	internal static ConcurrentDictionary<string, SocketModal> SubmittedExportLogModalSockets = new();
	private static async Task ExportLog(SocketModal component, DiscordBotAdminSimpleAction simpleAction, IServiceProvider serviceProvider)
	{
		var webSocketService = serviceProvider.GetRequiredService<IWebSocketService>();
		var session = GetSelectedSession(component);
		var connection = webSocketService.GetConnection(session);

		//component.GuildId
		var guildId = $"{component.GuildId}";
		SubmittedExportLogModalSockets[guildId] = component;

		var command = new Arma3PayloadServiceRequest(2, guildId);
		await connection.SendArmaCallBackMessage(command);
	}
	private static async Task AdminRestartMission(SocketModal component, DiscordBotAdminSimpleAction simpleAction, IServiceProvider serviceProvider)
	{
		
	}
	private static async Task AdminBroadcast(SocketModal component, DiscordBotAdminSimpleAction simpleAction, IServiceProvider serviceProvider)
	{
		//- Saving Url
		var webSocketService= serviceProvider.GetRequiredService<IWebSocketService>();
		var serviceScopeFactory= serviceProvider.GetRequiredService<IServiceScopeFactory>();
		await using var scoped = serviceScopeFactory.CreateAsyncScope();
		await using var dbContext = scoped.ServiceProvider.GetRequiredService<ServiceDbContext>();
		
		//- Get correct server info
		var sessionName = GetSelectedSession(component);
		var serverIdentity= await dbContext.GetServerIdentityFromProfileNameAsync(sessionName);
		if (serverIdentity is null)
			throw new NullReferenceException($"\"serverIdentity : {serverIdentity}\" is not exist.");

		var componentCustomId = simpleAction.ModalType.GetComponentCustomId().First();
		var inputComponent = component.Data.Components.First(x => string.Equals(x.CustomId, componentCustomId, StringComparison.OrdinalIgnoreCase));
		
		var remoteCommand = new Arma3RemoteCommand
		{
			gameId = serverIdentity.profileName,
			payload = new Arma3PayloadCallBack(
				nameof(AdminBroadcast),
				$"[\"{inputComponent.Value}\"]"
			)
		};
		await webSocketService.InvokeArmaCallBack(remoteCommand);
		await component.RespondAsync($"`\"{nameof(AdminBroadcast)}\" Completed !`", ephemeral: true);
	}

	private static string GetSelectedSession(SocketModal component)
	{
		var sessionSelectMenu = component.Data.Components.FirstOrDefault(o => 
			o.CustomId == RespondHelper.SessionSelectMenuComponentCustomId);
		
		return sessionSelectMenu is null
			? throw new Exception("Cannot Find Session Select Menu")
			: sessionSelectMenu.Values.First();
	}
	
}
