#:property TargetFramework=net10.0
#:property ImplicitUsings=enable
#:property Nullable=enable

using System;
using System.IO;

if (args.Length is < 1 or > 2 || (args.Length == 2 && args[1] != "--delete"))
{
    Console.Error.WriteLine("用法：dotnet run --file Program.cs -- <SemanticKernelSamples 根目录> [--delete]");
    return 1;
}

string root = Path.GetFullPath(args[0]);
if (!Directory.Exists(Path.Combine(root, "AgentLib")) || !Directory.Exists(Path.Combine(root, "ChatRoom", "Code")))
{
    Console.Error.WriteLine($"不是预期工作区：{root}");
    return 1;
}

(string Path, string Reason)[] files =
[
    (
        "ChatRoom/Code/CodingChatRoom.AvaloniaShell.Tests/WindowsSandboxIntegrationTests.cs",
        "依赖临时探针的开发过程集成测试，随探针撤回；不删除沙箱工具的确定性测试。"
    ),
    (
        "ChatRoom/Code/WindowsSandboxProbe/Program.cs",
        "仅写入固定标记的开发验证载荷，不属于产品。"
    ),
    (
        "ChatRoom/Code/WindowsSandboxProbe/WindowsSandboxProbe.csproj",
        "临时探针项目配置；测试项目引用已移除，不纳入新产品。"
    ),
];

bool delete = args.Length == 2;
foreach (var item in files)
{
    string path = Path.Combine(root, item.Path.Replace('/', Path.DirectorySeparatorChar));
    Console.WriteLine($"{item.Path}\n  原因：{item.Reason}");
    if (!File.Exists(path))
    {
        Console.WriteLine("  [不存在，跳过]");
        continue;
    }
    if (delete)
    {
        File.Delete(path);
        Console.WriteLine("  [已删除]");
    }
    else Console.WriteLine("  [预览，将删除]");
}

// 只删除明确列出的探针目录，且仅在目录已经为空时删除；不递归删除未审查的产物或文件。
string probeDirectory = Path.Combine(root, "ChatRoom", "Code", "WindowsSandboxProbe");
if (delete && Directory.Exists(probeDirectory))
{
    if (!Directory.EnumerateFileSystemEntries(probeDirectory).Any())
        Directory.Delete(probeDirectory);
    else Console.WriteLine($"[保留目录内未列入清单的文件] {probeDirectory}");
}
Console.WriteLine(delete ? "清理完成。" : "仅预览。审查后添加 --delete 执行。");
return 0;
