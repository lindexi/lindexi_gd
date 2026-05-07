using VolcEngineSdk.OpenSpeech;

namespace KadefihalldokaiChairwedone.CoursewareSpeechGenerators;

/// <summary>
/// 语音合成配置。
/// </summary>
/// <param name="Authentication">鉴权信息。</param>
/// <param name="Speaker">发音人。</param>
/// <param name="Model">模型版本。</param>
/// <param name="AudioFormat">输出音频格式。</param>
/// <param name="SampleRate">输出采样率。</param>
/// <param name="UsageTokensToReturn">返回的用量标记。</param>
public record CoursewareSpeechSynthesisOptions(
    OpenSpeechAuthentication Authentication,
    string Speaker,
    string Model,
    string AudioFormat = "mp3",
    int SampleRate = 24000,
    string UsageTokensToReturn = "text_words");