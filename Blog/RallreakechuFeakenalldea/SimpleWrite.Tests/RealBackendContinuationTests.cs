using AgentLib.Core;
using AgentLib.Reducers;

using AvaloniaAgentLib.ViewModel;

using Microsoft.Extensions.AI;

namespace SimpleWrite.Tests;

[TestClass]
public sealed class RealBackendContinuationTests
{
    [TestMethod]
    [TestCategory("RealBackend")]
    [Timeout(180000)]
    public async Task ToolCallAwareReducer_WithRealBackend_SkipsProtocolInvalidHistory()
    {
        using var timeoutCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        CancellationToken cancellationToken = timeoutCancellationTokenSource.Token;
        FileInfo configurationFile = FindWorkspaceFile("AgentConfiguration.json");

        var viewModel = new CopilotViewModel();
        await viewModel.AgentApiEndpointManager.LoadConfigurationFromJsonFileAsync(configurationFile);
        IChatClient chatClient = await viewModel.AgentApiEndpointManager.PrimaryModel.GetChatClientAsync();

        var connectivityReducer = new CopilotChatManagerToolCallChatReducer(chatClient)
        {
            ConditionalCompressionTokenCountThreshold = 1,
            ForcedCompressionTokenCountThreshold = 1,
        };
        bool realBackendCompleted = false;
        connectivityReducer.CompressionCompleted += (_, _) => realBackendCompleted = true;

        await connectivityReducer.ReduceAsync(
            [
                new ChatMessage(ChatRole.User, "请总结以下测试对话。"),
                new ChatMessage(ChatRole.Assistant, "这是用于验证真实模型后台连通性的测试内容。"),
            ],
            cancellationToken);

        Assert.IsTrue(realBackendCompleted, "合法历史应实际调用真实模型后台并完成压缩。");

        var guardedInnerReducer = new CopilotChatManagerToolCallChatReducer(chatClient)
        {
            ConditionalCompressionTokenCountThreshold = 1,
            ForcedCompressionTokenCountThreshold = 1,
        };
        bool invalidHistoryWasSentToBackend = false;
        guardedInnerReducer.CompressionStarted += (_, _) => invalidHistoryWasSentToBackend = true;
        var reducer = new ToolCallAwareChatReducer(guardedInnerReducer);
        var invalidHistory = new List<ChatMessage>
        {
            new(ChatRole.User, "读取文件并继续分析。"),
            new(ChatRole.Assistant,
            [
                new FunctionCallContent("real-backend-call-1", "ReadFileLines",
                    new Dictionary<string, object?>())
            ]),
            new(ChatRole.Assistant, "工具调用后的非 Tool 消息。"),
            new(ChatRole.Tool,
            [
                new FunctionResultContent("real-backend-call-1", "文件内容")
            ]),
        };

        IEnumerable<ChatMessage> result = await reducer.ReduceAsync(invalidHistory, cancellationToken);

        Assert.IsFalse(invalidHistoryWasSentToBackend,
            "协议非法历史不得发送给真实压缩模型，否则会触发 insufficient tool messages 错误。");
        CollectionAssert.AreEqual(invalidHistory, result.ToList());
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
