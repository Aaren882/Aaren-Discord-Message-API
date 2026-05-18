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
	private delegate Task<(string sessionName, string? additionMessage)> SubmitAction(SocketModal component, DiscordBotAdminSimpleAction simpleAction, IServiceProvider serviceProvider);

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
		
		var (sessionName, additionMessage) = await content(component, simpleAction, serviceProvider);
		
		var discordBotService = serviceProvider.GetRequiredService<IDiscordBotService>();
		var channelId = discordBotService.GetPresetMessageChannelId(DiscordBotChannel.AdminLogging);
		var channel = await discordBotService.GetMessageChannelAsync(channelId);

		var embedBuilder = new EmbedBuilder()
			.WithAuthor(component.User.GlobalName)
			.WithThumbnailUrl(component.User.GetAvatarUrl(size: 64))
			.WithTitle("⚡ Admin Console Command Executed")
			.AddField("Command", $"`{simpleAction.ModalType.ToString()}`", true)
			.AddField("📨 To", $"`{sessionName}`", true)
			.AddField("🔶 Message", $"```arm\n{additionMessage ?? "N/A"}\n```")
			.AddField("Channel", $"https://discord.com/channels/{component.GuildId}/{component.Channel.Id}",true)
			.AddField("Panel", component.Message.GetJumpUrl(), true)
			.WithColor(3447003)
			.WithFooter("System Logger")
			.WithCurrentTimestamp();
		
		_ = channel.SendMessageAsync(embed: embedBuilder.Build()).ConfigureAwait(false);
	}
	private static readonly HttpClient _httpClient = new ();
	private static async Task<(string sessionName, string? additionMessage)> UploadList(SocketModal component, DiscordBotAdminSimpleAction simpleAction, IServiceProvider serviceProvider)
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
		var url = attachment.Url;

		//- Respond with a message
		var discordBotService = serviceProvider.GetRequiredService<IDiscordBotService>();
		await using (var content = await _httpClient.GetStreamAsync(url))
		{
		
			//- Send to logging channel
			var channelId = discordBotService.GetPresetMessageChannelId(DiscordBotChannel.AdminLogging);
			var channel = await discordBotService.GetMessageChannelAsync(channelId);

			var embedBuilder = new EmbedBuilder()
				.WithAuthor(component.User.GlobalName)
				.WithThumbnailUrl(component.User.GetAvatarUrl(size: 64))
				.WithTitle("📂 Mod List Update")
				.WithColor(3447003)
				.AddField("Filename", attachment.Filename, true)
				.AddField("Size", $"{attachment.Size:##,###} Bytes", true)
				.AddField("Session", $"`{serverIdentity.profileName}`", true)
				.AddField("Channel", $"https://discord.com/channels/{component.GuildId}/{component.Channel.Id}",true)
				.AddField("Panel", component.Message.GetJumpUrl(), true)
				.WithFooter("System Logger")
				.WithCurrentTimestamp();

			_ = component.RespondAsync($"`Mod List update Completed !` \n\n {url}", ephemeral: true)
				.ConfigureAwait(false);
			var message = await channel.SendFileAsync(
				content,
				attachment.Filename,
				embed: embedBuilder.Build()
			);

			var sentAttachment = message.Attachments.First();
			serverIdentity.modListUrl = sentAttachment.Url;
		}
		
		await dbContext.SaveChangesAsync();

		return (sessionName, null);
	}
	
	internal static ConcurrentDictionary<string, SocketModal> SubmittedPrintLogModalSockets = new();
	private static async Task<(string sessionName, string? additionMessage)> PrintLog(SocketModal component, DiscordBotAdminSimpleAction simpleAction, IServiceProvider serviceProvider)
	{
		var webSocketService = serviceProvider.GetRequiredService<IWebSocketService>();
		var sessionName = GetSelectedSession(component);
		var connection = webSocketService.GetConnection(sessionName);

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
		
		return (sessionName, null);
	}

	internal static ConcurrentDictionary<string, SocketModal> SubmittedExportLogModalSockets = new();
	private static async Task<(string sessionName, string? additionMessage)> ExportLog(SocketModal component, DiscordBotAdminSimpleAction simpleAction, IServiceProvider serviceProvider)
	{
		var webSocketService = serviceProvider.GetRequiredService<IWebSocketService>();
		var sessionName = GetSelectedSession(component);
		var connection = webSocketService.GetConnection(sessionName);

		//component.GuildId
		var guildId = $"{component.GuildId}";
		SubmittedExportLogModalSockets[guildId] = component;

		var command = new Arma3PayloadServiceRequest(2, guildId);
		await connection.SendArmaCallBackMessage(command);
		
		return (sessionName, null);
	}
	private static async Task<(string sessionName, string? additionMessage)> AdminRestartMission(SocketModal component, DiscordBotAdminSimpleAction simpleAction, IServiceProvider serviceProvider)
	{
		var password = Environment.GetEnvironmentVariable("AdminPassword");
		if (password is null) throw new Exception("Missing AdminPassword (make sure password is set in environment variables)");
		
		var webSocketService = serviceProvider.GetRequiredService<IWebSocketService>();
		var sessionName = GetSelectedSession(component);
		
		var remoteCommand = new Arma3RemoteCommand
		{
			gameId = sessionName,
			payload = new Arma3PayloadCallBack(nameof(AdminRestartMission), $"[\"{password}\", \"{component.User.GlobalName}\", \"{component.User.Id}\"]")
		};
		await webSocketService.InvokeArmaCallBack(remoteCommand);
		await component.RespondAsync($"`\"{nameof(AdminRestartMission)}\" => \"{sessionName}\" Completed !`", ephemeral: true);
	
		return (sessionName, $"[\"##password##\", \"{component.User.GlobalName}\", \"{component.User.Id}\"]");
	}
	private static async Task<(string sessionName, string? additionMessage)> AdminBroadcast(SocketModal component, DiscordBotAdminSimpleAction simpleAction, IServiceProvider serviceProvider)
	{
		//- Saving Url
		var webSocketService= serviceProvider.GetRequiredService<IWebSocketService>();
		
		//- Get correct server info
		var sessionName = GetSelectedSession(component);

		var componentCustomId = simpleAction.ModalType.GetComponentCustomId().First();
		var inputComponent = component.Data.Components.First(x => string.Equals(x.CustomId, componentCustomId, StringComparison.OrdinalIgnoreCase));
		
		var data = $"[\"{inputComponent.Value}\", \"{component.User.GlobalName}\", \"{component.User.Id}\"]";
		var remoteCommand = new Arma3RemoteCommand
		{
			gameId = sessionName,
			payload = new Arma3PayloadCallBack(nameof(AdminBroadcast), data)
		};
		await webSocketService.InvokeArmaCallBack(remoteCommand);
		await component.RespondAsync($"`\"{nameof(AdminBroadcast)}\" => \"{sessionName}\" Completed !`", ephemeral: true);
		
		return (sessionName, data);
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
