namespace XiaoXiIme.Foundation;

public readonly record struct ImeSessionId(string Value)
{
    public static ImeSessionId Default { get; } = new("default");

    public static ImeSessionId Create() => new(Guid.NewGuid().ToString("N"));

    public override string ToString() => Value;
}