// See https://aka.ms/new-console-template for more information

using System.Text;
using System.Text.RegularExpressions;


var folder = @"C:\lindexi\Blog\";

var fileList = Directory.GetFiles(folder, "*.md");
List<BlogFileInfo> list = new List<BlogFileInfo>();
foreach (var file in fileList)
{
    using FileStream fileStream = new FileStream(file, FileMode.Open);
    StreamReader streamReader = new StreamReader(fileStream);
    while (streamReader.ReadLine() is { } line)
    {
        var match = Regex.Match(line, @"CreateTime:([\W\w]*) -->");
        if (match.Success)
        {
            var createTimeText = match.Groups[1].Value;
            if (DateTime.TryParse(createTimeText, out var createTime))
            {
                var fileName = Path.GetFileName(file);
                BlogFileInfo blogFileInfo = new BlogFileInfo(fileName, createTime, file);
                list.Add(blogFileInfo);
            }
            else
            {
            }
        }
    }
}

list = list.OrderByDescending(t => t.CreateTime)
    .ToList();

var yearsList = list.Where(t => t.CreateTime > new DateTime(2024, 04, 01)).ToList();

var count = yearsList.Count;

var outputFile = "1.csv";

foreach (var blogFileInfo in yearsList)
{
    Console.WriteLine(blogFileInfo.FileName);
}

File.WriteAllLines(outputFile, yearsList.Select(blogFileInfo =>
{
    var c = string.Empty;

    var fileName = blogFileInfo.FileName;

    if (Contains(fileName))
    {
        c = "Windows";
    }
    else
    {
        var content = File.ReadAllText(blogFileInfo.FullFileName);
        if (Contains(content))
        {
            c = "Windows";
        }
    }

    return $"\"{blogFileInfo.FileName}\",{c}";

    bool Contains(string input)
    {
        Span<string> matchNameSpan = ["WPF", "WinUI", "Window", "DirectX"];
        foreach (var name in matchNameSpan)
        {
            if (input.Contains(name))
            {
                return true;
            }
        }

        return false;
    }

}), Encoding.UTF8);

Console.WriteLine("Hello, World!");

record BlogFileInfo(string FileName, DateTime CreateTime, string FullFileName);