// See https://aka.ms/new-console-template for more information

using System.Text;
using System.Text.RegularExpressions;

var blogFolder = @"C:\lindexi\Blog\";

if (args.Length > 0)
{
    blogFolder = args[0];
}

var textFile = Path.Join(blogFolder, "Windows 通过编辑注册表设置左右手使用习惯更改 Popup 弹出位置.md");
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
    var currentBlogFile = "";
    var blogOutputText = new StringBuilder();

    foreach (var line in File.ReadLines(blogFile))
    {
        if (!isImage)
        {
            var match = imageFileRegex.Match(line);
            if (match.Success)
            {
                currentBlogFile = Path.Join(blogFolder, "image", match.Groups[1].ValueSpan);
                if (File.Exists(currentBlogFile))
                {
                    isImage = true;
                }
                else
                {
                    Console.WriteLine($"本地文件找不到");
                }
            }

            blogOutputText.AppendLine(line);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                blogOutputText.AppendLine();

                continue;
            }

            var match = imageLinkRegex.Match(line);
            if (match.Success)
            {
                Console.WriteLine($"本地文件 {currentBlogFile}");

                blogOutputText.AppendLine(
                    $"![](http://cdn.lindexi.com/{Uri.EscapeDataString(Path.GetFileName(currentBlogFile) ?? string.Empty)})");
            }
            else
            {
                blogOutputText.AppendLine();
            }

            isImage = false;
            currentBlogFile = null;
        }
    }
}