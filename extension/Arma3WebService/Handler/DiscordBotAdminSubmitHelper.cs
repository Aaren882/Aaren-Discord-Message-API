using System.Collections.Concurrent;
using System.Net.Mime;
using System.Net.WebSockets;
using Arma3WebService.DBContext.Repositories;
using Arma3WebService.Entity.DiscordBotAction;
using Arma3WebService.Extensions;
using Arma3WebService.Models;
using Components.Entity;
using Discord;
using Discord.WebSocket;

namespace Arma3WebService.Handler;

using RespondHelper = DiscordBotAdminModalRespondHelper;
internal static class DiscordBotAdminSubmitHelper
{
	private delegate Task<(string sessionName, string? additionMessage)> SubmitAction(SocketModal component, DiscordBotAdminSimpleAction simpleAction, IServiceProvider serviceProvider);

	public static async ValueTask Extension(this DiscordBotAdminSimpleAction simpleAction, SocketModal component,
		IServiceProvider serviceProvider)
	{
		SubmitAction content = (simpleAction.ModalType) switch
		{
			DiscordBotAdminModalType.upload_list => UploadList,
			DiscordBotAdminModalType.print_log => PrintLog,
			DiscordBotAdminModalType.export_log => ExportLog,
			DiscordBotAdminModalType.admin_mp_command => AdminMpCommand,
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
			.AddField("Command", $"`{simpleAction.ModalType}`", true)
			.AddField("📨 To", $"`{sessionName}`", true)
			.AddField("🔶 Message", $"```arm\n{additionMessage ?? "N/A"}\n```")
			.AddField("Channel", $"https://discord.com/channels/{component.GuildId}/{component.Channel.Id}", true)
			.AddField("Panel", component.Message.GetJumpUrl(), true)
			.WithColor(3447003)
			.WithFooter("System Logger")
			.WithCurrentTimestamp();

		// _ = channel.SendMessageAsync(embed: embedBuilder.Build()).ConfigureAwait(false);
		await channel.SendMessageAsync(embed: embedBuilder.Build());
	}
	private static readonly HttpClient _httpClient = new();
	private static async Task<(string sessionName, string? additionMessage)> UploadList(SocketModal component, DiscordBotAdminSimpleAction simpleAction, IServiceProvider serviceProvider)
	{
		var attachments = component.Data.Attachments.ToList();
		var attachment = attachments[0];

		if (!attachment.ContentType.Contains(MediaTypeNames.Text.Html))
			throw new ArgumentOutOfRangeException(attachment.ContentType);

		//- Saving Url
		//- Get correct server info
		var sessionName = GetSelectedSession(component);
		var identityRepository = serviceProvider.GetRequiredService<IServerIdentityRepository>();
		var serverIdentity = await identityRepository.GetByProfileNameAsync(sessionName);

		ArgumentNullException.ThrowIfNull(serverIdentity);

		//- Respond with a message
		var discordBotService = serviceProvider.GetRequiredService<IDiscordBotService>();
		var url = attachment.Url;
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
				.AddField("Channel", $"https://discord.com/channels/{component.GuildId}/{component.Channel.Id}", true)
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

			serverIdentity.modListMessageId = message.Id;
		}

		//- Update Save into DB
		await identityRepository.UpdateServerIdentityAsync(serverIdentity);

		return (sessionName, null);
	}

	internal static ConcurrentDictionary<string, SocketModal> SubmittedModalSockets = new();
	private static async Task<(string sessionName, string? additionMessage)> PrintLog(SocketModal component, DiscordBotAdminSimpleAction simpleAction, IServiceProvider serviceProvider)
	{
		var webSocketService = serviceProvider.GetRequiredService<IWebSocketService>();
		var sessionName = GetSelectedSession(component);

		if (!webSocketService.TryGetConnection(sessionName, out var websocketServer))
			ArgumentNullException.ThrowIfNull(websocketServer, nameof(websocketServer));

		//component.GuildId
		var guildId = $"{component.GuildId}";
		/*if (!SubmittedPrintLogModalSockets.TryAdd(guildId, component))
		{
			await component.RespondAsync("Your request is already submitted (Still waiting for a response).");
			return;
		}*/
		SubmittedModalSockets[guildId] = component;
		Arma3PayloadServiceRequest command = new(1, guildId);
		await websocketServer!.SendAsync(command.ToJsonBytes(), WebSocketMessageType.Text, true);

		return (sessionName, null);
	}

	private static async Task<(string sessionName, string? additionMessage)> ExportLog(SocketModal component, DiscordBotAdminSimpleAction simpleAction, IServiceProvider serviceProvider)
	{
		var webSocketService = serviceProvider.GetRequiredService<IWebSocketService>();
		var sessionName = GetSelectedSession(component);

		if (!webSocketService.TryGetConnection(sessionName, out var websocketServer))
			ArgumentNullException.ThrowIfNull(websocketServer, nameof(websocketServer));

		//component.GuildId
		var guildId = $"{component.GuildId}";
		SubmittedModalSockets[guildId] = component;

		var command = new Arma3PayloadServiceRequest(2, guildId);
		await websocketServer!.SendAsync(command.ToJsonString(), WebSocketMessageType.Text, true);

		return (sessionName, null);
	}
	private static async Task<(string sessionName, string? additionMessage)> AdminMpCommand(SocketModal component, DiscordBotAdminSimpleAction simpleAction, IServiceProvider serviceProvider)
	{
		var password = Environment.GetEnvironmentVariable("AdminPassword");
		if (password is null) throw new Exception("Missing AdminPassword (make sure password is set in environment variables)");

		var webSocketService = serviceProvider.GetRequiredService<IWebSocketService>();
		var sessionName = GetSelectedSession(component);

		var componentCustomId = simpleAction.ModalType.GetComponentCustomId().First();
		var inputComponent = component.Data.Components.First(x => string.Equals(x.CustomId, componentCustomId, StringComparison.OrdinalIgnoreCase));

		var remoteCommand = new Arma3RemoteCommand
		{
			gameId = sessionName,
			payload = new Arma3PayloadCallBack(
				nameof(AdminMpCommand),
				$"""[["{password}", "{inputComponent.Value}"], "{component.User.GlobalName}", "{component.User.Id}"]"""
			)
		};

		await webSocketService.InvokeArmaCallBack(remoteCommand);
		await component.RespondAsync($"`\"{nameof(AdminMpCommand)}\" => \"{sessionName}\" Completed !`", ephemeral: true);

		return (sessionName, $"[[\"##password##\", \"{inputComponent.Value}\"], \"{component.User.GlobalName}\", \"{component.User.Id}\"]");
	}
	private static async Task<(string sessionName, string? additionMessage)> AdminBroadcast(SocketModal component, DiscordBotAdminSimpleAction simpleAction, IServiceProvider serviceProvider)
	{
		//- Saving Url
		var webSocketService = serviceProvider.GetRequiredService<IWebSocketService>();

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
