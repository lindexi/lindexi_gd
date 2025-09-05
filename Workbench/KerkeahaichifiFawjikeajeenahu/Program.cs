// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

interface IFoo
{
    void F1(int n);
    void F2(nint n);
    void F3((int n1, nint n2) x);
    void F4((int n1, nint n2) x);
}

abstract class Foo : IFoo
{
    public abstract void F1(Int32 n);
    public abstract void F2(IntPtr n);
    public abstract void F3((Int32 n1, nint n2) x);
    public abstract void F4((int n1, IntPtr n2) x);
}