namespace MiniMaxSdk.Music.Models;

/// <summary>
/// 表示一次 MiniMax 歌词生成请求的结果。
/// </summary>
/// <param name="SongTitle">生成的歌名；如果请求传入标题，则保持一致。</param>
/// <param name="StyleTags">风格标签，使用逗号分隔。</param>
/// <param name="Lyrics">生成的歌词，包含结构标签，可直接用于音乐生成接口。</param>
/// <param name="BaseResponse">接口返回的状态码及详情。</param>
public sealed record MiniMaxLyricsGenerationResult(string? SongTitle, string? StyleTags, string? Lyrics, MiniMaxBaseResponse BaseResponse);
