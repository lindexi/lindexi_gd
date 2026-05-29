namespace MiniMaxSdk.Music.Models;

/// <summary>
/// MiniMax 音乐相关接口支持的模型名称。
/// </summary>
public static class MiniMaxMusicGenerationModels
{
    /// <summary>
    /// 文本生成音乐模型 <c>music-2.6</c>。
    /// </summary>
    public const string Music26 = "music-2.6";

    /// <summary>
    /// 基于参考音频生成翻唱版本的模型 <c>music-cover</c>。
    /// </summary>
    public const string MusicCover = "music-cover";

    /// <summary>
    /// 文本生成音乐限免模型 <c>music-2.6-free</c>。
    /// </summary>
    public const string Music26Free = "music-2.6-free";

    /// <summary>
    /// 翻唱限免模型 <c>music-cover-free</c>。
    /// </summary>
    public const string MusicCoverFree = "music-cover-free";

    /// <summary>
    /// 判断指定模型名称是否受支持。
    /// </summary>
    /// <param name="model">待判断的模型名称。</param>
    /// <returns>如果受支持则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    public static bool IsSupported(string model) => model is Music26 or MusicCover or Music26Free or MusicCoverFree;

    /// <summary>
    /// 判断指定模型是否为翻唱模型。
    /// </summary>
    /// <param name="model">待判断的模型名称。</param>
    /// <returns>如果为翻唱模型则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    public static bool IsCoverModel(string model) => model is MusicCover or MusicCoverFree;

    /// <summary>
    /// 判断指定模型是否为文本生成音乐模型。
    /// </summary>
    /// <param name="model">待判断的模型名称。</param>
    /// <returns>如果为文本生成音乐模型则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    public static bool IsTextToMusicModel(string model) => model is Music26 or Music26Free;
}
