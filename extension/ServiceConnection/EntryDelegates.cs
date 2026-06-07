using System.Text.Json;
using ExtensionComponents;
using ExtensionComponents.Entity;
using ExtensionComponents.Tools;
using static ServiceConnection.ServiceStartup;
using ServiceConnectionUtil = ServiceConnection.Tools.Util;

namespace ServiceConnection;

public sealed class EntryDelegates: EntryDelegatesBase
{
    public EntryDelegates()
    {
        ActionsDict = GetActionsMap(typeof(Actions));
    }
    private sealed class Actions
    {
        internal static int GetDirectoryFileNames(IOutputBuilder output, string[] args, int argCount)
        {
            var path = args[0];
            var fileNames = ServiceConnectionUtil.GetDirectoryFileNames(path);
            output.Append($"[\"{string.Join("\",\"", fileNames)}\"]");

            return fileNames.Count;
        }

        internal static int GetDirectoryFilesDateTime(IOutputBuilder output, string[] args, int argCount)
        {
            var fileInfos = ServiceConnectionUtil.GetFilesFileInfos(args)
	            .Select(x => 
		            ((DateTimeOffset) x.LastWriteTime).ToUnixTimeSeconds()
		        ).ToList();
            
            output.Append($"[\"{string.Join("\",\"", fileInfos)}\"]");
            return fileInfos.Count;
        }
        internal static int UpdateRptDirectory(IOutputBuilder output, string[] args, int argCount)
        {
            var dir = args[0];
            serviceInteractions.RPTDirectory = dir;
            RptFileDirectory = ServiceConnectionUtil.GetCurrentRpt();
            ExtensionStartup.Logger(null, "Update RPT File : " + RptFileDirectory);
            
            return 1;
        }
        #if DEBUG
        internal static int GetCurrentRpt(IOutputBuilder output, string[] args, int argCount)
        {
            output.Append(ServiceConnectionUtil.GetCurrentRpt());
            return 1;
        }
        #endif
        
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
            
            _ = InitializeAsync(accessName, profilePayload);
            
            return 1;
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
            _ = ShutdownAsync();
            return 1;
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
            
            _ = serviceInteractions.ReconnectWebSocket(profilePayload);
            return 1;
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
            
            serviceInteractions.SendWebSocketMessage(message);
		    
            return 1;
        }
        /*internal static int SendWebSocketRPT(IOutputBuilder output, string[] args, int argCount)
        {
            var lastestRpt= Util.GetLatestFile(serviceInteractions.RPTDirectory);
            output.Append(lastestRpt); //- Return lastest Rpt directory
            
            serviceInteractions.SendWebSocketBinary(lastestRpt, args[0]);
		    serviceInteractions.WebSocketTrafficWriter(task);
            
            return 1;
        }*/
        internal static int SendWebSocketBinaries(IOutputBuilder output, string[] args, int argCount)
        {
            var binaryDict = JsonSerializer.Deserialize(args[0], ExtensionSerializable.Default.DictionaryStringString);
            serviceInteractions.SendWebSocketBinaries(binaryDict!);
            
            return 1;
        }
        internal static int SendWebSocketRptLines(IOutputBuilder output, string[] args, int argCount)
        {
            if (!int.TryParse(args[0], out var linesCount))
	            throw new Exception("INCORRECT NUMBER OF ARGUMENTS");

            serviceInteractions.SendWebSocketRptLines(RptFileDirectory, linesCount);
            
            return 1;
        }
        internal static int SendWebSocketBinariesFromAssemblyDirectory(IOutputBuilder output, string[] args, int argCount)
        {
            var binaryDict = JsonSerializer.Deserialize(args[0], ExtensionSerializable.Default.DictionaryStringString);
            
            if (binaryDict is null)
	            throw new Exception("INVALID ARGUMENT. (Dictionary for binaries is null)");
            
            foreach (var (key, value) in binaryDict)
	            binaryDict[key] = Path.Combine(Util.AssemblyPath, value);
            
            serviceInteractions.SendWebSocketBinaries(binaryDict);
            
            return 1;
        }
    }

}
