#if DirectTextEditorDefinition
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Document;

namespace LightTextEditorPlus.Core.Diagnostics.LogInfos;

/// <summary>
/// 设置字符属性的日志
/// </summary>
/// <param name="PropertyType"></param>
/// <param name="Selection"></param>
public readonly record struct SetRunPropertyLogInfo(PropertyType PropertyType, Selection Selection)
{
    /// <inheritdoc />
    public override string ToString()
    {
        string selectionText;
        if (Selection.IsEmpty)
        {
            selectionText = "设置范围为空，没有更改到任何文档内容";
        }
        else
        {
            selectionText = $"设置范围 {Selection}";
        }
        return $"设置字符属性，属性类型：{PropertyType}，{selectionText}";
    }
}
#endif