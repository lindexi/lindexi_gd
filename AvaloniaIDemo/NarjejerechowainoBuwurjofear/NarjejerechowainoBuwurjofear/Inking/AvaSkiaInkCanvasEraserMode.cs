using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;

namespace NarjejerechowainoBuwurjofear.Inking;

class AvaSkiaInkCanvasEraserMode
{
    public AvaSkiaInkCanvasEraserMode(AvaSkiaInkCanvas inkCanvas)
    {
        InkCanvas = inkCanvas;
    }

    public AvaSkiaInkCanvas InkCanvas { get; }
    public bool IsErasing { get; private set; }
    private int MainEraserInputId { set; get; }

 

    public void StartEraser()
    {
        var staticStrokeList = InkCanvas.StaticStrokeList;
        
    }

    public void EraserDown(InkingInputArgs args)
    {
        InkCanvas.EnsureInputConflicts();
        if (!IsErasing)
        {
            MainEraserInputId = args.Id;

            IsErasing = true;
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
        }
    }

    public void EraserUp(InkingInputArgs args)
    {
        InkCanvas.EnsureInputConflicts();
        if (IsErasing && args.Id == MainEraserInputId)
        {
            IsErasing = false;
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

        public Rect Bounds { get; }
    }

    class ErasingSkiaStroke
    {
        public ErasingSkiaStroke(SkiaStroke originSkiaStroke)
        {
            OriginSkiaStroke = originSkiaStroke;
        }

        public SkiaStroke OriginSkiaStroke { get; }
    }

    readonly record struct PointListSpan(int Start, int Length);
}