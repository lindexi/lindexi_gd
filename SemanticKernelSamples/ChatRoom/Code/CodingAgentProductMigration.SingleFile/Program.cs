#:property TargetFramework=net10.0
#:property ImplicitUsings=enable
#:property Nullable=enable

using System;
using System.IO;

if (args.Length is < 1 or > 2 || (args.Length == 2 && args[1] != "--move"))
{
    Console.Error.WriteLine("用法：dotnet run --file Program.cs -- <SemanticKernelSamples 根目录> [--move]");
    return 1;
}

string root = Path.GetFullPath(args[0]);
if (!Directory.Exists(Path.Combine(root, "AgentLib"))
    || !Directory.Exists(Path.Combine(root, "ChatRoom", "Code")))
{
    Console.Error.WriteLine($"不是预期的 SemanticKernelSamples 根目录：{root}");
    return 1;
}

// 只移动下列明确列出的目录，不扫描其他项目、不改写文件内容。
// 文档必须先移出应用目录，随后再移动应用项目。
(string Source, string Destination, string Reason)[] migrations =
[
    (
        "ChatRoom/Code/CodingChatRoom.AvaloniaShell/Docs",
        "CodingAgent/Docs",
        "将产品文档移到一级 Docs；保留其现有内部结构，本步不重新整理文档。"
    ),
    (
        "ChatRoom/Code/CodingChatRoom.AvaloniaShell",
        "CodingAgent/Code/CodingAgent.Avalonia",
        "移动当前 Avalonia 产品实现；框架标识保留在目录名称中。"
    ),
    (
        "ChatRoom/Code/CodingChatRoom.AvaloniaShell.Tests",
        "CodingAgent/Tests/CodingAgent.Avalonia.Tests",
        "将应用测试放入独立产品的 Tests 目录。"
    ),
];

bool move = args.Length == 2;
Console.WriteLine($"工作区：{root}");
Console.WriteLine(move ? "模式：执行目录移动" : "模式：预览，不移动文件");

// 执行前检查完整清单，目标冲突或源目录缺失时不开始移动。
foreach (var migration in migrations)
{
    string source = Resolve(migration.Source);
    string destination = Resolve(migration.Destination);
    Console.WriteLine($"{migration.Source}\n  -> {migration.Destination}\n  原因：{migration.Reason}");
    if (!Directory.Exists(source))
    {
        Console.Error.WriteLine($"源目录不存在：{source}");
        return 1;
    }
    if (Directory.Exists(destination) || File.Exists(destination))
    {
        Console.Error.WriteLine($"目标已存在，不合并、不覆盖：{destination}");
        return 1;
    }
}

if (!move)
{
    Console.WriteLine("预览完成。审查清单后添加 --move 执行。");
    return 0;
}

foreach (var migration in migrations)
{
    string source = Resolve(migration.Source);
    string destination = Resolve(migration.Destination);
    Directory.CreateDirectory(Path.GetDirectoryName(destination)
        ?? throw new InvalidOperationException($"目标目录没有父路径：{destination}"));
    Directory.Move(source, destination);
    Console.WriteLine($"[已移动] {migration.Source} -> {migration.Destination}");
}

Console.WriteLine("移动完成。未修改项目文件名、程序集名、命名空间、资源 URI、项目引用、解决方案或用户数据路径。");
Console.WriteLine("现有项目引用与构建配置可能暂时失效，请在下一步统一调整后构建。");
return 0;

string Resolve(string relativePath) => Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
