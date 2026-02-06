// See https://aka.ms/new-console-template for more information

using System.Text;
using System.Text.RegularExpressions;

var file = @"C:\lindexi\Blog\博客导航.md";
var outputFile = Path.GetFullPath(@"博客导航.md");

var outputStringBuilder = new StringBuilder();
var textLineArray = File.ReadAllLines(file);

var linkRegex = new Regex(@"\[(.*)]\(.+\)", RegexOptions.Compiled);
var commentLinkRegex = new Regex(@"<!--\s*(\[(.*)]\((.+)\))\s*-->", RegexOptions.Compiled);

for (var i = 0; i < textLineArray.Length; i++)
{
    var currentLine = textLineArray[i];

    if (i == 0 && currentLine == "# 博客导航")
    {
        continue;
    }

    // 是否博客链接行
    var blogLineMatch = linkRegex.Match(currentLine);
    var commentBlogLineMatch = commentLinkRegex.Match(currentLine);
    var isBlogLine = blogLineMatch.Success;
    if (commentBlogLineMatch.Success)
    {
        // 如果当前行是被注释的博客内容，则忽略
        isBlogLine = false;
    }

    if (!isBlogLine)
    {
        outputStringBuilder.AppendLine(currentLine);
        continue;
    }

    if (i == textLineArray.Length - 1)
    {
        // 最后一行
        outputStringBuilder.AppendLine(currentLine);
        continue;
    }

    var skipStep = 1;

    var nextLine = textLineArray[i + skipStep];

    while (string.IsNullOrEmpty(nextLine))
    {
        // 跳过一些空的行
        skipStep++;
        nextLine = textLineArray[i + skipStep];
    }

    // 下一行如果是被注释的博客园链接，那就取出来
    var nextLineMatch = commentLinkRegex.Match(nextLine);

    if (IsBlogCanReplace())
    {
        outputStringBuilder.AppendLine(nextLineMatch.Groups[1].Value);
        outputStringBuilder.AppendLine($"<!-- {currentLine} -->");
        i += skipStep;
        continue;
    }
    else
    {
        outputStringBuilder.AppendLine(currentLine);
        continue;
    }


    bool IsBlogCanReplace()
    {
        // 如果下一行是博客行，且标题包含，则执行替换
        if (!nextLineMatch.Success)
        {
            return false;
        }

        if (!nextLineMatch.Groups[3].ValueSpan.Contains("www.cnblogs.com".AsSpan(), StringComparison.Ordinal))
        {
            // 不是博客园的，忽略
            return false;
        }

        var title = blogLineMatch.Groups[1].ValueSpan;
        var nextLineTitle = nextLineMatch.Groups[2].ValueSpan;

        if (nextLineTitle.Contains(title, StringComparison.Ordinal))
        {
            // 如果标题包含，则可以替换
            return true;
        }

        return false;
    }
}

var outputText = outputStringBuilder.ToString();

File.WriteAllText(outputFile, outputText);

Console.WriteLine($"已将博客导航同步");
