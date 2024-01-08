using System;

namespace LightTextEditorPlus.Core.Utils.Patterns;

/// <summary>
/// 文本内容范围判断类
/// </summary>
public class TextRangePattern : IPattern
{
    /// <summary>
    /// 创建文本内容范围判断类
    /// </summary>
    public TextRangePattern(char minChar, char maxChar)
    {
        MinChar = minChar;
        MaxChar = maxChar;
    }

    /// <summary>
    /// 最小字符
    /// </summary>
    public char MinChar { get; }

    /// <summary>
    /// 最大字符
    /// </summary>
    public char MaxChar { get; }

    /// <summary>
    /// 是否输入的字符在范围内
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    public bool IsInRange(char c)
    {
        return !(c < MinChar || c > MaxChar);
    }

    /// <summary>
    /// 确定字符串中包含范围内字符，对空白字符串返回不符合
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public bool ContainInRange(string text)
    {
        if (ReferenceEquals(text, null)) throw new ArgumentNullException(nameof(text));

        foreach (var c in text)
        {
            if (c >= MinChar && c <= MaxChar)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 是否输入的字符串每个字符都在范围内，对空白字符串返回符合
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public bool AreAllInRange(string text)
    {
        if (ReferenceEquals(text, null)) throw new ArgumentNullException(nameof(text));

        foreach (var c in text)
        {
            if (c < MinChar || c > MaxChar)
            {
                return false;
            }
        }

        return true;
    }
}