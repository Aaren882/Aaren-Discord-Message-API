using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Components.Entity;
using ServiceConnection.Discord;
using ServiceConnection.Entity;
using ServiceConnection.Tools;
using static ServiceConnection.ServiceStartup;

namespace ServiceConnection.Delegates;
public static class EntryDelegates
{
	public static readonly IDictionary<string, InitActions> ActionsDict = _initActionsMap(typeof(Actions));
	
	public delegate int InitActions(IOutputBuilder output, string[] args, int argCount);
	public delegate Task<int> InitTaskActions(IOutputBuilder output, string[] args, int argCount);
	
    private static Dictionary<string, InitActions> _initActionsMap(
	    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)] Type actionType
	)
    {
        var methods = actionType
	        .GetMethods(
		        BindingFlags.Static | BindingFlags.NonPublic
		    )
	        .ToDictionary(
		        prop => prop.Name, 
		        prop => (InitActions)Delegate.CreateDelegate(
			        typeof(InitActions),
			        null,
			        prop
		        )!
	        );

        return methods;
    }
    
    private class Actions
    {
	    /// <summary>
        /// Initation for Clients (Players)
        /// </summary>
        /// <param name="output"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        internal static int Init_Player(IOutputBuilder output, string[] args, int argCount)
        {
            /*if (ExtensionInit)
            {
                throw new Exception("Extension has already been initiated.");
            }*/
            InitTime = args[0]; //- From Server
			
            return 1;
        }

	    internal static int Init_Server(IOutputBuilder output, string[] args, int argCount)
        {
	        // var webhooksCount = 0;
	        // if (ExtensionInit) return webhooksCount;
	        _ = ConnectWebSocket(output, args, argCount); //- Access Backend (Setup Relay)
	        var webhooksCount = Refresh_Webhooks(output, ["-1"], argCount); //- Get Webhooks

	        return webhooksCount;
        }
        /// <summary>
        /// Fetch discord webhooks from `Webhooks.json`
        /// </summary>
        /// <param name="output"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        internal static int Refresh_Webhooks(IOutputBuilder output, string[] args, int argCount)
        {
            var jsonString = Util.ParseJson("Webhooks.json");
            Tracer("Refresh_Webhooks", jsonString);

            ALLWebhooks = JsonSerializer.Deserialize(
                jsonString,
                WebhooksStorage_JsonContext.Default.WebhooksStorage
            );

            var webhooksCount = ALLWebhooks?.Webhooks.Length ?? 0;
            var webhookSel = Math.Min(Int32.Parse(args[0]), webhooksCount - 1);
            ExtensionInit = true;

            //- Exit if there's no Webhook
            if (webhooksCount == 0)
            {
                throw new Exception("No Webhook Exist.");
            }

            output.Append(webhookSel < 0 // output can be like ["ww", "ww"]
	            ? $"[[\"{string.Join("\",\"", ALLWebhooks?.Webhooks)}\"],\"{InitTime}\"]"
	            : $"[\"{ALLWebhooks?.Webhooks[webhookSel]}\",\"{InitTime}\"]");

            return webhooksCount;
        }
        
        /// <summary>
        /// Parses the first JSON string in the specified arguments, converts it to a UTF-32 code point array, and
        /// appends the result to the output.
        /// </summary>
        /// <param name="output">The output builder to which the UTF-32 code point array representation will be appended.</param>
        /// <param name="args">An array of strings containing the arguments. The first element is expected to be a JSON string to
        /// parse.</param>
        /// <param name="argCount">The number of arguments provided in the args array.</param>
        /// <returns>Always returns 1 to indicate successful processing of the first argument.</returns>
        internal static int ParseJson(IOutputBuilder output, string[] args, int argCount)
        {
            var utf = Util.StringToCode32(Util.ParseJson(args[0]));
            output.Append($"[{string.Join(",", utf)}]");
            return 1;
        }
        
        /// <summary>
        /// Handles a JSON-related command using the specified arguments.
        /// </summary>
        /// <param name="output">The output builder used to construct the command's response. This parameter is not modified by this
        /// method.</param>
        /// <param name="args">An array of command-line arguments to process.</param>
        /// <param name="argCount">The number of arguments provided in the <paramref name="args"/> array.</param>
        /// <returns>Always returns 1 to indicate successful handling of the command.</returns>
        internal static int HandlerJson(IOutputBuilder output, string[] args, int argCount)
        {
	        _ = Worker.HandlerJson(args);
            return 1;
        }
        /// <summary>
        /// Formats the first argument as a JSON string, converts it to a UTF-32 code point array, and appends the
        /// result to the specified output.
        /// </summary>
        /// <param name="output">The output builder to which the formatted UTF-32 code point array will be appended.</param>
        /// <param name="args">An array of arguments, where the first element is expected to be a JSON-formatted string to process.</param>
        /// <param name="argCount">The number of arguments provided in the <paramref name="args"/> array.</param>
        /// <returns>Always returns 1 to indicate successful processing.</returns>
        internal static int HandlerJsonFormat(IOutputBuilder output, string[] args, int argCount)
        {
	        _ = Worker.HandlerJsonFormat(args);
            return 1;
        }
        /// <summary>
        /// Processes and sends a message using the specified arguments and output builder.
        /// </summary>
        /// <remarks>If the sixth argument contains multiple Unicode code points enclosed in
        /// brackets, they are converted to their corresponding characters before sending the message. This method
        /// does not expect a reply and is intended for asynchronous use.</remarks>
        /// <param name="output">The output builder used to append status or error messages during processing.</param>
        /// <param name="args">An array of strings containing the arguments required to construct and send the message. The array must
        /// contain at least six elements, with the sixth element representing code points or message content.</param>
        /// <param name="argCount">The number of arguments provided in the <paramref name="args"/> array. Must be exactly 8.</param>
        /// <returns>An integer value of 1 if the message is processed and sent successfully.</returns>
        /// <exception cref="Exception">Thrown if <paramref name="argCount"/> is not equal to 8.</exception>
        internal static int SendMessage(IOutputBuilder output, string[] args, int argCount)
        {
            if (argCount != 8) // async without await because we don't expect a reply
                throw new Exception("INCORRECT NUMBER OF ARGUMENTS");

            var codePointStrings = Regex.Replace(args[5], @"[\[\]]", "").Split(',');

            if (codePointStrings.Length > 1)
                args[5] = string.Concat(codePointStrings.Select(cp => char.ConvertFromUtf32(int.Parse(cp))));

            _ = Worker.HandleRequest(args);
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
	        if (string.IsNullOrEmpty(accessName)) 
		        throw new Exception("No access name provided.");
	        
	        _ = InitializeAsync(accessName);
	        
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
	        _ = serviceInteractions.ReconnectWebSocket();
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
            _ = serviceInteractions.SendWebSocketMessage(message);
            return 1;
        }
        /*internal static int SendWebSocketArray(IOutputBuilder output, string[] args, int argCount)
        {
            var messageObj = new Arma3PayloadArrayString(args);
            
            _ = serviceInteractions.SendWebSocketMessage(messageObj);
            return 1;
        }
        internal static int SendWebSocketJson(IOutputBuilder output, string[] args, int argCount)
        {
            var JsonString = args[0];
            var messageObj = new Arma3PayloadJson(JsonString);
            
            _ = serviceInteractions.SendWebSocketMessage(messageObj);
            return 1;
        }*/
        internal static int SendWebSocketRPT(IOutputBuilder output, string[] args, int argCount)
        {
	        _ = serviceInteractions.SendWebSocketBinary(Util.GetLastestFile(serviceInteractions.RPTDirectory));
            return 1;
        }
        /*internal static int GetRPTInfo(IOutputBuilder output, string[] args, int argCount)
        {
	        var filePath = Util.GetLastestFile(serviceInteractions.RPTDirectory);
	        var chunkSize = argCount < 1 ? 64 * 1024 : Convert.ToInt32(args[1]);
	        
	        var fileInfo = new FileInfo(filePath);
	        var totalChunks = (int)Math.Ceiling((double)fileInfo.Length / chunkSize);
	        
	        output.Append($"[{fileInfo.Name},{fileInfo.Length},{fileInfo.CreationTime},{totalChunks}]");
            return 1;
        }*/
    }
}
