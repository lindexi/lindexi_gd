namespace LightTextEditorPlus.Highlighters.CodeHighlighters;

/// <summary>
/// 表示一次代码高亮所需的输入和输出上下文。
/// </summary>
/// <param name="PlainCode">原始代码文本。</param>
/// <param name="ColorCode">用于写入颜色结果的目标。</param>
public readonly record struct HighlightCodeContext(string PlainCode, IColorCode ColorCode);