using BujeeberehemnaNurgacolarje;
using ReewheaberekaiNayweelehe;
using SkiaSharp;

namespace SkiaInkCore.Interactives;

class TestInput(SkInkCanvas skInkCanvas)
{
    public void RenderSplashScreen()
    {
        var lineStep = 100;
        var pointStep = 500;

        for (int y = 0; y < skInkCanvas.ApplicationDrawingSkBitmap.Height * 2; y += lineStep)
        {
            var color = new SKColor((uint) Random.Shared.Next()).WithAlpha((byte) Random.Shared.Next(100, 0xFF));

            var inkPointList = new List<StylusPoint>();
            for (int i = 0; i < skInkCanvas.ApplicationDrawingSkBitmap.Width * 2; i += pointStep)
            {
                inkPointList.Add(new StylusPoint(i, y));
            }

            AddInk(color, inkPointList);
        }

        for (int x = 0; x < skInkCanvas.ApplicationDrawingSkBitmap.Width * 2; x += lineStep)
        {
            var color = new SKColor((uint) Random.Shared.Next()).WithAlpha((byte) Random.Shared.Next(100, 0xFF));

            var inkPointList = new List<StylusPoint>();
            for (int i = 0; i < skInkCanvas.ApplicationDrawingSkBitmap.Height * 2; i += pointStep)
            {
                inkPointList.Add(new StylusPoint(x, i));
            }

            AddInk(color, inkPointList);
        }

        skInkCanvas.DrawAllInk();

        void AddInk(SKColor color, List<StylusPoint> inkPointList)
        {
            var inkId = InkId.NewId();

            var outline = SimpleInkRender.GetOutlinePointList([.. inkPointList], 10);
            var skPath = new SKPath();
            skPath.AddPoly(outline.Select(t => new SKPoint((float) t.X, (float) t.Y)).ToArray());

            skInkCanvas.StaticInkInfoList.Add(new SkiaStrokeSynchronizer(0, inkId, color, 10, skPath, inkPointList));
        }
    }
}