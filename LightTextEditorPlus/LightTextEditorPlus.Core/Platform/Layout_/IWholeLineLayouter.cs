using LightTextEditorPlus.Core.Layout;

namespace LightTextEditorPlus.Core.Platform;

/// <summary>
/// 整行的布局器，用来布局一整行
/// </summary>
public interface IWholeLineLayouter
{
    WholeRunLineLayoutResult LayoutWholeLine(in WholeLineLayoutArgument argument);
}