using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SkiaSharp;

namespace LightTextEditorPlus.Primitive;

/// <summary>
/// 文本的 Skia 渲染画刷
/// </summary>
public abstract record SkiaTextBrush
{
    /// <summary>
    /// 应用画刷到指定的 <see cref="SKPaint"/> 对象上
    /// </summary>
    /// <param name="context"></param>
    protected internal abstract void Apply(in SkiaTextBrushRenderContext context);

    /// <summary>
    /// 转换纯色画刷
    /// </summary>
    /// <param name="color"></param>
    public static implicit operator SkiaTextBrush(SKColor color)
    {
        return new SolidColorSkiaTextBrush(color);
    }

    /// <summary>
    /// 当成纯色画刷
    /// </summary>
    /// <returns></returns>
    public abstract SKColor AsSolidColor();

    /// <summary>
    /// 默认的黑色纯色画刷
    /// </summary>
    public static SkiaTextBrush DefaultBlackSolidColorBrush { get; } = SKColors.Black;
}