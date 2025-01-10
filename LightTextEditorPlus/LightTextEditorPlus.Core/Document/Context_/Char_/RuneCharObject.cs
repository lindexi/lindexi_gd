using System.Text;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 使用符文表示的字符对象
/// </summary>
public sealed class RuneCharObject : ICharObject
{
    /// <summary>
    /// 使用符文表示的字符对象
    /// </summary>
    public RuneCharObject(Rune rune)
    {
        Rune = rune;
    }

    /// <inheritdoc />
    public bool Equals(string? other)
    {
        return string.Equals(Rune.ToString(), other);
    }

    /// <inheritdoc />
    public ICharObject DeepClone()
    {
        // 对象是不可变的，所以直接返回自己
        return this;
    }

    /// <inheritdoc />
    public string ToText()
    {
        return Rune.ToString();
    }

    /// <summary>
    /// 符文
    /// </summary>
    public Rune Rune { get; }

    /// <inheritdoc />
    public Utf32CodePoint CodePoint => new Utf32CodePoint(Rune.Value);
}
