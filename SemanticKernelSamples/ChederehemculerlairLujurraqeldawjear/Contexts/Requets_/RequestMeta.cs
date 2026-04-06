using System.Text.Json.Serialization;

namespace ChederehemculerlairLujurraqeldawjear.Contexts;

public class RequestMeta
{
    [JsonPropertyName("model_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string ModelName { get; set; }

    [JsonPropertyName("enable_itn")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool EnableITN { get; set; }

    [JsonPropertyName("enable_punc")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool EnablePUNC { get; set; }

    [JsonPropertyName("enable_ddc")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool EnableDDC { get; set; }

    [JsonPropertyName("show_utterancies")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool ShowUtterances { get; set; }

    [JsonPropertyName("corpus")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public CorpusMeta Corpus { get; set; }
}