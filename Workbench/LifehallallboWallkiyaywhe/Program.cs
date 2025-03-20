// See https://aka.ms/new-console-template for more information

using System.Collections;

foreach (var t in GetF1())
{

}


Console.WriteLine("Hello, World!");

F1 GetF1()
{
    return new F1();
}

struct F1 : IEnumerable<int>
{
    public F2 GetEnumerator()
    {
        return new F2();
    }

    IEnumerator<int> IEnumerable<int>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

struct F2 : IEnumerator<int>
{
    private int _current;

    public void Dispose()
    {
        
    }

    public bool MoveNext()
    {
        return true;
    }

    public void Reset()
    {
    }

    public int Current => _current;

    int IEnumerator<int>.Current => _current;

    object? IEnumerator.Current => _current;
}