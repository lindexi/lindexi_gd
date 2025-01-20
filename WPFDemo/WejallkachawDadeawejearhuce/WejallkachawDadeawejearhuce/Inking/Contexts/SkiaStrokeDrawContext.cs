using Avalonia;
using SkiaSharp;

namespace WejallkachawDadeawejearhuce.Inking.Contexts;

readonly record struct SkiaStrokeDrawContext(SKColor Color, SKPath Path, Rect DrawBounds, bool ShouldDisposePath) : IDisposable
{
    public void Dispose()
    {
        if (ShouldDisposePath)
        {
            Path.Dispose();
        }
    }
}