namespace LightTextEditorPlus.Core.Layout.LayoutUtils.WordDividers;

public interface IWordDivider
{
    DivideWordResult DivideWord(in DivideWordArgument argument);
}