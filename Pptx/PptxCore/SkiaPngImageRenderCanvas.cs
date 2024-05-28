using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Skia;

using SkiaSharp;

namespace PptxCore;

/// <summary>
///     提供使用 png 作为输出的 Skia 画板
/// </summary>
public class SkiaPngImageRenderCanvas : IRenderCanvas
{
    private readonly int _height;
    private readonly FileInfo _outputPngFile;

    private readonly int _width;

    public SkiaPngImageRenderCanvas(int width, int height, FileInfo outputPngFile)
    {
        _width = width;
        _height = height;
        _outputPngFile = outputPngFile;
    }

    public void Render(Action<ICanvas> action)
    {
        var skImageInfo = new SKImageInfo(_width, _height, SKColorType.Bgra8888, SKAlphaType.Unpremul,
            SKColorSpace.CreateSrgb());

        using (var skImage = SKImage.Create(skImageInfo))
        {
            using (var skBitmap = SKBitmap.FromImage(skImage))
            {
                using (var skCanvas = new SKCanvas(skBitmap))
                {
                    skCanvas.Clear(SKColors.Transparent);

                    var skiaCanvas = new SkiaCanvas();
                    skiaCanvas.Canvas = skCanvas;

                    ICanvas canvas = skiaCanvas;

                    action(canvas);

                    skCanvas.Flush();

                    using (var skData = skBitmap.Encode(SKEncodedImageFormat.Png, 100))
                    {
                        var file = _outputPngFile;
                        using (var fileStream = file.OpenWrite())
                        {
                            fileStream.SetLength(0);
                            skData.SaveTo(fileStream);
                        }
                    }
                }
            }
        }
    }
}