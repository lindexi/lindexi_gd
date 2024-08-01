using SkiaSharp;

namespace ReewheaberekaiNayweelehe;

class EraserView
{
    public SKBitmap GetEraserView(int width, int height)
    {
        var skBitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul));

        using var skCanvas = new SKCanvas(skBitmap);
        DrawEraserView(skCanvas, width, height);

        return skBitmap;
    }

    public void DrawEraserView(SKCanvas skCanvas, float width, float height)
    {
        var pathWidth = 30;
        var pathHeight = 45;

        bool needScale = Math.Abs(width - pathWidth) > 0.001 || Math.Abs(height - pathHeight) > 0.001;

        if (needScale)
        {
            skCanvas.Save();
            skCanvas.Scale(width / pathWidth, height / pathHeight);
        }

        using var path1 = SKPath.ParseSvgPathData(
            "M0,5.0093855C0,2.24277828,2.2303666,0,5.00443555,0L24.9955644,0C27.7594379,0,30,2.23861485,30,4.99982044L30,17.9121669C30,20.6734914,30,25.1514578,30,27.9102984L30,40.0016889C30,42.7621799,27.7696334,45,24.9955644,45L5.00443555,45C2.24056212,45,0,42.768443,0,39.9906145L0,5.0093855z");
        using var skPaint = new SKPaint();
        skPaint.IsAntialias = true;
        skPaint.Style = SKPaintStyle.Fill;
        skPaint.Color = new SKColor(0, 0, 0, 0x33);
        skCanvas.DrawPath(path1, skPaint);

        skPaint.Color = new SKColor(0xF2, 0xEE, 0xEB, 0xFF);
        skCanvas.DrawRoundRect(1, 1, 28, 43, 4, 4, skPaint);

        using var path2 = SKPath.ParseSvgPathData(
            "M20,29.1666667L20,16.1666667C20,15.3382395 19.3284271,14.6666667 18.5,14.6666667 17.6715729,14.6666667 17,15.3382395 17,16.1666667L17,29.1666667C17,29.9950938 17.6715729,30.6666667 18.5,30.6666667 19.3284271,30.6666667 20,29.9950938 20,29.1666667z M13,29.1666667L13,16.1666667C13,15.3382395 12.3284271,14.6666667 11.5,14.6666667 10.6715729,14.6666667 10,15.3382395 10,16.1666667L10,29.1666667C10,29.9950938 10.6715729,30.6666667 11.5,30.6666667 12.3284271,30.6666667 13,29.9950938 13,29.1666667z");
        skPaint.Color = new SKColor(0x00, 0x00, 0x00, 0x26);
        skCanvas.DrawPath(path2, skPaint);

        if (needScale)
        {
            skCanvas.Restore();
        }
    }
}