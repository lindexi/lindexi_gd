namespace SimpleWrite.Business.TextEditors.Highlighters.CodeHighlighters;

public interface ICodeHighlighter
{
    void ApplyHighlight(in HighlightCodeContext context);
}