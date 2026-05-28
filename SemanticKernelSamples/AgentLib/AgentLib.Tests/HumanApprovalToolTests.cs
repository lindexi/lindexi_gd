using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Model;
using AgentLib.Tests.Fakes;
using AgentLib.Tools;

using Microsoft.Extensions.AI;

using System.Reflection;
using System.Text.Json;

namespace AgentLib.Tests;

[TestClass]
public class HumanApprovalToolTests
{
    [TestMethod]
    [Description("需要审批的工具在同意前应保持等待，审批后继续执行并保留审批片段")]
    public async Task SendMessageAsync_WhenApprovalToolIsPending_WaitsUntilApproved()
    {
        var primaryChatClient = new FakeChatClient();
        var testTool = new ApprovalAwareTestTool();
        string toolName = "DangerousTool";
        primaryChatClient.OnGetStreamingResponseAsync = (_, options, cancellationToken) =>
            CreateToolInvocationAsyncEnumerable(options, "tool-call-1", toolName, new Dictionary<string, object?>
            {
                ["path"] = "demo.txt"
            }, cancellationToken);
        var context = CopilotChatManagerTestContext.Create(primaryChatClient);

        Task sendTask = context.ChatManager.SendMessageAsync(
            inputText: "请执行危险操作",
            withHistory: true,
            createNewSession: false,
            tools: [HumanApprovalTool.Wrap(testTool.CreateTool(toolName, "危险工具"), "该工具会修改工作区内容，需要审批。")],
            toolMode: ChatToolMode.RequireAny,
            cancellationToken: CancellationToken.None);

        CopilotChatApprovalToolItem approvalToolItem = await WaitForApprovalItemAsync(context.ChatManager, toolName);
        Assert.IsFalse(sendTask.IsCompleted);
        Assert.AreEqual(CopilotToolApprovalState.Pending, approvalToolItem.ApprovalState);
        Assert.IsTrue(approvalToolItem.CanRespondToApproval);

        context.ChatManager.ApproveToolExecution(approvalToolItem);
        await sendTask;

        Assert.AreEqual(1, testTool.InvocationCount);
        Assert.AreEqual(CopilotToolApprovalState.Approved, approvalToolItem.ApprovalState);
        Assert.IsFalse(approvalToolItem.CanRespondToApproval);
        StringAssert.Contains(approvalToolItem.OutputText, "执行完成");
    }

    [TestMethod]
    [Description("需要审批的工具被拒绝后不应继续执行，并应把拒绝结果回填到审批片段")]
    public async Task SendMessageAsync_WhenApprovalToolIsRejected_DoesNotInvokeInnerTool()
    {
        var primaryChatClient = new FakeChatClient();
        var testTool = new ApprovalAwareTestTool();
        string toolName = "DangerousTool";
        primaryChatClient.OnGetStreamingResponseAsync = (_, options, cancellationToken) =>
            CreateToolInvocationAsyncEnumerable(options, "tool-call-2", toolName, new Dictionary<string, object?>
            {
                ["path"] = "demo.txt"
            }, cancellationToken);
        var context = CopilotChatManagerTestContext.Create(primaryChatClient);

        Task sendTask = context.ChatManager.SendMessageAsync(
            inputText: "请执行危险操作",
            withHistory: true,
            createNewSession: false,
            tools: [HumanApprovalTool.Wrap(testTool.CreateTool(toolName, "危险工具"), "该工具会修改工作区内容，需要审批。")],
            toolMode: ChatToolMode.RequireAny,
            cancellationToken: CancellationToken.None);

        CopilotChatApprovalToolItem approvalToolItem = await WaitForApprovalItemAsync(context.ChatManager, toolName);
        context.ChatManager.RejectToolExecution(approvalToolItem, "当前不允许执行");
        await sendTask;

        Assert.AreEqual(0, testTool.InvocationCount);
        Assert.AreEqual(CopilotToolApprovalState.Rejected, approvalToolItem.ApprovalState);
        Assert.AreEqual("当前不允许执行", approvalToolItem.DecisionReason);
        StringAssert.Contains(approvalToolItem.OutputText, "已被人工拒绝");
    }

    private static async Task<CopilotChatApprovalToolItem> WaitForApprovalItemAsync(CopilotChatManager chatManager, string toolName)
    {
        for (int i = 0; i < 100; i++)
        {
            CopilotChatApprovalToolItem? approvalToolItem = chatManager.ChatMessages
                .LastOrDefault()?
                .MessageItems
                .OfType<CopilotChatApprovalToolItem>()
                .FirstOrDefault(item => string.Equals(item.ToolName, toolName, StringComparison.Ordinal));

            if (approvalToolItem is not null)
            {
                return approvalToolItem;
            }

            await Task.Delay(20);
        }

        throw new AssertFailedException($"未在限定时间内找到 {toolName} 的审批片段。");
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> CreateToolInvocationAsyncEnumerable(
        ChatOptions? options,
        string callId,
        string toolName,
        IDictionary<string, object?>? arguments,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return CopilotChatManagerTestContext.AssistantFunctionCall(callId, toolName, arguments);

        AITool tool = options?.Tools?.FirstOrDefault(candidate => string.Equals(candidate.Name, toolName, StringComparison.Ordinal))
                      ?? throw new InvalidOperationException($"未找到名为 {toolName} 的工具。");

        if (tool is not AIFunction function)
        {
            throw new InvalidOperationException($"工具 {toolName} 不是可调用函数。");
        }

        object? result = await function.InvokeAsync(new AIFunctionArguments(arguments?.ToDictionary(pair => pair.Key, pair => pair.Value)), cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        yield return CopilotChatManagerTestContext.AssistantFunctionResult(callId, NormalizeResult(result));
    }

    private static object? NormalizeResult(object? result)
    {
        if (result is JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.String => jsonElement.GetString(),
                _ => jsonElement.ToString()
            };
        }

        return result;
    }

    private sealed class ApprovalAwareTestTool
    {
        public int InvocationCount { get; private set; }

        public AITool CreateTool(string name, string description)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(description);

            MethodInfo methodInfo = GetType().GetMethod(nameof(ExecuteAsync), BindingFlags.Instance | BindingFlags.Public)
                                    ?? throw new InvalidOperationException($"未找到 {nameof(ExecuteAsync)} 方法。");
            return AIFunctionFactory.Create(methodInfo, this, name, description, serializerOptions: null);
        }

        [global::System.ComponentModel.DescriptionAttribute("执行一个受审批保护的测试工具。")]
        public Task<string> ExecuteAsync(string path, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            cancellationToken.ThrowIfCancellationRequested();
            InvocationCount++;
            return Task.FromResult("执行完成");
        }
    }
}