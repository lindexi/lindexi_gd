// See https://aka.ms/new-console-template for more information

var folder = @"F:\temp\CulubanaichelayRahelurjibaw\";

foreach (var file in Directory.EnumerateFiles(folder, "*.exe|*.dll"))
{
    Console.WriteLine(file);
}

Console.WriteLine("Hello, World!");
