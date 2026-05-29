namespace MiniMaxSdk.Images.Models;

/// <summary>
/// MiniMax 文生图接口支持的模型名称。
/// </summary>
public static class MiniMaxImageGenerationModels
{
    /// <summary>
    /// 标准文生图模型 <c>image-01</c>。
    /// </summary>
    public const string Image01 = "image-01";

    /// <summary>
    /// 实时文生图模型 <c>image-01-live</c>。
    /// </summary>
    public const string Image01Live = "image-01-live";

    /// <summary>
    /// 判断指定模型名称是否受支持。
    /// </summary>
    /// <param name="model">待判断的模型名称。</param>
    /// <returns>如果受支持则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    public static bool IsSupported(string model) => model is Image01 or Image01Live;
}