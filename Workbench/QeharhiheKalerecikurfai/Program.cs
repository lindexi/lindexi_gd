// See https://aka.ms/new-console-template for more information

foreach (var file in Directory.EnumerateFiles(".", "*.ico", SearchOption.AllDirectories))
{
    var fullPath = Path.GetFullPath(file);
    Console.WriteLine(fullPath);
}

Console.WriteLine("Hello, World!");
