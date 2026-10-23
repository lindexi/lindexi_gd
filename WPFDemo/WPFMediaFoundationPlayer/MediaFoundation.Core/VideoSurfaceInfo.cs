namespace MediaFoundation.Core;

/// <summary>源元数据与当前 GPU 输出尺寸；所有尺寸均为像素，显示宽高比包含 PAR 和 aperture。</summary>
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