using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Diagnostics;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Events;
using LightTextEditorPlus.Core.Exceptions;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Rendering;
using SkiaSharp;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Diagnostics;
using LightTextEditorPlus.Utils;
using LightTextEditorPlus.Document;
using LightTextEditorPlus.Rendering.Core;

namespace LightTextEditorPlus.Rendering;

class RenderManager
{
    public RenderManager(SkiaTextEditor textEditor)
    {
        TextEditor = textEditor;
    }

    public SkiaTextEditor TextEditor { get; }
    #region 光标渲染

    /// <summary>
    /// 更新光标和选择的渲染
    /// </summary>
    /// <param name="renderInfoProvider"></param>
    /// <param name="selection"></param>
    [MemberNotNull(nameof(_currentCaretAndSelectionRender))]
    public void UpdateCaretAndSelectionRender(RenderInfoProvider renderInfoProvider, in Selection selection)
    {
        _currentCaretAndSelectionRender =
            BuildCaretAndSelectionRender(renderInfoProvider, in selection, new CaretAndSelectionRenderContext(IsOvertypeModeCaret));
    }

    public ITextEditorCaretAndSelectionRenderSkiaRenderer BuildCaretAndSelectionRender
        (RenderInfoProvider renderInfoProvider, in Selection selection, in CaretAndSelectionRenderContext renderContext)
    {
        if (selection.IsEmpty)
        {
            // 无选择，只有光标
            CaretRenderInfo currentCaretRenderInfo = renderInfoProvider.GetCurrentCaretRenderInfo();
            TextRect caretBounds = currentCaretRenderInfo.GetCaretBounds(TextEditor.CaretConfiguration.CaretThickness, renderContext.IsOvertypeModeCaret);

            SKColor? caretColor = TextEditor.CaretConfiguration.CaretBrush;
            if (caretColor == null)
            {
                // 如果没有设置光标颜色，则使用当前光标的前景色
                IReadOnlyRunProperty currentCaretRunProperty = TextEditor.TextEditorCore.DocumentManager.CurrentCaretRunProperty;
                caretColor = currentCaretRunProperty.AsSkiaRunProperty().Foreground.AsSolidColor();
            }

            return new TextEditorCaretSkiaRender(caretBounds.ToSKRect(), caretColor.Value);
        }
        else
        {
            SKColor selectionColor = TextEditor.CaretConfiguration.SelectionBrush;

            IReadOnlyList<TextRect> selectionBoundsList = renderInfoProvider.GetSelectionBoundsList(selection);

            return new TextEditorSelectionSkiaRender(selectionBoundsList, selectionColor);
        }
    }

    private ITextEditorCaretAndSelectionRenderSkiaRenderer? _currentCaretAndSelectionRender;

    /// <summary>
    /// 光标是否是覆盖模式、替换模式
    /// </summary>
    private bool IsOvertypeModeCaret { get; set; }

    public ITextEditorCaretAndSelectionRenderSkiaRenderer GetCurrentCaretAndSelectionRender(in CaretAndSelectionRenderContext renderContext)
    {
        if (IsOvertypeModeCaret != renderContext.IsOvertypeModeCaret)
        {
            IsOvertypeModeCaret = renderContext.IsOvertypeModeCaret;
            if (TextEditor.TextEditorCore.TryGetRenderInfo(out var renderInfo))
            {
                UpdateCaretAndSelectionRender(renderInfo, TextEditor.TextEditorCore.CurrentSelection);
            }
        }

        Debug.Assert(_currentCaretAndSelectionRender != null, "不可能一开始就获取当前渲染，必然调用过 Render 方法");
        return _currentCaretAndSelectionRender;
    }

    #endregion

    // 在 Skia 里面的 SKPicture 就和 DX 的 Command 地位差不多，都是预先记录的绘制命令，而不是立刻进行绘制
    private TextEditorSkiaRender? _currentRender;

    public ITextEditorContentSkiaRenderer GetCurrentTextRender()
    {
        //Debug.Assert(_currentRender != null, "不可能一开始就获取当前渲染，必然调用过 Render 方法");
        if (_currentRender is null)
        {
            // 首次渲染，需要尝试获取一下
            Debug.Assert(!TextEditor.TextEditorCore.IsDirty);
            RenderInfoProvider renderInfoProvider = TextEditor.TextEditorCore.GetRenderInfo();
            Render(renderInfoProvider);
        }

        Debug.Assert(!_currentRender.IsDisposed);

        _currentRender.IsUsed = true;
        return _currentRender;
    }

    [MemberNotNull(nameof(_currentRender), nameof(_currentCaretAndSelectionRender))]
    public void Render(RenderInfoProvider renderInfoProvider)
    {
        Debug.Assert(!renderInfoProvider.IsDirty);

        UpdateCaretAndSelectionRender(renderInfoProvider, TextEditor.TextEditorCore.CurrentSelection);

        if (_currentRender is not null)
        {
            if (!_currentRender.IsUsed)
            {
                // 如果被使用了，那就交给使用方释放。如果没有被使用，那就直接释放
                // 比如快速的两次布局，此时 UI 框架一次渲染都没有触发，那么第一次的渲染就会被释放
                _currentRender.Dispose("RenderManager.Render IsUsed=false");
            }
            else
            {
                // 标记为需要释放的，因为很快就进行更新了
                _currentRender.IsObsoleted = true;
            }

            _currentRender = null;
        }

        TextEditorSkiaRender render = BuildTextEditorSkiaRender(new TextEditorSkiaRenderContext(renderInfoProvider, null));

        _currentRender = render;
    }

    /// <summary>
    /// 为什么会考虑使用 SKPicture 来进行渲染？原因是在上层 UI 程序里面，可能会面临未更改但多次渲染的情况。此时文本内容没有变化，通过 SKPicture 可以缓存渲染命令，提升性能且不会过多占用内存
    /// </summary>
    /// <param name="renderContext"></param>
    /// <returns></returns>
    public TextEditorSkiaRender BuildTextEditorSkiaRender(in TextEditorSkiaRenderContext renderContext)
    {
        RenderInfoProvider renderInfoProvider = renderContext.RenderInfoProvider;
        BaseSkiaTextRenderer textRenderer = GetSkiaTextRender();

        DocumentLayoutBounds layoutBounds = renderInfoProvider.GetDocumentLayoutBounds();
        TextRect documentOutlineBounds = layoutBounds.DocumentOutlineBounds;

        var textWidth = (float) documentOutlineBounds.Width;
        var textHeight = (float) documentOutlineBounds.Height;

        TextRect renderBounds = documentOutlineBounds;

        using SKPictureRecorder skPictureRecorder = new SKPictureRecorder();

        float renderWidth = textWidth;
        float renderHeight = textHeight;

        if (TextEditor.DebugConfiguration.IsInDebugMode)
        {
            if (renderWidth < 1)
            {
                double documentWidth = TextEditor.TextEditorCore.DocumentManager.DocumentWidth;
                if (documentWidth > 0)
                {
                    renderWidth = (float) documentWidth;
                }
            }
        }

        using (SKCanvas canvas = skPictureRecorder.BeginRecording(SKRect.Create(0, 0, renderWidth, renderHeight)))
        {
            SkiaTextRenderResult skiaTextRenderResult = textRenderer.Render(new SkiaTextRenderArgument()
            {
                Canvas = canvas,
                RenderInfoProvider = renderInfoProvider,
                RenderBounds = renderBounds,
                Viewport = renderContext.Viewport,
            });
            renderBounds = skiaTextRenderResult.RenderBounds;

            SkiaTextEditorDebugConfiguration skiaTextEditorDebugConfiguration = TextEditor.DebugConfiguration;
            if (skiaTextEditorDebugConfiguration.IsInDebugMode)
            {
                textRenderer.DrawDebugBoundsInfo(canvas, renderBounds.ToSKRect(), skiaTextEditorDebugConfiguration.DebugDrawDocumentRenderBoundsInfo);
                textRenderer.DrawDebugBoundsInfo(canvas, layoutBounds.DocumentContentBounds.ToSKRect(), skiaTextEditorDebugConfiguration.DebugDrawDocumentContentBoundsInfo);
                textRenderer.DrawDebugBoundsInfo(canvas, layoutBounds.DocumentOutlineBounds.ToSKRect(), skiaTextEditorDebugConfiguration.DebugDrawDocumentOutlineBoundsInfo);
            }
        }

        SKPicture skPicture = skPictureRecorder.EndRecording();
        var render = new TextEditorSkiaRender(TextEditor, skPicture, renderBounds);
        return render;
    }

    private BaseSkiaTextRenderer GetSkiaTextRender()
    {
        if (TextEditor.TextEditorCore.ArrangingType.IsHorizontal)
        {
            if (_textRender is not HorizontalSkiaTextRenderer)
            {
                _textRender?.Dispose();
                _textRender = new HorizontalSkiaTextRenderer(this);
            }

            return _textRender;
        }
        else if (TextEditor.TextEditorCore.ArrangingType.IsVertical)
        {
            if (_textRender is not VerticalSkiaTextRenderer)
            {
                _textRender?.Dispose();
                _textRender = new VerticalSkiaTextRenderer(this);
            }

            return _textRender;
        }
        else
        {
            throw new ArgumentOutOfRangeException();
        }
    }

    private BaseSkiaTextRenderer? _textRender;
}

/// <summary>
/// 渲染上下文
/// </summary>
/// <param name="RenderInfoProvider"></param>
/// <param name="Viewport">可见范围，为空则代表需要全文档渲染</param>
public readonly record struct TextEditorSkiaRenderContext(RenderInfoProvider RenderInfoProvider, TextRect? Viewport);