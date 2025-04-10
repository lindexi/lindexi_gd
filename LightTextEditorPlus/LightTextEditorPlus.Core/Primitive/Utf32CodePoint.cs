using System;
using System.Collections.Generic;
using System.Text;

namespace LightTextEditorPlus.Core.Primitive;

/// <summary>
/// UTF-32 码点，等价于 <see cref="System.Text.Rune"/> 类型
/// </summary>
/// https://en.wikipedia.org/wiki/Code_point
/// 感觉不如 Rune 类型
/// [.NET 中的字符编码简介 - .NET Microsoft Learn](https://learn.microsoft.com/zh-cn/dotnet/standard/base-types/character-encoding-introduction#grapheme-clusters )
public readonly record struct Utf32CodePoint(int Value) : IEquatable<char>
{
    /// <summary>
    /// 将码点转换为符文
    /// </summary>
    public Rune Rune => new Rune(Value);

    /// <summary>
    /// 无效的码点
    /// </summary>
    public static Utf32CodePoint Invalid => new Utf32CodePoint(-1);
    /// <summary>
    /// 是否是无效的码点
    /// </summary>
    public bool IsInvalid => Value < 0;
    /// <summary>
    /// 是否是有效的码点
    /// </summary>
    public bool IsValid => Value >= 0;

    /// <summary>
    /// 将码点转换为字符数组
    /// </summary>
    /// <returns></returns>
    public char[] ToCharArray()
    {
        Span<char> buffer = stackalloc char[2];
        int length = Rune.EncodeToUtf16(buffer);
        return buffer.Slice(0, length).ToArray();
    }

    /// <summary>
    /// 将码点追加到字符列表中
    /// </summary>
    /// <param name="charCollection"></param>
    public void AppendToCharCollection(ICollection<char> charCollection)
    {
        Span<char> buffer = stackalloc char[2];
        int length = Rune.EncodeToUtf16(buffer);
        for (int i = 0; i < length; i++)
        {
            charCollection.Add(buffer[i]);
        }
    }

    /// <summary>
    /// 将码点追加到字符串构建器中
    /// </summary>
    /// <param name="stringBuilder"></param>
    /// 等待 [Flow System.Text.Rune through more APIs · Issue #27912 · dotnet/runtime](https://github.com/dotnet/runtime/issues/27912 )
    public void AppendToStringBuilder(StringBuilder stringBuilder)
    {
        Span<char> buffer = stackalloc char[2];
        int length = Rune.EncodeToUtf16(buffer);
        for (int i = 0; i < length; i++)
        {
            stringBuilder.Append(buffer[i]);
        }
    }

    /// <summary>
    /// 获取码点的字符长度，即需要转换为多少个 char 结构体
    /// </summary>
    public int CharLength
    {
        get
        {
            Span<char> buffer = stackalloc char[2];
            int length = Rune.EncodeToUtf16(buffer);
            return length;
        }
    }

    /// <summary>
    /// 将码点追加到字符列表中
    /// </summary>
    public void AppendToCharList(List<char> charList) => AppendToCharCollection(charList);

    /// <inheritdoc />
    public bool Equals(char other)
    {
        return Value == other;
    }

    /// <inheritdoc />
    public override string ToString() => Char.ConvertFromUtf32(Value);
}
