using LightTextEditorPlus.Core.Primitive.Collections;
using System.Text;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 字符数据只读列表的扩展方法
/// </summary>
public static class CharDataReadOnlyListSpanExtension
{
    /// <summary>
    /// 将字符数据列表转换为文本
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    public static string ToText(this ReadOnlyListSpan<CharData> list)
    {
        var stringBuilder = new StringBuilder();
        foreach (CharData charData in list)
        {
            stringBuilder.Append(charData.CharObject.ToText());
        }
        return stringBuilder.ToString();
    }
}