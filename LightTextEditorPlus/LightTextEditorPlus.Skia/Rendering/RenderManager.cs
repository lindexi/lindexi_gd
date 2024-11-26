using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using LightTextEditorPlus.Core.Diagnostics;
using LightTextEditorPlus.Core.Document;
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
            _debugDrawCharBounds = SKColors.Bisque.WithAlpha(0xA0);
            _debugDrawCharSpanBounds = SKColors.Red.WithAlpha(0xA0);
            _debugDrawLineBounds = SKColors.Blue.WithAlpha(0x50);
        }
    }

    private readonly SkiaTextEditor _textEditor;

    /// <summary>
    /// 默认的光标宽度
    /// </summary>
    public const double DefaultCaretWidth = 2;

    private Rect CurrentCaretBounds { set; get; }

    // 在 Skia 里面的 SKPicture 就和 DX 的 Command 地位差不多，都是预先记录的绘制命令，而不是立刻进行绘制
    private TextEditorSkiaRender? _currentRender;

    public ITextEditorSkiaRender GetCurrentRender()
    {
        Debug.Assert(_currentRender != null, "不可能一开始就获取当前渲染，必然调用过 Render 方法");

        return _currentRender;
    }

    private SKColor _debugDrawCharBounds;
    private SKColor _debugDrawCharSpanBounds;
    private SKColor _debugDrawLineBounds;
    private SKPaint? _debugSkPaint;

    [MemberNotNull(nameof(_currentRender))]
    public void Render(RenderInfoProvider renderInfoProvider)
    {
        if (_currentRender is not null)
        {
            if (!_currentRender.IsUsed)
            {
                // 如果被使用了，那就交给使用方释放。如果没有被使用，那就直接释放
                _currentRender.Dispose();
            }

            _currentRender = null;
        }

        var textWidth = 1000;
        var textHeight = 1000;

        using SKPictureRecorder skPictureRecorder = new SKPictureRecorder();

        using (SKCanvas canvas = skPictureRecorder.BeginRecording(SKRect.Create(0, 0, textWidth, textHeight)))
        {
            CaretRenderInfo currentCaretRenderInfo = renderInfoProvider.GetCurrentCaretRenderInfo();
            Rect caretBounds = currentCaretRenderInfo.GetCaretBounds(DefaultCaretWidth);
            CurrentCaretBounds = caretBounds;

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

                        SKFont skFont = skiaTextRunProperty.GetRenderSKFont();

                        using SKPaint textRenderSKPaint = new SKPaint(skFont);
                        textRenderSKPaint.IsAntialias = true;

                        var runBounds = firstCharData.GetBounds();
                        var startPoint = runBounds.LeftTop;

                        float x = (float)startPoint.X;
                        float y = (float)startPoint.Y;
                        float width = 0;
                        float height = (float)runBounds.Height;

                        stringBuilder.Clear();

                        foreach (CharData charData in charList)
                        {
                            stringBuilder.Append(charData.CharObject.ToText());

                            DrawDebugBounds(charData.GetBounds().ToSKRect(), _debugDrawCharBounds);

                            width += (float)charData.Size!.Value.Width;
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