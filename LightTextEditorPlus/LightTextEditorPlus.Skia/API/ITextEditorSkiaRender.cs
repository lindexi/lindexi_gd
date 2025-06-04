using System;
using LightTextEditorPlus.Core.Primitive;
using SkiaSharp;

namespace LightTextEditorPlus;

/// <summary>
/// 文本的 Skia 渲染器
/// </summary>
public interface ITextEditorSkiaRender : IDisposable
{
    /// <summary>
    /// 渲染
    /// </summary>
    /// <param name="canvas"></param>
    void Render(SKCanvas canvas);

    /// <summary>
    /// 添加引用计数
    /// </summary>
    void AddReference();

    /// <summary>
    /// 释放引用计数
    /// </summary>
    void ReleaseReference();
}

/// <summary>
/// 文本编辑器的 Skia 渲染内容
/// </summary>
public interface ITextEditorContentSkiaRender : ITextEditorSkiaRender
{
    /// <summary>
    /// 渲染范围
    /// </summary>
    TextRect RenderBounds { get; }

    /// <summary>
    /// 是否已经被废弃
    /// </summary>
    bool IsObsoleted { get; }

    /// <summary>
    /// 是否已经被释放
    /// </summary>
    bool IsDisposed { get; }
}

/// <summary>
/// 光标和选择的 Skia 渲染内容
/// </summary>
public interface ITextEditorCaretAndSelectionRenderSkiaRender : ITextEditorSkiaRender
{
}