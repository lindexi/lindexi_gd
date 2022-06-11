// See https://aka.ms/new-console-template for more information

Fx fx = new Fx();

Console.WriteLine(fx.F3());

foreach (var file in Directory.GetFiles(@"f:\temp\Ink RuheajercheWhereneebane\Main\", "*.dll"))
{
    var name = Path.GetFileName(file);
    Console.WriteLine($"<Module file=\"$(InPath)\\{name}\" />");
}

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