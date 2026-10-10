#:property TargetFramework=net10.0
#:property ImplicitUsings=enable
#:property Nullable=enable

using System;
using System.IO;

if (args.Length is < 1 or > 2 || (args.Length == 2 && args[1] is not ("--move" or "--finish-root")))
{
    Console.Error.WriteLine("用法：dotnet run --file Program.cs -- <SemanticKernelSamples 根目录> [--move|--finish-root]");
    return 1;
}

string root = Path.GetFullPath(args[0]);
if (!Directory.Exists(Path.Combine(root, "AgentLib")) || !Directory.Exists(Path.Combine(root, "CodingAgent")))
{
    Console.Error.WriteLine($"不是预期的迁移前工作区：{root}");
    return 1;
}

if (args.Length == 2 && args[1] == "--finish-root")
{
    // 仅续跑已完成前六项的现场；不重新执行内部更名。
    string[] completedFiles =
    [
        "CodingAgent/Code/PrismCodingAgent.AvaloniaShell/PrismCodingAgent.AvaloniaShell.csproj",
        "CodingAgent/Code/PrismCodingAgent.AvaloniaShell/PrismCodingAgent.AvaloniaShell.csproj.user",
        "CodingAgent/Tests/PrismCodingAgent.AvaloniaShell.Tests/PrismCodingAgent.AvaloniaShell.Tests.csproj",
        "CodingAgent/PrismCodingAgent.slnx",
    ];
    foreach (string path in completedFiles)
    {
        if (!File.Exists(Resolve(path)) && !File.Exists(Resolve("PrismCodingAgent" + path["CodingAgent".Length..])))
        {
            Console.Error.WriteLine($"内部迁移尚未完成，不能续跑根目录移动：{path}");
            return 1;
        }
    }
    return MoveProductRoot();
}

// 依次改名内部文件、内部目录，最后移动产品根目录。
// 所有源路径都是迁移前路径；先检查完整清单，再按下列顺序执行。
(bool IsDirectory, string Source, string Destination, string Reason)[] migrations =
[
    (
        false,
        "CodingAgent/Code/CodingAgent.Avalonia/CodingAgent.Avalonia.csproj",
        "CodingAgent/Code/CodingAgent.Avalonia/PrismCodingAgent.AvaloniaShell.csproj",
        "应用项目采用新产品名，并用 AvaloniaShell 标识实现框架，避免命名空间使用 Avalonia 段。"
    ),
    (
        false,
        "CodingAgent/Code/CodingAgent.Avalonia/CodingAgent.Avalonia.csproj.user",
        "CodingAgent/Code/CodingAgent.Avalonia/PrismCodingAgent.AvaloniaShell.csproj.user",
        "现有 IDE 用户配置随应用项目文件更名，内容不变。"
    ),
    (
        false,
        "CodingAgent/Tests/CodingAgent.Avalonia.Tests/CodingAgent.Avalonia.Tests.csproj",
        "CodingAgent/Tests/CodingAgent.Avalonia.Tests/PrismCodingAgent.AvaloniaShell.Tests.csproj",
        "测试项目文件与新产品和 UI 项目命名一致。"
    ),
    (
        false,
        "CodingAgent/CodingAgent.slnx",
        "CodingAgent/PrismCodingAgent.slnx",
        "独立产品解决方案采用 PrismCodingAgent 名称。"
    ),
    (
        true,
        "CodingAgent/Code/CodingAgent.Avalonia",
        "CodingAgent/Code/PrismCodingAgent.AvaloniaShell",
        "应用实现目录与更名后的项目一致。"
    ),
    (
        true,
        "CodingAgent/Tests/CodingAgent.Avalonia.Tests",
        "CodingAgent/Tests/PrismCodingAgent.AvaloniaShell.Tests",
        "测试目录与更名后的项目一致。"
    ),
    (
        true,
        "CodingAgent",
        "PrismCodingAgent",
        "整体迁移产品根目录，保留一级 Code、Tests、Docs 及其内容，方便后续 git subtree 分离。"
    ),
];

bool move = args.Length == 2;
Console.WriteLine($"根目录：{root}");
Console.WriteLine(move ? "模式：执行明确清单中的迁移" : "模式：预览，不修改文件");
foreach (var item in migrations)
{
    string source = Resolve(item.Source);
    string destination = Resolve(item.Destination);
    Console.WriteLine($"{item.Source}\n  -> {item.Destination}\n  原因：{item.Reason}");
    if (!(item.IsDirectory ? Directory.Exists(source) : File.Exists(source)))
    {
        Console.Error.WriteLine($"源不存在，停止：{source}");
        return 1;
    }
    if (File.Exists(destination) || Directory.Exists(destination))
    {
        Console.Error.WriteLine($"目标存在，不合并、不覆盖，停止：{destination}");
        return 1;
    }
}

if (!move)
{
    Console.WriteLine("预览完成。审查后添加 --move 执行。");
    return 0;
}

foreach (var item in migrations)
{
    if (item.Source == "CodingAgent")
    {
        int result = MoveProductRoot();
        if (result != 0) return result;
        continue;
    }
    if (item.IsDirectory) Directory.Move(Resolve(item.Source), Resolve(item.Destination));
    else File.Move(Resolve(item.Source), Resolve(item.Destination));
    Console.WriteLine($"[已迁移] {item.Source} -> {item.Destination}");
}

Console.WriteLine("迁移完成。未修改任何文件内容，未删除构建产物，也未执行构建。");
Console.WriteLine("下一步修正解决方案路径、ProjectReference、AssemblyName、RootNamespace、XAML/C# 命名空间及 avares 资源 URI。");
Console.WriteLine("AgentLib 保持原位置；用户数据目录、配置和会话格式不因迁移自动改变。");
return 0;

string Resolve(string path) => Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar));

int MoveProductRoot()
{
    string sourceRoot = Resolve("CodingAgent");
    string destinationRoot = Resolve("PrismCodingAgent");
    // 这些目录不属于产品源码迁移；保留原处，不尝试移动或删除。
    string[] excludedDirectories = [".vs", ".idea", ".git", "bin", "obj", "artifacts", "TestResults", ".avalonia-build-tasks"];
    var files = new List<(string Source, string Destination)>();
    CollectFiles(sourceRoot);
    foreach (var file in files)
    {
        if (File.Exists(file.Destination) || Directory.Exists(file.Destination))
        {
            Console.Error.WriteLine($"目标已存在，不覆盖：{file.Destination}");
            return 1;
        }
    }
    foreach (var file in files)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file.Destination)!);
        File.Move(file.Source, file.Destination);
        Console.WriteLine($"[已移动文件] {Path.GetRelativePath(root, file.Source)} -> {Path.GetRelativePath(root, file.Destination)}");
    }
    Console.WriteLine("产品文件迁移完成；原目录及缓存/产物保留，不移动根目录，不删除任何文件。");
    Console.WriteLine("下一步修正项目配置。若迁移中断，可重新执行 --finish-root，已移走的文件不会再次枚举。");
    return 0;

    void CollectFiles(string directory)
    {
        foreach (string file in Directory.EnumerateFiles(directory))
        {
            files.Add((file, Path.Combine(destinationRoot, Path.GetRelativePath(sourceRoot, file))));
        }
        foreach (string child in Directory.EnumerateDirectories(directory))
        {
            if (excludedDirectories.Contains(Path.GetFileName(child), StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[不迁移目录] {Path.GetRelativePath(root, child)}");
                continue;
            }
            CollectFiles(child);
        }
    }
}
