namespace MiniMaxSdk.Music.Models;

/// <summary>
/// MiniMax 音乐生成结果中的合成状态值。
/// </summary>
public static class MiniMaxMusicGenerationStatuses
{
    /// <summary>
    /// 音乐仍在合成中。
    /// </summary>
    public const int Processing = 1;

    /// <summary>
    /// 音乐已合成完成。
    /// </summary>
    public const int Completed = 2;
}
