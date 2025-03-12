// See https://aka.ms/new-console-template for more information

using System.Collections;

var foo = new Foo();
foreach (var item in foo)
{
    Console.WriteLine(item);
}

Console.WriteLine("Hello, World!");

struct Foo
{
    public S GetEnumerator()
    {
        return new S();
    }
}

struct S : IEnumerator<int>
{
    private int _current;

    public void Dispose()
    {

    }

    public bool MoveNext()
    {
        _current++;
        return true;
    }

    public void Reset()
    {
        throw new NotImplementedException();
    }

    public int Current => _current;

    object? IEnumerator.Current => _current;
}