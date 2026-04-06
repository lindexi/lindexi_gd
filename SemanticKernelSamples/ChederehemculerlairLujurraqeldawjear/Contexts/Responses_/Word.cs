using System.Text.Json.Serialization;

namespace ChederehemculerlairLujurraqeldawjear.Contexts;

/// <summary>
/// 单字识别单元
/// </summary>
public class Word
{
    /// <summary>
    /// 字间空白时长（毫秒）
    /// </summary>
    [JsonPropertyName("blank_duration")]
    public int BlankDuration { get; set; }

    /// <summary>
    /// 单字结束时间（毫秒）
    /// </summary>
    [JsonPropertyName("end_time")]
    public int EndTime { get; set; }

    /// <summary>
    /// 单字开始时间（毫秒）
    /// </summary>
    [JsonPropertyName("start_time")]
    public int StartTime { get; set; }

    /// <summary>
    /// 单字文本
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; }
}