// See https://aka.ms/new-console-template for more information
// 请帮我编写一个正则表达式，匹配项目名、定制版、打包平台。输入的格式是 "项目名(_定制版)(_打包平台)"，其中(_定制版)和(_打包平台)是可选项
// 例如输入 "Lindexi_Custom_win-x86"，输出项目名是 "Lindexi"，定制版是 "Custom"，打包平台是 "win-x86"
// 例如输入 "Lindexi_Custom"，输出项目名是 "Lindexi"，定制版是 "Custom"，打包平台是 null
// 例如输入 "Lindexi"，输出项目名是 "Lindexi"，定制版是 null，打包平台是 null
// 正则表达式的命名分组请分别命名为 ProjectName、Custom、RuntimeIdentifier
// 请在此处编写正则表达式
using System.Text.RegularExpressions;

var regex = new Regex(@"(?<ProjectName>\w+)(_(?<Custom>\w+))?(_(?<RuntimeIdentifier>\w+-\w+))?");

var input = "Lindexi_Custom_win-x86";
var match = regex.Match(input);
if (match.Success)
{
    Console.WriteLine($"项目名：{match.Groups["ProjectName"].Value}");
    Console.WriteLine($"定制版：{match.Groups["Custom"].Value}");
    Console.WriteLine($"打包平台：{match.Groups["RuntimeIdentifier"].Value}");
}
else
{
    Console.WriteLine("输入的格式不正确");
}

input = "Lindexi_Custom";
match = regex.Match(input);
if (match.Success)
{
    Console.WriteLine($"项目名：{match.Groups["ProjectName"].Value}");
    Console.WriteLine($"定制版：{match.Groups["Custom"].Value}");
    Console.WriteLine($"打包平台：{match.Groups["RuntimeIdentifier"].Value}");
}
else
{
    Console.WriteLine("输入的格式不正确");
}

input = "Lindexi";
match = regex.Match(input);
if (match.Success)
{
    Console.WriteLine($"项目名：{match.Groups["ProjectName"].Value}");
    Console.WriteLine($"定制版：{match.Groups["Custom"].Value}");
    Console.WriteLine($"打包平台：{match.Groups["RuntimeIdentifier"].Value}");
}
else
{
    Console.WriteLine("输入的格式不正确");
}
