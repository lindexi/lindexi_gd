namespace SimpleWrite.Business.TextEditors.Highlighters.CodeHighlighters;

public readonly record struct HighlightCodeContext(string PlainCode, IColorCode ColorCode);