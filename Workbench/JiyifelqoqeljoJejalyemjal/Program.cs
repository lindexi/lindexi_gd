// See https://aka.ms/new-console-template for more information

using System.Collections;

var foo = new Foo();
foreach (var item in foo)
{
    Console.WriteLine(item);
}

Console.WriteLine("Hello, World!");


class Foo : IReadOnlyList<int>
{
    public IEnumerator<int> GetEnumerator()
    {
        return new S();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count => 1;

    public int this[int index]
    {
        get => throw new NotImplementedException();
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

        int IEnumerator<int>.Current => _current;

        object? IEnumerator.Current => _current;
    }
}