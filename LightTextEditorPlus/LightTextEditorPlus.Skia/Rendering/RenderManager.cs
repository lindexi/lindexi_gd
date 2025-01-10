using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Diagnostics;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Events;
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

            SKColor caretColor = _textEditor.CaretConfiguration.CaretBrush
                                 // todo 获取当前前景色作为光标颜色
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

    public ITextEditorSkiaRender GetCurrentTextRender()
    {
        //Debug.Assert(_currentRender != null, "不可能一开始就获取当前渲染，必然调用过 Render 方法");
        if (_currentRender is null)
        {
            // 首次渲染，需要尝试获取一下
            Debug.Assert(!_textEditor.TextEditorCore.IsDirty);
            RenderInfoProvider renderInfoProvider = _textEditor.TextEditorCore.GetRenderInfo();
            Render(renderInfoProvider);
        }

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
                _currentRender.Dispose();
            }

            _currentRender = null;
        }

        TextRect documentLayoutBounds = renderInfoProvider.GetDocumentLayoutBounds();

        var textWidth = (float) documentLayoutBounds.Width;
        var textHeight = (float) documentLayoutBounds.Height;

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

                        string text = stringBuilder.ToString();

                        if (!skFont.ContainsGlyphs(text))
                        {
                            // todo 处理无法渲染的字符，自动字符降级
                            // 预计不会出现这样的问题，在渲染之前已经处理过了
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
        _currentRender = new TextEditorSkiaRender(skPicture);
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
