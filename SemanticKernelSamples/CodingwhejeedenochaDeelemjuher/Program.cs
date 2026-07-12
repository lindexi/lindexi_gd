using CodingwhejeedenochaDeelemjuher;

string workspacePath = args.FirstOrDefault()
    ?? Directory.GetParent(AppContext.BaseDirectory)?.FullName
    ?? Environment.CurrentDirectory;

await using RoslynAgentTools roslynTools = await RoslynAgentTools.CreateAsync(workspacePath);
var agentTools = roslynTools.AsAITools();

Console.WriteLine($"已为工作区 {Path.GetFullPath(workspacePath)} 创建以下 Agent 工具：");
foreach (var tool in agentTools)
{
    Console.WriteLine($"- {tool.Name}");
}
