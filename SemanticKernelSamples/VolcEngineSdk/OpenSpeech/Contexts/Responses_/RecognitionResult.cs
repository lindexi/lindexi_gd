using System.Text.Json.Serialization;

namespace VolcEngineSdk.OpenSpeech.Contexts;

/// <summary>
/// 核心识别结果
/// </summary>
public class RecognitionResult
{
    /// <summary>
    /// 完整识别文本
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; }

    /// <summary>
    /// 分句识别结果列表
    /// </summary>
    [JsonPropertyName("utterances")]
    public List<Utterance> Utterances { get; set; }
}