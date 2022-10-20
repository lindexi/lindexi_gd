using System;

namespace LightTextEditorPlus.Core.Document;

public class TextSpanCharObject : ICharObject, IEquatable<string>
{
    /// <summary>
    /// 字符串的某个字符
    /// </summary>
    /// 用来减少字符串分割
    /// <param name="originText">原始字符串</param>
    /// <param name="charIndex">是哪个字符</param>
    /// <param name="charCount">有多少个字符，这是因为有一些字符，如表情，是需要使用两个 char 表示</param>
    public TextSpanCharObject(string originText,int charIndex,int charCount=1)
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

    public object Clone()
    {
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
}


public class TextCharObject : ICharObject, IEquatable<string>
{
    /// <summary>
    /// 文本字符
    /// </summary>
    /// <param name="textChar">由于有一些字符，如表情，是需要使用两个 char 表示，这里就采用 string 存放</param>
    public TextCharObject(string textChar)
    {
        TextChar = textChar;
    }

    /// <summary>
    /// 只包含一个字符的文本。由于某些表情需要多个 <see cref="char"/> 才能表示，这里采用字符串
    /// </summary>
    public string TextChar { get; }

    public bool Equals(string? other)
    {
        return TextChar.Equals(other);
    }

    public object Clone()
    {
        return this;
    }

    public string ToText()
    {
        return TextChar;
    }
}