#:property TargetFramework=net10.0
#:property ImplicitUsings=enable
#:property Nullable=enable

using System;
using System.IO;

if (args.Length is < 1 or > 2 || (args.Length == 2 && args[1] != "--delete"))
{
    Console.Error.WriteLine("用法：dotnet run --file Program.cs -- <工作区根目录> [--delete]");
    return 1;
}

string root = Path.GetFullPath(args[0]);
if (!Directory.Exists(Path.Combine(root, "AgentLib"))
    || !Directory.Exists(Path.Combine(root, "ChatRoom", "Code")))
{
    Console.Error.WriteLine($"不是预期的工作区根目录：{root}");
    return 1;
}

string[] obsoleteFiles =
[
    // 旧结果类型文件；已拆分为 ICodingAgentRunResult.cs 和 CompletedCodingAgentRunResult.cs。
    "AgentLib/AgentLib.Coding/CodingAgentRunResult.cs",
    // 参数容器已合并到 ResponsesCodingAgentRunner，不再有独立职责。
    "AgentLib/AgentLib.Coding/Responses/ResponsesCodingRunContext.cs",
    // 原生历史已迁移到 AgentLib 的 CopilotResponsesSession，旧状态类型废弃。
    "AgentLib/AgentLib.Coding/Responses/ResponsesConversationState.cs",
    // 依赖固定本机临时报告的过程审计，确定性回归测试已保留。
    "ChatRoom/Code/CodingChatRoom.AvaloniaShell.Tests/ResponsesCompactionReportTests.cs",
    // 模型列表与替代模型的过程实验已结束，调查结果已记录到交接文档。
    "ChatRoom/Code/CodingChatRoom.AvaloniaShell.Tests/ResponsesModelCatalogLiveTests.cs",
    // 临时提供商实验已明确撤回，不属于长期验收测试。
    "ChatRoom/Code/CodingChatRoom.AvaloniaShell.Tests/ResponsesProviderCompactionLiveTests.cs",
];

bool delete = args.Length == 2;
int candidates = 0;
foreach (string relativePath in obsoleteFiles)
{
    string path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
    if (!File.Exists(path))
    {
        Console.WriteLine($"[不存在，跳过] {relativePath}");
        continue;
    }

    if (delete)
    {
        File.Delete(path);
        Console.WriteLine($"[已删除] {relativePath}");
    }
    else
    {
        Console.WriteLine($"[预览，将删除] {relativePath}");
    }
    candidates++;
}

Console.WriteLine($"{(delete ? "已删除" : "待删除")}：{candidates} 个。");
if (!delete) Console.WriteLine("本次仅预览，没有删除文件。审查后添加 --delete 执行。");
return 0;
