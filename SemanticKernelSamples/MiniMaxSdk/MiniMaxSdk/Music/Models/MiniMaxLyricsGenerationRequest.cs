namespace MiniMaxSdk.Music.Models;

/// <summary>
/// 表示一次 MiniMax 歌词生成请求。
/// </summary>
/// <param name="Mode">生成模式，可选值参考 <see cref="MiniMaxLyricsGenerationModes"/>。</param>
/// <param name="Prompt">提示词或指令，用于描述歌曲主题、风格或编辑方向；为空时随机生成。</param>
/// <param name="Lyrics">现有歌词内容，仅在 <c>edit</c> 模式下有效。</param>
/// <param name="Title">歌曲标题；传入后输出将保持该标题不变。</param>
public sealed record MiniMaxLyricsGenerationRequest(string Mode, string? Prompt = null, string? Lyrics = null, string? Title = null)
{
    internal void Validate()
    {
        if (!MiniMaxLyricsGenerationModes.IsSupported(Mode))
        {
            throw new ArgumentException($"不支持的歌词生成模式：{Mode}", nameof(Mode));
        }

        if (Prompt is { Length: > 2000 })
        {
            throw new ArgumentException("Prompt 长度不能超过 2000 个字符。", nameof(Prompt));
        }

        if (Lyrics is { Length: > 3500 })
        {
            throw new ArgumentException("Lyrics 长度不能超过 3500 个字符。", nameof(Lyrics));
        }

        if (Mode == MiniMaxLyricsGenerationModes.Edit && string.IsNullOrWhiteSpace(Lyrics))
        {
            throw new ArgumentException("编辑模式下，Lyrics 为必填。", nameof(Lyrics));
        }
    }
}
