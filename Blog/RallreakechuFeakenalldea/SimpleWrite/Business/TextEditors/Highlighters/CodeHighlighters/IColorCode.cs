using Microsoft.CodeAnalysis.Text;

namespace SimpleWrite.Business.TextEditors.Highlighters.CodeHighlighters;

public interface IColorCode
{
    void FillCodeColor(TextSpan span, ScopeType scope);
}