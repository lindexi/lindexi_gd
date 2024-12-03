// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

abstract class F1
{
    public abstract T X<T>();
}

class F2 : F1
{
    public override int X<int>()
    {
        return default;
    }
}