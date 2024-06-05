// See https://aka.ms/new-console-template for more information

using SkiaSharp;

var skImageInfo = new SKImageInfo(1920, 1080, SKColorType.Bgra8888, SKAlphaType.Unpremul, SKColorSpace.CreateSrgb());
var fileName = $"1.png";
using (var skImage = SKImage.Create(skImageInfo))
{
    using var skBitmap = SKBitmap.FromImage(skImage);
    using (var skCanvas = new SKCanvas(skBitmap))
    {
        skCanvas.Clear(SKColors.White);

        (double X, double Y)[] outlinePointList = [(362, 416),
        (362,416),
        (362,418),
        (360,426),
        (360,426),
        (360,426),];

        outlinePointList = [(100, 100), (100, 200), (200,200), (50, 150)];

        using var skPaint = new SKPaint();
        skPaint.StrokeWidth = 5f;
        skPaint.IsAntialias = true;
        skPaint.IsStroke = true;
        skPaint.FilterQuality = SKFilterQuality.High;
        skPaint.Style = SKPaintStyle.Stroke;

        skPaint.Color = SKColors.Red;

        /*
           DrawStroke (362,416),
           DrawStroke (362,418),
           DrawStroke (360,426),
           DrawStroke (360,426),
           DrawStroke (360,426),
         */
        using var skPath = new SKPath() { FillType = SKPathFillType.EvenOdd };
        skPath.AddPoly(outlinePointList.Select(t => new SKPoint((float) t.X, (float) t.Y)).ToArray());
        //skPath.Close();

        skCanvas.DrawPath(skPath, skPaint);
    }

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

Console.WriteLine("Hello, World!");
