using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;

namespace NarjejerechowainoBuwurjofear.Inking.Erasing;

public class AvaSkiaInkCanvasEraserMode
{
    public AvaSkiaInkCanvasEraserMode(AvaSkiaInkCanvas inkCanvas)
    {
        InkCanvas = inkCanvas;
    }

    public AvaSkiaInkCanvas InkCanvas { get; }
    public bool IsErasing { get; private set; }
    private int MainEraserInputId { set; get; }

    private PointPathEraserManager PointPathEraserManager { get; } = new PointPathEraserManager();

    public void StartEraser()
    {
        var staticStrokeList = InkCanvas.StaticStrokeList;
        PointPathEraserManager.StartEraserPointPath(staticStrokeList);
    }

    public void EraserDown(InkingInputArgs args)
    {
        InkCanvas.EnsureInputConflicts();
        if (!IsErasing)
        {
            MainEraserInputId = args.Id;

            IsErasing = true;

            StartEraser();
        }
        else
        {
            // 忽略其他的输入点
        }
    }

    public void EraserMove(InkingInputArgs args)
    {
        InkCanvas.EnsureInputConflicts();
        if (IsErasing && args.Id == MainEraserInputId)
        {
            // 擦除
            var eraserWidth = 50d;
            var eraserHeight = 70d;

            var rect = new Rect(args.Point.Point.X - eraserWidth / 2, args.Point.Point.Y - eraserHeight / 2, eraserWidth, eraserHeight);

            PointPathEraserManager.Move(rect.ToMauiRect());
        }
    }

    public void EraserUp(InkingInputArgs args)
    {
        InkCanvas.EnsureInputConflicts();
        if (IsErasing && args.Id == MainEraserInputId)
        {
            IsErasing = false;
            var pointPathEraserResult = PointPathEraserManager.Finish();
        }
    }

    public void Render(DrawingContext context)
    {
        context.Custom(new EraserModeCustomDrawOperation(this));
    }

    class EraserModeCustomDrawOperation : ICustomDrawOperation
    {
        public EraserModeCustomDrawOperation(AvaSkiaInkCanvasEraserMode eraserMode)
        {

        }

        public void Dispose()
        {

        }

        public bool Equals(ICustomDrawOperation? other)
        {
            return false;
        }

        public bool HitTest(Point p)
        {
            return false;
        }

        public void Render(ImmediateDrawingContext context)
        {
        }

        public Avalonia.Rect Bounds { get; }
    }

}