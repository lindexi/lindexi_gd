using Avalonia;
using SkiaSharp;

namespace DotNetCampus.Inking.Contexts;

readonly record struct SkiaStrokeDrawContext(SKColor Color, SKPath Path, Rect DrawBounds, SKMatrix Transform, bool ShouldDisposePath) : IDisposable
{
    public void Dispose()
    {
        if (ShouldDisposePath)
        {
            Path.Dispose();
        }
    }
}