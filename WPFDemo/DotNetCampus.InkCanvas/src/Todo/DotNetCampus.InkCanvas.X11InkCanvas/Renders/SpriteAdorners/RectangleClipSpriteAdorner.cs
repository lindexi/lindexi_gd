using SkiaSharp;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.InkCanvas.X11InkCanvas.Renders.SpriteAdorners;

/// <summary>
/// 提供一个矩形裁剪的装饰器，可以用于裁剪一个矩形区域
/// </summary>
class RectangleClipSpriteAdorner : ISpriteAdorner
{
    public RectangleClipSpriteAdorner(SKRectI bounds)
    {
        Bounds = bounds;
        OutputBitmap = new SKBitmap(bounds.Width, bounds.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
    }

    public SKRectI Bounds { get; }
    public SKBitmap OutputBitmap { get; }
    public void Draw(IApplicationBackendRenderContext applicationBitmap)
    {
        // 这里是裁剪，啥都不需要做
    }
}
