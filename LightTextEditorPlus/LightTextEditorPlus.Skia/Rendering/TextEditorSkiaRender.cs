using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Utils;
using SkiaSharp;

namespace LightTextEditorPlus.Rendering;

class TextEditorSkiaRender : ITextEditorSkiaRender
{
    public TextEditorSkiaRender(SKPicture picture)
    {
        _picture = picture;
    }

    internal bool IsUsed { get; set; }

    private readonly SKPicture _picture;

    // todo 考虑光标的绘制

    public void Dispose()
    {
        _picture.Dispose();
    }

    public void Render(SKCanvas canvas)
    {
        canvas.DrawPicture(_picture);
    }
}

record TextEditorSelectionSkiaRender(IReadOnlyList<Rect> SelectionBoundsList, SKColor SelectionColor) : ITextEditorCaretAndSelectionRenderSkiaRender
{
    public void Render(SKCanvas canvas)
    {
        using SKPaint skPaint = new SKPaint();
        skPaint.Style = SKPaintStyle.Fill;
        skPaint.Color = SelectionColor;
        foreach (Rect selectionBounds in SelectionBoundsList)
        {
            canvas.DrawRect(selectionBounds.ToSKRect(), skPaint);
        }
    }

    public void Dispose()
    {
    }
}

record TextEditorCaretSkiaRender(SKRect CaretBounds, SKColor CaretColor) : ITextEditorCaretAndSelectionRenderSkiaRender
{
    public void Render(SKCanvas canvas)
    {
        using SKPaint skPaint = new SKPaint();
        skPaint.Style = SKPaintStyle.Fill;
        skPaint.Color = CaretColor;
        canvas.DrawRect(CaretBounds, skPaint);
    }

    public void Dispose()
    {
    }
}

