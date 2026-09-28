using AgentLib.Core;
using AgentLib.Model;

using AvaloniaAgentLib.ViewModel;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace SimpleWrite.Tests;

[TestClass]
public sealed class CopilotEditorToolFlowTests
{
    [TestMethod]
    [TestCategory("RealBackend")]
    [Timeout(180000)]
    public async Task WriteBlogSkill_WhenApprovalIsDisabled_CompletesToolCallAndCanContinue()
    {
        using var testTimeoutSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        FileInfo configurationFile = FindWorkspaceFile("AgentConfiguration.json");
        var copilotViewModel = new CopilotViewModel();
        await copilotViewModel.AgentApiEndpointManager.LoadConfigurationFromJsonFileAsync(configurationFile);

        copilotViewModel.AIContextProviders =
        [
            new AgentSkillsProvider(
                @"C:\Users\lindexi\AppData\Local\SimpleWrite\CopilotAbilities\Skills",
                options: new AgentSkillsProviderOptions
                {
                    DisableLoadSkillApproval = true,
                    DisableReadSkillResourceApproval = true,
                })
        ];

        SendMessageResult firstResult = copilotViewModel.SendMessage(new SendMessageRequest(
            "必须调用 load_skill 工具加载 write-blog 技能。加载完成后只回复：技能加载完成。")
        {
            WithHistory = true,
            CancellationToken = testTimeoutSource.Token,
        });
        SendMessageRunState firstRunState = await firstResult.RunTask;

        Assert.IsTrue(firstRunState.IsSuccess, $"首轮未成功结束。WasCanceled={firstRunState.WasCanceled}");

        AgentSession agentSession = copilotViewModel.SelectedSession.AgentSession
            ?? throw new AssertFailedException("首轮自然结束后未创建 AgentSession。");
        Assert.IsTrue(agentSession.TryGetInMemoryChatHistory(out List<ChatMessage>? history));

        List<FunctionCallContent> loadSkillCalls = history
            .SelectMany(message => message.Contents.OfType<FunctionCallContent>())
            .Where(call => string.Equals(call.Name, AgentSkillsProvider.LoadSkillToolName, StringComparison.Ordinal))
            .ToList();
        HashSet<string?> resultIds = history
            .SelectMany(message => message.Contents.OfType<FunctionResultContent>())
            .Select(result => result.CallId)
            .ToHashSet(StringComparer.Ordinal);

        Assert.IsNotEmpty(loadSkillCalls, "真实模型应调用 load_skill。");
        Assert.IsTrue(loadSkillCalls.All(call => resultIds.Contains(call.CallId)),
            "每个 load_skill 调用都应有对应的工具结果。");

        await copilotViewModel.SendMessageAsync(
            [new TextContent("只回复：继续成功。不要调用工具。")],
            withHistory: true,
            cancellationToken: testTimeoutSource.Token);

        string conversation = string.Join('\n', copilotViewModel.ChatMessages.Select(message => message.Content));
        Assert.DoesNotContain("insufficient tool messages", conversation, StringComparison.OrdinalIgnoreCase);
    }

    private static FileInfo FindWorkspaceFile(string fileName)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidatePath = Path.Combine(directory.FullName, fileName);
            if (File.Exists(candidatePath))
            {
                return new FileInfo(candidatePath);
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"未找到工作区文件 {fileName}。");
    }
}
