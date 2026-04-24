using System.Text.Json.Serialization;
using Discord;
using ServiceConnection.Discord;

namespace Arma3WebService.Entity;

public class DiscordDto
{
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
    public abstract record ComponentBase
    {
	    [JsonIgnore] 
	    public abstract ComponentType type { get; }
	    public abstract int? Id { get; set; }
	    public abstract IMessageComponentBuilder Convert();
    }

    public record ActionRowComponent(
	    IEnumerable<ComponentBase> Components
	) : ComponentBase
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
	    Emote? emoji,
	    string? url,
	    ButtonStyle style = ButtonStyle.Primary,
	    bool disabled = false
    ): ComponentBase
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
	): ComponentBase
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
    ) : ComponentBase
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
	    IEnumerable<ComponentBase> components,
	    ComponentBase? accessory
    ) : ComponentBase
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
    ) : ComponentBase
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
    ) : ComponentBase
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
    ) : ComponentBase
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
    ) : ComponentBase
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
    ) : ComponentBase
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
		IEnumerable<ComponentBase> components,
	    Color? accentColor, 
		bool? isSpoiler
    ) : ComponentBase
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

public record DiscordMessageDto : DiscordMessage
{
	public string? FileName { get; set; }
	public MessageFlags? Flags { get; set; }
	public IEnumerable<FileAttachment>? Attachments { get; set; }
	public IEnumerable<EmbedData>? Embeds { get; set; }
	public IReadOnlyCollection<DiscordDto.ComponentBase>? Components { get; set; }
	
	public MessageComponent? ConvertComponents()
	{
		if (Components is null) return null;

		return new ComponentBuilderV2(
			Components
				.Select(x => x.Convert())
		).Build();
	}
	public Embed[]? ConvertEmbeds()
	{
		return Embeds?.Select(x => 
			new EmbedBuilder
			{
				Author = new EmbedAuthorBuilder
				{
					IconUrl	= x.author.icon_url,
					Name = x.author.name,
					Url = x.author.url
				},
				ThumbnailUrl = x.thumbnail.url,
				ImageUrl = x.image.url,
				Description = x.description,
				Fields = x.fields
					.Select(f => new EmbedFieldBuilder
					{
						IsInline = f.inline,
						Name = f.name,
						Value = f.value 
					})
					.ToList(),
				Footer = new EmbedFooterBuilder
				{
					IconUrl	= x.footer.icon_url,
					Text = x.footer.text
				}
			}.Build()
		).ToArray();
	}
}

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true)] // Optional: Add desired options
[JsonSerializable(typeof(DiscordMessageDto))]
public partial class MsgPayload_JsonContext : JsonSerializerContext;
