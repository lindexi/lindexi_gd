using SkiaSharp;

namespace LightTextEditorPlus;

/// <summary>
/// 文本的 Skia 渲染器
/// </summary>
public interface ITextEditorSkiaRender
{
    void Render(SKCanvas canvas);
}