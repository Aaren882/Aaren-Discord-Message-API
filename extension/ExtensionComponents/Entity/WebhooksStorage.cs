using System.Collections.Immutable;
using System.Text.Json.Serialization;
using ExtensionComponents.Tools;

namespace ExtensionComponents.Entity;

public record WebhooksStorage
{
	private ImmutableArray<string> _webhooks;
	public ImmutableArray<string> Webhooks
	{
		get => _webhooks;
		set => _webhooks = [.. value.Select(x => Util.EncryptString(x))];
	}
};

[JsonSerializable(typeof(WebhooksStorage))]
public partial class WebhooksStorage_JsonContext : JsonSerializerContext;
