using BujeeberehemnaNurgacolarje;

using Microsoft.Maui.Graphics;

using ReewheaberekaiNayweelehe;

using SkiaInkCore.Primitive;

using SkiaSharp;

namespace SkiaInkCore.Interactives;

enum InputMode
{
    Ink,
    Manipulate,
}

class InkingInputManager
{
    public InkingInputManager(SkInkCanvas skInkCanvas)
    {
        SkInkCanvas = skInkCanvas;
        var testInput = new TestInput(skInkCanvas);
        testInput.RenderSplashScreen();
    }

    public SkInkCanvas SkInkCanvas { get; }

    public InputMode InputMode { set; get; } = InputMode.Manipulate;

    private int _downCount;

    private StylusPoint _lastStylusPoint;
    private StylusPoint _firstStylusPoint;
    private int MainInput { get; set; }

    public void Down(InkingModeInputArgs args)
    {
        _downCount++;

        if (_downCount == 1)
        {
            _firstStylusPoint = args.StylusPoint;
            MainInput = args.Id;
        }

        if (args.Id == MainInput)
        {
            _lastStylusPoint = args.StylusPoint;
        }

        if (InputMode == InputMode.Ink)
        {
            SkInkCanvas.DrawStrokeDown(args);
        }
        else if (InputMode == InputMode.Manipulate)
        {
        }
    }

    public void Move(InkingModeInputArgs args)
    {
        if (InputMode == InputMode.Ink)
        {
            SkInkCanvas.DrawStrokeMove(args);
        }
        else if (InputMode == InputMode.Manipulate)
        {
            if (_downCount == 1)
            {
                SkInkCanvas.ManipulateMove(new Point(args.StylusPoint.Point.X - _lastStylusPoint.Point.X, args.StylusPoint.Point.Y - _lastStylusPoint.Point.Y));
            }
            else
            {
                if (args.Id != MainInput)
                {
                    return;
                }

                var x = (float) (args.StylusPoint.Point.X - _lastStylusPoint.Point.X);
                var y = (float) (args.StylusPoint.Point.Y - _lastStylusPoint.Point.Y);

                x = 1 + x / 100;
                y = 1 + y / 100;

                x = MathF.Max(0.1f, MathF.Min(10, x));
                y = MathF.Max(0.1f, MathF.Min(10, y));

                SkInkCanvas.ManipulateScale(new ScaleContext(x, y, (float) _firstStylusPoint.Point.X, (float) _firstStylusPoint.Point.Y));
            }

            _lastStylusPoint = args.StylusPoint;
        }
    }

    public void Up(InkingModeInputArgs args)
    {
        _downCount--;
        if (InputMode == InputMode.Ink)
        {
            SkInkCanvas.DrawStrokeUp(args);
        }
        else if (InputMode == InputMode.Manipulate)
        {
            if (args.Id != MainInput)
            {
                return;
            }

            SkInkCanvas.ManipulateMove(new Point(args.StylusPoint.Point.X - _lastStylusPoint.Point.X, args.StylusPoint.Point.Y - _lastStylusPoint.Point.Y));
            SkInkCanvas.ManipulateFinish();

            _lastStylusPoint = args.StylusPoint;
        }
    }
}

class TestInput(SkInkCanvas skInkCanvas)
{
    public void RenderSplashScreen()
    {
        for (int y = 0; y < skInkCanvas.ApplicationDrawingSkBitmap.Height * 2; y += 25)
        {
            var color = new SKColor((uint) Random.Shared.Next()).WithAlpha((byte) Random.Shared.Next(100, 0xFF));

            var inkPointList = new List<StylusPoint>();
            for (int i = 0; i < skInkCanvas.ApplicationDrawingSkBitmap.Width * 2; i++)
            {
                inkPointList.Add(new StylusPoint(i, y));
            }

            AddInk(color, inkPointList);
        }

        for (int x = 0; x < skInkCanvas.ApplicationDrawingSkBitmap.Width * 2; x += 25)
        {
            var color = new SKColor((uint) Random.Shared.Next()).WithAlpha((byte) Random.Shared.Next(100, 0xFF));

            var inkPointList = new List<StylusPoint>();
            for (int i = 0; i < skInkCanvas.ApplicationDrawingSkBitmap.Height * 2; i++)
            {
                inkPointList.Add(new StylusPoint(x, i));
            }

            AddInk(color, inkPointList);
        }

        skInkCanvas.DrawAllInk();

        void AddInk(SKColor color, List<StylusPoint> inkPointList)
        {
            var inkId = new InkId(Random.Shared.Next());

            var outline = SimpleInkRender.GetOutlinePointList([.. inkPointList], 10);
            var skPath = new SKPath();
            skPath.AddPoly(outline.Select(t => new SKPoint((float) t.X, (float) t.Y)).ToArray());

            skInkCanvas.StaticInkInfoList.Add(new SkiaStrokeSynchronizer(0, inkId, color, 10, skPath, inkPointList));
        }
    }
}