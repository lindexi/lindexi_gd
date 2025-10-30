using DotNetCampus.Inking.Primitive;
using SkiaSharp;
using WpfInk;

namespace DotNetCampus.Inking.StrokeRenderers.WpfForSkiaInkStrokeRenderers;

/// <summary>
/// 用 WPF 的笔迹算法提供给 Skia 这边的笔迹支持
/// </summary>
public class WpfForSkiaInkStrokeRenderer : ISkiaInkStrokeRenderer
{
    public SKPath RenderInkToPath(IReadOnlyList<InkStylusPoint> pointList, double inkThickness)
    {
        if (pointList.Count == 0)
        {
            return new SKPath();
        }

        var path = new SKPath();
        var skiaStreamGeometryContext = new SkiaStreamGeometryContext(path);

        InkStrokeRenderer.Render(skiaStreamGeometryContext, new StrokeRendererInfo()
        {
            Width = inkThickness / 2,
            Height = inkThickness / 2,
            StylusPointCollection = pointList
        });
        return path;
    }
}