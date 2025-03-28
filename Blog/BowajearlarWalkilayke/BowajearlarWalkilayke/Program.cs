// See https://aka.ms/new-console-template for more information
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
            if(DateTime.TryParse(createTimeText, out var createTime))
            {
                var fileName = Path.GetFileName(file);
                BlogFileInfo blogFileInfo = new BlogFileInfo(fileName, createTime);
                list.Add(blogFileInfo);
            }
            else
            {
            }
        }
    }
}

list = list.OrderByDescending(t => t.CreateTime).ToList();

var count = list.Count(t => t.CreateTime > new DateTime(2023, 03, 01));

Console.WriteLine("Hello, World!");

record BlogFileInfo(string FileName, DateTime CreateTime);