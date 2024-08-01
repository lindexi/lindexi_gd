// See https://aka.ms/new-console-template for more information

using System.Text;
using System.Text.RegularExpressions;

var blogFolder = @"C:\lindexi\Blog\";

if (args.Length > 0)
{
    blogFolder = args[0];
}

var textFile = Path.Join(blogFolder, "WPF 下拉框选项做鼠标 Hover 预览效果.md");
GetImageLink(textFile);

foreach (var blogFile in Directory.EnumerateFiles(blogFolder, "*.md"))
{
    var blogOutputText = new StringBuilder();

   
}

Console.WriteLine("Hello, World!");

void GetImageLink(string blogFile)
{
    var imageFileRegex = new Regex(@"<!--\s*!\[\]\(image/([\w /\.]*)\)\s-->");
    var imageLinkRegex = new Regex(@"!\[\]\(http://image.acmx.xyz/");

    bool isImage = false;

    foreach (var line in File.ReadLines(blogFolder))
    {

    }
}