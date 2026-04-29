namespace LightTextEditorPlus.Highlighters.CodeHighlighters;

public interface ICodeHighlighter
{
    void ApplyHighlight(in HighlightCodeContext context);
}