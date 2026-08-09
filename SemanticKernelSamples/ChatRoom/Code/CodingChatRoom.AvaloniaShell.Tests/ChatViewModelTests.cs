using System.Collections.Generic;
using System.ComponentModel;

using AgentLib;
using AgentLib.Coding;
using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Logging;
using AgentLib.Model;

using Avalonia.Controls;
using Avalonia.Controls.Templates;

using CodingChatRoom.AvaloniaShell.ViewModels;
using CodingChatRoom.AvaloniaShell.Views;
using CodingChatRoom.AvaloniaShell.Services;

using Microsoft.Agents.AI;
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
        var application = new CodingChatApplication(manager, new EmptySessionStore());
        await application.InitializeAsync();
        CopilotChatSession previousSession = manager.SelectedSession;
        await previousSession.AddMessageAsync(new CopilotChatMessage(ChatRole.User, "旧会话问题"));
        using var viewModel = new ChatViewModel(manager, application, "当前模型：测试模型");

        manager.CreateNewSession();
        int currentMessageCount = viewModel.Messages.Count;
        await previousSession.AddMessageAsync(new CopilotChatMessage(ChatRole.Assistant, "旧会话迟到更新"));

        Assert.AreEqual(manager.SelectedSession.SessionId, viewModel.CurrentSessionId);
        Assert.HasCount(currentMessageCount, viewModel.Messages);
    }

    [TestMethod(DisplayName = "审批入口应复用聊天管理器完成决策")]
    [Timeout(5000)]
    public void ApprovalActionsShouldDelegateToChatManager()
    {
        var manager = new CopilotChatManager();
        var application = new CodingChatApplication(manager, new EmptySessionStore());
        using var viewModel = new ChatViewModel(manager, application, "当前模型：测试模型");
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

    [TestMethod(DisplayName = "运行期间插话应显示提交反馈并保持发送入口可用")]
    [Timeout(5000)]
    public async Task InterruptionShouldShowSubmissionFeedbackAndKeepSendAvailable()
    {
        var manager = new CopilotChatManager();
        var runner = new CancelableRunner(manager);
        var application = new CodingChatApplication(manager, new EmptySessionStore(), runner);
        await application.InitializeAsync();
        using var viewModel = new ChatViewModel(manager, application, "当前模型：测试模型")
        {
            InputText = "开始长任务",
        };
        viewModel.SendCommand.Execute(null);
        await runner.Started.Task;
        viewModel.InputText = "改为优先修复测试";

        Assert.AreEqual("插话", viewModel.SendButtonText);
        Assert.IsTrue(viewModel.SendCommand.CanExecute(null));
        viewModel.SendCommand.Execute(null);
        await runner.Injected.Task;
        await WaitUntilAsync(() => viewModel.StatusText == "插话已提交，等待 Agent 处理");

        Assert.AreEqual(string.Empty, viewModel.InputText);
        Assert.AreEqual("改为优先修复测试", runner.InjectedText);
        viewModel.StopCommand.Execute(null);
        await runner.Canceled.Task;
    }

    [TestMethod(DisplayName = "循环迭代发送应取消勾选并可由停止命令结束")]
    [Timeout(5000)]
    public async Task LoopIterationShouldClearOptionAndStopActiveRun()
    {
        var manager = new CopilotChatManager();
        var runner = new CancelableRunner(manager);
        var application = new CodingChatApplication(manager, new EmptySessionStore(), runner);
        await application.InitializeAsync();
        using var viewModel = new ChatViewModel(manager, application, "当前模型：测试模型")
        {
            InputText = "继续处理交接文档",
            IsLoopIterationEnabled = true,
        };

        viewModel.SendCommand.Execute(null);
        await runner.Started.Task;

        Assert.IsFalse(viewModel.IsLoopIterationEnabled);
        viewModel.StopCommand.Execute(null);
        await runner.Canceled.Task;
        await WaitUntilAsync(() => !viewModel.IsRunning);

        Assert.AreEqual("继续处理交接文档", Assert.IsInstanceOfType<TextContent>(runner.ObservedContents![0]).Text);
    }

    [TestMethod(DisplayName = "循环迭代模式应要求输入固定文本")]
    [Timeout(5000)]
    public void LoopIterationShouldRequireTextPrompt()
    {
        var manager = new CopilotChatManager();
        var application = new CodingChatApplication(manager, new EmptySessionStore(), new ImmediateRunner(manager));
        using var viewModel = new ChatViewModel(manager, application, "当前模型：测试模型");
        Assert.IsTrue(viewModel.TryAddImageAttachment("sample.png", new byte[] { 1, 2, 3 }));

        viewModel.IsLoopIterationEnabled = true;

        Assert.IsFalse(viewModel.SendCommand.CanExecute(null));
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

        Assert.IsFalse(viewModel.SendCommand.CanExecute(null));
        Assert.IsEmpty(viewModel.PendingImages);
        IReadOnlyList<AIContent> observedContents = runner.ObservedContents!;
        Assert.HasCount(1, observedContents);
        DataContent imageContent = Assert.IsInstanceOfType<DataContent>(observedContents[0]);
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

    [TestMethod(DisplayName = "后台创建代理会话时应由完整应用操作刷新命令状态")]
    [Timeout(5000)]
    public async Task BackgroundAgentSessionChangeShouldWaitForApplicationStateNotification()
    {
        var chatClient = new FakeChatClient();
        CopilotChatManager manager = CreateChatManager(chatClient);
        IManualSendMessageContext context = await manager.CreateManualSendMessageContextAsync();
        AgentSession agentSession = await context.GetAgentSessionAsync();
        manager.SelectedSession.SetAgentSession(null);
        var runner = new ImmediateRunner(manager);
        var application = new CodingChatApplication(manager, new EmptySessionStore(), runner);
        await application.InitializeAsync();
        using var viewModel = new ChatViewModel(manager, application, "当前模型：测试模型")
        {
            InputText = "刷新命令状态",
        };
        int notificationCount = 0;
        viewModel.CompressConversationCommand.CanExecuteChanged += (_, _) => notificationCount++;

        await Task.Run(() => manager.SelectedSession.SetAgentSession(agentSession));
        int countAfterAgentSessionChanged = notificationCount;
        viewModel.SendCommand.Execute(null);
        await runner.Completed.Task;
        await WaitUntilAsync(() => viewModel.CompressConversationCommand.CanExecute(null));

        Assert.AreEqual(
            (0, true, true),
            (countAfterAgentSessionChanged, notificationCount > 0, viewModel.CompressConversationCommand.CanExecute(null)));
    }

    [TestMethod(DisplayName = "压缩命令应压缩当前对话并显示完成消息")]
    [Timeout(5000)]
    public async Task CompressConversationCommandShouldReduceCurrentHistoryAndShowCompletionMessage()
    {
        const string summaryText = "压缩后的编程对话摘要";
        var chatClient = new FakeChatClient
        {
            OnGetResponseAsync = (_, _, _) =>
                Task.FromResult(new ChatResponse([new ChatMessage(ChatRole.Assistant, summaryText)])),
        };
        CopilotChatManager manager = CreateChatManager(chatClient);
        IManualSendMessageContext context = await manager.CreateManualSendMessageContextAsync();
        AgentSession agentSession = await context.GetAgentSessionAsync();
        agentSession.SetInMemoryChatHistory(
        [
            new ChatMessage(ChatRole.System, "系统提示"),
            new ChatMessage(ChatRole.User, "用户问题"),
            new ChatMessage(ChatRole.Assistant, "助手回答"),
        ]);
        var application = new CodingChatApplication(manager, new EmptySessionStore());
        await application.InitializeAsync();
        using var viewModel = new ChatViewModel(manager, application, "当前模型：测试模型");

        Assert.IsTrue(viewModel.CompressConversationCommand.CanExecute(null));
        viewModel.CompressConversationCommand.Execute(null);
        await WaitUntilAsync(() => viewModel.Messages[^1].Content == "对话压缩完成。");

        Assert.IsTrue(agentSession.TryGetInMemoryChatHistory(out List<ChatMessage>? compressedMessages));
        Assert.IsTrue(compressedMessages.Any(message => message.Text.Contains(summaryText, StringComparison.Ordinal)));
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

    private static CopilotChatManager CreateChatManager(FakeChatClient chatClient)
    {
        var manager = new CopilotChatManager();
        var model = new FakeLanguageModel(chatClient)
        {
            ModelDefinition = new ModelDefinition
            {
                Provider = "fake",
                ModelId = "fake",
                ModelName = "Fake",
            },
        };
        manager.AgentApiEndpointManager.RegisterLanguageModelProvider(new FakeLanguageModelProvider([model]));
        return manager;
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

        public TaskCompletionSource Injected { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public CancellationToken CancellationToken { get; private set; }

        public IReadOnlyList<AIContent>? ObservedContents { get; private set; }

        public string? InjectedText { get; private set; }

        public async Task<CodingAgentRunResult> RunAsync(
            IReadOnlyList<AIContent> contents,
            string? workspacePath,
            CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
            ObservedContents = contents;
            await manager.AppendMessageAsync(CopilotChatMessage.CreateUser(contents), cancellationToken);
            var assistantMessage = CopilotChatMessage.CreateAssistant(CopilotChatMessage.PlaceholderContent, isPresetInfo: false);
            await manager.SelectedSession.AddMessageAsync(assistantMessage);
            Started.TrySetResult();
            return new CodingAgentRunResult(assistantMessage, WaitForCancellationAsync(cancellationToken));
        }

        public Task InjectMessageAsync(
            IReadOnlyList<AIContent> contents,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            InjectedText = Assert.IsInstanceOfType<TextContent>(contents[0]).Text;
            Injected.TrySetResult();
            return Task.CompletedTask;
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
