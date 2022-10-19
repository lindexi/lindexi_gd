using System;

namespace LightTextEditorPlus.Core.Document;

public class TextCharObject : ICharObject, IEquatable<string>
{
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