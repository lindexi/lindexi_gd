namespace DotNetCampus.Storage.Lib.StorageNodes;

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

    public static implicit operator StorageTextSpan(string text) => new StorageTextSpan(text);
}