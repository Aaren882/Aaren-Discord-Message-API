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
    [JsonDerivedType(typeof(LabelComponent), (int)ComponentType.Label)]
    [JsonDerivedType(typeof(FileUploadComponent), (int)ComponentType.FileUpload)]
    public abstract record ComponentBase
    {
	    [JsonIgnore] 
	    public abstract ComponentType type { get; }
	    public abstract int? Id { get; set; }
	    public virtual IMessageComponentBuilder Convert()
		    => throw new NotImplementedException();
    }

    public record ActionRowComponent(
	    IEnumerable<ComponentBase> Components
	) : ComponentBase
    {
	    [JsonIgnore] 
	    public override ComponentType type => ComponentType.ActionRow;
	    public override int? Id { get; set; }
	    
	    public override ActionRowBuilder Convert()
		    => new (Components.Select(x => x.Convert()).ToArray(), Id);
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
	    
	    public override ButtonBuilder Convert()
		    => new (label, custom_id, style, url, emoji, disabled, sukId, Id);
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
	    
	    public override SelectMenuBuilder Convert()
	    {
		    var selectMenuOptionBuilders = options?.Select(x => new SelectMenuOptionBuilder(x.Label, x.Value, x.Description, x.emoji, x.Default)); 
		    return new SelectMenuBuilder(custom_Id, selectMenuOptionBuilders?.ToList(), placeholder, max_values, min_values, disabled, ComponentType.SelectMenu, channelTypes, defaultValues, Id, required);
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
	    
	    public override TextInputBuilder Convert()
		    => new (custom_id, style, placeholder, min_Length, max_Length, required, value, Id);
    }
    
    public record SectionComponent(
	    IEnumerable<ComponentBase> components,
	    ComponentBase? accessory
    ) : ComponentBase
    {
	    [JsonIgnore] 
		public override ComponentType type => ComponentType.Section;
	    public override int? Id { get; set; }
	    
	    public override SectionBuilder Convert()
		    => new (accessory?.Convert(), components.Select(x => x.Convert()), Id);
    }
    public record TextDisplayComponent(
	    string content
    ) : ComponentBase
    {
	    [JsonIgnore] 
		public override ComponentType type => ComponentType.TextDisplay;
	    public override int? Id { get; set; }
	    
	    public override TextDisplayBuilder Convert()
		    => new (content, Id);
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
	    
	    public override ThumbnailBuilder Convert()
		    => new (media, description, isSpoiler, Id);
    }
    
    public record MediaGalleryComponent(
	    IEnumerable<MediaGalleryItemProperties> items
    ) : ComponentBase
    {
	    [JsonIgnore] 
		public override ComponentType type => ComponentType.MediaGallery;
	    public override int? Id { get; set; }
	    
	    public override MediaGalleryBuilder Convert()
		    => new (items, Id);
    }
    public record FileComponent(
	    UnfurledMediaItemProperties file, 
		bool isSpoiler = false
    ) : ComponentBase
    {
	    [JsonIgnore] 
		public override ComponentType type => ComponentType.File;
	    public override int? Id { get; set; }
	    
	    public override FileComponentBuilder Convert()
		    => new (file, isSpoiler, Id);
    }
    public record SeparatorComponent(
		bool divider = true,
		SeparatorSpacingSize spacing = SeparatorSpacingSize.Small
    ) : ComponentBase
    {
	    [JsonIgnore] 
		public override ComponentType type => ComponentType.Separator;
	    public override int? Id { get; set; }
	    
	    public override SeparatorBuilder Convert()
		    => new (divider, spacing, Id);
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
	    
	    public override ContainerBuilder Convert()
		    => new (accentColor, isSpoiler, Id, components.Select(x => x.Convert()));
    }
    
    public record LabelComponent(
	    string label,
	    string? description,
	    ComponentBase component
    ) : ComponentBase
    {
	    [JsonIgnore] 
	    public override ComponentType type => ComponentType.Label;
		
	    public override int? Id { get; set; }

	    public override LabelBuilder Convert()
		    => new (label, component.Convert(), description, Id);
    }
    
    public record FileUploadComponent(
	    string Custom_Id,
	    bool Required = true,
	    int Min_Values = 1,
	    int Max_Values = 1
    ) : ComponentBase
    {
	    [JsonIgnore] 
	    public override ComponentType type => ComponentType.FileUpload;
		
	    public override int? Id { get; set; }
	    public override IInteractableComponentBuilder Convert()
			=> new FileUploadComponentBuilder(Custom_Id, Min_Values, Max_Values, Required, Id);
    }
    
    public record ModalComponent(
	    string Title,
		string custom_Id
	)
    {
	    public ComponentType type { get; }
	    public int? Id { get; set; }
	    public IEnumerable<ComponentBase>? components { get; set; }
	    public ComponentBase? component { get; set; }

	    public Modal Build()
	    {
		    var builder = (this) switch
		    {
			    { component: null, components: not null } =>
				    new ModalBuilder(
					    Title,
					    custom_Id,
					    components.Select(x => x.Convert().Build())
				    ),
			    { component: not null, components: null } =>
				    new ModalBuilder(
					    Title,
					    custom_Id,
					    component.Convert().Build()
				    ),
			    _ => throw new ArgumentOutOfRangeException()
		    };
		    return builder.Build();
	    }
    }
    
    public record PollMediaPropertiesDto
    {
	    public string Text { get; set; }
	    public Emote? Emoji { get; set; }
    }
    public record PollPropertiesDto
    {
	    public PollMediaPropertiesDto Question { get; set; }
	    public List<PollMediaPropertiesDto> Answers { get; set; }
	    public uint Duration { get; set; }
	    public bool AllowMultiselect { get; set; }
	    public PollLayout LayoutType { get; set; } = PollLayout.Default;

	    public PollProperties Build()
	    {
		    return new PollProperties
		    {
			    // Set the question
			    Question = new PollMediaProperties
			    {
				    Text = Question.Text,
				    Emoji = Question.Emoji,
			    },
			    Duration = Duration,
			    Answers = Answers.Select(x => 
				    new PollMediaProperties
				    {
					    Text = x.Text,
					    Emoji = x.Emoji,
				    }).ToList(),
			    AllowMultiselect = AllowMultiselect,
			    LayoutType = LayoutType
		    };
	    }
    }
}

public record DiscordMessageDto : DiscordMessage
{
	public string? FileName { get; set; }
	public MessageFlags Flags { get; set; } = MessageFlags.None;
	public IEnumerable<FileAttachment>? Attachments { get; set; }
	public IEnumerable<EmbedData>? Embeds { get; set; }
	public IReadOnlyCollection<DiscordDto.ComponentBase>? Components { get; set; }
	public DiscordDto.PollPropertiesDto? poll { get; set; }
	
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
			{
				List<EmbedFieldBuilder> fields = [];
				if (x.fields is not null)
				{
					fields = x.fields
						.Select(f => 
							new EmbedFieldBuilder
							{
								IsInline = f.inline,
								Name = f.name,
								Value = f.value
							}
						).ToList();
				}
				
				
				return new EmbedBuilder
				{
					Title = x.title,
					Description = x.description,
					Author = new EmbedAuthorBuilder
					{
						IconUrl = x.author.icon_url,
						Name = x.author.name,
						Url = x.author.url
					},
					ThumbnailUrl = x.thumbnail.url,
					ImageUrl = x.image.url,
					Fields = fields,
					Footer = new EmbedFooterBuilder
					{
						IconUrl = x.footer.icon_url,
						Text = x.footer.text
					},
					Timestamp = string.IsNullOrEmpty(x.timestamp) ? null : DateTime.Parse(x.timestamp),
					Color = string.IsNullOrEmpty(x.color) ? null : uint.Parse(x.color)
				}.Build();
			}
		).ToArray();
	}
	public PollProperties? ConvertPolls() => poll?.Build();
}

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true, AllowOutOfOrderMetadataProperties = true)] // Optional: Add desired options
[JsonSerializable(typeof(DiscordMessageDto))]
public partial class MsgPayload_JsonContext : JsonSerializerContext;
