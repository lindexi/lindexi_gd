// See https://aka.ms/new-console-template for more information

var folder = @"C:\Program Files\dotnet\";

foreach (var directory in Directory.EnumerateDirectories(folder,"*",SearchOption.AllDirectories))
{
    if (directory == @"C:\Program Files\dotnet\sdk")
    {

    }

    Console.WriteLine(directory);
}

Console.WriteLine("Hello, World!");
