using System;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 文本字符
/// </summary>
/// 暂时没逻辑使用，标记为 file 范围
file class TextCharObject : ICharObject, IEquatable<string>
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

    /// <inheritdoc />
    public bool Equals(string? other)
    {
        return TextChar.Equals(other);
    }

    /// <inheritdoc />
    public ICharObject DeepClone()
    {
        return this;
    }

    /// <inheritdoc />
    public string ToText()
    {
        return TextChar;
    }
}