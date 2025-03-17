// See https://aka.ms/new-console-template for more information
// 请帮我编写一个正则表达式，匹配项目名、定制版、打包平台。输入的格式是 "项目名(_定制版)(_打包平台)"，其中(_定制版)和(_打包平台)是可选项
// 例如输入 "Lindexi_Custom_win-x86"，输出项目名是 "Lindexi"，定制版是 "Custom"，打包平台是 "win-x86"
// 例如输入 "Lindexi_Custom"，输出项目名是 "Lindexi"，定制版是 "Custom"，打包平台是 null
// 例如输入 "Lindexi"，输出项目名是 "Lindexi"，定制版是 null，打包平台是 null
// 正则表达式的命名分组请分别命名为 ProjectName、Custom、RuntimeIdentifier
// 请在此处编写正则表达式
using System.Text.RegularExpressions;

Span<string> inputList =
[
    "package/Lindexi_Custom_win-x64",
    "package/Lindexi_Custom",
    "package/Lindexi_linux-x64",
    "package/Lindexi"
];

foreach (var input in inputList)
{
    Console.WriteLine($"输入 {input}");

    string? projectName = null;
    string? custom = null;
    string? runtimeIdentifier = null;
  
    var packingConfigurationInput = input;

    if (packingConfigurationInput.StartsWith("package/"))
    {
        packingConfigurationInput = packingConfigurationInput["package/".Length..];
    }

    // 直接使用字符串分割
    var split = packingConfigurationInput.Split('_');
    projectName = split[0];
    if (split.Length == 3)
    {
        custom = split[1];
        runtimeIdentifier = split[2];
    }
    else if (split.Length == 2)
    {
        // 第二项可能是定制版，也可能是打包平台
        if (split[1].Contains("win-") || split[1].Contains("linux-"))
        {
            runtimeIdentifier = split[1];
        }
        else
        {
            custom = split[1];
        }
    }

    Console.WriteLine($"项目名：{projectName}");
    Console.WriteLine($"定制版：{custom}");
    Console.WriteLine($"打包平台：{runtimeIdentifier}\r\n");
}


/*
   输入 package/Lindexi_Custom_win-x64
   项目名：Lindexi
   定制版：Custom
   打包平台：win-x64
   
   输入 package/Lindexi_Custom
   项目名：Lindexi
   定制版：Custom
   打包平台：
   
   输入 package/Lindexi_linux-x64
   项目名：Lindexi
   定制版：
   打包平台：linux-x64
   
   输入 package/Lindexi
   项目名：Lindexi
   定制版：
   打包平台：
 */