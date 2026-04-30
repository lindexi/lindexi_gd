namespace VideoComposerLib;

/// <summary>
/// 视频编码配置
/// </summary>
/// <param name="Width">输出视频宽度</param>
/// <param name="Height">输出视频高度</param>
/// <param name="Fps">输出视频帧率</param>
/// <param name="VideoBitrate">视频码率，比如"5M"表示5Mbps</param>
/// <param name="AudioBitrate">音频码率，比如"192k"表示192kbps</param>
public record VideoEncodeSettings
(
    int Width = 1920,
    int Height = 1080,
    int Fps = 25,
    string VideoBitrate = "3M",
    string AudioBitrate = "192k"
);