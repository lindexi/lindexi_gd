namespace JuqawhicaqarLairciwholeni;

class Program
{
    static void Main()
    {
        var c = new Foo();
        c.WriteLine(1);
        c.WriteLine(2);
        c.WriteLine(3);
    }
}

class Foo
{
    public void WriteLine(int message)
    {
        Console.WriteLine($"Foo: {message}");
    }
}