using System.Text.Json.Serialization;

namespace VolcEngineSdk.OpenSpeech.Contexts;

public class CorpusMeta
{
    [JsonPropertyName("boosting_table_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string BoostingTableName { get; set; }

    [JsonPropertyName("correct_table_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string CorrectTableName { get; set; }

    [JsonPropertyName("context")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Context { get; set; }
}

public class CorpusContext
{
    [JsonPropertyName("context_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string ContextType { get; set; } = "dialog_ctx";

    [JsonPropertyName("context_data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<CorpusContextData> ContextData { get; set; } = [];
}

public class CorpusContextData 
{
    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; set; }

    [JsonPropertyName("image_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ImageUrl { get; set; }
}