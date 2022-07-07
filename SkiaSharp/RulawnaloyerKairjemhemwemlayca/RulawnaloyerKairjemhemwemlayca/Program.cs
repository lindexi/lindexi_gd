using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Skia;

using SkiaSharp;

var skImageInfo = new SKImageInfo(1920, 1080, SKColorType.Bgra8888, SKAlphaType.Unpremul, SKColorSpace.CreateSrgb());

var fileName = $"xx.jpg";

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
            canvas.Font = new Font("微软雅黑");

            var linearGradientPaint = new LinearGradientPaint(new PaintGradientStop[]
            {
                new PaintGradientStop(0,Colors.Blue),
                new PaintGradientStop(100,Colors.Black),
            })
            {
                StartPoint = new Point(),
                EndPoint = new Point(1,1)
            };
            canvas.FillColor = Colors.Beige;
            canvas.FillRectangle(new RectF(10, 10, 200, 200));
            canvas.SetFillPaint(linearGradientPaint, new RectF(10, 10, 200, 200));
            canvas.FillRectangle(new RectF(10, 10, 200, 200));

            skCanvas.Flush();

            using (var skData = skBitmap.Encode(SKEncodedImageFormat.Jpeg, 2))
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
