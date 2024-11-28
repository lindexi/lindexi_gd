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
using LightTextEditorPlus.Utils;
using LightTextEditorPlus.Document;

namespace LightTextEditorPlus.Rendering;

class RenderManager
{
    public RenderManager(SkiaTextEditor textEditor)
    {
        _textEditor = textEditor;
        TextEditorDebugConfiguration debugConfiguration = textEditor.TextEditorCore.DebugConfiguration;

        if (debugConfiguration.IsInDebugMode)
        {
            //_debugDrawCharBounds = SKColors.Bisque.WithAlpha(0xA0);
            //_debugDrawCharSpanBounds = SKColors.Red.WithAlpha(0xA0);
            //_debugDrawLineBounds = SKColors.Blue.WithAlpha(0x50);
        }

        _textEditor.TextEditorCore.CurrentSelectionChanged += TextEditorCore_CurrentSelectionChanged;
    }

    private readonly SkiaTextEditor _textEditor;

    #region 光标渲染

    private void TextEditorCore_CurrentSelectionChanged(object? sender, TextEditorValueChangeEventArgs<Selection> e)
    {
        if (_textEditor.TextEditorCore.IsDirty)
        {
            // 如果是脏的，那就不需要更新光标和选择区域的渲染，等待后续自动进入渲染
            return;
        }

        RenderInfoProvider renderInfoProvider = _textEditor.TextEditorCore.GetRenderInfo();
        UpdateCaretAndSelectionRender(renderInfoProvider, e.NewValue);
    }

    [MemberNotNull(nameof(_currentCaretAndSelectionRender))]
    private void UpdateCaretAndSelectionRender(RenderInfoProvider renderInfoProvider, Selection selection)
    {
        if (selection.IsEmpty)
        {
            // 无选择，只有光标
            CaretRenderInfo currentCaretRenderInfo = renderInfoProvider.GetCurrentCaretRenderInfo();
            Rect caretBounds = currentCaretRenderInfo.GetCaretBounds(_textEditor.CaretConfiguration.CaretWidth);

            SKColor caretColor = _textEditor.CaretConfiguration.CaretBrush
                                 // todo 获取当前前景色作为光标颜色
                                 ?? SKColors.Black;
            _currentCaretAndSelectionRender  = new TextEditorCaretSkiaRender(caretBounds.ToSKRect(), caretColor);
        }
        else
        {
            SKColor selectionColor = _textEditor.CaretConfiguration.SelectionBrush;

            IReadOnlyList<Rect> selectionBoundsList = renderInfoProvider.GetSelectionBoundsList(selection);

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
        Debug.Assert(_currentRender != null, "不可能一开始就获取当前渲染，必然调用过 Render 方法");

        return _currentRender;
    }

#pragma warning disable CS0649 // 从未对字段赋值，字段将一直保持其默认值。 调试的就忽略警告
    private SKColor _debugDrawCharBounds;
    private SKColor _debugDrawCharSpanBounds;
    private SKColor _debugDrawLineBounds;
    private SKPaint? _debugSkPaint;
#pragma warning restore CS0649 // 从未对字段赋值，字段将一直保持其默认值

    [MemberNotNull(nameof(_currentRender), nameof(_currentCaretAndSelectionRender))]
    public void Render(RenderInfoProvider renderInfoProvider)
    {
        Debug.Assert(!renderInfoProvider.IsDirty);

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

        Rect documentLayoutBounds = renderInfoProvider.GetDocumentLayoutBounds();

        var textWidth = (float) documentLayoutBounds.Width;
        var textHeight = (float) documentLayoutBounds.Height;

        using SKPictureRecorder skPictureRecorder = new SKPictureRecorder();

        using (SKCanvas canvas = skPictureRecorder.BeginRecording(SKRect.Create(0, 0, textWidth, textHeight)))
        {
            var stringBuilder = new StringBuilder();

            foreach (ParagraphRenderInfo paragraphRenderInfo in renderInfoProvider.GetParagraphRenderInfoList())
            {
                foreach (ParagraphLineRenderInfo lineInfo in paragraphRenderInfo.GetLineRenderInfoList())
                {
                    // 先不考虑缓存
                    LineDrawingArgument argument = lineInfo.Argument;
                    foreach (ReadOnlyListSpan<CharData> charList in argument.CharList.GetCharSpanContinuous())
                    {
                        CharData firstCharData = charList[0];

                        SkiaTextRunProperty skiaTextRunProperty = firstCharData.RunProperty.AsSkiaRunProperty();

                        using SKFont skFont = skiaTextRunProperty.GetRenderSKFont();

                        using SKPaint textRenderSKPaint = new SKPaint(skFont);
                        textRenderSKPaint.IsAntialias = true;

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

                            DrawDebugBounds(charData.GetBounds().ToSKRect(), _debugDrawCharBounds);

                            width += (float) charData.Size!.Value.Width;
                        }

                        SKRect charSpanBounds = SKRect.Create(x, y, width, height);
                        DrawDebugBounds(charSpanBounds, _debugDrawCharSpanBounds);

                        string text = stringBuilder.ToString();

                        if (!skFont.ContainsGlyphs(text))
                        {
                            // todo 处理无法渲染的字符，自动字符降级
                        }

                        // todo 这里应该是从 RunProperty 中获取颜色
                        textRenderSKPaint.Color = SKColors.Black;

                        //float x = skiaTextRenderInfo.X;
                        //float y = skiaTextRenderInfo.Y;

                        var baselineY = -skFont.Metrics.Ascent;

                        // 由于 Skia 的 DrawText 传入的 Point 是文本的最下方，因此需要调整 Y 值
                        y += baselineY;
                        // todo 不要一个个字符渲染，应该是一行渲染
                        canvas.DrawText(text, new SKPoint(x, y), textRenderSKPaint);
                    }

                    DrawDebugBounds(new Rect(argument.StartPoint, argument.Size).ToSKRect(), _debugDrawLineBounds);
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

    private SKPaint GetDebugPaint(SKColor color)
    {
        if (_debugSkPaint is null)
        {
            _debugSkPaint = new SKPaint();
            _debugSkPaint.Style = SKPaintStyle.Stroke;
            _debugSkPaint.StrokeWidth = 1;
        }

        _debugSkPaint.Color = color;
        return _debugSkPaint;
    }
}