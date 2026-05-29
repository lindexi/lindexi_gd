namespace MiniMaxSdk.Music.Models;

/// <summary>
/// MiniMax 歌词生成接口支持的生成模式。
/// </summary>
public static class MiniMaxLyricsGenerationModes
{
    /// <summary>
    /// 写完整歌曲模式 <c>write_full_song</c>。
    /// </summary>
    public const string WriteFullSong = "write_full_song";

    /// <summary>
    /// 编辑或续写歌词模式 <c>edit</c>。
    /// </summary>
    public const string Edit = "edit";

    /// <summary>
    /// 判断指定模式是否受支持。
    /// </summary>
    /// <param name="mode">待判断的模式。</param>
    /// <returns>如果受支持则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    public static bool IsSupported(string mode) => mode is WriteFullSong or Edit;
}
