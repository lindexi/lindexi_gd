namespace MiniMaxSdk.Images.Models;

/// <summary>
/// MiniMax 文生图接口支持的图片返回格式。
/// </summary>
public static class MiniMaxImageResponseFormats
{
    /// <summary>
    /// 以图片链接形式返回。
    /// </summary>
    /// <remarks>
    /// 返回的 URL 有效期为 24 小时。
    /// </remarks>
    public const string Url = "url";

    /// <summary>
    /// 以 Base64 编码形式返回。
    /// </summary>
    public const string Base64 = "base64";

    /// <summary>
    /// 判断指定返回格式是否受支持。
    /// </summary>
    /// <param name="responseFormat">待判断的返回格式。</param>
    /// <returns>如果受支持则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    public static bool IsSupported(string responseFormat) => responseFormat is Url or Base64;
}