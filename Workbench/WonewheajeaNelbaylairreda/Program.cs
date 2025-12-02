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
/*
   [NullableContext(1)]
   [Nullable(0)]
   internal class F2 : F1
   {
     [CompilerGenerated]
     [DebuggerBrowsable(DebuggerBrowsableState.Never)]
     private Foo <foo>P;
   
     public F2(Foo foo)
     {
       this.<foo>P = foo;
       base..ctor(this.<foo>P);
     }
   
     public override Foo Foo
     {
       get
       {
         return this.<foo>P;
       }
     }
   
     public override void D()
     {
       Foo f = this.Foo;
     }
   }
   
 */
}