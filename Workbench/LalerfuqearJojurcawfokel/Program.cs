// See https://aka.ms/new-console-template for more information

var f2 = new F2();
f2.Do(new F1());

Console.WriteLine("Hello, World!");


class Foo<T> where T : F1
{
    public void Do(F1 foo)
    {
        Do((T)foo);
    }

    protected virtual void Do(T foo)
    {
    }
}

class F1 { }

class F2 : Foo<F1>
{
}