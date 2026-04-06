using System.Text.Json.Serialization;

namespace VolcEngineSdk.OpenSpeech.Contexts;

/// <summary>
/// 分句识别单元
/// </summary>
public class Utterance
{
    /// <summary>
    /// 是否为确定性识别结果
    /// </summary>
    [JsonPropertyName("definite")]
    public bool Definite { get; set; }

    /// <summary>
    /// 分句结束时间（毫秒）
    /// </summary>
    [JsonPropertyName("end_time")]
    public int EndTime { get; set; }

    /// <summary>
    /// 分句开始时间（毫秒）
    /// </summary>
    [JsonPropertyName("start_time")]
    public int StartTime { get; set; }

    /// <summary>
    /// 分句文本
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; }

    /// <summary>
    /// 单字识别结果列表
    /// </summary>
    [JsonPropertyName("words")]
    public List<Word> Words { get; set; }
}