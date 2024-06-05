// See https://aka.ms/new-console-template for more information

using System.Text.RegularExpressions;

var folder = @"C:\lindexi\Code\WPF\";

foreach (var file in Directory.EnumerateFiles(folder,"*.cs",SearchOption.AllDirectories))
{
    foreach (var line in File.ReadAllLines(file))
    {
        if (line.Contains(" <item>"))
        {
            continue;
        }

        var match = Regex.Match(line, @"///[\W\w]*base\.");
        if (match.Success)
        {
            
        }
    }
}

Console.WriteLine("Hello, World!");
