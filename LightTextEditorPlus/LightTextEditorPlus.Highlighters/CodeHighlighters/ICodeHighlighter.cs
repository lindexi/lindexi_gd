namespace LightTextEditorPlus.Highlighters.CodeHighlighters;

/// <summary>
/// 定义代码高亮器能力。
/// </summary>
public interface ICodeHighlighter
{
    /// <summary>
    /// 对代码上下文应用高亮。
    /// </summary>
    /// <param name="context">代码内容与着色输出上下文。</param>
    void ApplyHighlight(in HighlightCodeContext context);
}