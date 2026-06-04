using System;
using System.Collections.Generic;

using LightTextEditorPlus.Highlighters;

namespace SimpleWrite.Business.TextEditors.AutoIndentStrategies;

/// <summary>
/// 根据文档高亮定义选择合适的自动缩进策略。
/// </summary>
public static class AutoIndentStrategySelector
{
    private static readonly Dictionary<string, IAutoIndentStrategy> LanguageStrategyMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["xml"] = new XmlAutoIndentStrategy(),
        ["xaml"] = new XmlAutoIndentStrategy(),
        ["html"] = new XmlAutoIndentStrategy(),
        ["svg"] = new XmlAutoIndentStrategy(),
    };

    /// <summary>
    /// 根据文档高亮定义获取对应的自动缩进策略。
    /// </summary>
    /// <param name="definition">文档高亮定义。</param>
    /// <returns>对应的自动缩进策略，如果无匹配则返回默认的 Markdown 策略。</returns>
    public static IAutoIndentStrategy GetStrategy(DocumentHighlightDefinition definition)
    {
        return definition.Category switch
        {
            DocumentHighlightCategory.Markdown => _markdownStrategy,
            DocumentHighlightCategory.CSharp => _markdownStrategy,
            DocumentHighlightCategory.Other => GetOtherStrategy(definition.LanguageId),
            _ => _markdownStrategy,
        };
    }

    private static IAutoIndentStrategy GetOtherStrategy(string? languageId)
    {
        if (languageId is not null && LanguageStrategyMap.TryGetValue(languageId, out var strategy))
        {
            return strategy;
        }

        return _markdownStrategy;
    }

    private static readonly MarkdownAutoIndentStrategy _markdownStrategy = new();
}
