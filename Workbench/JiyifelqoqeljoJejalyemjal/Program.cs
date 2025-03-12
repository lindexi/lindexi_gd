// See https://aka.ms/new-console-template for more information

using System.Collections;

var s = new S();
foreach (var t in s)
{
    
}

Console.WriteLine("Hello, World!");


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

    int IEnumerator<int>.Current => _current;

    object? IEnumerator.Current => _current;
}