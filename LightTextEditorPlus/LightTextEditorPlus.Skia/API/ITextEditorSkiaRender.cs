using System;
using LightTextEditorPlus.Core.Primitive;

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
    TextRect RenderBounds { get; }
    bool IsObsoleted { get; }
    bool IsDisposed { get; }
}

public interface ITextEditorCaretAndSelectionRenderSkiaRender : ITextEditorSkiaRender
{
}
