using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive.Collections;

namespace LightTextEditorPlus.Core.Layout;

// todo 重命名，去掉 s 字符
public readonly record struct SingleCharInLineLayoutArguments(ReadOnlyListSpan<CharData> RunList, int CurrentIndex,
    double LineRemainingWidth, ParagraphProperty ParagraphProperty)
{
    public CharData CurrentCharData => RunList[CurrentIndex];
}