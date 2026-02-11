using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DiscordMessageAPI
{
    public class DllEntry
    {
        //private static readonly string SessionKey = Tools.GenTimeEncode();
        public static string InitTime = null;
        public static bool ExtensionInit = false;
        public static Webhooks_Storage ALLWebhooks = null;
        internal class OutputBuilder
        {
            nint destination;
            int outputSize;

            public OutputBuilder(IntPtr destination, int outputSize)
            {
                this.destination = destination;
                this.outputSize = outputSize;
            }

            /// <summary>
            /// Construct output buffer for Arma
            /// </summary>
            /// <param name="destination">Output destination for the buffer</param>
            /// <param name="outputSize">Output destination buffer Size</param>
            /// <param name="data">String data that will be output</param>
            public void Append(string data)
            {
                var bytes = Encoding.UTF8.GetBytes(data);
                Marshal.Copy(bytes, 0, this.destination, Math.Min(bytes.Length, this.outputSize));
            }
        };

        /// <summary>
        /// Gets called when Arma starts up and loads all extension.
		/// It's perfect to load in static objects in a separate thread so that the extension doesn't needs any separate initalization
        /// </summary>
        /// <param name="output"></param>
        /// <param name="outputSize"></param>
        [UnmanagedCallersOnly(EntryPoint = "RvExtensionVersion")]
        public static void RvExtensionVersion(nint outputPrt, int outputSize)
        {
            OutputBuilder output = new (outputPrt, outputSize);
            output.Append("1.0.0");
        }

        /// <summary>
        /// The entry point for the default callExtension command.
        /// </summary>
        /// <param name="output"></param>
        /// <param name="outputSize"></param>
        /// <param name="function"></param>
        [UnmanagedCallersOnly(EntryPoint = "RvExtension")]
        public static void RvExtension(nint output, int outputSize,nint function)
        {

        }

        /// <summary>
        /// The entry point for the callExtensionArgs command.
        /// </summary>
        /// <param name="outputPrt"></param>
        /// <param name="outputSize"></param>
        /// <param name="function"></param>
        /// <param name="argPrt"></param>
        /// <param name="argCount"></param>
        /// <returns>
        ///     numbers
        /// </returns>
        [UnmanagedCallersOnly(EntryPoint = "RvExtensionArgs")]
        public static int RvExtensionArgs(nint outputPrt, int outputSize, nint function, nint argPrt, int argCount)
        {
            OutputBuilder output = new(outputPrt, outputSize);

            var inputKey = Marshal.PtrToStringUTF8(function);
            var args = new string?[argCount];
            for (int i = 0; i < argCount; i++)
            {
                var str = Marshal.PtrToStringUTF8(Marshal.ReadIntPtr(argPrt + (i * Marshal.SizeOf<nint>())));
                args[i] = str;
            }
            try
            {
                // Use time as Key (for Server , Player)
                if (ExtensionInit && inputKey == "init_player")
                {
                    output.Append("Extension has already been initiated.");
                    return -1;
                }

                // Remove arma quotations
                args = args.Select(arg => arg.Trim('"', ' ').Replace("\"\"", "\"")).ToArray();

                //Entry
                switch (inputKey == "init_player" || inputKey == "Refresh_Webhooks")
                {
                    //- Init Functions 
                    case true:
                        {
                            // Get all Webhooks
                            if (inputKey == "Refresh_Webhooks")
                            {
                                string jsonString = Tools.ParseJson("Webhooks.json");
                                ALLWebhooks = JsonSerializer.Deserialize<Webhooks_Storage>(jsonString);
                                int webhooksCount = ALLWebhooks.Webhooks.Length;
                                int webhook_sel = Math.Min(Int32.Parse(args[0]), webhooksCount - 1);
                                ExtensionInit = true;

                                //- Exit if there's no Webhook
                                if (webhooksCount == 0)
                                {
                                    output.Append("No Webhook Exist.");
                                    return 0;
                                }

                                if (webhook_sel < 0) // output can be like ["ww", "ww"]
                                    output.Append($"[[\"{string.Join("\",\"", ALLWebhooks.Webhooks)}\"],\"{InitTime}\"]");
                                else
                                    output.Append($"[\"{ALLWebhooks.Webhooks[webhook_sel]}\",\"{InitTime}\"]");

                                return webhooksCount;
                            }
                            else //- Initation for Clients (Players)
                                InitTime = args[0]; //- From Server
                            break;
                        }
                    default:
                        {
                            if (inputKey == "ParseJson")
                            {
                                int[] utf = Tools.StringToCode32(Tools.ParseJson(args[0]));
                                output.Append($"[{string.Join(",", utf)}]");
                                break;
                            }
                            if (InitTime == null)
                            {
                                output.Append("Find No Key.");
                                break;
                            }

                            //- args[0] :
                            //- Http(s) Handlers ["url", HandlerType<int>, Optional :[Necessary Payload] ]
                            switch (inputKey)
                            {
                                //- Load Json as Message format
                                case "HandlerJson":
                                    {
                                        Discord.HandlerJson(args);
                                        break;
                                    }
                                case "HandlerJsonFormat":
                                    {
                                        Discord.HandlerJsonFormat(args);
                                        break;
                                    }
                                case "SendMessage":
                                    {
                                        if (argCount == 8) // async without await because we don't expect a reply
                                        {
                                            string[] codePointStrings = Regex.Replace(args[5], @"[\[\]]", "").Split(',');
                                            if (codePointStrings.Length > 1)
                                                args[5] = string.Concat(codePointStrings.Select(cp => char.ConvertFromUtf32(int.Parse(cp))));
                                            Discord.HandleRequest(args);
                                        }
                                        else
                                        {
                                            output.Append("INCORRECT NUMBER OF ARGUMENTS");
                                            return -2;
                                        }
                                        break;
                                    }
                                default: //- Other conditions
                                    break;
                            }

                            break; //- Exit
                        }
                }

                return 0;
            }
            catch (Exception e)
            {
                Tools.Logger(e, $"{e}");
                output.Append("Error!! Check Log.");
                return -11;
            }
        }
    }
}
