using SkiaSharp;

namespace LightTextEditorPlus;

/// <summary>
/// 文本的 Skia 渲染器
/// </summary>
public interface ITextEditorSkiaRender : IDisposable
{
    void Render(SKCanvas canvas);

    void AddReference();
    void ReleaseReference();
}

public interface ITextEditorContentSkiaRender : ITextEditorSkiaRender
{
}

public interface ITextEditorCaretAndSelectionRenderSkiaRender : ITextEditorSkiaRender
{
}