using System.Text.Json.Serialization;

namespace VolcEngineSdk.OpenSpeech.Contexts;

public class UserMeta
{
    [JsonPropertyName("uid")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Uid { get; set; }

    [JsonPropertyName("did")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Did { get; set; }

    [JsonPropertyName("platform")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Platform { get; set; }

    [JsonPropertyName("sdk_version")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string SDKVersion { get; set; }

    [JsonPropertyName("app_version")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string APPVersion { get; set; }
}