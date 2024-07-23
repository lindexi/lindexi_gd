using BujeeberehemnaNurgacolarje;
using Microsoft.Maui.Graphics;
using ReewheaberekaiNayweelehe;

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
    }

    public SkInkCanvas SkInkCanvas { get; }

    public InputMode InputMode { set; get; } = InputMode.Manipulate;

    private int _downCount;

    private StylusPoint _lastStylusPoint;

    public void Down(InkingModeInputArgs args)
    {
        _downCount++;
        if (_downCount > 2)
        {
            InputMode = InputMode.Manipulate;
        }

        if (InputMode == InputMode.Ink)
        {
            SkInkCanvas.DrawStrokeDown(args);
        }
        else if (InputMode == InputMode.Manipulate)
        {
            _lastStylusPoint = args.StylusPoint;
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
            SkInkCanvas.ManipulateMove(new Point(args.StylusPoint.Point.X - _lastStylusPoint.Point.X, args.StylusPoint.Point.Y - _lastStylusPoint.Point.Y));

            _lastStylusPoint = args.StylusPoint;
        }
    }

    public void Up(InkingModeInputArgs args)
    {
        if (InputMode == InputMode.Ink)
        {
            SkInkCanvas.DrawStrokeUp(args);
        }
        else if (InputMode == InputMode.Manipulate)
        {
            SkInkCanvas.ManipulateMove(new Point(args.StylusPoint.Point.X - _lastStylusPoint.Point.X, args.StylusPoint.Point.Y - _lastStylusPoint.Point.Y));
            SkInkCanvas.ManipulateFinish();

            _lastStylusPoint = args.StylusPoint;
        }
    }
}