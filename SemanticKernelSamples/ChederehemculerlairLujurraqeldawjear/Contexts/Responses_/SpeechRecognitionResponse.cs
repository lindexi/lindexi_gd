using System.Text.Json.Serialization;

namespace ChederehemculerlairLujurraqeldawjear.Contexts;

/// <summary>
/// 语音识别响应根对象
/// </summary>
public class SpeechRecognitionResponse
{
    /// <summary>
    /// 音频信息
    /// </summary>
    [JsonPropertyName("audio_info")]
    public AudioInfo AudioInfo { get; set; }

    /// <summary>
    /// 识别结果
    /// </summary>
    [JsonPropertyName("result")]
    public RecognitionResult Result { get; set; }
}