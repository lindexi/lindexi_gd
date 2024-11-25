using System.Text;

using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Document;

using SkiaSharp;

namespace LightTextEditorPlus.Platform;

internal class SkiaSingleCharInLineLayouter : ISingleCharInLineLayouter
{
    public SkiaSingleCharInLineLayouter(SkiaTextEditor textEditor)
    {
        TextEditor = textEditor;
    }

    private SkiaTextEditor TextEditor { get; } // todo 后续考虑弱引用，方便释放

    public SingleCharInLineLayoutResult LayoutSingleCharInLine(in SingleCharInLineLayoutArgument argument)
    {
        CharData currentCharData = argument.CurrentCharData;
        var runProperty = currentCharData.RunProperty.AsRunProperty();

        // todo 处理连续字符属性的情况
        SKFont font = runProperty.GetRenderSKFont();
        // todo 考虑 SKPaint 的复用，不要频繁创建，可以考虑在 SkiaTextEditor 中创建一个 SKPaint 的缓存池
        using SKPaint skPaint = new SKPaint(font);
        // todo skPaint 是否已经用上 SKFont 的字号属性？

        var lineRemainingWidth = (float) argument.LineRemainingWidth;

        // todo 优化以下代码写法
        var stringBuilder = new StringBuilder();
        for (var i = argument.CurrentIndex; i < argument.RunList.Count; i++)
        {
            CharData charData = argument.RunList[i];
            stringBuilder.Append(charData.ToString());
        }

        string text = stringBuilder.ToString();

        // todo 这里需要处理换行规则
        long charCount = skPaint.BreakText(text, lineRemainingWidth, out var measuredWidth);
        // todo 这里假定 charCount 数量为 CharData 数量，后续需要优化，如 CharData 可以包含多个字符的情况
        return new SingleCharInLineLayoutResult((int) charCount, new Size(measuredWidth, 0));
    }
}