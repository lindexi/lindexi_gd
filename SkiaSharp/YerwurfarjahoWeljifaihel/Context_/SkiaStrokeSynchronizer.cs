using BujeeberehemnaNurgacolarje;
using SkiaSharp;

namespace SkiaInkCore;

/// <summary>
/// 笔迹信息 用于静态笔迹层
/// </summary>
record SkiaStrokeSynchronizer(uint StylusDeviceId,
        InkId InkId,
        SKColor StrokeColor,
        double StrokeInkThickness,
        SKPath? InkStrokePath,
        List<StylusPoint> StylusPoints)
    ;