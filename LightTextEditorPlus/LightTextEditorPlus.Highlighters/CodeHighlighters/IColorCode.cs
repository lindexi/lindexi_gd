using Microsoft.CodeAnalysis.Text;

namespace LightTextEditorPlus.Highlighters.CodeHighlighters;

/// <summary>
/// 表示支持高亮代码的类
/// </summary>
public interface IColorCode
{
    /// <summary>
    /// 填充高亮代码
    /// </summary>
    /// <param name="span"></param>
    /// <param name="scope"></param>
    void FillCodeColor(TextSpan span, ScopeType scope);
}