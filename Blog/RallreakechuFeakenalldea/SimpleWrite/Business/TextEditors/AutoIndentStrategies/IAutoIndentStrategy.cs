using LightTextEditorPlus;

namespace SimpleWrite.Business.TextEditors.AutoIndentStrategies;

/// <summary>
/// 自动缩进策略接口。根据当前行文本和光标位置，决定下一行的缩进内容。
/// </summary>
public interface IAutoIndentStrategy
{
    /// <summary>
    /// 获取新行的缩进文本。
    /// </summary>
    /// <param name="currentLineText">当前行的完整文本（不含换行符）。</param>
    /// <param name="caretColumnInLine">光标在当前行中的列位置（0-based）。当光标在行尾时，此值等于 <paramref name="currentLineText"/> 的长度。</param>
    /// <returns>要插入到新行开头的缩进字符串（如 "    " 或 ""），不包含换行符。</returns>
    string GetIndentText(string currentLineText, int caretColumnInLine);
}
