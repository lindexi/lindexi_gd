using System.Diagnostics;

using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Skia;

using SkiaSharp;

var skImageInfo = new SKImageInfo(1920, 1080, SKColorType.Bgra8888, SKAlphaType.Unpremul, SKColorSpace.CreateSrgb());

var fileName = $"xx.png";

ShowWorkingSet64();

var count = 0;
foreach (var defaultFontFamily in SKFontManager.Default.FontFamilies)
{
    Console.WriteLine(defaultFontFamily);
    count++;
}

Console.WriteLine("===========");
ShowWorkingSet64();

foreach (var file in Directory.GetFiles(@"E:\软件\字体", "*.ttf", SearchOption.AllDirectories))
{
    var skTypeface = SKFontManager.Default.CreateTypeface(file);
}

ShowWorkingSet64();
foreach (var file in Directory.GetFiles(@"C:\windows\fonts\", "*.ttf", SearchOption.AllDirectories))
{
    var skTypeface = SKFontManager.Default.CreateTypeface(file);
}

ShowWorkingSet64();

Console.WriteLine(SKFontManager.Default.GetFontFamilies().Length);

foreach (var defaultFontFamily in SKFontManager.Default.FontFamilies)
{
    Console.WriteLine(defaultFontFamily);
    count--;
}

Console.WriteLine(count);

ShowWorkingSet64();

void ShowWorkingSet64()
{
    var currentProcess = Process.GetCurrentProcess();
    Console.WriteLine($"{currentProcess.WorkingSet64 / 1024.0 / 1024}MB");
}

using (var skImage = SKImage.Create(skImageInfo))
{
    using (var skBitmap = SKBitmap.FromImage(skImage))
    {
        using (var skCanvas = new SKCanvas(skBitmap))
        {
            skCanvas.Clear(SKColors.White);

            var skiaCanvas = new SkiaCanvas();
            skiaCanvas.Canvas = skCanvas;

            ICanvas canvas = skiaCanvas;
            canvas.Font = new Font("微软雅黑");

            canvas.FontSize = 100;
            canvas.DrawString("汉字", 100, 100, 500, 500, HorizontalAlignment.Left, VerticalAlignment.Bottom);



            canvas.StrokeColor = Colors.Blue;
            canvas.StrokeSize = 2;
            canvas.DrawRectangle(100, 100, 500, 500);

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

