using LightTextEditorPlus.Core.Primitive;

using SkiaSharp;

namespace LightTextEditorPlus.Rendering;

class TextEditorSkiaRender : ITextEditorSkiaRender
{
    public TextEditorSkiaRender(SKPicture picture, Rect currentCaretBounds)
    {
        Picture = picture;
        CurrentCaretBounds = currentCaretBounds;
    }

    internal bool IsUsed { get; set; }

    internal SKPicture Picture { get; }

    // todo 考虑光标的绘制
    public Rect CurrentCaretBounds { get; internal set; }

    public void Dispose()
    {
        Picture.Dispose();
    }

    public void Render(SKCanvas canvas)
    {
        canvas.DrawPicture(Picture);
    }
}