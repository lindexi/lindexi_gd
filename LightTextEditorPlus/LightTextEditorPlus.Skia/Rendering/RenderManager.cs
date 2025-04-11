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
    public void UpdateCaretAndSelectionRender(RenderInfoProvider renderInfoProvider, Selection selection)
    {
        if (selection.IsEmpty)
        {
            // 无选择，只有光标
            CaretRenderInfo currentCaretRenderInfo = renderInfoProvider.GetCurrentCaretRenderInfo();
            TextRect caretBounds = currentCaretRenderInfo.GetCaretBounds(TextEditor.CaretConfiguration.CaretThickness, IsOvertypeModeCaret);

            SkiaTextRunProperty? skiaTextRunProperty = currentCaretRenderInfo.CharData?.RunProperty.AsSkiaRunProperty();

            SKColor caretColor = TextEditor.CaretConfiguration.CaretBrush
                                 // 获取当前前景色作为光标颜色
                                 ?? skiaTextRunProperty?.Foreground
                                 ?? SKColors.Black;
            _currentCaretAndSelectionRender = new TextEditorCaretSkiaRender(caretBounds.ToSKRect(), caretColor);
        }
        else
        {
            SKColor selectionColor = TextEditor.CaretConfiguration.SelectionBrush;

            IReadOnlyList<TextRect> selectionBoundsList = renderInfoProvider.GetSelectionBoundsList(selection);

            _currentCaretAndSelectionRender = new TextEditorSelectionSkiaRender(selectionBoundsList, selectionColor);
        }
    }

    private ITextEditorCaretAndSelectionRenderSkiaRender? _currentCaretAndSelectionRender;

    /// <summary>
    /// 光标是否是覆盖模式、替换模式
    /// </summary>
    private bool IsOvertypeModeCaret { get; set; }

    public ITextEditorCaretAndSelectionRenderSkiaRender GetCurrentCaretAndSelectionRender(in CaretAndSelectionRenderContext renderContext)
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

    public ITextEditorContentSkiaRender GetCurrentTextRender()
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

        BaseSkiaTextRender textRender = GetSkiaTextRender();

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

        DocumentLayoutBounds layoutBounds = renderInfoProvider.GetDocumentLayoutBounds();
        TextRect documentLayoutBounds = layoutBounds.DocumentOutlineBounds;

        var textWidth = (float) documentLayoutBounds.Width;
        var textHeight = (float) documentLayoutBounds.Height;

        TextRect renderBounds = documentLayoutBounds;

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
            SkiaTextRenderResult skiaTextRenderResult = textRender.Render(new SkiaTextRenderArgument()
            {
                Canvas = canvas,
                RenderInfoProvider = renderInfoProvider,
                RenderBounds = renderBounds
            });
            renderBounds = skiaTextRenderResult.RenderBounds;

            SkiaTextEditorDebugConfiguration skiaTextEditorDebugConfiguration = TextEditor.DebugConfiguration;
            if (skiaTextEditorDebugConfiguration.IsInDebugMode)
            {
                textRender.DrawDebugBoundsInfo(canvas, renderBounds.ToSKRect(), skiaTextEditorDebugConfiguration.DebugDrawDocumentRenderBoundsInfo);
                textRender.DrawDebugBoundsInfo(canvas, layoutBounds.DocumentContentBounds.ToSKRect(), skiaTextEditorDebugConfiguration.DebugDrawDocumentContentBoundsInfo);
                textRender.DrawDebugBoundsInfo(canvas, layoutBounds.DocumentOutlineBounds.ToSKRect(), skiaTextEditorDebugConfiguration.DebugDrawDocumentOutlineBoundsInfo);
            }
        }

        SKPicture skPicture = skPictureRecorder.EndRecording();
        _currentRender = new TextEditorSkiaRender(TextEditor, skPicture, renderBounds);
    }

    private BaseSkiaTextRender GetSkiaTextRender()
    {
        if (TextEditor.TextEditorCore.ArrangingType.IsHorizontal)
        {
            if (_textRender is not HorizontalSkiaTextRender)
            {
                _textRender?.Dispose();
                _textRender = new HorizontalSkiaTextRender(this);
            }

            return _textRender;
        }
        else if (TextEditor.TextEditorCore.ArrangingType.IsVertical)
        {
            if (_textRender is not VerticalSkiaTextRender)
            {
                _textRender?.Dispose();
                _textRender = new VerticalSkiaTextRender(this);
            }

            return _textRender;
        }
        else
        {
            throw new ArgumentOutOfRangeException();
        }

        throw new NotSupportedException();
    }

    private BaseSkiaTextRender? _textRender;
}
