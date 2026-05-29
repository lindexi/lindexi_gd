namespace MiniMaxSdk.Music.Models;

/// <summary>
/// MiniMax 音乐生成接口支持的音频编码格式。
/// </summary>
public static class MiniMaxMusicAudioFormats
{
    /// <summary>
    /// MP3 编码格式。
    /// </summary>
    public const string Mp3 = "mp3";

    /// <summary>
    /// WAV 编码格式。
    /// </summary>
    public const string Wav = "wav";

    /// <summary>
    /// PCM 编码格式。
    /// </summary>
    public const string Pcm = "pcm";

    /// <summary>
    /// 判断指定音频编码格式是否受支持。
    /// </summary>
    /// <param name="format">待判断的编码格式。</param>
    /// <returns>如果受支持则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    public static bool IsSupported(string format) => format is Mp3 or Wav or Pcm;
}
