// See https://aka.ms/new-console-template for more information

var foo = new Foo();
var f2 = new F2(foo);
F1 f1 = f2;

Console.WriteLine("Hello, World!");

class Foo
{

}
class F1
{
    public F1(Foo foo)
    {
        Foo = foo;
        D();
    }

    public virtual Foo Foo { get; }

    public virtual void D()
    {
    }
}

class F2(Foo foo) : F1(foo)
{
    public override Foo Foo => foo;

    public override void D()
    {
        var f = Foo;
    }
}