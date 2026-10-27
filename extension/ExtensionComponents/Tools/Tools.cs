using System.Security.Cryptography;
using System.Text;
using Components.Entity;
using ExtensionComponents.Entity;
using static ExtensionComponents.ExtensionStartup;

namespace ExtensionComponents.Tools;

public static class Util
{
	public static readonly string AssemblyPath = Path.Combine(AppContext.BaseDirectory, "Discord_Message_API");
	private static byte[] Webkey = GenerateRandomWebKey();

	public static string ParseJson(string file)
	{
		// if no disk "dir" defined
		var dir = file.IndexOf(':') < 0 ? Path.Combine(AssemblyPath, file) : file;
		return File.ReadAllText(dir);
	}
	public static ReadOnlyMemory<byte> ParseJsonMemory(string file)
	{
		// if no disk "dir" defined
		var dir = file.IndexOf(':') < 0 ? Path.Combine(AssemblyPath, file) : file;
		var bytes = File.ReadAllBytes(dir);
		return bytes.AsMemory<byte>();
	}

	public static string DecryptString(string cipherText, byte[]? key = null)
	{
		var fullCipher = Convert.FromBase64String(cipherText);

		using var aesAlg = Aes.Create();

		var iv = new byte[aesAlg.BlockSize / 8];
		Array.Copy(fullCipher, iv, iv.Length);

		aesAlg.Key = key ?? Webkey;
		aesAlg.IV = iv;

		var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

		using var msDecrypt = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length);
		using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
		using var srDecrypt = new StreamReader(csDecrypt);
		return srDecrypt.ReadToEnd();
	}
	public static string EncryptString(string plainText, byte[]? key = null)
	{
		// Generate a new AES object with a random IV
		using var aesAlg = Aes.Create();
		aesAlg.Key = key ?? Webkey;
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
	public static void RefreshKey()
	{
		Logger(null, "Refreshing Key.");
		Webkey = GenerateRandomWebKey();
	}
	private static byte[] GenerateRandomWebKey()
	{
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
		Tracer(nameof(GenerateRandomWebKey), time);

		var hash = SHA256.HashData(Encoding.UTF8.GetBytes(time))[..32];

		return hash;
	}

	public static int CallExtensionCallback(this ExtensionCallback extensionCallback, Arma3Payload payload)
	{
		var data = payload.ToJsonString();
		return extensionCallback("DISCORD_API", ((int)payload.Type).ToString(), data);
	}
}
