using System;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 表示字符串的某个片段的字符对象
/// </summary>
public sealed class TextSpanCharObject : ICharObject, IEquatable<string>
{
    /// <summary>
    /// 字符串的某个字符
    /// </summary>
    /// 用来减少字符串分割
    /// <param name="originText">原始字符串</param>
    /// <param name="charIndex">是哪个字符</param>
    /// <param name="charCount">有多少个字符，这是因为有一些字符，如表情，是需要使用两个 char 表示</param>
    public TextSpanCharObject(string originText, int charIndex, int charCount = 1)
    {
        _originText = originText;
        _charIndex = charIndex;
        _charCount = charCount;
    }

    private readonly string _originText;
    private readonly int _charIndex;
    private readonly int _charCount;

    public bool Equals(string? other)
    {
        if (other is null) return false;
        if (other.Length != _charCount) return false;

        if (_charCount == 1)
        {
            return other[0].Equals(_originText[_charIndex]);
        }
        else
        {
            var text = ToText();
            return other.Equals(text, StringComparison.Ordinal);
        }
    }

    public ICharObject DeepClone()
    {
        // 由于毫无可变，因此深拷贝等于自身
        return this;
    }

    public string ToText()
    {
        if (_charCount == 1)
        {
            return _originText[_charIndex].ToString();
        }
        else
        {
            return _originText.Substring(_charIndex, _charCount);
        }
    }

    public string GetOriginText() => _originText;

    // todo 加上判断是否连续的方法

    /// <summary>
    /// 判断传入的 <paramref name="other"/> 和当前的是否字符串连续的字符。传入的 <paramref name="other"/> 是否属于相同的一个字符串的下一个字符
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool IsIncreasingContinuous(TextSpanCharObject other)
    {
        if (!ReferenceEquals(_originText, other._originText))
        {
            // 性能考虑，而不是准确性考虑。判断引用即可，不需要遍历字符串内容
            return false;
        }

        return _charIndex + 1 == other._charIndex;
    }
}