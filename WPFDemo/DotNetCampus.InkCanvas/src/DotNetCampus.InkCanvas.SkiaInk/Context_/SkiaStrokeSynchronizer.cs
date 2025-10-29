using SkiaSharp;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetCampus.Inking.Primitive;
using UnoInk.Inking.InkCore;
using WpfInk;

namespace DotNetCampus.Inking;

/// <summary>
/// 笔迹信息 用于静态笔迹层
/// </summary>
record SkiaStrokeSynchronizer(
    uint StylusDeviceId,
    InkId InkId,
    SKColor StrokeColor,
    double StrokeInkThickness,
    SKPath? InkStrokePath,
    List<InkStylusPoint> StylusPoints)// : InkSynchronizer.StrokeSynchronizer(StylusDeviceId)
{
    public SKRect GetBounds()
    {
        return _bounds ??= InkStrokePath?.Bounds ?? SKRect.Empty;
    }

    private SKRect? _bounds;
}
;
