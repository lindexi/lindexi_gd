using LightTextEditorPlus.Core.Layout;

namespace LightTextEditorPlus.Core.Platform;

/// <summary>
/// 文本的行内单个字符布局器，用于将单个字符布局到行内
/// </summary>
/// todo 重新命名为 ISingleCharInLineLayouter
public interface ISingleRunInLineLayouter
{
    // todo 重新命名为 LayoutSingleCharInLine
    SingleCharInLineLayoutResult LayoutSingleRunInLine(in SingleCharInLineLayoutArguments arguments);
}