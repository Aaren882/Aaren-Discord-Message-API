using System.Text;
using System.Text.Json.Serialization;
namespace DiscordMessageAPI
{

    public class Types
    {
        public class EmbedData
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public string Color { get; set; }
            public string timestamp { get; set; }
            public string AuthorName { get; set; }
            public string AuthorUrl { get; set; }
            public string AuthorIconUrl { get; set; }
            public string ImageUrl { get; set; }
            public string ThumbnailUrl { get; set; }
            public string FooterText { get; set; }
            public string FooterIconUrl { get; set; }
            public List<Types.FieldData> Fields { get; set; } = new List<Types.FieldData>();

            string RandomColor()
            {
                Random random = new Random();
                int red = random.Next(256);
                int green = random.Next(256);
                int blue = random.Next(256);

                // Combine red, green, and blue into a single 24-bit integer
                return $"{(red << 16) | (green << 8) | blue}";
            }

            public string BuildFields()
            {
                var result = new StringBuilder();

                foreach (var Field in this.Fields)
                {
                    result.Append($@"{{
                        ""name"": ""{Field.name}"",
                        ""value"": ""{Field.value}"",
                        ""inline"": ""{Field.inline}""
                    }},");
                }

                return result.ToString().Trim(',');
            }

            public EmbedData(List<string> data)
            {
                Title = data.Count > 0 ? (string)data[0] : "";
                Description = data.Count > 1 ? (string)data[1] : "";
                Color = data.Count > 2 ? (string)data[2] : "14177041";

                if (data.Count > 2)
                {
                    Color = data[2] != "" ? "" : RandomColor();
                }
                else
                {
                    Color = RandomColor();
                }

                if (data.Count > 3)
                {
                    timestamp = data[3].ToLower() == "true" ? DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") : "";
                }
                else
                {
                    timestamp = "";
                }

                AuthorName = data.Count > 4 ? (string)data[4] : "";
                AuthorUrl = data.Count > 5 ? (string)data[5] : "";
                AuthorIconUrl = data.Count > 6 ? (string)data[6] : "";
                ImageUrl = data.Count > 7 ? (string)data[7] : "";
                ThumbnailUrl = data.Count > 8 ? (string)data[8] : "";
                FooterText = data.Count > 9 ? (string)data[9] : "";
                FooterIconUrl = data.Count > 10 ? (string)data[10] : "";

                if (data.Count > 11)
                {
                    for (int i = 11; i < data.Count; i = i + 3)
                    {
                        List<string> FieldStrings = new List<string>();
                        for (int j = 0; j < 3; j++)
                        {
                            FieldStrings.Add(data[i + j]);
                        }
                        Fields.Add(new Types.FieldData(FieldStrings));
                    }
                }
            }
        }

        public class FieldData
        {

            public string name { get; set; }
            public string value { get; set; }
            public string inline { get; set; }

            public FieldData(List<string> data)
            {
                name = data.Count > 0 ? (string)data[0] : "";
                value = data.Count > 1 ? (string)data[1] : "";

                if (data.Count > 2)
                    inline = data[2].ToLower() == "true" ? "true" : "false";
                else
                    inline = "false";
            }
        }
    }
    public class Webhooks_Storage
    {
        private string[] _Webhooks;
        public string[] Webhooks
        {
            get => _Webhooks;
            set
            {
                value = value.Select(data => Tools.EncryptString(data)).ToArray();
                _Webhooks = value;
            }
        }
    }

    public class JSON_MessageTypes
    {
        class JSON_Message
        {
            public string username { get; set; }
            public string avatar_url { get; set; }
            public string content { get; set; }
            public List<Embeds> embeds { get; set; }
        };

        class Embeds
        {
            public author author { get; set; }
            public string title { get; set; }
            public string url { get; set; }
            public string description { get; set; }
            public int color { get; set; }
            public List<Field> fields { get; set; }
            public thumbnail thumbnail { get; set; }
            public image image { get; set; }
            public footer footer { get; set; }
        }
        class Field
        {
            public string name { get; set; }
            public string value { get; set; }
            public bool inline { get; set; }
        }
        class image
        {
            public string url { get; set; }
        }
        class thumbnail : image { }
        class author : image
        {
            public string name { get; set; }
            public string icon_url { get; set; }
        }
        class footer
        {
            public string text { get; set; }
            public string icon_url { get; set; }
        }
    }

    public class MsgPayload
    {
        public required string Url { get; set; }
        public required int HandlerType { get; set; }
        public string? MessageID { get; set; }
    }

    [JsonSerializable(typeof(Webhooks_Storage))]
    internal partial class Webhooks_Storage_JsonContext : JsonSerializerContext
    {
    }

    [JsonSerializable(typeof(JSON_MessageTypes))]
    internal partial class MessageTypes_JsonContext : JsonSerializerContext
    {
    }

    [JsonSerializable(typeof(MsgPayload))]
    internal partial class MsgPayload_JsonContext : JsonSerializerContext
    {
    }

}
