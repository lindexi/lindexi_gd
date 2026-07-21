using AgentLib.Model;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentLib.Coding;

/// <summary>
/// 使用固定编程工作流和工作区工具运行代码任务。
/// </summary>
public sealed class CodingAgent : IAsyncDisposable
{
    private const string SystemPrompt = """
        你是一个自动化编程代理，负责完成用户明确提出的软件开发任务。

        工作规则：
        - 只处理用户要求的范围，不主动修改无关代码，也不把建议误报为已完成修改。
        - 开始工作前先了解解决方案、项目和相关文件结构，不猜测项目或文件路径。
        - 查询代码符号、定义、实现和引用时优先使用代码理解工具；修改文件前必须读取相关内容。
        - 读取并遵守仓库中的编码指令、项目约定和现有代码风格。
        - 使用最小修改完成目标，优先复用已有库、方法和项目模式。
        - 不编辑生成目录或生成文件，不覆盖工作区外文件，不通过禁用检查或删除测试隐藏问题。
        - 修改后运行相关构建或测试；失败时读取相关日志并区分本次引入的问题与已有问题。
        - 无法验证时明确说明阻塞原因和未验证范围。
        - 最终汇报修改文件、主要行为变化、实际执行的构建测试结果以及剩余风险。
        """;

    private readonly CodingWorkspaceToolProvider _toolProvider;
    private readonly SemaphoreSlim _startupLock = new(1, 1);
    private readonly object _lifecycleLock = new();
    private readonly CancellationTokenSource _disposeCancellationTokenSource = new();
    private TaskCompletionSource? _runCompletion;
    private Task? _disposeTask;
    private bool _hasActiveRun;
    private int _isDisposed;

    /// <summary>
    /// 创建编程代理。
    /// </summary>
    /// <param name="languageServerCommand">Roslyn Language Server 启动命令。</param>
    public CodingAgent(string languageServerCommand = "roslyn-language-server")
        : this(new CodingWorkspaceToolProvider(languageServerCommand))
    {
    }

    internal CodingAgent(CodingWorkspaceToolProvider toolProvider)
    {
        ArgumentNullException.ThrowIfNull(toolProvider);
        _toolProvider = toolProvider;
    }

    /// <summary>
    /// 获取当前已提交的代码工作区路径。
    /// </summary>
    public string? WorkspacePath => _toolProvider.WorkspacePath;

    /// <summary>
    /// 使用纯文本运行一次编程任务。
    /// </summary>
    /// <param name="context">现有手动发送上下文。</param>
    /// <param name="prompt">用户任务文本。</param>
    /// <param name="workspacePath">本次运行期望使用的工作区路径。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>流式消息和完整生命周期任务。</returns>
    public Task<CodingAgentRunResult> RunAsync(
        IManualSendMessageContext context,
        string prompt,
        string? workspacePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("编程任务文本不能为空。", nameof(prompt));
        }

        return RunAsync(context, [new TextContent(prompt)], workspacePath, cancellationToken);
    }

    /// <summary>
    /// 使用有序多模态内容运行一次编程任务。
    /// </summary>
    /// <param name="context">现有手动发送上下文。</param>
    /// <param name="contents">保持原始顺序的用户输入内容。</param>
    /// <param name="workspacePath">本次运行期望使用的工作区路径。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>流式消息和完整生命周期任务。</returns>
    public async Task<CodingAgentRunResult> RunAsync(
        IManualSendMessageContext context,
        IReadOnlyList<AIContent> contents,
        string? workspacePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(contents);
        AIContent[] runContents = [.. contents];
        if (runContents.Length == 0)
        {
            throw new ArgumentException("编程任务内容不能为空。", nameof(contents));
        }

        EnterRun();
        CodingWorkspaceToolLease? lease = null;
        CancellationTokenSource? runCancellationTokenSource = null;
        bool ownershipTransferred = false;
        try
        {
            runCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                _disposeCancellationTokenSource.Token);
            CancellationToken runCancellationToken = runCancellationTokenSource.Token;
            await _startupLock.WaitAsync(runCancellationToken).ConfigureAwait(false);
            try
            {
                ThrowIfDisposed();
                if (!AreSameWorkspace(_toolProvider.WorkspacePath, workspacePath))
                {
                    await _toolProvider.SetWorkspacePathAsync(workspacePath, runCancellationToken).ConfigureAwait(false);
                }

                lease = await _toolProvider.AcquireLeaseAsync(runCancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _startupLock.Release();
            }

            Task<string?> completionTask = RunCoreAsync(
                context,
                runContents,
                lease,
                runCancellationTokenSource);
            ownershipTransferred = true;
            return new CodingAgentRunResult(context.AssistantChatMessage, completionTask);
        }
        finally
        {
            if (!ownershipTransferred)
            {
                runCancellationTokenSource?.Dispose();
                if (lease is not null)
                {
                    await lease.DisposeAsync().ConfigureAwait(false);
                }

                ExitRun();
            }
        }
    }

    internal async Task<CodingWorkspaceToolCandidate> CreateWorkspaceCandidateAsync(
        string? workspacePath,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _disposeCancellationTokenSource.Token);
        CodingWorkspaceToolCandidate candidate = await _toolProvider
            .CreateCandidateAsync(workspacePath, linkedCancellationTokenSource.Token)
            .ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            return candidate;
        }
        catch
        {
            await candidate.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    internal Task PublishWorkspaceCandidateAsync(
        CodingWorkspaceToolCandidate candidate,
        Action? commit,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        return _toolProvider.PublishCandidateAsync(candidate, commit, cancellationToken);
    }

    /// <summary>
    /// 异步取消活动运行，并在它们完成清理后释放工作区资源。
    /// </summary>
    public ValueTask DisposeAsync()
    {
        Task disposeTask;
        Task activeRunTask;
        TaskCompletionSource disposeCompletion;
        lock (_lifecycleLock)
        {
            if (_disposeTask is not null)
            {
                return new ValueTask(_disposeTask);
            }

            Volatile.Write(ref _isDisposed, 1);
            activeRunTask = _hasActiveRun
                ? (_runCompletion ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)).Task
                : Task.CompletedTask;
            disposeCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _disposeTask = disposeCompletion.Task;
            disposeTask = _disposeTask;
        }

        _ = DisposeCoreAsync(activeRunTask, disposeCompletion);
        return new ValueTask(disposeTask);
    }

    private async Task DisposeCoreAsync(Task activeRunTask, TaskCompletionSource completion)
    {
        try
        {
            _disposeCancellationTokenSource.Cancel();
            await activeRunTask.ConfigureAwait(false);
            await _toolProvider.DisposeAsync().ConfigureAwait(false);
            completion.TrySetResult();
        }
        catch (Exception ex)
        {
            completion.TrySetException(ex);
        }
        finally
        {
            _startupLock.Dispose();
            _disposeCancellationTokenSource.Dispose();
        }
    }

    private static void CopyContents(CopilotChatMessage userChatMessage, IReadOnlyList<AIContent> contents)
    {
        userChatMessage.ClearMessageItems();
        foreach (AIContent content in contents)
        {
            switch (content)
            {
                case TextContent textContent:
                    userChatMessage.AppendText(textContent.Text);
                    break;
                case DataContent dataContent when dataContent.Data.Length > 0:
                    if (dataContent.MediaType?.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        userChatMessage.MessageItems.Add(new CopilotChatAudioItem(
                            BinaryData.FromBytes(dataContent.Data),
                            dataContent.MediaType));
                    }
                    else
                    {
                        userChatMessage.MessageItems.Add(new CopilotChatImageItem(
                            BinaryData.FromBytes(dataContent.Data),
                            dataContent.MediaType ?? "application/octet-stream"));
                    }
                    break;
            }
        }
    }

    private async Task<string?> RunCoreAsync(
        IManualSendMessageContext context,
        IReadOnlyList<AIContent> contents,
        CodingWorkspaceToolLease lease,
        CancellationTokenSource runCancellationTokenSource)
    {
        bool hasResponseUpdate = false;
        try
        {
            await Task.Yield();
            CancellationToken cancellationToken = runCancellationTokenSource.Token;
            CopilotChatMessage userChatMessage = context.UserChatMessage;
            CopyContents(userChatMessage, contents);
            await context.AppendMessagesToSessionAsync().ConfigureAwait(false);
            using IDisposable chatting = context.StartChatting();
            ChatClientAgent chatClientAgent = await context.GetChatClientAgentAsync(options =>
            {
                options.ChatOptions ??= new ChatOptions();
                options.ChatOptions.Tools = [.. lease.Tools];
                options.AIContextProviders = [];
            }, cancellationToken).ConfigureAwait(false);
            AgentSession agentSession = await context.GetAgentSessionAsync(cancellationToken).ConfigureAwait(false);
            ChatMessage[] inputMessages =
            [
                new ChatMessage(ChatRole.System, SystemPrompt),
                new ChatMessage(ChatRole.User, new List<AIContent>(contents)),
            ];

            await foreach (AgentResponseUpdate update in chatClientAgent.RunWithHistoryCompletionAsync(
                inputMessages,
                agentSession,
                cancellationToken).ConfigureAwait(false))
            {
                await AppendResponseUpdateAsync(context, update).ConfigureAwait(false);
                hasResponseUpdate = true;
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (!hasResponseUpdate)
            {
                await ClearAssistantPlaceholderAsync(context).ConfigureAwait(false);
            }

            string content = context.AssistantChatMessage.Content;
            return string.IsNullOrWhiteSpace(content) ? null : content;
        }
        finally
        {
            try
            {
                if (!hasResponseUpdate)
                {
                    await ClearAssistantPlaceholderAsync(context).ConfigureAwait(false);
                }
            }
            finally
            {
                try
                {
                    await lease.DisposeAsync().ConfigureAwait(false);
                }
                finally
                {
                    runCancellationTokenSource.Dispose();
                    ExitRun();
                }
            }
        }
    }

    private static async Task AppendResponseUpdateAsync(
        IManualSendMessageContext context,
        AgentResponseUpdate update)
    {
        IMainThreadDispatcher? dispatcher = context.MainThreadDispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            context.AppendResponseUpdate(update);
            return;
        }

        await dispatcher.InvokeAsync(() =>
        {
            context.AppendResponseUpdate(update);
            return Task.CompletedTask;
        }).ConfigureAwait(false);
    }

    private static async Task ClearAssistantPlaceholderAsync(IManualSendMessageContext context)
    {
        if (context.AssistantChatMessage.Content != CopilotChatMessage.PlaceholderContent)
        {
            return;
        }

        IMainThreadDispatcher? dispatcher = context.MainThreadDispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            context.AssistantChatMessage.ClearMessageItems();
            return;
        }

        await dispatcher.InvokeAsync(() =>
        {
            context.AssistantChatMessage.ClearMessageItems();
            return Task.CompletedTask;
        }).ConfigureAwait(false);
    }

    private void EnterRun()
    {
        lock (_lifecycleLock)
        {
            ThrowIfDisposed();
            if (_hasActiveRun)
            {
                throw new InvalidOperationException("同一个 CodingAgent 不能同时运行多个任务。");
            }

            _hasActiveRun = true;
        }
    }

    private void ExitRun()
    {
        TaskCompletionSource? runCompletion = null;
        lock (_lifecycleLock)
        {
            _hasActiveRun = false;
            if (Volatile.Read(ref _isDisposed) != 0)
            {
                runCompletion = _runCompletion;
            }
        }

        runCompletion?.TrySetResult();
    }

    private static bool AreSameWorkspace(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) && string.IsNullOrWhiteSpace(right))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        return string.Equals(
            Path.GetFullPath(left),
            Path.GetFullPath(right),
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _isDisposed) != 0)
        {
            throw new ObjectDisposedException(nameof(CodingAgent));
        }
    }
}
