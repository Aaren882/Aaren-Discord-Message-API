using System.Text.Json.Serialization;

namespace Component.DiscordEntity;

public record DiscordMessage
{
	public string? Content { get; set; }
	public bool? Tts { get; set; }
	public string? Username { get; set; }
	public string? Avatar_Url { get; set; }
	public string? File { get; set; }
};

public record EmbedData
{
	public string title { get; init; }
	public string description { get; init; }

	private string? _color;
	public string? color
	{
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
	public Types.AuthorEmbed? author { get; init; }
	public Types.Image? image { get; init; }
	public Types.Thumbnail? thumbnail { get; init; }
	public Types.Footer? footer { get; init; }
	public List<Types.FieldEmbed>? fields { get; init; }
	
	[JsonConstructor]
	public EmbedData() : this([], []){ }

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
			footer = new Types.Footer(data[9], data[10]);

		if (data.Count <= 11) return;

		fields = fieldsData
			.ConvertAll(field => new Types.FieldEmbed(field));
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

public sealed class Types
{
	public record Image(string url);

	public record Thumbnail(string url);
	public record Footer(string text, string? icon_url)
	{
		[JsonConstructor]
		public Footer() : this(default!, default) { }
	}

	public record AuthorEmbed(string name, string? url, string? icon_url)
	{
		[JsonConstructor]
		public AuthorEmbed() : this(default!, default, default) { }
	}
	public record FieldEmbed
	{
		public string name { get; set; }
		public string value { get; set; }
		public bool inline { get; set; }
		
		[JsonConstructor]
		public FieldEmbed(): this([]) { }

		public FieldEmbed(List<string> data)
		{
			name = data.Count > 0 ? data[0] : "";
			value = data.Count > 1 ? data[1] : "";

			inline = data.Count > 2 &&
					 string.Equals(data[2], "true", StringComparison.OrdinalIgnoreCase);
		}
	}
}

public record struct MsgPayload(string Url, int HandlerType, string? MessageID);


