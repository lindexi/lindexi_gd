#:property TargetFramework=net10.0
#:property ImplicitUsings=enable
#:property Nullable=enable

using System;
using System.IO;

if (args.Length is < 1 or > 2 || (args.Length == 2 && args[1] != "--rename"))
{
    Console.Error.WriteLine("用法：dotnet run --file Program.cs -- <SemanticKernelSamples 根目录> [--rename]");
    return 1;
}

string root = Path.GetFullPath(args[0]);
if (!Directory.Exists(Path.Combine(root, "AgentLib")) || !Directory.Exists(Path.Combine(root, "CodingAgent")))
{
    Console.Error.WriteLine($"不是预期的 SemanticKernelSamples 根目录：{root}");
    return 1;
}

(string Source, string Destination, string Reason)[] renames =
[
    (
        "CodingAgent/Code/CodingAgent.Avalonia/CodingChatRoom.AvaloniaShell.csproj",
        "CodingAgent/Code/CodingAgent.Avalonia/CodingAgent.Avalonia.csproj",
        "应用项目名与目录和 Avalonia 实现身份一致；输出程序集仍为 CodingAgent。"
    ),
    (
        "CodingAgent/Code/CodingAgent.Avalonia/CodingChatRoom.AvaloniaShell.csproj.user",
        "CodingAgent/Code/CodingAgent.Avalonia/CodingAgent.Avalonia.csproj.user",
        "现有项目用户配置随对应 csproj 更名，内容保持不变。"
    ),
    (
        "CodingAgent/Tests/CodingAgent.Avalonia.Tests/CodingChatRoom.AvaloniaShell.Tests.csproj",
        "CodingAgent/Tests/CodingAgent.Avalonia.Tests/CodingAgent.Avalonia.Tests.csproj",
        "测试项目名与目录及测试程序集名称一致。"
    ),
];

bool rename = args.Length == 2;
Console.WriteLine($"工作区：{root}");
Console.WriteLine(rename ? "模式：执行明确清单中的重命名" : "模式：仅预览");
foreach (var item in renames)
{
    string source = Resolve(item.Source);
    string destination = Resolve(item.Destination);
    Console.WriteLine($"{item.Source}\n  -> {item.Destination}\n  原因：{item.Reason}");
    if (!File.Exists(source))
    {
        Console.Error.WriteLine($"源文件不存在，停止：{source}");
        return 1;
    }
    if (File.Exists(destination) || Directory.Exists(destination))
    {
        Console.Error.WriteLine($"目标已存在，不覆盖，停止：{destination}");
        return 1;
    }
}

if (!rename)
{
    Console.WriteLine("预览完成，没有移动文件。审查后添加 --rename 执行。");
    return 0;
}

foreach (var item in renames)
{
    File.Move(Resolve(item.Source), Resolve(item.Destination));
    Console.WriteLine($"[已重命名] {item.Source} -> {item.Destination}");
}

Console.WriteLine("三项重命名完成，文件内容未改动。");
Console.WriteLine("下一步需更新 CodingAgent.slnx、测试项目引用及文档中的项目路径，再验证构建。");
Console.WriteLine("命名空间、资源文件、用户数据目录及配置格式不在本次清单内。");
return 0;

string Resolve(string path) => Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar));
