using System;
using System.Collections.Generic;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Utils;
using SkiaSharp;

namespace LightTextEditorPlus.Rendering;

class TextEditorSkiaRender : ITextEditorContentSkiaRender
{
    public TextEditorSkiaRender(SkiaTextEditor textEditor, SKPicture picture, TextRect renderBounds)
    {
        _picture = picture;
        RenderBounds = renderBounds;
        _textEditor = textEditor;
        IsInDebugMode = _textEditor.TextEditorCore.IsInDebugMode;
    }

    /// <summary>
    /// 关联的文本编辑器。此字段仅用于调试，切不要在此字段上调用任何方法，因为可能是在渲染线程上调用，而不是 UI 线程上调用
    /// </summary>
    private readonly SkiaTextEditor _textEditor;

    public TextRect RenderBounds { get; }

    private readonly SKPicture _picture;

    /// <summary>
    /// 是否已经被返回出去被使用了，被使用了的，就应该将释放权给到外部，而不是自己释放
    /// </summary>
    /// 如果为 false 则表示尚未在渲染线程中使用，为 true 则表示可能在渲染线程中使用了。一旦在渲染线程中使用了，就不能在 UI 线程中释放，就需要通过 <see cref="IsObsoleted"/> 交给 UI 线程来释放
    internal bool IsUsed { get; set; }

    /// <summary>
    /// 是否已经被标记为过时了。在 UI 线程被文本编辑器标记为过时了，但是在渲染线程上可能还在使用中，于是渲染线程应该判断一下这个属性，决定是否在渲染线程释放
    /// </summary>
    public bool IsObsoleted { get; set; }

    public bool IsDisposed { get; private set; }
    public bool IsInDebugMode { get; }

    public string? DisposeReason { get; private set; }

    public void Dispose(string disposeReason)
    {
        DisposeReason = disposeReason;
        IDisposable disposable = this;
        disposable.Dispose();
    }

    void IDisposable.Dispose()
    {
        _picture.Dispose();
        IsDisposed = true;
    }

    public void Render(SKCanvas canvas)
    {
        if (IsDisposed)
        {
#if DEBUG
            var name = _textEditor.TextEditorCore.DebugName;
            string? disposeReason = DisposeReason;
            GC.KeepAlive(name);
            GC.KeepAlive(disposeReason);
#endif
        }

        canvas.DrawPicture(_picture);
    }

    private int _count;

    public void AddReference()
    {
        if (IsDisposed)
        {
            // 在这里打断点，这是不符合预期的分支
        }

        _count++;
    }

    public void ReleaseReference()
    {
        _count--;
        if (_count == 0 && IsObsoleted)
        {
            Dispose("ReleaseReference to 0");
        }
    }
}

/// <summary>
/// 选择和光标范围的渲染器
/// </summary>
/// <param name="SelectionBoundsList"></param>
/// <param name="SelectionColor"></param>
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