// See https://aka.ms/new-console-template for more information

var c = new C();
B b = c;

c.Foo();
b.Foo();

Console.WriteLine("Hello, World!");

abstract class A
{
    public abstract void Foo();
}

class B : A
{
    public override void Foo()
    {
        Console.WriteLine("B");
    }
}

class C : B
{
    public override void Foo()
    {
        Console.WriteLine("C");
    }
}