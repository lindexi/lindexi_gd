namespace MiniMaxSdk.Music.Models;

/// <summary>
/// 表示 MiniMax 音乐生成请求中的音频输出配置。
/// </summary>
/// <param name="SampleRate">采样率，可选值为 <c>16000</c>、<c>24000</c>、<c>32000</c>、<c>44100</c>。</param>
/// <param name="Bitrate">比特率，可选值为 <c>32000</c>、<c>64000</c>、<c>128000</c>、<c>256000</c>。</param>
/// <param name="Format">音频编码格式，可选值参考 <see cref="MiniMaxMusicAudioFormats"/>。</param>
public sealed record MiniMaxMusicAudioSetting(int? SampleRate = null, int? Bitrate = null, string? Format = null)
{
    internal void Validate()
    {
        if (SampleRate.HasValue && SampleRate.Value is not (16000 or 24000 or 32000 or 44100))
        {
            throw new ArgumentOutOfRangeException(nameof(SampleRate), SampleRate, "不支持的采样率，可选值为 16000、24000、32000、44100。");
        }

        if (Bitrate.HasValue && Bitrate.Value is not (32000 or 64000 or 128000 or 256000))
        {
            throw new ArgumentOutOfRangeException(nameof(Bitrate), Bitrate, "不支持的比特率，可选值为 32000、64000、128000、256000。");
        }

        if (Format is not null && !MiniMaxMusicAudioFormats.IsSupported(Format))
        {
            throw new ArgumentException($"不支持的音频编码格式：{Format}", nameof(Format));
        }
    }
}
