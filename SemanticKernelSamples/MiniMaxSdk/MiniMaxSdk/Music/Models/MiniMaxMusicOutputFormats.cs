namespace MiniMaxSdk.Music.Models;

/// <summary>
/// MiniMax 音乐生成接口支持的音频返回格式。
/// </summary>
public static class MiniMaxMusicOutputFormats
{
    /// <summary>
    /// 以音频链接形式返回。
    /// </summary>
    /// <remarks>
    /// 返回的 URL 有效期为 24 小时，请及时下载。
    /// </remarks>
    public const string Url = "url";

    /// <summary>
    /// 以十六进制编码字符串形式返回。
    /// </summary>
    public const string Hex = "hex";

    /// <summary>
    /// 判断指定返回格式是否受支持。
    /// </summary>
    /// <param name="outputFormat">待判断的返回格式。</param>
    /// <returns>如果受支持则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    public static bool IsSupported(string outputFormat) => outputFormat is Url or Hex;
}
