using System.Diagnostics.CodeAnalysis;

namespace LightTextEditorPlus.Core.Utils.Patterns;

class MultiTextRangePattern
{
    /// <summary>
    /// 多个文本范围的判断
    /// </summary>
    /// <param name="textRange">存放格式是 (MinChar,MaxChar) 两个两个存放，这是为了性能考虑</param>
    public MultiTextRangePattern(params char[] textRange)
    {
        TextRange = textRange;
    }

    /// <summary>
    /// 存放格式是 (MinChar,MaxChar) 两个两个存放，这是为了性能考虑
    /// </summary>
    private char[] TextRange { get; }

    /// <summary>
    /// 是否输入的字符串每个字符都在范围内，对空白字符串返回符合
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public bool AreAllInRange([NotNull] string text)
    {
        foreach (var c in text)
        {
            bool isInRange = false;
            for (var i = 0; i < TextRange.Length; i += 2)
            {
                var minChar = TextRange[i];
                var maxChar = TextRange[i + 1];

                if (c >= minChar && c <= maxChar)
                {
                    isInRange = true;
                }
            }

            if (!isInRange)
            {
                return false;
            }
        }

        return true;
    }
}