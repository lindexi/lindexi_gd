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
    private readonly object _disposeSync = new();
    private readonly CancellationTokenSource _disposeCancellationTokenSource = new();
    private Task? _disposeTask;
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

        CodingWorkspaceToolLease? lease = null;
        CancellationTokenSource? runCancellationTokenSource = null;
        bool ownershipTransferred = false;
        try
        {
            ThrowIfDisposed();
            runCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                _disposeCancellationTokenSource.Token);
            CancellationToken runCancellationToken = runCancellationTokenSource.Token;
            if (!AreSameWorkspace(_toolProvider.WorkspacePath, workspacePath))
            {
                await _toolProvider.SetWorkspacePathAsync(workspacePath, runCancellationToken).ConfigureAwait(false);
            }

            lease = await _toolProvider.AcquireLeaseAsync(runCancellationToken).ConfigureAwait(false);
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
            }
        }
    }

    /// <summary>
    /// 准备一次工作区切换事务。准备阶段不会改变当前已提交工作区。
    /// </summary>
    /// <param name="workspacePath">候选工作区路径；为空表示清除工作区。</param>
    /// <param name="cancellationToken">取消令牌，仅影响候选资源准备。</param>
    /// <returns>必须提交、回滚或释放的工作区事务。</returns>
    public async Task<IWorkspaceChangeTransaction> PrepareWorkspaceChangeAsync(
        string? workspacePath,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _disposeCancellationTokenSource.Token);
        IWorkspaceChangeTransaction transaction = await _toolProvider
            .PrepareWorkspaceChangeAsync(workspacePath, linkedCancellationTokenSource.Token)
            .ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            return transaction;
        }
        catch
        {
            await transaction.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// 异步取消活动运行，并在它们完成清理后释放工作区资源。
    /// </summary>
    public ValueTask DisposeAsync()
    {
        lock (_disposeSync)
        {
            _disposeTask ??= DisposeCoreAsync();
            return new ValueTask(_disposeTask);
        }
    }

    private async Task DisposeCoreAsync()
    {
        Volatile.Write(ref _isDisposed, 1);
        try
        {
            _disposeCancellationTokenSource.Cancel();
            await _toolProvider.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            _disposeCancellationTokenSource.Dispose();
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
            userChatMessage.ClearMessageItems();
            foreach (TextContent textContent in contents.OfType<TextContent>())
            {
                userChatMessage.AppendText(textContent.Text);
            }

            await context.AppendMessagesToSessionAsync().ConfigureAwait(false);
            using IDisposable chatting = context.StartChatting();
            ChatClientAgent chatClientAgent = await context.GetChatClientAgentAsync(options =>
            {
                options.ChatOptions ??= new ChatOptions();
                options.ChatOptions.Tools = [.. lease.Tools];
                options.AIContextProviders = [];
            }, cancellationToken).ConfigureAwait(false);
            AgentSession agentSession = await context.GetAgentSessionAsync(cancellationToken).ConfigureAwait(false);
            EnsureSystemPromptInSession(agentSession);
            ChatMessage[] inputMessages =
            [
                new ChatMessage(ChatRole.User, new List<AIContent>(contents)),
            ];

            await foreach (AgentResponseUpdate update in chatClientAgent.RunWithHistoryCompletionAsync(
                inputMessages,
                agentSession,
                cancellationToken).ConfigureAwait(false))
            {
                context.AppendResponseUpdate(update);
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
                }
            }
        }
    }

    private static void EnsureSystemPromptInSession(AgentSession agentSession)
    {
        if (agentSession.TryGetInMemoryChatHistory(out List<ChatMessage>? messages)
            && messages.Any(message =>
                message.Role == ChatRole.System
                && string.Equals(message.Text, SystemPrompt, StringComparison.Ordinal)))
        {
            return;
        }

        var initializedMessages = new List<ChatMessage>((messages?.Count ?? 0) + 1)
        {
            new(ChatRole.System, SystemPrompt),
        };
        if (messages is not null)
        {
            initializedMessages.AddRange(messages);
        }

        agentSession.SetInMemoryChatHistory(initializedMessages);
    }

    private static Task ClearAssistantPlaceholderAsync(IManualSendMessageContext context)
    {
        if (context.AssistantChatMessage.Content != CopilotChatMessage.PlaceholderContent)
        {
            return Task.CompletedTask;
        }

        context.AssistantChatMessage.ClearMessageItems();
        return Task.CompletedTask;
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
