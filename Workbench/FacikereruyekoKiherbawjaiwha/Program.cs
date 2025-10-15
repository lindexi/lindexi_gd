// See https://aka.ms/new-console-template for more information

Foo();
Console.WriteLine("Hello, World!");


void Foo()
{
    using var iro = 10.AsReadOnly();
    using var fro = 1.1f.AsReadOnly();

    int foo = iro;
    int bar = 20 * iro;  // can use as 'int'
    float baz = fro * fro * iro;

    // error: 'using variable' is protected by the system
    //iro = 20.AsReadOnly();
}

public readonly record struct ReadOnly<T>(T Value) : IDisposable
{
    void IDisposable.Dispose() { }
    public static implicit operator T(ReadOnly<T> x) => x.Value;
}

public static class ReadOnly
{
    public static ReadOnly<T> AsReadOnly<T>(this T value) => new(value);
}