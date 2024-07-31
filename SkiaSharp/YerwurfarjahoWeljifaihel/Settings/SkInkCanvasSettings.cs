using SkiaSharp;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkiaInkCore.Settings;

/// <summary>
/// 画板的配置
/// </summary>
record SkInkCanvasSettings
{
    /// <summary>
    /// 笔迹颜色
    /// </summary>
    public SKColor Color { get; init; } = SKColors.Red;

    /// <summary>
    /// 清空时使用的颜色
    /// </summary>
    public SKColor ClearColor { get; init; } = SKColors.Empty;
}
