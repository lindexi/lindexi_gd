namespace MediaFoundation.Core;

/// <summary>MFPlay D3DImage 视频缩放位置；不影响 HWND 渲染器。</summary>
public enum VideoScalingMode
{
    /// <summary>mixer 输出目标尺寸的 RGB32，sample 和 staging 使用目标尺寸。设备不支持时报告失败，不回退。</summary>
    MixerOutput,
    /// <summary>保留 mixer 的原始尺寸，在 staging 到展示表面的原有复制中使用 GPU 线性缩放。</summary>
    PresentationSurface
}

/// <summary>源元数据与当前 GPU 输出尺寸；所有尺寸均为像素，显示宽高比包含 PAR 和 aperture。</summary>
public sealed record VideoSurfaceInfo(int NativeWidth, int NativeHeight, double DisplayAspectRatio,
    int MixerWidth, int MixerHeight, int DisplayWidth, int DisplayHeight, long Generation,
    VideoScalingMode ScalingMode);
