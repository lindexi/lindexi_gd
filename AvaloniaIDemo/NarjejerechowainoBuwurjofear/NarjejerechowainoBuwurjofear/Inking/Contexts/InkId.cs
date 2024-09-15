namespace NarjejerechowainoBuwurjofear.Inking.Contexts;

public readonly partial record struct InkId(int Value)
{
    public static InkId NewId() => new InkId(_nextId++);

    private static int _nextId = 0;

    public override string ToString()
        => $"InkId={Value}";
}