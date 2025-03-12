// See https://aka.ms/new-console-template for more information

using System.Collections;

var foo = new Foo();
foreach (var item in foo)
{
    Console.WriteLine(item);
}

Console.WriteLine("Hello, World!");

struct Foo : IReadOnlyList<int>
{
    IEnumerator<int> IEnumerable<int>.GetEnumerator()
    {
        return GetEnumerator();
    }

    public S GetEnumerator()
    {
/*
   .method public hidebysig instance valuetype S
     GetEnumerator() cil managed
   {
     .maxstack 1
     .locals init (
       [0] valuetype S V_0,
       [1] valuetype S V_1
     )

     // [21 5 - 21 6]
     IL_0000: nop

     // [22 9 - 22 24]
     IL_0001: ldloca.s     V_0
     IL_0003: initobj      S
     IL_0009: ldloc.0      // V_0
     IL_000a: stloc.1      // V_1
     IL_000b: br.s         IL_000d

     // [23 5 - 23 6]
     IL_000d: ldloc.1      // V_1
     IL_000e: ret

   }
 */
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
        set => throw new NotImplementedException();
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