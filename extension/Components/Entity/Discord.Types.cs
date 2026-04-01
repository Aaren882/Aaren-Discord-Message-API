using System.Text.Json.Serialization;
using Discord;

namespace ServiceConnection.Discord;

public record struct DiscordMessage
{
	public string? Content { get; set; }
	public bool? Tts { get; set; }
	public string? Username { get; set; }
	public string? Avatar_Url { get; set; }
	public string? File { get; set; }

	public IEnumerable<EmbedData>? Embeds { get; set; }
	public IReadOnlyCollection<Types.IComponent>? Components { get; set; }
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

public sealed class Types
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

    public record struct SelectMenuOption(
	    string Label,
	    string Value,
	    string? Description,
	    Emote? emoji,
	    bool Default = false
    );

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(ActionRowComponent), (int)ComponentType.ActionRow)]
    [JsonDerivedType(typeof(ButtonComponent), (int)ComponentType.Button)]
    [JsonDerivedType(typeof(SelectMenuComponent), (int)ComponentType.SelectMenu)]
    [JsonDerivedType(typeof(TextInputComponent), (int)ComponentType.TextInput)]
    [JsonDerivedType(typeof(SectionComponent), (int)ComponentType.Section)]
    [JsonDerivedType(typeof(TextDisplayComponent), (int)ComponentType.TextDisplay)]
    [JsonDerivedType(typeof(ThumbnailComponent), (int)ComponentType.Thumbnail)]
    [JsonDerivedType(typeof(MediaGalleryComponent), (int)ComponentType.MediaGallery)]
    [JsonDerivedType(typeof(FileComponent), (int)ComponentType.File)]
    [JsonDerivedType(typeof(SeparatorComponent), (int)ComponentType.Separator)]
    [JsonDerivedType(typeof(ContainerComponent), (int)ComponentType.Container)]
    [JsonDerivedType(typeof(LabelComponent), (int)ComponentType.Label)]
    public abstract record IComponent
    {
	    [JsonIgnore] 
	    public abstract ComponentType type { get; }
	    public abstract int? Id { get; set; }
	    public abstract IMessageComponentBuilder Convert();
    }

    public record ActionRowComponent(
	    IEnumerable<IComponent> Components
	) : IComponent
    {
	    [JsonIgnore] 
	    public override ComponentType type => ComponentType.ActionRow;
	    public override int? Id { get; set; }
	    
	    public override IMessageComponentBuilder Convert()
	    {
		    return new ActionRowBuilder(Components.Select(x => x.Convert()).ToArray(), Id);
	    }
    }
    public record ButtonComponent(
	    ulong? sukId,
	    string? label,
	    string? custom_id,
	    IEmote? emoji,
	    string? url,
	    ButtonStyle style = ButtonStyle.Primary,
	    bool disabled = false
    ): IComponent
    {
	    [JsonIgnore] 
		public override ComponentType type => ComponentType.Button;
		public override int? Id { get; set; }
	    
	    public override IMessageComponentBuilder Convert()
	    {
		    return new ButtonBuilder(label, custom_id, style, url, emoji, disabled, sukId, Id);
	    }
    }
    
    public record SelectMenuComponent(
	    string custom_Id, 
	    string? placeholder,
	    List<SelectMenuOption>? options,
	    List<ChannelType>? channelTypes,
	    List<SelectMenuDefaultValue>? defaultValues,
	    int min_values = 1,
	    int max_values = 1,
	    bool required = true,
	    bool disabled = false
	): IComponent
    {
	    [JsonIgnore] 
		public override ComponentType type => ComponentType.SelectMenu;
	    public override int? Id { get; set; }
	    
	    public override IMessageComponentBuilder Convert()
	    {
		    var selectMenuOptionBuilders = options.Select(x => new SelectMenuOptionBuilder(x.Label, x.Value, x.Description, x.emoji, x.Default)); 
		    return new SelectMenuBuilder(custom_Id, selectMenuOptionBuilders.ToList(), placeholder, max_values, min_values, disabled, ComponentType.SelectMenu, channelTypes, defaultValues, Id, required);
	    }
    }

    public record TextInputComponent(
	    string custom_id,
	    string? placeholder,
	    int? min_Length,
	    int? max_Length,
	    bool? required,
	    string? value,
	    TextInputStyle style = TextInputStyle.Short
    ) : IComponent
    {
	    [JsonIgnore] 
		public override ComponentType type => ComponentType.TextInput;
	    public override int? Id { get; set; }
	    
	    public override IMessageComponentBuilder Convert()
	    { 
		    return new TextInputBuilder(custom_id, style, placeholder, min_Length, max_Length, required, value, Id);
	    }
    }
    
    public record SectionComponent(
	    IEnumerable<IComponent> components,
	    IComponent? accessory
    ) : IComponent
    {
	    [JsonIgnore] 
		public override ComponentType type => ComponentType.Section;
	    public override int? Id { get; set; }
	    
	    public override IMessageComponentBuilder Convert()
	    {
		    return new SectionBuilder(accessory?.Convert(), components.Select(x => x.Convert()), Id);
	    }
    }
    public record TextDisplayComponent(
	    string content
    ) : IComponent
    {
	    [JsonIgnore] 
		public override ComponentType type => ComponentType.TextDisplay;
	    public override int? Id { get; set; }
	    
	    public override IMessageComponentBuilder Convert()
	    {
		    return new TextDisplayBuilder(content, Id);
	    }
    }
    public record ThumbnailComponent(
	    UnfurledMediaItemProperties media, 
		string? description, 
		bool isSpoiler = false
    ) : IComponent
    {
	    [JsonIgnore] 
		public override ComponentType type => ComponentType.Thumbnail;
	    public override int? Id { get; set; }
	    
	    public override IMessageComponentBuilder Convert()
	    {
		    return new ThumbnailBuilder(media, description, isSpoiler, Id);
	    }
    }
    
    public record MediaGalleryComponent(
	    IEnumerable<MediaGalleryItemProperties> items
    ) : IComponent
    {
	    [JsonIgnore] 
		public override ComponentType type => ComponentType.MediaGallery;
	    public override int? Id { get; set; }
	    
	    public override IMessageComponentBuilder Convert()
	    {
		    return new MediaGalleryBuilder(items, Id);
	    }
    }
    public record FileComponent(
	    UnfurledMediaItemProperties file, 
		bool isSpoiler = false
    ) : IComponent
    {
	    [JsonIgnore] 
		public override ComponentType type => ComponentType.File;
	    public override int? Id { get; set; }
	    
	    public override IMessageComponentBuilder Convert()
	    {
		    return new FileComponentBuilder(file, isSpoiler, Id);
	    }
    }
    public record SeparatorComponent(
		bool divider = true,
		SeparatorSpacingSize spacing = SeparatorSpacingSize.Small
    ) : IComponent
    {
	    [JsonIgnore] 
		public override ComponentType type => ComponentType.Separator;
	    public override int? Id { get; set; }
	    
	    public override IMessageComponentBuilder Convert()
	    {
		    return new SeparatorBuilder(divider, spacing, Id);
	    }
    }
    
    public record ContainerComponent(
		IEnumerable<IComponent> components,
	    Color? accentColor, 
		bool? isSpoiler
    ) : IComponent
    {
	    [JsonIgnore] 
		public override ComponentType type => ComponentType.Container;
	    public override int? Id { get; set; }
	    
	    public override IMessageComponentBuilder Convert()
	    {
		    return new ContainerBuilder(accentColor, isSpoiler, Id, components.Select(x => x.Convert()));
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
