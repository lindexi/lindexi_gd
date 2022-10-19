using LightTextEditorPlus.Core.Layout;

namespace LightTextEditorPlus.Core.Platform;

/// <summary>
/// 整行的 Run 的测量器，用来测量一整行
/// </summary>
public interface IWholeRunLineLayouter
{
    WholeRunLineLayoutResult LayoutWholeRunLine(in WholeRunLineLayoutArgument argument);
}