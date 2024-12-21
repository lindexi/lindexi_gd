using System.Collections.Generic;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Utils;
using SkiaSharp;

namespace LightTextEditorPlus.Rendering;

class TextEditorSkiaRender : ITextEditorContentSkiaRender
{
    public TextEditorSkiaRender(SKPicture picture)
    {
        _picture = picture;
    }

    internal bool IsUsed { get; set; }

    private readonly SKPicture _picture;

    public void Dispose()
    {
        _picture.Dispose();
    }

    public void Render(SKCanvas canvas)
    {
        canvas.DrawPicture(_picture);
    }

    private int _count;

    public void AddReference()
    {
        _count++;
    }

    public void ReleaseReference()
    {
        _count--;
        if (_count == 0)
        {
            Dispose();
        }
    }
}

record TextEditorSelectionSkiaRender(IReadOnlyList<TextRect> SelectionBoundsList, SKColor SelectionColor) : ITextEditorCaretAndSelectionRenderSkiaRender
{
    public void Render(SKCanvas canvas)
    {
        using SKPaint skPaint = new SKPaint();
        skPaint.Style = SKPaintStyle.Fill;
        skPaint.Color = SelectionColor;
        foreach (TextRect selectionBounds in SelectionBoundsList)
        {
            canvas.DrawRect(selectionBounds.ToSKRect(), skPaint);
        }
    }

    public void AddReference()
    {
    }

    public void ReleaseReference()
    {
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

    public void AddReference()
    {
    }

    public void ReleaseReference()
    {
    }

    public void Dispose()
    {
    }
}

