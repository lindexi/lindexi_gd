namespace MiniMaxSdk.Music.Models;

/// <summary>
/// 表示一次 MiniMax 翻唱前处理请求。
/// </summary>
/// <param name="Model">模型名称，必须为 <c>music-cover</c>。</param>
/// <param name="AudioUrl">参考音频的 URL 地址。</param>
/// <param name="AudioBase64">参考音频的 Base64 编码。</param>
/// <remarks>
/// <para><paramref name="AudioUrl"/> 与 <paramref name="AudioBase64"/> 必须且只能提供其中一个。</para>
/// <para>参考音频要求：时长 6 秒至 6 分钟，大小最大 50 MB，支持常见音频格式。</para>
/// </remarks>
public sealed record MiniMaxCoverPreprocessRequest(
    string Model = MiniMaxMusicGenerationModels.MusicCover,
    string? AudioUrl = null,
    string? AudioBase64 = null)
{
    internal void Validate()
    {
        if (!string.Equals(Model, MiniMaxMusicGenerationModels.MusicCover, StringComparison.Ordinal))
        {
            throw new ArgumentException($"翻唱前处理仅支持模型：{MiniMaxMusicGenerationModels.MusicCover}", nameof(Model));
        }

        var sourceCount = 0;
        sourceCount += string.IsNullOrWhiteSpace(AudioUrl) ? 0 : 1;
        sourceCount += string.IsNullOrWhiteSpace(AudioBase64) ? 0 : 1;

        if (sourceCount != 1)
        {
            throw new ArgumentException("AudioUrl 和 AudioBase64 必须且只能提供其中一个。");
        }
    }
}
