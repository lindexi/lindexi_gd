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
    public string Context { get; set; }
}

public class CorpusContext
{
    [JsonPropertyName("context_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string ContextType { get; set; } = "dialog_ctx";

    [JsonPropertyName("context_data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Dictionary<string, string> ContextData { get; set; } = [];
}