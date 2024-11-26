using System.Text;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Rendering;

using SkiaSharp;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Utils;
using LightTextEditorPlus.Document;

namespace LightTextEditorPlus.Rendering;

class RenderManager : IRenderManager, ITextEditorSkiaRender
{
    public RenderManager(SkiaTextEditor textEditor)
    {
        _textEditor = textEditor;
    }

    private readonly SkiaTextEditor _textEditor;

    private List<SkiaTextRenderInfo>? RenderInfoList { set; get; }

    /// <summary>
    /// 默认的光标宽度
    /// </summary>
    public const double DefaultCaretWidth = 2;

    private Rect CurrentCaretBounds { set; get; }

    private SKBitmap? _debugBitmap;

    public void Render(RenderInfoProvider renderInfoProvider)
    {
        var textWidth = 1000;
        var textHeight = 1000;
        if (_debugBitmap != null && (_debugBitmap.Width != textWidth || _debugBitmap.Height != textHeight))
        {
            _debugBitmap.Dispose();
            _debugBitmap = null;
        }

        _debugBitmap ??= new SKBitmap(textWidth, textHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
        using SKCanvas debugSkCanvas = new SKCanvas(_debugBitmap);
        using SKPaint debugSkPaint = new SKPaint();
        debugSkPaint.Color = SKColors.Blue.WithAlpha(0x50);
        debugSkPaint.Style = SKPaintStyle.Stroke;
        debugSkPaint.StrokeWidth = 1;
        debugSkPaint.IsAntialias = true;

        var list = new List<SkiaTextRenderInfo>();

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
                    var runBounds = firstCharData.GetBounds();
                    var startPoint = runBounds.LeftTop;

                    float x = (float) startPoint.X;
                    float y = (float) startPoint.Y;
                    float width = (float) runBounds.Width;
                    float height = (float) runBounds.Height;

                    stringBuilder.Clear();

                    foreach (CharData charData in charList)
                    {
                        stringBuilder.Append(charData.CharObject.ToText());

                        width += (float) charData.Size!.Value.Width;
                    }

                    var skiaTextRenderInfo = new SkiaTextRenderInfo(stringBuilder.ToString(), x, y, width, height, firstCharData.RunProperty);
                    list.Add(skiaTextRenderInfo);
                }

                Rect rect = new Rect(argument.StartPoint, argument.Size);
                debugSkCanvas.DrawRect(rect.ToSKRect(), debugSkPaint);
            }
        }

        RenderInfoList = list;
    }

    public void Render(SKCanvas canvas)
    {
        if (RenderInfoList is null)
        {
            return;
        }

        foreach (SkiaTextRenderInfo skiaTextRenderInfo in RenderInfoList)
        {
            SkiaTextRunProperty skiaTextRunProperty = skiaTextRenderInfo.RunProperty.AsSkiaRunProperty();
            SKFont skFont = skiaTextRunProperty.GetRenderSKFont();

            using SKPaint textRenderSKPaint = new SKPaint(skFont);
            textRenderSKPaint.IsAntialias = true;

            // todo 这里应该是从 RunProperty 中获取颜色
            textRenderSKPaint.Color = SKColors.Black;

            float x = skiaTextRenderInfo.X;
            float y = skiaTextRenderInfo.Y;

            var baselineY = -skFont.Metrics.Ascent;

            // 由于 Skia 的 DrawText 传入的 Point 是文本的最下方，因此需要调整 Y 值
            y += baselineY;
            // todo 不要一个个字符渲染，应该是一行渲染
            canvas.DrawText(skiaTextRenderInfo.Text, new SKPoint(x, y), textRenderSKPaint);
        }

        using SKPaint caretPaint = new SKPaint();
        caretPaint.Color = SKColors.Black;
        caretPaint.Style = SKPaintStyle.Fill;
        caretPaint.StrokeWidth = 1;

        if (_debugBitmap != null)
        {
            canvas.DrawBitmap(_debugBitmap, 0, 0);
            // 经过画一条线的测试，可以发现 DrawRect 时的高度，其实偏小了一个像素的高度
            //canvas.DrawLine(0, (float) CurrentCaretBounds.Height,100, (float) CurrentCaretBounds.Height, caretPaint);
        }

        canvas.DrawRect((CurrentCaretBounds with
        {
            Height = CurrentCaretBounds.Height + 1
        }).ToSKRect(), caretPaint);
    }

    record SkiaTextRenderInfo(string Text, float X, float Y, float Width, float Height, IReadOnlyRunProperty RunProperty);
}