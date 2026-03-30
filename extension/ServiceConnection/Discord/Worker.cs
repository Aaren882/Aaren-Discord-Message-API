using System.Text;
using System.Text.Json;
using DiscordMessageAPI.ServiceConnection.WebService;
using ServiceConnection.Tools;
using static ServiceConnection.LocalServices;

namespace ServiceConnection.Discord;

public static class Worker
{
	public static async Task HandlerJson(string[] args)
	{
		//- ["url","json"]
		var json = Util.ParseJson(args[1]);
		
		using var package = new MultipartFormDataContent();

		package.Add(new StringContent(json, Encoding.UTF8), "payload_json");
		await DiscordMsg(args[0], package);
	}
	public static async Task HandlerJsonFormat(string[] args)
	{
		//- ["url","json"]
		var json = args[1];

		using var package = new MultipartFormDataContent();
		ServiceStartup.Tracer("HandlerJsonFormat", json);
		package.Add(new StringContent(json, Encoding.UTF8), "payload_json");
		await DiscordMsg(args[0], package);
	}
	public static async Task HandleRequest(string[] args)
	{
		using var package = new MultipartFormDataContent();
		var content = args[1];
		var username = args[2];
		var avatar = args[3];
		var tts = args[4];

		//- File Stream
		var filePath = args[5];

		// Discord 2000 character limit
		if (content.Length > 1999) content = content.Substring(0, 1999);

		// Build embeds array
		//- Turn Data into List<List<string>> e.g [ ["TITLE","DESC"] , ["11","22] ]
		var embedsData = ParseStringToList(args[6]);
		var fieldsData = ParseStringToList(args[7].Replace("[[]]", ""));

		foreach (var embed in embedsData)
		{
			embed.Resize(11, "");
			foreach (var field in fieldsData)
			{
				field.Resize(3, "");
				embed.AddRange(field);
			}
		}
		ServiceStartup.Tracer("HandleRequest (fieldsData)", args[7]);
		//- pass Data into "class Types.EmbedData"
		var embeds = embedsData.Select(data =>
			new EmbedData(data, fieldsData)
		).ToList();

		// Prepare the embeds JSON data
		var embedsJson = BuildEmbedsJson(embeds);
		ServiceStartup.Tracer("HandleRequest (embedsJson)", embedsJson);

		// Bare bones
		package.Add(new StringContent(content), "content");
		package.Add(new StringContent(tts), "tts");

		//- Send File .png
		if (filePath.Length > 0)
		{
			ServiceStartup.Tracer("HandleRequest [filePath] : ", filePath);
			filePath = Path.GetFullPath(filePath);
				
			await using var fileStream = new FileStream(filePath, FileMode.Open);
			var fileBytes = new byte[fileStream.Length];
				
			await fileStream.ReadAsync(fileBytes, 0, fileBytes.Length);
			package.Add(new ByteArrayContent(fileBytes), "file", filePath);
		}
		if (username.Length > 0) package.Add(new StringContent(username), "username");
		if (avatar.Length > 0) package.Add(new StringContent(avatar), "avatar_url");
		if (embeds.Count > 0) package.Add(new StringContent(embedsJson, Encoding.UTF8), "payload_json");

		await DiscordMsg(args[0], package);
	}

	private static async Task DiscordMsg(string handlerPayload, MultipartFormDataContent package)
	{
		ServiceStartup.Tracer("DiscordMsg => \"handlerPayload\"", handlerPayload);
        ServiceStartup.Tracer("DiscordMsg => \"package\"", package.ToString()!);

        //- [ Handler<int> , Required Payload<object> ]
        var handlerType = JsonSerializer.Deserialize(handlerPayload, MsgPayload_JsonContext.Default.MsgPayload);

		ServiceStartup.Tracer("DiscordMsg", "========================");
		ServiceStartup.Tracer("DiscordMsg => \"Url\"", handlerType!.Url);
        ServiceStartup.Tracer("DiscordMsg => \"HandlerType\"", handlerType.HandlerType.ToString());
		ServiceStartup.Tracer("DiscordMsg => \"MessageID\"", handlerType.MessageID!);
        var url = handlerType.Url;

        url = Util.DecryptString(url);

		switch (handlerType!)
		{
			case { HandlerType : 1 }: //- Http(s) (Patch) request for Editing Message 
			{
				_ = await APIRequest.PatchRequest($"https://discord.com/api/webhooks/{url}/messages/{handlerType.MessageID}", package);
				break;
			}
			default:
			{
				_ = await APIRequest.PostRequest($"https://discord.com/api/webhooks/{url}", package);
				break;
			}
		}
	}


	private static void Resize<T>(this List<T> list, int sz, T c)
	{
		var cur = list.Count;
		if (sz < cur)
			list.RemoveRange(sz, cur - sz);
		else if (sz > cur)
		{
			if (sz > list.Capacity) //this bit is purely an optimization, to avoid multiple automatic capacity changes.
				list.Capacity = sz;
			list.AddRange(Enumerable.Repeat(c, sz - cur));
		}
	}

	//- Translating Data
	private static List<List<string>> ParseStringToList(string input)
	{
		var result = new List<List<string>>();

		if (!input.StartsWith("[[") || !input.EndsWith("]]")) return result;

		input = input.Substring(2, input.Length - 4);
		var innerLists = input.Split(
			["],["], 
			StringSplitOptions.None
		);

		foreach (var innerList in innerLists)
		{
			var elements = innerList.Split(',')
				.Select(e => e.Trim(' '));
		
			var innerResult = elements.Select(
				element => element.Trim('"', '[', ']')
			).ToList();

			result.Add(innerResult);
		}

		return result;
	}

	private static string BuildEmbedsJson(List<EmbedData> embeds)
	{
		var embedsJson = new StringBuilder();
		embedsJson.Append("{ \"embeds\": ");
		ServiceStartup.Tracer("BuildEmbedsJson (embeds.Count)", $"{embeds.Count}");
		embedsJson.Append(
			JsonSerializer.Serialize(
				embeds,
				MsgPayload_JsonContext.Default.ListEmbedData
			)
		);
		embedsJson.Append("}");

		return embedsJson.ToString();
	}
}
