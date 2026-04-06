using System.Text.Json.Serialization;

namespace ChederehemculerlairLujurraqeldawjear.Contexts;

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