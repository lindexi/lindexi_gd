namespace DotNetCampus.Storage.StorageNodes;

/// <summary>
/// 表示存储的字符内容
/// </summary>
/// <param name="Text"></param>
/// <param name="Start"></param>
/// <param name="Length"></param>
public readonly record struct StorageTextSpan(string Text, int Start, int Length)
{
    public StorageTextSpan(string text) : this(text, 0, text.Length)
    {
    }

    public ReadOnlySpan<char> AsSpan() => Text.AsSpan(Start, Length);

    public string ToText()
    {
        if (Start == 0 && Length == Text.Length)
        {
            return Text;
        }

        if (Length == -1)
        {
            return string.Empty;
        }

        return AsSpan().ToString();
    }

    public override string ToString()
    {
        if (Equals(NullValue))
        {
            return "<NULL>";
        }

        return ToText();
    }

    public static StorageTextSpan NullValue => new StorageTextSpan(string.Empty, -1, -1);

    public bool IsNull => Start == -1 && Length == -1;

    /// <summary>
    /// 是否空字符串
    /// </summary>
    public bool IsEmptyOrNull => Length <= 0;

    public static implicit operator StorageTextSpan(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return NullValue;
        }

        return new StorageTextSpan(text);
    }
}