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
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Rendering;
using SkiaSharp;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Diagnostics;
using LightTextEditorPlus.Utils;
using LightTextEditorPlus.Document;

namespace LightTextEditorPlus.Rendering;

class RenderManager
{
    public RenderManager(SkiaTextEditor textEditor)
    {
        _textEditor = textEditor;
    }

    private readonly SkiaTextEditor _textEditor;

    #region 光标渲染

    [MemberNotNull(nameof(_currentCaretAndSelectionRender))]
    public void UpdateCaretAndSelectionRender(RenderInfoProvider renderInfoProvider, Selection selection)
    {
        if (selection.IsEmpty)
        {
            // 无选择，只有光标
            CaretRenderInfo currentCaretRenderInfo = renderInfoProvider.GetCurrentCaretRenderInfo();
            TextRect caretBounds = currentCaretRenderInfo.GetCaretBounds(_textEditor.CaretConfiguration.CaretWidth);

            SkiaTextRunProperty? skiaTextRunProperty = currentCaretRenderInfo.CharData?.RunProperty.AsSkiaRunProperty();

            SKColor caretColor = _textEditor.CaretConfiguration.CaretBrush
                                 // 获取当前前景色作为光标颜色
                                 ?? skiaTextRunProperty?.Foreground
                                 ?? SKColors.Black;
            _currentCaretAndSelectionRender = new TextEditorCaretSkiaRender(caretBounds.ToSKRect(), caretColor);
        }
        else
        {
            SKColor selectionColor = _textEditor.CaretConfiguration.SelectionBrush;

            IReadOnlyList<TextRect> selectionBoundsList = renderInfoProvider.GetSelectionBoundsList(selection);

            _currentCaretAndSelectionRender = new TextEditorSelectionSkiaRender(selectionBoundsList, selectionColor);
        }
    }

    private ITextEditorCaretAndSelectionRenderSkiaRender? _currentCaretAndSelectionRender;

    public ITextEditorCaretAndSelectionRenderSkiaRender GetCurrentCaretAndSelectionRender()
    {
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
            Debug.Assert(!_textEditor.TextEditorCore.IsDirty);
            RenderInfoProvider renderInfoProvider = _textEditor.TextEditorCore.GetRenderInfo();
            Render(renderInfoProvider);
        }

        Debug.Assert(!_currentRender.IsDisposed);

        _currentRender.IsUsed = true;
        return _currentRender;
    }

    private SKColor? _debugDrawCharBoundsColor;
    private SKColor? _debugDrawCharSpanBoundsColor;
    private SKColor? _debugDrawLineBoundsColor;
    private SKPaint? _debugSKPaint;

    [MemberNotNull(nameof(_currentRender), nameof(_currentCaretAndSelectionRender))]
    public void Render(RenderInfoProvider renderInfoProvider)
    {
        Debug.Assert(!renderInfoProvider.IsDirty);

        UpdateDebugColor();

        UpdateCaretAndSelectionRender(renderInfoProvider, _textEditor.TextEditorCore.CurrentSelection);

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

        TextRect documentLayoutBounds = renderInfoProvider.GetDocumentLayoutBounds();

        var textWidth = (float) documentLayoutBounds.Width;
        var textHeight = (float) documentLayoutBounds.Height;

        TextRect renderBounds = documentLayoutBounds;

        using SKPictureRecorder skPictureRecorder = new SKPictureRecorder();

        float renderWidth = textWidth;
        float renderHeight = textHeight;

        if (_textEditor.DebugConfiguration.IsInDebugMode)
        {
            if (renderWidth < 1)
            {
                double documentWidth = _textEditor.TextEditorCore.DocumentManager.DocumentWidth;
                if (documentWidth > 0)
                {
                    renderWidth = (float) documentWidth;
                }
            }
        }

        using (SKCanvas canvas = skPictureRecorder.BeginRecording(SKRect.Create(0, 0, renderWidth, renderHeight)))
        {
            var stringBuilder = new StringBuilder();

            foreach (ParagraphRenderInfo paragraphRenderInfo in renderInfoProvider.GetParagraphRenderInfoList())
            {
                foreach (ParagraphLineRenderInfo lineInfo in paragraphRenderInfo.GetLineRenderInfoList())
                {
                    // 先不考虑缓存
                    LineDrawingArgument argument = lineInfo.Argument;
                    foreach (TextReadOnlyListSpan<CharData> charList in argument.CharList.GetCharSpanContinuous())
                    {
                        CharData firstCharData = charList[0];

                        SkiaTextRunProperty skiaTextRunProperty = firstCharData.RunProperty.AsSkiaRunProperty();

                        // 不需要在这里处理字体回滚，在输入的过程中已经处理过了
                        ////  考虑字体回滚问题
                        RenderingRunPropertyInfo renderingRunPropertyInfo = skiaTextRunProperty.GetRenderingRunPropertyInfo(firstCharData.CharObject.CodePoint);

                        SKFont skFont = renderingRunPropertyInfo.Font;

                        SKPaint textRenderSKPaint = renderingRunPropertyInfo.Paint;

                        var runBounds = firstCharData.GetBounds();
                        var startPoint = runBounds.LeftTop;

                        float x = (float) startPoint.X;
                        float y = (float) startPoint.Y;
                        float width = 0;
                        float height = (float) runBounds.Height;

                        stringBuilder.Clear();

                        foreach (CharData charData in charList)
                        {
                            stringBuilder.Append(charData.CharObject.ToText());

                            DrawDebugBounds(charData.GetBounds().ToSKRect(), _debugDrawCharBoundsColor);

                            width += (float) charData.Size!.Value.Width;
                        }

                        SKRect charSpanBounds = SKRect.Create(x, y, width, height);
                        DrawDebugBounds(charSpanBounds, _debugDrawCharSpanBoundsColor);
                        renderBounds = renderBounds.Union(charSpanBounds.ToTextRect());

                        string text = stringBuilder.ToString();

                        if (!skFont.ContainsGlyphs(text))
                        {
                            // 预计不会出现这样的问题，在渲染之前已经处理过了
                            throw new TextEditorInnerException($"文本框架内应该确保进入渲染层时，不会出现字体不能包含字符的情况");
                        }

                        // 绘制四线三格调试信息
                        if (_textEditor.DebugConfiguration.ShowHandwritingPaperDebugInfo)
                        {
                            CharHandwritingPaperInfo charHandwritingPaperInfo =
                                renderInfoProvider.GetHandwritingPaperInfo(in lineInfo, firstCharData);
                            DrawDebugHandwritingPaper(canvas, charSpanBounds, charHandwritingPaperInfo);
                        }

                        //float x = skiaTextRenderInfo.X;
                        //float y = skiaTextRenderInfo.Y;

                        var baselineY = /*skFont.Metrics.Leading +*/ (-skFont.Metrics.Ascent);

                        // 由于 Skia 的 DrawText 传入的 Point 是文本的最下方，因此需要调整 Y 值
                        y += baselineY;
                        canvas.DrawText(text, new SKPoint(x, y), textRenderSKPaint);
                    }

                    if (argument.CharList.Count == 0)
                    {
                        // 空行
                        // 绘制四线三格调试信息
                        if (_textEditor.DebugConfiguration.ShowHandwritingPaperDebugInfo)
                        {
                            CharHandwritingPaperInfo charHandwritingPaperInfo =
                                renderInfoProvider.GetHandwritingPaperInfo(in lineInfo);
                            DrawDebugHandwritingPaper(canvas, new TextRect(argument.StartPoint, argument.LineSize with
                            {
                                // 空行是 0 宽度，需要将其设置为整个文本的宽度才好计算
                                Width = renderInfoProvider.TextEditor.DocumentManager.DocumentWidth,
                            }).ToSKRect(), charHandwritingPaperInfo);
                        }
                    }

                    DrawDebugBounds(new TextRect(argument.StartPoint, argument.LineSize).ToSKRect(), _debugDrawLineBoundsColor);
                }
            }

            void DrawDebugBounds(SKRect bounds, SKColor? color)
            {
                if (color is null)
                {
                    return;
                }

                SKPaint debugPaint = GetDebugPaint(color.Value);
                canvas.DrawRect(bounds, debugPaint);
            }
        }

        SKPicture skPicture = skPictureRecorder.EndRecording();
        _currentRender = new TextEditorSkiaRender(_textEditor, skPicture, renderBounds);
    }

    /// <summary>
    /// 绘制四线三格调试信息
    /// </summary>
    /// <param name="canvas"></param>
    /// <param name="charSpanBounds"></param>
    /// <param name="charHandwritingPaperInfo"></param>
    private void DrawDebugHandwritingPaper(SKCanvas canvas, SKRect charSpanBounds, in CharHandwritingPaperInfo charHandwritingPaperInfo)
    {
        HandwritingPaperDebugDrawInfo drawInfo = _textEditor.DebugConfiguration.HandwritingPaperDebugDrawInfo;
        DrawLine(charHandwritingPaperInfo.TopLineGradation, drawInfo.TopLineGradationDebugDrawInfo);
        DrawLine(charHandwritingPaperInfo.MiddleLineGradation, drawInfo.MiddleLineGradationDebugDrawInfo);
        DrawLine(charHandwritingPaperInfo.BaselineGradation, drawInfo.BaselineGradationDebugDrawInfo);
        DrawLine(charHandwritingPaperInfo.BottomLineGradation, drawInfo.BottomLineGradationDebugDrawInfo);

        void DrawLine(double gradation, HandwritingPaperGradationDebugDrawInfo info)
        {
            SKPaint debugPaint = GetDebugPaint(info.DebugColor);
            debugPaint.StrokeWidth = info.StrokeThickness;

            float y = (float) gradation;
            float x = charSpanBounds.Left;
            float width = charSpanBounds.Width;
            canvas.DrawLine(x, y, x + width, y, debugPaint);
        }
    }

    private SKPaint GetDebugPaint(SKColor color)
    {
        _debugSKPaint ??= new SKPaint();

        _debugSKPaint.Style = SKPaintStyle.Stroke;
        _debugSKPaint.StrokeWidth = 1;
        _debugSKPaint.Color = color;
        return _debugSKPaint;
    }

    private void UpdateDebugColor()
    {
        SkiaTextEditorDebugConfiguration debugConfiguration = _textEditor.DebugConfiguration;

        if (debugConfiguration.IsInDebugMode)
        {
            _debugDrawCharBoundsColor = debugConfiguration.DebugDrawCharBoundsColor;
            _debugDrawCharSpanBoundsColor = debugConfiguration.DebugDrawCharSpanBoundsColor;
            _debugDrawLineBoundsColor = debugConfiguration.DebugDrawLineBoundsColor;
        }
    }
}
