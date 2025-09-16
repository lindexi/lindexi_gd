using LightTextEditorPlus.Core.Document;

namespace LightTextEditorPlus.Core.Diagnostics.LogInfos;

/// <summary>
/// 针对字符的字体回滚的日志信息
/// </summary>
/// <param name="OriginFontName"></param>
/// <param name="NotSupportCharObject"></param>
/// <param name="FallbackFontName"></param>
public readonly record struct CharObjectFontFallbackLogInfo(string OriginFontName, ICharObject NotSupportCharObject, string FallbackFontName)
{
    /// <inheritdoc />
    public override string ToString()
    {
        return $"当前字体 '{OriginFontName}' 不支持字符 '{NotSupportCharObject.ToText()}'，回滚为 '{FallbackFontName}' 字体";
    }
}