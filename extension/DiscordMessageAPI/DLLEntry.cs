using System.Runtime.InteropServices;
using System.Text;
using DiscordMessageAPI.Tools;
using DiscordMessageAPI.WebService;
using static DiscordMessageAPI.Delegates.EntryDelegates;

namespace DiscordMessageAPI
{
	internal struct CallContext
	{
		public UInt64 steamId;
		public string fileSource;
		public string missionName;
		public string serverName;
		public Int16 remoteExecutedOwner;
	};

	public class DllEntry
	{
		//private static readonly string SessionKey = Tools.GenTimeEncode();
		public static string InitTime = null;
		public static bool ExtensionInit = false;
		public static Webhooks_Storage? ALLWebhooks = null;
		private static CallContext contextInfo;
		internal static OutputBuilder CurrentOutputBuilder;
		private static readonly ServiceInteractions ServiceInteractions = new();

		private static void Output(IntPtr destination, int outputSize, string data)
		{
			var bytes = Encoding.UTF8.GetBytes(data);
			Marshal.Copy(bytes, 0, destination, Math.Min(bytes.Length, outputSize));
		}

		private static void SetOutput(OutputBuilder Builder)
		{
			CurrentOutputBuilder = Builder;
		}

		public class OutputBuilder(IntPtr destination, int outputSize)
		{
			private readonly nint _destination = destination;
			private readonly int _outputSize = outputSize;

			/// <summary>
			/// Construct output buffer for Arma
			/// </summary>
			/// <param name="data">String data that will be output</param>
			public void Append(string data)
			{
				var bytes = Encoding.UTF8.GetBytes(data);
				Marshal.Copy(bytes, 0, _destination, Math.Min(bytes.Length, _outputSize));
			}
		}

		/// <summary>
		/// Gets called when Arma starts up and loads all extension.
		/// It's perfect to load in static objects in a separate thread so that the extension doesn't need any separate initialization
		/// </summary>
		/// <param name="outputPrt"></param>
		/// <param name="outputSize"></param>
		[UnmanagedCallersOnly(EntryPoint = "RVExtensionVersion")]
		public static void RVExtensionVersion(nint outputPrt, int outputSize)
		{
			//- Clean up logs
			Logger.CleanLogs();
			
			ServiceInteractions.AccessTokenReceived += (authTokenPayload) => 
				_ = ServiceInteractions.EstablishWebSocketConnection(authTokenPayload);
			
			_ = ServiceInteractions.GetAccessToken(accessName: "New Arma Server");
			
			Output(outputPrt, outputSize, "26.2.0");
		}
		
		/// <summary>
		/// Receives context information .
		/// </summary>from Arma 3 about the execution environment
		/// <param name="argsPtr">Pointer to the array of strings containing context data.</param>
		/// <param name="argCount">The number of arguments passed in the context.</param>
		[UnmanagedCallersOnly(EntryPoint = "RVExtensionContext")]
		public static void RVExtensionContext(nint argsPtr, int argCount)
		{
			var args = new string?[argCount];

			for (int i = 0; i < argCount; i++)
			{
				var str = Marshal.PtrToStringUTF8(Marshal.ReadIntPtr(argsPtr + (i * Marshal.SizeOf<nint>())));
				args[i] = str;
			}

			contextInfo.steamId = Convert.ToUInt64(args[0]);
			contextInfo.fileSource = args[1];
			contextInfo.missionName = args[2];
			contextInfo.serverName = args[3];
			contextInfo.remoteExecutedOwner = Convert.ToInt16(args[4]);
		}


		/// <summary>
		/// The entry point for the default callExtension command.
		/// </summary>
		/// <param name="outputPrt">The string builder object that contains the result of the function</param>
		/// <param name="outputSize">The maximum size of bytes that can be returned</param>
		/// <param name="function">The string argument that is used along with callExtension</param>
		[UnmanagedCallersOnly(EntryPoint = "RVExtension")]
		public static void RVExtension(nint outputPrt, int outputSize, nint function)
		{
			var inputKey = Marshal.PtrToStringUTF8(function)!;

			if (!string.IsNullOrEmpty(inputKey))
			{
				_ = ServiceInteractions._wsClient.SendMessageAsync(inputKey);
			}


			// - await will block the thread, makes the game stuttering - //
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
			//APIRequest.SendRequest("http://localhost:5000/api/Arma", inputKey);
#pragma warning restore CS4014

			Output(outputPrt, outputSize, inputKey);
		}

		/// <summary>
		/// The entry point for the callExtensionArgs command.
		/// </summary>
		/// <param name="outputPrt"></param>
		/// <param name="outputSize"></param>
		/// <param name="function"></param>
		/// <param name="argsPrt"></param>
		/// <param name="argCount"></param>
		/// <returns>
		///     numbers
		/// </returns>
		[UnmanagedCallersOnly(EntryPoint = "RVExtensionArgs")]
		public static int RvExtensionArgs(nint outputPrt, int outputSize, nint function, nint argsPrt, int argCount)
		{
			OutputBuilder output = new(outputPrt, outputSize);
			SetOutput(output);

			var inputKey = Marshal.PtrToStringUTF8(function);
			var args = new string?[argCount];

			for (int i = 0; i < argCount; i++)
			{
				var str = Marshal.PtrToStringUTF8(Marshal.ReadIntPtr(argsPrt + (i * Marshal.SizeOf<nint>())))?
					.Trim('"', ' ') //- Remove Arma quotations
					.Replace("\"\"", "\"");

				args[i] = str;
				Logger.Trace($"DLL Entry => \"{i}\"", $"\"str = {str}\"");
				//args = args.Select(arg => arg.Trim('"', ' ').Replace("\"\"", "\"")).ToArray();
			}

			try
			{
				Logger.Trace("DLL Entry", inputKey);
				InitActions action = ActionsDict.GetValueOrDefault(inputKey, EntryActions.NullDefault);
				int actionReturn = action(output, args, argCount);

				if (InitTime == null)
					throw new Exception($"Function \"{inputKey}\" is not exist.");

				// Use time as Key (for Server , Player)
				/*if (ExtensionInit && inputKey == "init_player")
				{
					output.Append("Extension has already been initiated.");
					return -1;
				}*/

				//Entry
				/*switch (inputKey == "init_player" || inputKey == "Refresh_Webhooks")
				{
					//- Init Functions 
					case true:
					{
							// Get all Webhooks
							if (inputKey == "Refresh_Webhooks")
							{
								string jsonString = Tools.ParseJson("Webhooks.json");

								ALLWebhooks = JsonSerializer.Deserialize<Webhooks_Storage>(
									jsonString,
									Webhooks_Storage_JsonContext.Default.Webhooks_Storage);


								int webhooksCount = ALLWebhooks!.Webhooks.Length;
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
				}*/

				return 0;
			}
			catch (Exception e)
			{
				output.Append($"Error!! \"{e.Message}\"");
				Logger.Log(e);

				return -11;
			}
		}
	}
}
