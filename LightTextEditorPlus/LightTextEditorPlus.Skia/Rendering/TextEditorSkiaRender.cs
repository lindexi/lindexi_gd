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