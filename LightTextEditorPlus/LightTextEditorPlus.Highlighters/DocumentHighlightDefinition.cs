using System;

namespace LightTextEditorPlus.Highlighters;

/// <summary>
/// 表示文档高亮类别。
/// </summary>
public enum DocumentHighlightCategory
{
    /// <summary>
    /// Markdown 文档。
    /// </summary>
    Markdown,

    /// <summary>
    /// C# 文档。
    /// </summary>
    CSharp,

    /// <summary>
    /// 其他语言文档。
    /// </summary>
    Other,
}

/// <summary>
/// 描述文档应使用的高亮方案。
/// </summary>
/// <param name="Category">高亮类别。</param>
/// <param name="LanguageId">其他语言使用的语言标识。</param>
public readonly record struct DocumentHighlightDefinition(DocumentHighlightCategory Category, string? LanguageId = null)
{
    /// <summary>
    /// 获取 Markdown 高亮定义。
    /// </summary>
    public static DocumentHighlightDefinition Markdown { get; } = new(DocumentHighlightCategory.Markdown);

    /// <summary>
    /// 获取 C# 高亮定义。
    /// </summary>
    public static DocumentHighlightDefinition CSharp { get; } = new(DocumentHighlightCategory.CSharp);

    /// <summary>
    /// 创建其他语言的高亮定义。
    /// </summary>
    /// <param name="languageId">语言标识。</param>
    /// <returns>对应的高亮定义。</returns>
    public static DocumentHighlightDefinition CreateOther(string languageId)
    {
        if (string.IsNullOrWhiteSpace(languageId))
        {
            throw new ArgumentException($"{nameof(languageId)} cannot be null or whitespace.", nameof(languageId));
        }

        return new DocumentHighlightDefinition(DocumentHighlightCategory.Other, languageId);
    }
}
