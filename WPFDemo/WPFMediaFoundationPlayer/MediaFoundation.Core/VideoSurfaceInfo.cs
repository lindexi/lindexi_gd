namespace MediaFoundation.Core;

/// <summary>源元数据与当前 GPU 输出尺寸；所有尺寸均为像素，显示宽高比包含 PAR 和 aperture。</summary>
/// <param name="NativeWidth">源视频的原生像素宽度。</param>
/// <param name="NativeHeight">源视频的原生像素高度。</param>
/// <param name="DisplayAspectRatio">包含像素宽高比和显示区域的显示宽高比。</param>
/// <param name="MixerWidth">mixer 输出的像素宽度。</param>
/// <param name="MixerHeight">mixer 输出的像素高度。</param>
/// <param name="DisplayWidth">展示表面的像素宽度。</param>
/// <param name="DisplayHeight">展示表面的像素高度。</param>
/// <param name="Generation">输出资源更新时递增的代次。</param>
/// <param name="ScalingMode">当前使用的视频缩放策略。</param>
public sealed record VideoSurfaceInfo
(
    int NativeWidth,
    int NativeHeight,
    double DisplayAspectRatio,
    int MixerWidth,
    int MixerHeight,
    int DisplayWidth,
    int DisplayHeight,
    long Generation,
    VideoScalingMode ScalingMode
);