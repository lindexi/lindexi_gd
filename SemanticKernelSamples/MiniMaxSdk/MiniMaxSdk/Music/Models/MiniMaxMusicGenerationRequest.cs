namespace MiniMaxSdk.Music.Models;

/// <summary>
/// 表示一次 MiniMax 音乐生成请求。
/// </summary>
/// <param name="Model">使用的模型名称，可选值参考 <see cref="MiniMaxMusicGenerationModels"/>。</param>
/// <param name="Prompt">音乐描述，用于指定风格、情绪和场景。</param>
/// <param name="Lyrics">歌曲歌词，使用换行分隔每行内容。</param>
/// <param name="Stream">是否使用流式传输。</param>
/// <param name="OutputFormat">音频返回格式，可选值参考 <see cref="MiniMaxMusicOutputFormats"/>。</param>
/// <param name="AudioSetting">音频输出配置。</param>
/// <param name="AigcWatermark">是否在音频末尾添加水印，仅在非流式请求时生效。</param>
/// <param name="LyricsOptimizer">是否根据 <paramref name="Prompt"/> 自动生成歌词，仅文本生成音乐模型支持。</param>
/// <param name="IsInstrumental">是否生成纯音乐，仅文本生成音乐模型支持。</param>
/// <param name="AudioUrl">参考音频 URL，仅翻唱模型支持。</param>
/// <param name="AudioBase64">参考音频的 Base64 编码，仅翻唱模型支持。</param>
/// <param name="CoverFeatureId">翻唱前处理接口返回的特征 ID，仅翻唱模型支持。</param>
/// <remarks>
/// <para>当 <paramref name="Stream"/> 为 <see langword="true"/> 时，仅支持 <c>hex</c> 输出格式。</para>
/// <para>对于翻唱模型，<paramref name="AudioUrl"/>、<paramref name="AudioBase64"/> 与 <paramref name="CoverFeatureId"/> 互斥。</para>
/// <para>当使用 <paramref name="CoverFeatureId"/> 时，<paramref name="Lyrics"/> 为必填，且长度限制为 10 到 1000 个字符。</para>
/// <para>当 <paramref name="LyricsOptimizer"/> 为 <see langword="true"/> 且 <paramref name="Lyrics"/> 为空时，系统会根据 <paramref name="Prompt"/> 自动生成歌词。</para>
/// </remarks>
public sealed record MiniMaxMusicGenerationRequest(
    string Model = MiniMaxMusicGenerationModels.Music26,
    string? Prompt = null,
    string? Lyrics = null,
    bool Stream = false,
    string OutputFormat = MiniMaxMusicOutputFormats.Hex,
    MiniMaxMusicAudioSetting? AudioSetting = null,
    bool? AigcWatermark = null,
    bool LyricsOptimizer = false,
    bool IsInstrumental = false,
    string? AudioUrl = null,
    string? AudioBase64 = null,
    string? CoverFeatureId = null)
{
    internal void Validate()
    {
        if (!MiniMaxMusicGenerationModels.IsSupported(Model))
        {
            throw new ArgumentException($"不支持的模型：{Model}", nameof(Model));
        }

        if (Prompt is { Length: > 2000 })
        {
            throw new ArgumentException("Prompt 长度不能超过 2000 个字符。", nameof(Prompt));
        }

        if (Lyrics is { Length: > 3500 })
        {
            throw new ArgumentException("Lyrics 长度不能超过 3500 个字符。", nameof(Lyrics));
        }

        if (!MiniMaxMusicOutputFormats.IsSupported(OutputFormat))
        {
            throw new ArgumentException($"不支持的输出格式：{OutputFormat}", nameof(OutputFormat));
        }

        if (Stream && OutputFormat != MiniMaxMusicOutputFormats.Hex)
        {
            throw new ArgumentException("流式传输仅支持 hex 输出格式。", nameof(OutputFormat));
        }

        AudioSetting?.Validate();

        if (MiniMaxMusicGenerationModels.IsTextToMusicModel(Model))
        {
            ValidateTextToMusicModel();
            return;
        }

        ValidateCoverModel();
    }

    private void ValidateTextToMusicModel()
    {
        if (AudioUrl is not null)
        {
            throw new ArgumentException("文本生成音乐模型不支持 AudioUrl。", nameof(AudioUrl));
        }

        if (AudioBase64 is not null)
        {
            throw new ArgumentException("文本生成音乐模型不支持 AudioBase64。", nameof(AudioBase64));
        }

        if (CoverFeatureId is not null)
        {
            throw new ArgumentException("文本生成音乐模型不支持 CoverFeatureId。", nameof(CoverFeatureId));
        }

        if (LyricsOptimizer && string.IsNullOrWhiteSpace(Prompt))
        {
            throw new ArgumentException("启用 LyricsOptimizer 时，Prompt 不能为空。", nameof(Prompt));
        }

        if (IsInstrumental)
        {
            if (Prompt is null or { Length: 0 })
            {
                throw new ArgumentException("生成纯音乐时，Prompt 为必填且长度至少为 1。", nameof(Prompt));
            }
        }
        else if (string.IsNullOrWhiteSpace(Lyrics) && !LyricsOptimizer)
        {
            throw new ArgumentException("非纯音乐模式下，Lyrics 为必填；除非启用 LyricsOptimizer 自动生成歌词。", nameof(Lyrics));
        }
    }

    private void ValidateCoverModel()
    {
        if (LyricsOptimizer)
        {
            throw new ArgumentException("翻唱模型不支持 LyricsOptimizer。", nameof(LyricsOptimizer));
        }

        if (IsInstrumental)
        {
            throw new ArgumentException("翻唱模型不支持 IsInstrumental。", nameof(IsInstrumental));
        }

        if (string.IsNullOrWhiteSpace(Prompt))
        {
            throw new ArgumentException("翻唱模型的 Prompt 为必填。", nameof(Prompt));
        }

        if (Prompt.Length is < 10 or > 300)
        {
            throw new ArgumentException("翻唱模型的 Prompt 长度需在 10 到 300 个字符之间。", nameof(Prompt));
        }

        var sourceCount = 0;
        sourceCount += string.IsNullOrWhiteSpace(AudioUrl) ? 0 : 1;
        sourceCount += string.IsNullOrWhiteSpace(AudioBase64) ? 0 : 1;
        sourceCount += string.IsNullOrWhiteSpace(CoverFeatureId) ? 0 : 1;

        if (sourceCount != 1)
        {
            throw new ArgumentException("AudioUrl、AudioBase64 和 CoverFeatureId 必须且只能提供其中一个。");
        }

        if (!string.IsNullOrWhiteSpace(CoverFeatureId))
        {
            if (string.IsNullOrWhiteSpace(Lyrics))
            {
                throw new ArgumentException("使用 CoverFeatureId 时，Lyrics 为必填。", nameof(Lyrics));
            }

            if (Lyrics.Length is < 10 or > 1000)
            {
                throw new ArgumentException("使用 CoverFeatureId 时，Lyrics 长度需在 10 到 1000 个字符之间。", nameof(Lyrics));
            }
        }

        if (!string.IsNullOrWhiteSpace(Lyrics) && Lyrics.Length is < 10 or > 1000)
        {
            throw new ArgumentException("翻唱模型的 Lyrics 长度需在 10 到 1000 个字符之间。", nameof(Lyrics));
        }
    }
}
