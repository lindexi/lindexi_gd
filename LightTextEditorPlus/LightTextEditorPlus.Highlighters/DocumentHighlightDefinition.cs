using System;

namespace LightTextEditorPlus.Highlighters;

public enum DocumentHighlightCategory
{
    Markdown,
    CSharp,
    Other,
}

public readonly record struct DocumentHighlightDefinition(DocumentHighlightCategory Category, string? LanguageId = null)
{
    public static DocumentHighlightDefinition Markdown { get; } = new(DocumentHighlightCategory.Markdown);

    public static DocumentHighlightDefinition CSharp { get; } = new(DocumentHighlightCategory.CSharp);

    public static DocumentHighlightDefinition CreateOther(string languageId)
    {
        if (string.IsNullOrWhiteSpace(languageId))
        {
            throw new ArgumentException($"{nameof(languageId)} cannot be null or whitespace.", nameof(languageId));
        }

        return new DocumentHighlightDefinition(DocumentHighlightCategory.Other, languageId);
    }
}
