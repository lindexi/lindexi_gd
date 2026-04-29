namespace LightTextEditorPlus.Highlighters.CodeHighlighters;

public readonly record struct HighlightCodeContext(string PlainCode, IColorCode ColorCode);