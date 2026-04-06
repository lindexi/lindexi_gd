using System.Text.Json.Serialization;

namespace ChederehemculerlairLujurraqeldawjear.Contexts;

public class AsrRequest
{
    [JsonPropertyName("user")]
    public UserMeta User { get; set; }

    [JsonPropertyName("audio")]
    public AudioMeta Audio { get; set; }

    [JsonPropertyName("request")]
    public RequestMeta Request { get; set; }
}