// See https://aka.ms/new-console-template for more information

using System.Text;
using System.Text.RegularExpressions;

var blogFolder = @"C:\lindexi\Blog\";
var output = "Result.csv";

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

foreach (var blogFile in Directory.EnumerateFiles(blogFolder, "*.md"))
{
    var fileName = Path.GetFileNameWithoutExtension(blogFile);

    var dateText = ReadDateFromFile(blogFile);

    if (!string.IsNullOrEmpty(dateText))
    {
        File.AppendAllText(output, $"\"{fileName}\",{dateText}\r\n", Encoding.GetEncoding("GBK"));
        Console.WriteLine($"{fileName} {dateText}");
    }
}

string? ReadDateFromFile(string blogFile)
{
    using var fileStream = File.OpenRead(blogFile);
    using var streamReader = new StreamReader(fileStream);
    while (true)
    {
        var line = streamReader.ReadLine();
        if (line is null)
        {
            break;
        }

        var match = Regex.Match(line, @"<\!\-\- CreateTime\:([\S\s]*) \-\->");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
    }

    return null;
}