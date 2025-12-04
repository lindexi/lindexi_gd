// See https://aka.ms/new-console-template for more information

Console.WriteLine($"IsNormal 是否正规数：");

foreach (var (name, value) in GetTestValues())
{
    Console.WriteLine($"{name,10} : {double.IsNormal(value)}");
}

Console.WriteLine($"IsSubnormal 是否次正规数：");

foreach (var (name, value) in GetTestValues())
{
    Console.WriteLine($"{name,10} : {double.IsSubnormal(value)}");
}

Console.WriteLine($"IsNaN：");

foreach (var (name, value) in GetTestValues())
{
    Console.WriteLine($"{name,10} : {double.IsNaN(value)}");
}

Console.WriteLine($"IsRealNumber 是否实数：");

foreach (var (name, value) in GetTestValues())
{
    Console.WriteLine($"{name,10} : {double.IsRealNumber(value)}");
}

Console.WriteLine($"IsFinite 是否有限数：");

foreach (var (name, value) in GetTestValues())
{
    Console.WriteLine($"{name,10} : {double.IsFinite(value)}");
}

Console.WriteLine("Hello, World!");

IEnumerable<(string Name, double Value)> GetTestValues()
{
    yield return ("1.0", 1.0);
    yield return ("0", 0);
    yield return ("-0", double.NegativeZero);
    yield return ("4.0E-320", 4.0E-320);
    yield return ("NaN", double.NaN);
    yield return ("∞", double.PositiveInfinity);
    yield return ("-∞", double.NegativeInfinity);
    yield return ("Epsilon", double.Epsilon);
    yield return ("Math.E", double.E);
}