using System.Security.Cryptography;
using System.Text;
using Components.Entity;
using ServiceConnection.Entity;
using static ServiceConnection.ServiceStartup;

namespace ServiceConnection.Tools;

public class Util
{
	public static readonly string AssemblyPath = Path.Combine(AppContext.BaseDirectory, "Discord_Message_API");
	private static readonly byte[] Webkey = GenerateRandomWebKey();

	public static string ParseJson(string file)
	{
		// if no disk "dir" defined
		var dir = file.IndexOf(":") < 0 ? Path.Combine(AssemblyPath, file) : file;
		return File.ReadAllText(dir);
	}

	public static int[] StringToCode32(string str)
	{
		var code32 = new int[str.Length];

		for (var i = 0; i < str.Length; i++)
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
		var fullCipher = Convert.FromBase64String(cipherText);
	
		using var aesAlg = Aes.Create();
	
		var iv = new byte[aesAlg.BlockSize / 8];
		Array.Copy(fullCipher, iv, iv.Length);

		aesAlg.Key = Webkey;
		aesAlg.IV = iv;

		var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

		using var msDecrypt = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length);
		using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
		using var srDecrypt = new StreamReader(csDecrypt);
		return srDecrypt.ReadToEnd();
	}
	internal static string EncryptString(string plainText)
	{
		// Generate a new AES object with a random IV
		using var aesAlg = Aes.Create();
		aesAlg.Key = Webkey;
		aesAlg.GenerateIV();
		var iv = aesAlg.IV;

		// Create an encryptor to perform the stream transform.
		using var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

		using var msEncrypt = new MemoryStream();
		// Write the IV to the memory stream
		msEncrypt.Write(iv, 0, iv.Length);

		using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
		using (var swEncrypt = new StreamWriter(csEncrypt))
		{
			// Write all data to the stream.
			swEncrypt.Write(plainText);
		}

		var encrypted = msEncrypt.ToArray();

		// Return the encrypted bytes from the memory stream as a base64 encoded string
		return Convert.ToBase64String(encrypted);
	}
	private static byte[] GenerateRandomWebKey()
	{
		using var sha256 = SHA256.Create();
		string time;
		if (InitTime is null)
		{
			time = DateTime.Now.ToString("yyyy-MM-dd.HH-mm-ss");
			InitTime = time;
		}
		else
		{
			time = InitTime;
		}
		var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(time));
		var key = new byte[32];
		Array.Copy(hash, key, key.Length);
		return key;
	}

	public static string GetLastestFile(string path)
	{
		var files = Directory.GetFiles(path);

		//- Check how many logs
		Dictionary<string, DateTime> dict = new();
		foreach (var file in files)
		{
			var time = Directory.GetCreationTime(file);
			dict.Add(file, time);
		}

		var list = dict.OrderByDescending(x => x.Value).ToList();
	
		return list[0].Key;
	}

	public static int CallExtensionCallback(ExtensionCallback extensionCallback, Arma3PayloadCallBack? callBack)
	{
		return extensionCallback("DISCORD_API", callBack.Function, callBack.Data);
	}

}
