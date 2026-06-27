using LightTextEditorPlus.Highlighters;

namespace SimpleWrite.Business.TextEditors.PasteStrategies;

/// <summary>/// 根据文档高亮定义选择合适的粘贴策略。
/// </summary>
internal static class PasteStrategySelector
{
    /// <summary>
    /// 获取粘贴策略。返回 null 表示当前文档不支持自定义粘贴，调用方应回退到默认行为。
    /// </summary>
    /// <param name="definition">文档高亮定义，用于判断文档类型</param>
    /// <returns>匹配的粘贴策略，或 null 表示不支持自定义粘贴</returns>
    public static IPasteStrategy? GetStrategy(DocumentHighlightDefinition definition)
    {
        return definition.Category switch
        {
            DocumentHighlightCategory.Markdown => new MarkdownPasteStrategy(),
            _ => null,
        };
    }
}
