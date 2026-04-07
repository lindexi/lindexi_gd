using System.Text.Json.Serialization;

namespace VolcEngineSdk.OpenSpeech.Contexts;

/// <summary>
/// 音频基础信息
/// </summary>
public class AudioInfo
{
    /// <summary>
    /// 音频总时长（毫秒）
    /// </summary>
    [JsonPropertyName("duration")]
    public int Duration { get; set; }
}