using System.Security.Cryptography;
using System.Text;

namespace DiscordMessageAPI
{
	internal class Tools
	{
		//public static readonly string? AssemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly()?.Location!);
		public static readonly string? AssemblyPath = Path.Combine(AppContext.BaseDirectory, "Discord_Message_API");
		//public static readonly string? AssemblyPath = Path.GetDirectoryName(AppContext.BaseDirectory);
		private static readonly string ExtFilePath = AssemblyPath;
		private static readonly string LogFilePath = Path.Combine(ExtFilePath, "logs");
		private static readonly byte[] Webkey = GenerateRandomWebKey();
		private static readonly bool debug = true;
		private static readonly string LogFileName = Path.Combine(
			LogFilePath,
			$"{DateTime.Now.ToString("yyyy-MM-dd.HH-mm-ss")}.DiscordMessageAPI.log");

		/// <summary>
		/// Writes a trace message to the logger if debugging is enabled.
		/// </summary>
		/// <remarks>No output is generated if debugging is not enabled. This method is intended for
		/// internal diagnostic purposes.</remarks>
		/// <param name="Name">The name or category associated with the trace message. Used to identify the source or context of the trace
		/// output.</param>
		/// <param name="content">The content of the trace message to be logged.</param>
		internal static void Trace(string Name, string content)
		{
			if (!debug) return;
			Logger(null, $"TRACER - {Name} : {content}");
		}
		internal static void Logger(Exception? e, string s = "", bool loop = false)
		{
			try
			{
				if (!Directory.Exists(ExtFilePath))
					Directory.CreateDirectory(ExtFilePath);
				if (!Directory.Exists(LogFilePath))
					Directory.CreateDirectory(LogFilePath);

				using (StreamWriter file = new StreamWriter(LogFileName, true))
				{
					if (string.IsNullOrEmpty(s))
						s = e!.Message;
					if (s.Length > 0)
						file.WriteLine($"{DateTime.Now.ToString("T")} - {s}");
				}
			}
			catch (Exception i)
			{
				if (!loop)
					Logger(i, null, true);
			}
		}
		internal static string ParseJson(string file)
		{
			// if no disk "dir" defined
			string dir = file.IndexOf(":") < 0 ? Path.Combine(AssemblyPath, file) : file;
			Trace("ParseJson => \"file\"", file);
			Trace("ParseJson => \"dir\"", dir);
			return File.ReadAllText(dir);
		}

		internal static int[] StringToCode32(string str)
		{
			int[] code32 = new int[str.Length];

			for (int i = 0; i < str.Length; i++)
			{
				// Get the Unicode code point of each character in the string
				code32[i] = char.ConvertToUtf32(str, i);

				// If the character is a surrogate pair, skip the next character
				if (char.IsHighSurrogate(str, i))
				{
					i++;
				}
			}

			return code32;
		}
		internal static string DecryptString(string cipherText)
		{
			byte[] fullCipher = Convert.FromBase64String(cipherText);
			using (Aes aesAlg = Aes.Create())
			{
				byte[] iv = new byte[aesAlg.BlockSize / 8];
				Array.Copy(fullCipher, iv, iv.Length);

				aesAlg.Key = Webkey;
				aesAlg.IV = iv;

				ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

				using (MemoryStream msDecrypt = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length))
				using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
				using (StreamReader srDecrypt = new StreamReader(csDecrypt))
				{
					return srDecrypt.ReadToEnd();
				}
			}
		}
		internal static string EncryptString(string plainText)
		{
			// Generate a new AES object with a random IV
			using (Aes aesAlg = Aes.Create())
			{
				aesAlg.Key = Webkey;
				aesAlg.GenerateIV();
				byte[] iv = aesAlg.IV;

				// Create an encryptor to perform the stream transform.
				ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

				using (MemoryStream msEncrypt = new MemoryStream())
				{
					// Write the IV to the memory stream
					msEncrypt.Write(iv, 0, iv.Length);

					using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
					using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
					{
						// Write all data to the stream.
						swEncrypt.Write(plainText);
					}

					byte[] encrypted = msEncrypt.ToArray();

					// Return the encrypted bytes from the memory stream as a base64 encoded string
					return Convert.ToBase64String(encrypted);
				}
			}
		}
		internal static byte[] GenerateRandomWebKey()
		{
			using (SHA256 sha256 = SHA256.Create())
			{
				string time;
				if (DllEntry.InitTime == null)
				{
					time = DateTime.Now.ToString("yyyy-MM-dd.HH-mm-ss");
					DllEntry.InitTime = time;
				}
				else
				{
					time = DllEntry.InitTime;
				}
				byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(time));
				byte[] key = new byte[32];
				Array.Copy(hash, key, key.Length);
				return key;
			}
		}

		/*internal static async Task LogAsyncReply(HttpContent responseContent)
        {
            string readResponse = "";
            using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync()))
            {
                readResponse += await reader.ReadToEndAsync();
            }
            if (readResponse.Length > 0) Logger(null, $"AsyncRet: {readResponse}");
        }*/
	}
}
