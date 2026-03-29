using System.Text.Json.Serialization;
using ServiceConnection.Tools;

namespace ServiceConnection.Entity;

public record struct WebhooksStorage
{
	private string[] _webhooks { get; set; }
	public string[] Webhooks
	{
		get => _webhooks;
		set => _webhooks = value.Select(Util.EncryptString).ToArray();
	}
};

[JsonSerializable(typeof(WebhooksStorage))]
public partial class WebhooksStorage_JsonContext : JsonSerializerContext;
