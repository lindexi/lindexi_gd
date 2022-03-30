// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

Fx fx = new Fx();

Console.WriteLine(fx.F3());

class Fx
{
    public string F3()
    {
        _f2 = "l";
        _f1 = "xx";

        return _f2 + _f1;
    }

    private string _f2;
    private string _f1;
}