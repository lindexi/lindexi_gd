using DotNetCampus.Inking.Primitive;

using SkiaSharp;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.Inking.StrokeRenderers;

/// <summary>
/// 提供给 Skia 系的笔迹渲染器的接口
/// </summary>
public interface ISkiaInkStrokeRenderer
{
    /// <summary>
    /// 从给点的点集创建笔迹路径
    /// </summary>
    /// <param name="pointList"></param>
    /// <param name="inkThickness"></param>
    /// <returns></returns>
    SKPath RenderInkToPath(IReadOnlyList<InkStylusPoint> pointList, double inkThickness);
}