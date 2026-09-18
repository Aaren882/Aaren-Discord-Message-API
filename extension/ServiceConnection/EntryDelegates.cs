using System.Text.Json;
using System.Threading.Channels;
using ExtensionComponents.Entity;
using ExtensionComponents.Tools;
using Microsoft.Extensions.Logging;
using static ServiceConnection.ServiceStartup;
using ServiceConnectionUtil = ServiceConnection.Tools.Util;

#pragma warning disable CA1822
namespace ServiceConnection;

public sealed class EntryDelegates : EntryDelegatesBase
{
	private static readonly Channel<Func<Task>> channel = Channel.CreateBounded<Func<Task>>(100);
	public EntryDelegates(ILogger<EntryDelegates> logger) : base(logger)
	{
		ActionsDict = GetActionsMap<EntryDelegates>();
		Task.Run(() => ChannelExecuteLoopAsync());
	}

	private static async Task ChannelExecuteLoopAsync()
	{
		await foreach (var item in channel.Reader.ReadAllAsync())
		{
			await item();
		}
	}
	private static bool TryAddTask(Func<Task> action)
	{
		return channel.Writer.TryWrite(action);
	}

	internal static int GetDirectoryFileNames(IOutputBuilder output, string[] args, int argCount)
	{
		var path = args[0];
		Logger.LogDebug("Getting directory file names for path: {Path}", path);

		var fileNames = ServiceConnectionUtil.GetDirectoryFileNames(path);
		output.Append($"[\"{string.Join("\",\"", fileNames)}\"]");

		return fileNames.Count;
	}

	internal static int GetDirectoryFilesDateTime(IOutputBuilder output, string[] args, int argCount)
	{
		var fileInfos = ServiceConnectionUtil.GetFilesFileInfos(args)
			.Select(x =>
				((DateTimeOffset)x.LastWriteTime).ToUnixTimeSeconds()
			).ToList();

		output.Append($"[\"{string.Join("\",\"", fileInfos)}\"]");
		return fileInfos.Count;
	}
	internal static int UpdateRptDirectory(IOutputBuilder output, string[] args, int argCount)
	{
		var dir = args[0] ?? throw new NullReferenceException("Argument \"Directory path\" cannot be null.");
		ServiceStartup.RptFileDirectory = dir;
		RptFileDirectory = ServiceConnectionUtil.GetCurrentRpt();
		Logger.LogInformation("Update RPT File : {RptFileDirectory}", RptFileDirectory);

		return 1;
	}
	internal static int GetCurrentRpt(IOutputBuilder output, string[] args, int argCount)
	{
		output.Append(ServiceConnectionUtil.GetCurrentRpt());
		return 1;
	}

	/// <summary>
	/// Setup Websocket Connection to backend service
	/// </summary>
	/// <param name="output"></param>
	/// <param name="args"></param>
	/// <param name="argCount"></param>
	/// <returns></returns>
	/// <exception cref="Exception"></exception>
	internal static int ConnectWebSocket(IOutputBuilder output, string[] args, int argCount)
	{
		var accessName = args[0];
		var profilePayload = args[1];
		if (string.IsNullOrEmpty(accessName))
			throw new Exception("NO ACCESS NAME PROVIDED.");

		if (TryAddTask(() => InitializeAsync(accessName, profilePayload)))
		{
			return 1;
		}
		return -1;
	}
	/// <summary>
	/// Disrupt current WebSocket connection
	/// </summary>
	/// <param name="output"></param>
	/// <param name="args"></param>
	/// <param name="argCount"></param>
	/// <returns></returns>
	internal static int DisconnectWebSocket(IOutputBuilder output, string[] args, int argCount)
	{
		if (TryAddTask(() => ShutdownAsync()))
		{
			return 1;
		}
		return -1;
	}
	/// <summary>
	/// Reconnect Websocket relay
	/// </summary>
	/// <param name="output"></param>
	/// <param name="args"></param>
	/// <param name="argCount"></param>
	/// <returns></returns>
	internal static int ReconnectWebSocket(IOutputBuilder output, string[] args, int argCount)
	{
		var profilePayload = args[0];

		if (TryAddTask(() => ServiceInteractions.ReconnectWebSocket(profilePayload)))
		{
			return 1;
		}
		return -1;
	}

	/// <summary>
	/// Sends a message via WebSocket to the backend service.
	/// </summary>
	/// <param name="output"></param>
	/// <param name="args"></param>
	/// <param name="argCount"></param>
	/// <returns></returns>
	internal static int SendWebSocketMessage(IOutputBuilder output, string[] args, int argCount)
	{
		var message = args[0];

		if (TryAddTask(async () => await ServiceInteractions.SendWebSocketMessage(message)))
		{
			return 1;
		}
		return -1;
	}
	/*internal static int SendWebSocketRPT(IOutputBuilder output, string[] args, int argCount)
	{
		var lastestRpt= Util.GetLatestFile(ServiceInteractions.RPTDirectory);
		output.Append(lastestRpt); //- Return lastest Rpt directory

		ServiceInteractions.SendWebSocketBinary(lastestRpt, args[0]);
		ServiceInteractions.WebSocketTrafficWriter(task);

		return 1;
	}*/
	internal static int SendWebSocketBinaries(IOutputBuilder output, string[] args, int argCount)
	{
		if (string.IsNullOrEmpty(args[0]))
		{
			throw new NullReferenceException("Argument [0] cannot be null or empty.");
		}

		var binaryDict = JsonSerializer.Deserialize(args[0], ExtensionSerializable.Default.DictionaryStringString)
			?? throw new NullReferenceException("Binary dictionary is null");

		if (TryAddTask(() => ServiceInteractions.SendWebSocketBinaries(binaryDict)))
		{
			return 1;
		}
		return -1;
	}
	internal static int SendWebSocketRptLines(IOutputBuilder output, string[] args, int argCount)
	{
		if (!int.TryParse(args[0], out var linesCount))
			throw new Exception("INCORRECT NUMBER OF ARGUMENTS");

		ServiceInteractions.SendWebSocketRptLines(RptFileDirectory, linesCount);

		return 1;
	}
	internal static int SendWebSocketBinariesFromAssemblyDirectory(IOutputBuilder output, string[] args, int argCount)
	{
		var binaryDict = JsonSerializer.Deserialize(args[0], ExtensionSerializable.Default.DictionaryStringString)
			?? throw new Exception("INVALID ARGUMENT. (Dictionary for binaries is null)");

		foreach (var (key, value) in binaryDict)
			binaryDict[key] = Path.Combine(Util.AssemblyPath, value);

		if (TryAddTask(() => ServiceInteractions.SendWebSocketBinaries(binaryDict)))
		{
			return 1;
		}
		return -1;
	}
}
