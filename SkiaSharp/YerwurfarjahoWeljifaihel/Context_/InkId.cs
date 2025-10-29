namespace SkiaInkCore;

readonly partial record struct InkId(int Value)
{
    public static InkId NewId() => new InkId(_nextId++);

    private static int _nextId = 0;
}