using SkiaSharp;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.InkCanvas.X11InkCanvas.Renders.SpriteAdorners;

/// <summary>
/// 精灵装饰器，原本用于覆盖一层显示。然而实际测试 X11 将会出现闪烁问题，原因是 PutImage 一次将会显示一次，分为多次 PutImage 将会导致闪烁，因此此机制现在不使用
/// </summary>
interface ISpriteAdorner
{
    SKRectI Bounds { get; }
    SKBitmap OutputBitmap { get; }
    void Draw(IApplicationBackendRenderContext applicationBitmap);
}
