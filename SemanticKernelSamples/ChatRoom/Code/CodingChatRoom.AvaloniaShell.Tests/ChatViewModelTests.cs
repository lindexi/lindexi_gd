using System.Collections.Generic;
using System.ComponentModel;

using AgentLib;
using AgentLib.Coding;
using AgentLib.Logging;
using AgentLib.Model;

using Avalonia.Controls;
using Avalonia.Controls.Templates;

using CodingChatRoom.AvaloniaShell.ViewModels;
using CodingChatRoom.AvaloniaShell.Views;
using CodingChatRoom.AvaloniaShell.Services;

using Microsoft.Extensions.AI;

namespace CodingChatRoom.AvaloniaShell.Tests;

[TestClass]
public sealed class ChatViewModelTests
{
    [TestMethod(DisplayName = "用户消息应靠右且 Copilot 消息应靠左")]
    [Timeout(5000)]
    public void MessageProjectionShouldExposeUserAndAssistantAlignment()
    {
        var userMessage = new MessageItemViewModel(new CopilotChatMessage(ChatRole.User, "用户问题"));
        var assistantMessage = new MessageItemViewModel(new CopilotChatMessage(ChatRole.Assistant, "助手回复"));

        Assert.IsTrue(userMessage.IsUserMessage);
        Assert.IsFalse(userMessage.IsAssistantMessage);
        Assert.IsTrue(assistantMessage.IsAssistantMessage);
        Assert.AreEqual("Copilot", assistantMessage.Author);
    }

    [TestMethod(DisplayName = "工具和审批片段应选择对应模板")]
    [Timeout(5000)]
    public void TemplateSelectorShouldSelectToolAndApprovalTemplates()
    {
        var toolTemplate = new TestDataTemplate();
        var approvalTemplate = new TestDataTemplate();
        var selector = new ChatMessageItemTemplateSelector
        {
            ToolItemTemplate = toolTemplate,
            ApprovalToolItemTemplate = approvalTemplate,
        };

        Assert.AreSame(toolTemplate, selector.SelectTemplate(new CopilotChatToolItem("tool-call", "read_file", "path=a.cs")));
        Assert.AreSame(approvalTemplate, selector.SelectTemplate(new CopilotChatApprovalToolItem("approval-call", "write_file", "path=a.cs")));
    }

    [TestMethod(DisplayName = "图片片段应选择图片模板")]
    [Timeout(5000)]
    public void TemplateSelectorShouldSelectImageTemplate()
    {
        var imageTemplate = new TestDataTemplate();
        var selector = new ChatMessageItemTemplateSelector
        {
            ImageItemTemplate = imageTemplate,
        };
        var imageItem = new CopilotChatImageItem(BinaryData.FromBytes([1, 2, 3]), "image/png");

        Assert.AreSame(imageTemplate, selector.SelectTemplate(imageItem));
    }

    [TestMethod(DisplayName = "用户多模态消息投影应保留图片片段")]
    [Timeout(5000)]
    public void UserMultimodalMessageProjectionShouldExposeImageItem()
    {
        IReadOnlyList<AIContent> contents =
        [
            new TextContent("分析图片"),
            new DataContent(new byte[] { 1, 2, 3 }, "image/webp"),
        ];
        using var viewModel = new MessageItemViewModel(CopilotChatMessage.CreateUser(contents));

        Assert.HasCount(2, viewModel.MessageItems);
        CopilotChatImageItem imageItem = Assert.IsInstanceOfType<CopilotChatImageItem>(viewModel.MessageItems[1]);
        Assert.AreEqual("image/webp", imageItem.MimeType);
    }

    [TestMethod(DisplayName = "流式文本和用量更新应刷新消息投影属性")]
    [Timeout(5000)]
    public void StreamingTextAndUsageShouldRefreshProjectionProperties()
    {
        var message = new CopilotChatMessage(ChatRole.Assistant, string.Empty);
        using var viewModel = new MessageItemViewModel(message);
        var changedProperties = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

        message.AppendText("流式回复");
        message.AppendUsageDetails(new UsageDetails
        {
            InputTokenCount = 12,
            OutputTokenCount = 8,
            TotalTokenCount = 20,
        });

        Assert.AreEqual("流式回复", viewModel.Content);
        CollectionAssert.Contains(changedProperties, nameof(MessageItemViewModel.Content));
        CollectionAssert.Contains(changedProperties, nameof(MessageItemViewModel.FullContent));
        CollectionAssert.Contains(changedProperties, nameof(MessageItemViewModel.UsageSummaryText));
        Assert.IsTrue(viewModel.HasUsageDetails);
        StringAssert.StartsWith(viewModel.UsageSummaryText, "当前用量总计 20 用量总计 20");
    }

    [TestMethod(DisplayName = "用量摘要应先显示最后一次用量总计再显示累计用量总计")]
    [Timeout(5000)]
    public void UsageSummaryShouldShowCurrentTotalBeforeAccumulatedTotal()
    {
        var message = new CopilotChatMessage(ChatRole.Assistant, "回复");
        using var viewModel = new MessageItemViewModel(message);
        message.AppendUsageDetails(new UsageDetails { TotalTokenCount = 100 });
        message.AppendUsageDetails(new UsageDetails { TotalTokenCount = 40 });

        StringAssert.StartsWith(viewModel.UsageSummaryText, "当前用量总计 40 用量总计 140");
    }

    [TestMethod(DisplayName = "复制正文和整条消息应使用不同内容")]
    [Timeout(5000)]
    public void CopyContentAndFullMessageShouldUseExpectedText()
    {
        var message = new CopilotChatMessage(ChatRole.Assistant, "公开正文");
        message.MessageItems.Add(new CopilotChatToolItem("tool-call", "read_file", "path=a.cs", "文件内容"));
        using var viewModel = new MessageItemViewModel(message);

        Assert.AreEqual("公开正文", viewModel.Content);
        StringAssert.Contains(viewModel.FullContent, "工具：read_file");
        StringAssert.Contains(viewModel.FullContent, "文件内容");
    }

    [TestMethod(DisplayName = "切换会话后旧消息集合不应继续更新当前界面")]
    [Timeout(5000)]
    public async Task SwitchingSessionShouldUnsubscribePreviousMessageCollection()
    {
        var manager = new CopilotChatManager();
        CopilotChatSession previousSession = manager.SelectedSession;
        await previousSession.AddMessageAsync(new CopilotChatMessage(ChatRole.User, "旧会话问题"));
        using var viewModel = new ChatViewModel(manager, "当前模型：测试模型");

        manager.CreateNewSession();
        int currentMessageCount = viewModel.Messages.Count;
        await previousSession.AddMessageAsync(new CopilotChatMessage(ChatRole.Assistant, "旧会话迟到更新"));

        Assert.AreEqual(manager.SelectedSession.SessionId, viewModel.CurrentSessionId);
        Assert.AreEqual(currentMessageCount, viewModel.Messages.Count);
    }

    [TestMethod(DisplayName = "审批入口应复用聊天管理器完成决策")]
    [Timeout(5000)]
    public void ApprovalActionsShouldDelegateToChatManager()
    {
        var manager = new CopilotChatManager();
        using var viewModel = new ChatViewModel(manager, "当前模型：测试模型");
        var approvedItem = new CopilotChatApprovalToolItem("approved", "write_file", "path=a.cs");
        var rejectedItem = new CopilotChatApprovalToolItem("rejected", "delete_file", "path=b.cs");

        viewModel.ApproveTool(approvedItem);
        viewModel.RejectTool(rejectedItem);

        Assert.AreEqual(CopilotToolApprovalState.Approved, approvedItem.ApprovalState);
        Assert.AreEqual(CopilotToolApprovalState.Rejected, rejectedItem.ApprovalState);
    }

    [TestMethod(DisplayName = "输入有效消息时发送命令应运行并清空输入")]
    [Timeout(5000)]
    public async Task SendCommandShouldRunAndClearInput()
    {
        var manager = new CopilotChatManager();
        var runner = new ImmediateRunner(manager);
        var application = new CodingChatApplication(manager, new EmptySessionStore(), runner);
        await application.InitializeAsync();
        using var viewModel = new ChatViewModel(manager, application, "当前模型：测试模型")
        {
            InputText = "检查代码",
        };

        viewModel.SendCommand.Execute(null);
        await runner.Completed.Task;

        Assert.AreEqual(string.Empty, viewModel.InputText);
        Assert.AreEqual(1, runner.RunCount);
    }

    [TestMethod(DisplayName = "仅附加图片时发送命令应可用并清空附件")]
    [Timeout(5000)]
    public async Task ImageOnlyMessageShouldSendAndClearAttachments()
    {
        var manager = new CopilotChatManager();
        var runner = new ImmediateRunner(manager);
        var application = new CodingChatApplication(manager, new EmptySessionStore(), runner);
        await application.InitializeAsync();
        using var viewModel = new ChatViewModel(manager, application, "当前模型：测试模型");
        Assert.IsTrue(viewModel.TryAddImageAttachment("sample.png", new byte[] { 1, 2, 3 }));

        viewModel.SendCommand.Execute(null);
        await runner.Completed.Task;

        Assert.IsTrue(viewModel.SendCommand.CanExecute(null) is false);
        Assert.IsEmpty(viewModel.PendingImages);
        Assert.HasCount(1, runner.ObservedContents!);
        DataContent imageContent = Assert.IsInstanceOfType<DataContent>(runner.ObservedContents[0]);
        Assert.AreEqual("image/png", imageContent.MediaType);
    }

    [TestMethod(DisplayName = "不支持扩展名的图片不应加入附件集合")]
    [Timeout(5000)]
    public void UnsupportedImageExtensionShouldNotBeAdded()
    {
        using var viewModel = new ChatViewModel();

        bool added = viewModel.TryAddImageAttachment("sample.svg", new byte[] { 1, 2, 3 });

        Assert.AreEqual((false, 0), (added, viewModel.PendingImages.Count));
    }

    [TestMethod(DisplayName = "发送失败应在聊天列表中显示系统消息")]
    [Timeout(5000)]
    public async Task SendFailureShouldAppendSystemMessage()
    {
        var manager = new CopilotChatManager();
        var runner = new FailingRunner(new InvalidOperationException("模型失败"));
        var application = new CodingChatApplication(manager, new EmptySessionStore(), runner);
        await application.InitializeAsync();
        using var viewModel = new ChatViewModel(manager, application, "当前模型：测试模型")
        {
            InputText = "触发异常",
        };

        viewModel.SendCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.Messages[^1].Content == "运行失败：模型失败");

        MessageItemViewModel failureMessage = viewModel.Messages[^1];
        Assert.AreEqual(
            (true, true, "运行失败：模型失败"),
            (failureMessage.IsSystemMessage, failureMessage.Message.IsPresetInfo, failureMessage.Content));
    }

    [TestMethod(DisplayName = "活动运行时停止命令应可用并触发取消")]
    [Timeout(5000)]
    public async Task StopCommandShouldCancelActiveRun()
    {
        var manager = new CopilotChatManager();
        var runner = new CancelableRunner(manager);
        var application = new CodingChatApplication(manager, new EmptySessionStore(), runner);
        await application.InitializeAsync();
        using var viewModel = new ChatViewModel(manager, application, "当前模型：测试模型")
        {
            InputText = "长任务",
        };
        viewModel.SendCommand.Execute(null);
        await runner.Started.Task;

        Assert.IsTrue(viewModel.IsRunning);
        Assert.IsTrue(viewModel.StopCommand.CanExecute(null));
        viewModel.StopCommand.Execute(null);
        await runner.Canceled.Task;
        await WaitUntilAsync(() => viewModel.Messages[^1].Content == "运行已停止。");

        Assert.IsTrue(runner.CancellationToken.IsCancellationRequested);
        Assert.IsTrue(viewModel.Messages[^1].IsSystemMessage);
    }

    [TestMethod(DisplayName = "应用工作路径命令应提交输入并刷新状态")]
    [Timeout(5000)]
    public async Task ApplyWorkspaceCommandShouldCommitInputAndRefreshStatus()
    {
        string workspacePath = CreateTestDirectory();
        var manager = new CopilotChatManager();
        var application = new CodingChatApplication(manager, new EmptySessionStore());
        await application.InitializeAsync();
        var workspaceController = new CodingWorkspaceController(
            new TestWorkspaceRuntime(),
            new ImmediateMainThreadDispatcher());
        using var viewModel = new ChatViewModel(
            manager,
            application,
            workspaceController,
            "当前模型：测试模型")
        {
            WorkspaceInput = workspacePath,
        };

        viewModel.ApplyWorkspaceCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.CommittedWorkspacePath is not null);

        Assert.AreEqual(Path.GetFullPath(workspacePath), viewModel.CommittedWorkspacePath);
        StringAssert.Contains(viewModel.WorkspaceStatusText, "已设置");
        Assert.IsTrue(viewModel.CanApplyWorkspace);
    }

    [TestMethod(DisplayName = "工作路径失败应在聊天列表中显示系统消息")]
    [Timeout(5000)]
    public async Task WorkspaceFailureShouldAppendSystemMessage()
    {
        string missingPath = Path.Join(Path.GetTempPath(), $"CodingChatRoom.MissingWorkspace.{Guid.NewGuid():N}");
        var manager = new CopilotChatManager();
        var application = new CodingChatApplication(manager, new EmptySessionStore());
        await application.InitializeAsync();
        var workspaceController = new CodingWorkspaceController(
            new TestWorkspaceRuntime(),
            new ImmediateMainThreadDispatcher());
        using var viewModel = new ChatViewModel(
            manager,
            application,
            workspaceController,
            "当前模型：测试模型")
        {
            WorkspaceInput = missingPath,
        };

        viewModel.ApplyWorkspaceCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.Messages[^1].Content.StartsWith("工作路径切换失败：", StringComparison.Ordinal));

        MessageItemViewModel failureMessage = viewModel.Messages[^1];
        Assert.AreEqual(
            (true, true),
            (failureMessage.IsSystemMessage, failureMessage.Message.IsPresetInfo));
    }

    private sealed class TestDataTemplate : IDataTemplate
    {
        public Control? Build(object? param) => new Border();

        public bool Match(object? data) => true;
    }

    private static string CreateTestDirectory()
    {
        string path = Path.Join(Path.GetTempPath(), $"CodingChatRoom.ViewModelWorkspace.{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        while (!condition())
        {
            await Task.Delay(10, cancellationTokenSource.Token);
        }
    }

    private sealed class TestWorkspaceRuntime : ICodingWorkspaceRuntime
    {
        public Task<IWorkspaceChangeTransaction> PrepareWorkspaceChangeAsync(
            string? workspacePath,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IWorkspaceChangeTransaction>(new TestWorkspaceTransaction(workspacePath));
        }
    }

    private sealed class TestWorkspaceTransaction(string? workspacePath) : IWorkspaceChangeTransaction
    {
        public string? WorkspacePath { get; } = workspacePath;

        public void Apply()
        {
        }

        public ValueTask RollbackAsync() => default;

        public void CommitAfterPublish()
        {
        }

        public ValueTask DisposeAsync() => default;
    }

    private sealed class ImmediateMainThreadDispatcher : IMainThreadDispatcher
    {
        public Task InvokeAsync(Func<Task> action) => action();

        public Task<T> InvokeAsync<T>(Func<Task<T>> action) => action();

        public bool CheckAccess() => true;
    }

    private sealed class ImmediateRunner(CopilotChatManager manager) : ICodingChatRunner
    {
        public TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int RunCount { get; private set; }

        public IReadOnlyList<AIContent>? ObservedContents { get; private set; }

        public async Task<CodingAgentRunResult> RunAsync(
            IReadOnlyList<AIContent> contents,
            string? workspacePath,
            CancellationToken cancellationToken)
        {
            RunCount++;
            ObservedContents = contents;
            await manager.AppendMessageAsync(CopilotChatMessage.CreateUser(contents), cancellationToken);
            var assistantMessage = CopilotChatMessage.CreateAssistant("完成", isPresetInfo: false);
            await manager.SelectedSession.AddMessageAsync(assistantMessage);
            Completed.TrySetResult();
            return new CodingAgentRunResult(assistantMessage, Task.FromResult<string?>("完成"));
        }
    }

    private sealed class FailingRunner(Exception exception) : ICodingChatRunner
    {
        public Task<CodingAgentRunResult> RunAsync(
            IReadOnlyList<AIContent> contents,
            string? workspacePath,
            CancellationToken cancellationToken) =>
            Task.FromException<CodingAgentRunResult>(exception);
    }

    private sealed class CancelableRunner(CopilotChatManager manager) : ICodingChatRunner
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Canceled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public CancellationToken CancellationToken { get; private set; }

        public async Task<CodingAgentRunResult> RunAsync(
            IReadOnlyList<AIContent> contents,
            string? workspacePath,
            CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
            await manager.AppendMessageAsync(CopilotChatMessage.CreateUser(contents), cancellationToken);
            var assistantMessage = CopilotChatMessage.CreateAssistant(CopilotChatMessage.PlaceholderContent, isPresetInfo: false);
            await manager.SelectedSession.AddMessageAsync(assistantMessage);
            Started.TrySetResult();
            return new CodingAgentRunResult(assistantMessage, WaitForCancellationAsync(cancellationToken));
        }

        private async Task<string?> WaitForCancellationAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return null;
            }
            finally
            {
                Canceled.TrySetResult();
            }
        }
    }

    private sealed class EmptySessionStore : ICodingChatSessionStore
    {
        public Task<IReadOnlyList<CopilotChatSessionSummary>> ListSessionsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CopilotChatSessionSummary>>([]);

        public Task<CopilotChatSession> LoadSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task SaveSessionAsync(CopilotChatSession session, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
