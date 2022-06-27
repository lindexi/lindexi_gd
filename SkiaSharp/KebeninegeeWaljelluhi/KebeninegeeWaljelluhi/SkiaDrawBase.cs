using SkiaSharp;

namespace KebeninegeeWaljelluhi;

abstract class SkiaDrawBase
{
    public void Draw()
    {
        var fileName = $"{GetType().Name}.png";

        var skImageInfo = new SKImageInfo(1920, 1080, SKColorType.Bgra8888, SKAlphaType.Opaque, SKColorSpace.CreateSrgb());

        using var skImage = SKImage.Create(skImageInfo);

        using (SKBitmap skBitmap = SKBitmap.FromImage(skImage))
        {
            using (var skCanvas = new SKCanvas(skBitmap))
            {
                skCanvas.Clear(SKColors.White);

                OnDraw(skCanvas);

                skCanvas.Flush();

                using (var skData = skBitmap.Encode(SKEncodedImageFormat.Png, 100))
                {
                    var file = new FileInfo(fileName);
                    using (var fileStream = file.OpenWrite())
                    {
                        fileStream.SetLength(0);
                        skData.SaveTo(fileStream);
                    }
                }
            }
        }
    }

    protected abstract void OnDraw(SKCanvas canvas);
}