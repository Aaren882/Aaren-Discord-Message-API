using System.Text.Json;
using System.Text.RegularExpressions;
using Arma3WebService;
using DiscordMessageAPI.Discord;
using DiscordMessageAPI.Entity;
using DiscordMessageAPI.Tools;
using static DiscordMessageAPI.DllEntry;

namespace DiscordMessageAPI.Delegates;
public static class EntryDelegates
{
	public static readonly Dictionary<string, InitActions> ActionsDict = _Init();
	public delegate int InitActions(OutputBuilder output, string[] args, int argCount);

    private static Dictionary<string, InitActions> _Init()
    {
        var _dict = new Dictionary<string, InitActions>();

        string[] keys = [
            "init_player", "Init_Server", "Refresh_Webhooks",
            "ParseJson", "HandlerJson", "HandlerJsonFormat", "SendMessage", 
            "ConnectWebSocket","ReconnectWebSocket","DisconnectWebSocket",
            "SendWebSocketLog"
        ];
		
        InitActions[] actions = [
            EntryActions.Init_player, EntryActions.Init_Server, EntryActions.Refresh_Webhooks,
            Actions.ParseJson, Actions.HandlerJson, Actions.HandlerJsonFormat, Actions.SendMessage, 
            Actions.ConnectWebSocket, Actions.ReconnectWebSocket, Actions.DisconnectWebSocket,
            Actions.SendWebSocketLog
        ];

        //- Register functions
        for (var i = 0; i < keys.Length; i++)
        {
            var key = keys[i];
            var action = actions[i];
            _dict.Add(key, action);
        }

        return _dict;
    }

    public static class EntryActions
    {
        /// <summary>
        /// Initation for Clients (Players)
        /// </summary>
        /// <param name="output"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        internal static int Init_player(OutputBuilder output, string[] args, int argCount)
        {
            /*if (ExtensionInit)
            {
                throw new Exception("Extension has already been initiated.");
            }*/
            InitTime = args[0]; //- From Server
			
            return 0;
        }
        internal static int Init_Server(OutputBuilder output, string[] args, int argCount)
        {
	        // var webhooksCount = 0;
	        // if (ExtensionInit) return webhooksCount;
	        
	        Actions.ConnectWebSocket(output, args, argCount); //- Access Backend (Setup Relay)
	        var webhooksCount = Refresh_Webhooks(output, ["-1"], argCount); //- Get Webhooks

	        return webhooksCount;
        }
        /// <summary>
        /// Fetch discord webhooks from `Webhooks.json`
        /// </summary>
        /// <param name="output"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        internal static int Refresh_Webhooks(OutputBuilder output, string[] args, int argCount)
        {
            var jsonString = Util.ParseJson("Webhooks.json");
            Logger.Trace("Refresh_Webhooks", jsonString);

            ALLWebhooks = JsonSerializer.Deserialize(
                jsonString,
                Webhooks_Storage_JsonContext.Default.Webhooks_Storage
            );

            var webhooksCount = ALLWebhooks!.Webhooks.Length;
            var webhookSel = Math.Min(Int32.Parse(args[0]), webhooksCount - 1);
            ExtensionInit = true;

            //- Exit if there's no Webhook
            if (webhooksCount == 0)
            {
                throw new Exception("No Webhook Exist.");
            }

            output.Append(webhookSel < 0 // output can be like ["ww", "ww"]
	            ? $"[[\"{string.Join("\",\"", ALLWebhooks.Webhooks)}\"],\"{InitTime}\"]"
	            : $"[\"{ALLWebhooks.Webhooks[webhookSel]}\",\"{InitTime}\"]");

            return webhooksCount;
        }

        /// <summary>
        /// Default method for **ActionsDict**
        /// </summary>
        /// <param name="output"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static int NullDefault(OutputBuilder output, string[] args, int argCount)
        {
            throw new Exception("No method found.");
        }
    }

    private static class Actions
    {
        /// <summary>
        /// Parses the first JSON string in the specified arguments, converts it to a UTF-32 code point array, and
        /// appends the result to the output.
        /// </summary>
        /// <param name="output">The output builder to which the UTF-32 code point array representation will be appended.</param>
        /// <param name="args">An array of strings containing the arguments. The first element is expected to be a JSON string to
        /// parse.</param>
        /// <param name="argCount">The number of arguments provided in the args array.</param>
        /// <returns>Always returns 1 to indicate successful processing of the first argument.</returns>
        internal static int ParseJson(OutputBuilder output, string[] args, int argCount)
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
        internal static int HandlerJson(OutputBuilder output, string[] args, int argCount)
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
        internal static int HandlerJsonFormat(OutputBuilder output, string[] args, int argCount)
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
        internal static int SendMessage(OutputBuilder output, string[] args, int argCount)
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
        internal static int ConnectWebSocket(OutputBuilder output, string[] args, int argCount)
        {
	        var accessName = args[0];
	        if (string.IsNullOrEmpty(accessName)) 
		        throw new Exception("No access name provided.");
	        
	        _ = ServiceInteractions.EstablishWebSocketConnection(accessName);
	        return 1;
        }
        /// <summary>
        /// Disrupt current WebSocket connection
        /// </summary>
        /// <param name="output"></param>
        /// <param name="args"></param>
        /// <param name="argCount"></param>
        /// <returns></returns>
        internal static int DisconnectWebSocket(OutputBuilder output, string[] args, int argCount)
        {
	        _ = ServiceInteractions.DisconnectWebSocket();
	        return 1;
        }
        /// <summary>
        /// Reconnect Websocket relay
        /// </summary>
        /// <param name="output"></param>
        /// <param name="args"></param>
        /// <param name="argCount"></param>
        /// <returns></returns>
        internal static int ReconnectWebSocket(OutputBuilder output, string[] args, int argCount)
        {
	        _ = ServiceInteractions.ReconnectWebSocket();
	        return 1;
        }
        
        /// <summary>
        /// Sends a message via WebSocket to the backend service.
        /// </summary>
        /// <param name="output"></param>
        /// <param name="args"></param>
        /// <param name="argCount"></param>
        /// <returns></returns>
        internal static int SendWebSocketLog(OutputBuilder output, string[] args, int argCount)
        {
            var message = args[0];
            var messageObj = new Arma3Payload
            {
	            MessageType = Arma3PayLoadType.Logging,
	            Message = message,
            };
            
            _ = ServiceInteractions.SendWebSocketMessage(messageObj);
            return 1;
        }
    }
}
