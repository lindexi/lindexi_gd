using LightTextEditorPlus.Core.Layout;

namespace LightTextEditorPlus.Core.Platform;

/// <summary>
/// 文本的行测量器，用来测量一行内可布局上的文本
/// </summary>
public interface ISingleRunInLineLayouter
{
    SingleRunInLineLayoutResult LayoutSingleRunInLine(in SingleRunInLineLayoutArguments arguments);
}