using System.Text.Json;
using System.Text.RegularExpressions;
using static DiscordMessageAPI.DllEntry;

namespace DiscordMessageAPI.Delegates
{
    internal class EntryDelegates
    {
        internal static readonly Dictionary<string, InitActions> ActionsDict = _Init();
        internal delegate int InitActions(OutputBuilder output, string[] args, int argCount);


        private static Dictionary<string, InitActions> _Init()
        {
            var _dict = new Dictionary<string, InitActions>();
    
            string[] keys = [
                "init_player", "Refresh_Webhooks",
                "ParseJson", "HandlerJson", "HandlerJsonFormat", "SendMessage"
            ];

            InitActions[] actions = [
                EntryActions.Init_player, EntryActions.Refresh_Webhooks,
                Actions.ParseJson, Actions.HandlerJson, Actions.HandlerJsonFormat, Actions.SendMessage
            ];

            //- Register functions
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                var action = actions[i];
                _dict.Add(key, action);
            }

            return _dict;
        }

        internal static class EntryActions
        {
            /// <summary>
            /// Initation for Clients (Players)
            /// </summary>
            /// <param name="output"></param>
            /// <param name="args"></param>
            /// <returns></returns>
            internal static int Init_player(OutputBuilder output, string[] args, int argCount)
            {
                if (ExtensionInit)
                {
                    throw new Exception("Extension has already been initiated.");
                }
                InitTime = args[0]; //- From Server
                return 0;
            }
            /// <summary>
            /// Fetch discord webhooks from `Webhooks.json`
            /// </summary>
            /// <param name="output"></param>
            /// <param name="args"></param>
            /// <returns></returns>
            internal static int Refresh_Webhooks(OutputBuilder output, string[] args, int argCount)
            {
                string jsonString = Tools.ParseJson("Webhooks.json");
                Tools.Trace("Refresh_Webhooks", jsonString);

                ALLWebhooks = JsonSerializer.Deserialize<Webhooks_Storage>(
                    jsonString,
                    Webhooks_Storage_JsonContext.Default.Webhooks_Storage);


                int webhooksCount = ALLWebhooks!.Webhooks.Length;
                int webhook_sel = Math.Min(Int32.Parse(args[0]), webhooksCount - 1);
                ExtensionInit = true;

                //- Exit if there's no Webhook
                if (webhooksCount == 0)
                {
                    throw new Exception("No Webhook Exist.");
                }

                if (webhook_sel < 0) // output can be like ["ww", "ww"]
                    output.Append($"[[\"{string.Join("\",\"", ALLWebhooks.Webhooks)}\"],\"{InitTime}\"]");
                else
                    output.Append($"[\"{ALLWebhooks.Webhooks[webhook_sel]}\",\"{InitTime}\"]");

                return webhooksCount;
            }

            /// <summary>
            /// Default method for **ActionsDict**
            /// </summary>
            /// <param name="output"></param>
            /// <param name="args"></param>
            /// <returns></returns>
            /// <exception cref="Exception"></exception>
            internal static int NullDefault(OutputBuilder output, string[] args, int argCount)
            {
                throw new Exception("No method found.");
            }
        }

        internal static class Actions
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
                int[] utf = Tools.StringToCode32(Tools.ParseJson(args[0]));
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
                Discord.HandlerJson(args);
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
                Discord.HandlerJsonFormat(args);
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

                string[] codePointStrings = Regex.Replace(args[5], @"[\[\]]", "").Split(',');

                if (codePointStrings.Length > 1)
                    args[5] = string.Concat(codePointStrings.Select(cp => char.ConvertFromUtf32(int.Parse(cp))));

                Discord.HandleRequest(args);
                return 1;
            }
        }
    }
}
