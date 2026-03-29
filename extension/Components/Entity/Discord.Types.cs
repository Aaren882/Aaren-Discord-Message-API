using System.Text.Json.Serialization;

namespace ServiceConnection.Discord;

public record DiscordMessage
{
	public string? Content { get; set; }
	public bool? Tts { get; set; }
	public string? Username { get; set; }
	public string? Avatar_Url { get; set; }
	public string? File { get; set; }

	public List<EmbedData>? Embeds { get; set; }
	/*private List<EmbedData>? _embeds;

	public string? Embeds
	{
		get => JsonSerializer.Serialize(
			_embeds,
			MsgPayload_JsonContext.Default.ListEmbedData
		);
	}

	public DiscordMessage(
		string? Content,
		bool? Tts,
		string? Username,
		string? AvatarUrl,
		string? File,
		List<EmbedData>? Embeds
	)
	{
		this.Content = Content;
		this.Tts = Tts;
		this.Username = Username;
		this.AvatarUrl = AvatarUrl;
		this.File = File;
		_embeds = Embeds;
	}*/

	/*public async Task<MultipartFormDataContent> GetMultipartContent()
	{
		var form = new MultipartFormDataContent();
		var dictionary = this.GetType()
			.GetProperties(Instance | Public)
			.ToDictionary(prop =>
				{
					return prop switch
					{
						{ Name: "Embeds" } => "payload_json",
						{ Name: "File" } => "file",
						_ => prop.Name
					};
				},
				prop =>
				{
					var value = prop.GetValue(this);
					return prop switch
					{
						{ Name: "Embeds" } => "payload_json",
						_ => value
					};
				}
			);
		
		if (File is not null)
		{
			var filePath = Path.GetFullPath(File);
					
			await using var fileStream = new FileStream(filePath, FileMode.Open);
			var fileBytes = new byte[fileStream.Length];
					
			await fileStream.ReadAsync(fileBytes, 0, fileBytes.Length);
			form.Add(new ByteArrayContent(fileBytes), "file", File);
		}
		foreach (var kvp in dictionary)
		{
			form.Add(new StringContent(kvp.Value), kvp.Key);
		}
		
		return form;
	}*/
};

public record struct EmbedData
{
	public string title { get; init; }
    public string description { get; init; }
    
    private string _color;
    public string color { 
	    get => _color; 
	    set => _color = string.IsNullOrEmpty(value) ?
		    RandomColor() : 
		    value;
    }

	private readonly string? _timestamp;
    public string? timestamp
    {
	    get => _timestamp;
	    init => _timestamp = string.Equals(
			    value?.Trim(),
			    "true",
			    StringComparison.OrdinalIgnoreCase
			) ? 
		    DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") : 
		    value;
    }
    public Types.AuthorEmbed author { get; init; }
    public Types.Image image { get; init; }
    public Types.Thumbnail thumbnail { get; init; }
    public Types.Footer footer { get; init; }
    public List<Types.FieldEmbed> fields { get; init; }

    public EmbedData(List<string> data, List<List<string>> fieldsData)
    {
        title = data.Count > 0 ? data[0] : "";
        description = data.Count > 1 ? data[1] : "";

        if (data.Count > 2)
        {
            color = data[2];
        }

        if (data.Count > 3)
        {
	        timestamp = data[3];
        }

        if (data.Count > 4)
			author = new Types.AuthorEmbed(data[4], data[5], data[6]);
        if (data.Count > 7)
			image = new Types.Image(data[7]);
        if (data.Count > 8)
			thumbnail = new Types.Thumbnail(data[8]);
        if (data.Count > 9)
			footer = new Types.Footer(data[9],data[10]);

        if (data.Count <= 11) return;
        
        fields = fieldsData
            .Select(field => new Types.FieldEmbed(field))
            .ToList();
    }
    
    private static string RandomColor()
    {
        var random = new Random();
        var red = random.Next(256);
        var green = random.Next(256);
        var blue = random.Next(256);

        // Combine red, green, and blue into a single 24-bit integer
        return $"{(red << 16) | (green << 8) | blue}";
    }
}

public record struct Types
{
    public readonly record struct Image(string? url);
    public readonly record struct Thumbnail(string? url);
    public readonly record struct Footer(string? text, string? icon_url);
    public readonly record struct AuthorEmbed(string? name, string? url, string? icon_url);
    public record struct FieldEmbed
    {
        public string name { get; set; }
        public string value { get; set; }
        public bool inline { get; set; }

        public FieldEmbed(List<string> data)
        {
            name = data.Count > 0 ? data[0] : "";
            value = data.Count > 1 ? data[1] : "";

            inline = data.Count > 2 &&
                     string.Equals(data[2],"true", StringComparison.OrdinalIgnoreCase);
        }
    }
}

public record struct MsgPayload(string Url, int HandlerType, string? MessageID);

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true)] // Optional: Add desired options
[
	JsonSerializable(typeof(MsgPayload)),
	JsonSerializable(typeof(DiscordMessage)),
	JsonSerializable(typeof(EmbedData)),
	JsonSerializable(typeof(List<EmbedData>))
]
public partial class MsgPayload_JsonContext : JsonSerializerContext;
