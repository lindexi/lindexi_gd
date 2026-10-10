#:property TargetFramework=net10.0
#:property ImplicitUsings=enable
#:property Nullable=enable

using System;
using System.IO;

if (args.Length is < 1 or > 2 || (args.Length == 2 && args[1] != "--move"))
{
    Console.Error.WriteLine("用法：dotnet run --file Program.cs -- <工作区根目录> [--move]");
    return 1;
}

string root = Path.GetFullPath(args[0]);
if (!Directory.Exists(Path.Combine(root, "AgentLib"))
    || !Directory.Exists(Path.Combine(root, "ChatRoom", "Code")))
{
    Console.Error.WriteLine($"不是预期的工作区根目录：{root}");
    return 1;
}

(string Source, string Destination, string Reason)[] migrations =
[
    (
        "ChatRoom/Code/CodingChatRoom.AvaloniaShell/Services/Chat/CodingChatRunOptions.cs",
        "ChatRoom/Code/CodingChatRoom.AvaloniaShell/Services/Chat/CodingAgentRunExtensions.cs",
        "运行选项已迁移到 AgentLib.Coding，原文件仅包含 CodingAgentRunExtensions，文件名应与实际类型一致。"
    ),
    (
        "AgentLib/AgentLib.Coding.Tests/CodingWorkspaceRuntimeTests.cs",
        "AgentLib/AgentLib.Coding.Tests/CodingWorkspaceToolsReuseTests.cs",
        "旧 runtime 设计已撤回，当前测试类是 CodingWorkspaceToolsReuseTests，移除误导性的旧文件名。"
    ),
    (
        "AgentLib/AgentLib/Model/CopilotChatMessage.Responses.cs",
        "AgentLib/AgentLib/Model/CopilotChatMessageResponseInfo.cs",
        "文件实际定义独立的 CopilotChatMessageResponseInfo，不是 CopilotChatMessage 的 partial 文件。"
    ),
];

bool move = args.Length == 2;
int completed = 0;
foreach (var migration in migrations)
{
    string source = Path.Combine(root, migration.Source.Replace('/', Path.DirectorySeparatorChar));
    string destination = Path.Combine(root, migration.Destination.Replace('/', Path.DirectorySeparatorChar));
    Console.WriteLine($"{migration.Source}\n  -> {migration.Destination}\n  原因：{migration.Reason}");
    if (!File.Exists(source))
    {
        Console.WriteLine(File.Exists(destination) ? "  [目标已存在、源不存在，跳过]" : "  [源不存在，跳过]");
        continue;
    }
    if (File.Exists(destination))
    {
        Console.Error.WriteLine("  [目标已存在，不覆盖；停止执行]");
        return 1;
    }
    if (move)
    {
        File.Move(source, destination);
        Console.WriteLine("  [已迁移]");
    }
    else
    {
        Console.WriteLine("  [仅预览]");
    }
    completed++;
}

Console.WriteLine($"{(move ? "已迁移" : "待迁移")}：{completed} 个。");
if (!move) Console.WriteLine("本次没有移动文件。审查后添加 --move 执行。");
return 0;
