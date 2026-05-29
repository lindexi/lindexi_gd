namespace MiniMaxSdk.Images.Models;

/// <summary>
/// MiniMax 文生图接口支持的图片宽高比。
/// </summary>
public static class MiniMaxImageAspectRatios
{
    /// <summary>
    /// 1:1 宽高比，对应 1024x1024。
    /// </summary>
    public const string Square = "1:1";

    /// <summary>
    /// 16:9 宽高比，对应 1280x720。
    /// </summary>
    public const string Landscape16By9 = "16:9";

    /// <summary>
    /// 4:3 宽高比，对应 1152x864。
    /// </summary>
    public const string Standard4By3 = "4:3";

    /// <summary>
    /// 3:2 宽高比，对应 1248x832。
    /// </summary>
    public const string Standard3By2 = "3:2";

    /// <summary>
    /// 2:3 宽高比，对应 832x1248。
    /// </summary>
    public const string Portrait2By3 = "2:3";

    /// <summary>
    /// 3:4 宽高比，对应 864x1152。
    /// </summary>
    public const string Portrait3By4 = "3:4";

    /// <summary>
    /// 9:16 宽高比，对应 720x1280。
    /// </summary>
    public const string Portrait9By16 = "9:16";

    /// <summary>
    /// 21:9 宽高比，对应 1344x576，仅适用于 <c>image-01</c>。
    /// </summary>
    public const string Ultrawide21By9 = "21:9";

    /// <summary>
    /// 判断指定宽高比是否受支持。
    /// </summary>
    /// <param name="aspectRatio">待判断的宽高比。</param>
    /// <returns>如果受支持则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    public static bool IsSupported(string aspectRatio) => aspectRatio is Square or Landscape16By9 or Standard4By3 or Standard3By2 or Portrait2By3 or Portrait3By4 or Portrait9By16 or Ultrawide21By9;
}