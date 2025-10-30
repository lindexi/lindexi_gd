using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.Inking.Utils;

/// <summary>
/// 渲染同步器
/// </summary>
record SkiaStrokeRenderSynchronizer(IReadOnlyList<SkiaStroke> StrokeList, Action OnRender)
{
}

