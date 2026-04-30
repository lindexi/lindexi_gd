namespace VideoComposerLib;

/// <summary>
/// 视频分段参数：每个分段对应1张图片 + 多个音频
/// </summary>
/// <param name="ImageFile">分段对应的图片文件</param>
/// <param name="AudioFiles">分段对应的音频列表，按播放顺序排列</param>
public record VideoSegment
(
    FileInfo ImageFile,
    IReadOnlyList<FileInfo> AudioFiles
);