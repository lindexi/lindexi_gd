using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using AgentLib.Logging;
using AgentLib.Model;
using AgentLib.Tools;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable MAAI001

namespace AgentLib;

/// <summary>
/// 管理 Copilot 聊天会话的核心类，负责消息发送、会话管理、工具注册和流式响应处理。
/// </summary>
public class CopilotChatManager : NotifyBase
{
    private bool _isChatting;
    private bool _wasLastChatCanceled;
    private ICopilotChatLogger _chatLogger = null!;
    private CopilotChatSession _selectedSession = null!;
    private CancellationTokenSource? _currentChatCancellationTokenSource;
    private readonly CopilotToolManager _toolManager;
    private readonly SessionTitleGenerator _titleGenerator;

    /// <summary>
    /// 使用空日志记录器创建管理器。
    /// </summary>
    public CopilotChatManager()
        : this(new EmptyCopilotChatLogger())
    {
        // 无参构造，让 XAML 系开森
    }

    /// <summary>
    /// 使用指定的聊天日志记录器创建管理器。
    /// </summary>
    /// <param name="chatLogger">聊天日志记录器。</param>
    public CopilotChatManager(ICopilotChatLogger chatLogger)
    {
        ChatLogger = chatLogger;
        _toolManager = new CopilotToolManager(this.AgentApiEndpointManager);
        _titleGenerator = new SessionTitleGenerator(AgentApiEndpointManager);
        CreateNewSession();
    }

    /// <summary>
    /// API 终结点管理器，管理语言模型提供商和模型选择。
    /// </summary>
    public AgentApiEndpointManager AgentApiEndpointManager { get; init; } = new();

    /// <summary>
    /// 所有聊天会话的集合。
    /// </summary>
    public ObservableCollection<CopilotChatSession> ChatSessions { get; } = [];

    /// <summary>
    /// 当前选中会话的聊天消息列表。
    /// </summary>
    public ObservableCollection<CopilotChatMessage> ChatMessages => SelectedSession.ChatMessages;

    /// <summary>
    /// 聊天日志记录器。
    /// </summary>
    public ICopilotChatLogger ChatLogger
    {
        get => _chatLogger;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _chatLogger = value;
        }
    }

    /// <summary>
    /// 是否正在聊天（流式响应进行中）。
    /// </summary>
    public bool IsChatting
    {
        get => _isChatting;
        private set
        {
            if (!SetField(ref _isChatting, value))
            {
                return;
            }

            OnPropertyChanged(nameof(CanEditInput));
            OnPropertyChanged(nameof(SendButtonText));
        }
    }

    /// <summary>
    /// 是否可以编辑输入（不在聊天中时才可以编辑）。
    /// </summary>
    public bool CanEditInput => !IsChatting;

    /// <summary>
    /// 上一次聊天是否被取消。
    /// </summary>
    public bool WasLastChatCanceled
    {
        get => _wasLastChatCanceled;
        private set => SetField(ref _wasLastChatCanceled, value);
    }

    /// <summary>
    /// 发送按钮的显示文本（聊天中为"停止"，否则为"发送"）。
    /// </summary>
    public string SendButtonText => IsChatting ? "停止" : "发送";

    /// <summary>
    /// 当前选中的会话。
    /// </summary>
    public CopilotChatSession SelectedSession
    {
        get => _selectedSession;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (!SetField(ref _selectedSession, value))
            {
                return;
            }

            OnPropertyChanged(nameof(ChatMessages));
            OnPropertyChanged(nameof(CurrentSessionId));
        }
    }

    /// <summary>
    /// 当前会话的唯一标识符。
    /// </summary>
    public Guid CurrentSessionId => SelectedSession.SessionId;

    /// <summary>
    /// 工作区路径。设置后将启用文件系统相关工具。
    /// </summary>
    public string? WorkspacePath
    {
        get => _toolManager.WorkspacePath;
        set
        {
            string? normalizedPath = string.IsNullOrWhiteSpace(value) ? null : value;

            if (string.Equals(_toolManager.PrimaryWorkspacePath, normalizedPath, StringComparison.Ordinal))
            {
                return;
            }

            _toolManager.WorkspacePath = normalizedPath;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 辅助工作区路径（当主工作区为 null 时作为回退）。
    /// </summary>
    public string? SecondaryWorkspacePath
    {
        get => _toolManager.SecondaryWorkspacePath;
        set
        {
            string? normalizedPath = string.IsNullOrWhiteSpace(value) ? null : value;

            if (string.Equals(_toolManager.SecondaryWorkspacePath, normalizedPath, StringComparison.Ordinal))
            {
                return;
            }

            _toolManager.SecondaryWorkspacePath = normalizedPath;
            OnPropertyChanged();
            OnPropertyChanged(nameof(WorkspacePath));
        }
    }

    /// <summary>
    /// AI 上下文提供者集合。设置后将在每次创建 <see cref="ChatClientAgent"/> 时注入到 <see cref="ChatClientAgentOptions.AIContextProviders"/>。
    /// 调用方可添加任意 <see cref="AIContextProvider"/> 实现，如 <c>AgentSkillsProvider</c> 等。
    /// 为 <see langword="null"/> 或空集合时，不会注入任何上下文提供者。
    /// </summary>
    public IReadOnlyList<AIContextProvider>? AIContextProviders { get; set; }

    /// <summary>
    /// 主线程调度器。设置后，所有新创建的 <see cref="CopilotChatSession"/> 都将携带此调度器，
    /// <see cref="CopilotChatSession.AddMessage"/> 将自动调度到主线程执行。为 <see langword="null"/> 时不做线程调度。
    /// 仅在构造期可设置。
    /// </summary>
    public IMainThreadDispatcher? MainThreadDispatcher { get; init; }

    /// <summary>
    /// 从指定技能文件夹加载技能并追加到 <see cref="AIContextProviders"/> 中。
    /// </summary>
    /// <param name="skillFolder">技能文件夹路径。</param>
    public void AddSkillFolder(DirectoryInfo skillFolder)
    {
        ArgumentNullException.ThrowIfNull(skillFolder);

        var skillsProvider = new AgentSkillsProvider(skillFolder.FullName);
        AIContextProviders = AIContextProviders is { Count: > 0 }
            ? new List<AIContextProvider>(AIContextProviders) { skillsProvider }
            : new List<AIContextProvider> { skillsProvider };
    }

    /// <summary>
    /// 创建新会话并切换为当前选中会话。
    /// </summary>
    public void CreateNewSession()
    {
        CopilotChatSession session = FindReusableEmptySession() ?? CreateSession();
        SelectedSession = session;
    }

    /// <summary>
    /// 从指定消息创建新会话。新会话包含从最早消息到 <paramref name="fromMessage"/>（含）之间的所有消息的深拷贝。
    /// 原会话保持不变，创建后自动切换到新会话。
    /// </summary>
    /// <param name="fromMessage">新会话的截止消息。此消息必须是 <see cref="SelectedSession"/> 中的一条消息。</param>
    /// <exception cref="ArgumentNullException"><paramref name="fromMessage"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="InvalidOperationException">在 <see cref="SelectedSession"/> 中未找到 <paramref name="fromMessage"/>。新会话不能为空。</exception>
    public void CreateSessionFromMessage(CopilotChatMessage fromMessage)
    {
        ArgumentNullException.ThrowIfNull(fromMessage);

        CopilotChatSession currentSession = SelectedSession;
        int index = currentSession.ChatMessages.IndexOf(fromMessage);
        if (index < 0)
        {
            throw new InvalidOperationException("在当前的会话中找不到所选的聊天消息。");
        }

        var newSession = new CopilotChatSession(Guid.NewGuid(), DateTimeOffset.Now)
        {
            MainThreadDispatcher = MainThreadDispatcher,
        };
        for (int i = 0; i <= index; i++)
        {
            newSession.AddMessage(currentSession.ChatMessages[i].Clone());
        }

        ChatSessions.Insert(0, newSession);
        OnSessionCreated(newSession);
        SelectedSession = newSession;
    }

    /// <summary>
    /// 设置聊天日志文件夹路径。
    /// </summary>
    /// <param name="chatLogFolder">日志文件夹路径。为空时使用默认路径。</param>
    public void SetChatLogFolder(string? chatLogFolder)
    {
        ChatLogger = string.IsNullOrWhiteSpace(chatLogFolder)
            ? new FileCopilotChatLogger()
            : new FileCopilotChatLogger(chatLogFolder);
    }

    /// <summary>
    /// 取消当前正在进行的聊天。
    /// </summary>
    public void CancelCurrentChat()
    {
        _currentChatCancellationTokenSource?.Cancel();
    }

    /// <summary>
    /// 同意指定审批工具继续执行。
    /// </summary>
    public void ApproveToolExecution(CopilotChatApprovalToolItem approvalToolItem)
    {
        ArgumentNullException.ThrowIfNull(approvalToolItem);
        approvalToolItem.Approve();
    }

    /// <summary>
    /// 拒绝指定审批工具继续执行。
    /// </summary>
    public void RejectToolExecution(CopilotChatApprovalToolItem approvalToolItem, string? reason = null)
    {
        ArgumentNullException.ThrowIfNull(approvalToolItem);
        approvalToolItem.Reject(reason);
    }

    public async Task AddConversationAsync(string userText, string assistantText,
        bool isPresetInfo = true, CancellationToken cancellationToken = default)
    {
        ArgumentHelper.ThrowIfNullOrWhiteSpace(userText);
        ArgumentNullException.ThrowIfNull(assistantText);

        CopilotChatSession currentSession = SelectedSession;

        var userChatMessage = CopilotChatMessage.CreateUser(userText);
        userChatMessage.IsPresetInfo = isPresetInfo;
        await AppendMessageAsync(currentSession, userChatMessage, cancellationToken);

        var assistantChatMessage = CopilotChatMessage.CreateAssistant(assistantText, isPresetInfo);
        await AppendMessageAsync(currentSession, assistantChatMessage, cancellationToken);
    }

    /// <summary>
    /// 开启新的会话并发送消息。新会话适用于需要清晰上下文的场景
    /// </summary>
    /// <param name="contents">用户输入的多模态内容集合。</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task SendMessageInNewSessionAsync(IReadOnlyList<AIContent> contents, CancellationToken cancellationToken = default)
    {
        return SendMessageAsync(contents, withHistory: true, createNewSession: true, tools: null, toolMode: null, cancellationToken);
    }

    /// <summary>
    /// 开启新的会话并发送纯文本消息。新会话适用于需要清晰上下文的场景。
    /// </summary>
    /// <param name="inputText">用户输入的纯文本内容。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns></returns>
    public Task SendMessageInNewSessionAsync(string inputText, CancellationToken cancellationToken = default)
    {
        ArgumentHelper.ThrowIfNullOrWhiteSpace(inputText);
        return SendMessageInNewSessionAsync([new TextContent(inputText)], cancellationToken);
    }

    /// <summary>
    /// 发送消息并开始聊天。可以选择是否携带历史消息，是否创建新会话，以及使用哪些工具。
    /// </summary>
    /// <param name="contents">用户输入的多模态内容集合，可包含 <see cref="TextContent"/>、<see cref="DataContent"/> 等。</param>
    /// <param name="withHistory">是否携带历史消息</param>
    /// <param name="createNewSession">是否创建新会话</param>
    /// <param name="tools">使用的工具</param>
    /// <param name="toolMode">工具模式</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task SendMessageAsync(IReadOnlyList<AIContent> contents, bool withHistory = true, bool createNewSession = false, IReadOnlyList<AITool>? tools = null,
        ChatToolMode? toolMode = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contents);
        if (contents.Count == 0)
        {
            return;
        }

        SendMessageResult sendMessageResult = SendMessage(new SendMessageRequest(contents)
        {
            WithHistory = withHistory,
            CreateNewSession = createNewSession,
            Tools = tools ?? [],
            ToolMode = toolMode,
            CancellationToken = cancellationToken,
        });
        await sendMessageResult.RunTask;
    }

    /// <summary>
    /// 发送纯文本消息并开始聊天。
    /// </summary>
    /// <param name="inputText">用户输入的纯文本内容。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns></returns>
    public Task SendMessageAsync(string inputText, CancellationToken cancellationToken = default)
    {
        ArgumentHelper.ThrowIfNullOrWhiteSpace(inputText);
        return SendMessageAsync([new TextContent(inputText)], cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 发送消息并开始聊天
    /// </summary>
    /// <param name="request"></param>
    /// <returns>需要等待才能获取结果的任务</returns>
    public SendMessageResult SendMessage(SendMessageRequest request)
    {
        ArgumentNullException.ThrowIfNull(request.Contents);
        if (request.Contents.Count == 0)
        {
            throw new ArgumentException("Contents 不能为空。", nameof(request));
        }

        CancellationTokenSource currentChatCancellationTokenSource = CreateCurrentChatCancellationTokenSource(request.CancellationToken);
        CancellationToken currentChatCancellationToken = currentChatCancellationTokenSource.Token;
        _currentChatCancellationTokenSource = currentChatCancellationTokenSource;
        WasLastChatCanceled = false;

        if (request.CreateNewSession)
        {
            CreateNewSession();
        }

        CopilotChatSession currentSession = SelectedSession;
        IsChatting = true;

        CopilotChatMessage userChatMessage = CopilotChatMessage.CreateUser(request.Contents);
        CopilotChatMessage assistantChatMessage = CopilotChatMessage.CreateAssistant(CopilotChatMessage.PlaceholderContent, isPresetInfo: false);
        OnBeforeSendStreaming(currentSession, assistantChatMessage);

        CopilotChatContext chatContext = new(currentSession.ChatMessages, assistantChatMessage);
        IReadOnlyList<AITool> toolList = request.AppendDefaultTools
            ? ResolveTools(request.Tools, chatContext, currentChatCancellationToken)
            : request.Tools.ToList();

        Task<ChatClientAgentCreatedResult> createChatClientAgentTask = CreateChatClientAgentAsync();

        async Task<ChatClientAgentCreatedResult> CreateChatClientAgentAsync()
        {
            currentChatCancellationToken.ThrowIfCancellationRequested();

            IChatClient chatClient = request.ChatClient ?? await AgentApiEndpointManager.PrimaryModel.GetChatClientAsync();

            var chatClientAgentOptions = new ChatClientAgentOptions()
            {
                ChatOptions = new ChatOptions()
                {
                    Tools = [.. toolList],
                    ToolMode = toolList.Count > 0 ? request.ToolMode : null,
                }
            };

            if (request.ChatReducer is not null)
            {
                chatClientAgentOptions.ChatHistoryProvider = new InMemoryChatHistoryProvider(new InMemoryChatHistoryProviderOptions()
                {
                    ChatReducer = request.ChatReducer
                });
                chatClientAgentOptions.RequirePerServiceCallChatHistoryPersistence = request.RequirePerServiceCallChatHistoryPersistence;
            }
            else
            {
                // 当未指定 ChatReducer 时，自动启用内置的 ToolCall 压缩器。
                // 压缩器与聊天逻辑无关，始终使用 AgentApiEndpointManager.PrimaryModel 获取 IChatClient。
                IChatClient reducerChatClient = await AgentApiEndpointManager.PrimaryModel.GetChatClientAsync();
                chatClientAgentOptions.ChatHistoryProvider = new InMemoryChatHistoryProvider(new InMemoryChatHistoryProviderOptions()
                {
                    ChatReducer = new CopilotChatManagerToolCallChatReducer(reducerChatClient)
                });
                chatClientAgentOptions.RequirePerServiceCallChatHistoryPersistence = true;
            }

            IReadOnlyList<AIContextProvider>? aiContextProviders = request.AIContextProviders ?? AIContextProviders;
            if (aiContextProviders is { Count: > 0 })
            {
                chatClientAgentOptions.AIContextProviders = aiContextProviders as IList<AIContextProvider> ?? aiContextProviders.ToList();
            }

            ChatClientAgent chatClientAgent = chatClient.AsAIAgent(chatClientAgentOptions);

            // 决定是否追加历史消息
            AgentSession? runSession = request.WithHistory
                ? await GetOrCreateAgentSessionAsync(chatClientAgent, currentSession, currentChatCancellationToken)
                : null;

            return new ChatClientAgentCreatedResult(chatClient, chatClientAgent, runSession);
        }

        Task<SendMessageRunState> runTask = RunAsync();

        return new SendMessageResult(userChatMessage, assistantChatMessage, toolList, createChatClientAgentTask, runTask);

        async Task<SendMessageRunState> RunAsync()
        {
            try
            {
                currentChatCancellationToken.ThrowIfCancellationRequested();

                await AppendMessageAsync(currentSession, userChatMessage, currentChatCancellationToken);

                ChatClientAgentCreatedResult chatClientAgentCreatedResult = await createChatClientAgentTask;
                await AppendMessageAsync(currentSession, assistantChatMessage, currentChatCancellationToken);

                bool isFirst = true;
                ChatMessage userChatMessageContent = userChatMessage.ToChatMessage();
                IEnumerable<ChatMessage> runMessages = string.IsNullOrWhiteSpace(request.SystemPrompt)
                    ? [userChatMessageContent]
                    : [
                        new ChatMessage(ChatRole.System, request.SystemPrompt), userChatMessageContent
                    ];
                await foreach (AgentResponseUpdate agentRunResponseUpdate in chatClientAgentCreatedResult.ChatClientAgent.RunStreamingAsync(
                    runMessages, chatClientAgentCreatedResult.AgentSession, cancellationToken: currentChatCancellationToken))
                {
                    if (isFirst)
                    {
                        assistantChatMessage.ClearMessageItems();
                    }

                    isFirst = false;
                    AppendAssistantResponseUpdate(assistantChatMessage, agentRunResponseUpdate);
                }

                await ChatLogger.LogMessageAsync(currentSession.SessionId, assistantChatMessage,
                    CreateAgentSessionStateProvider(chatClientAgentCreatedResult.ChatClientAgent, chatClientAgentCreatedResult.AgentSession));
                return new SendMessageRunState(IsSuccess: true, WasCanceled: false);
            }
            catch (OperationCanceledException) when (currentChatCancellationToken.IsCancellationRequested)
            {
                WasLastChatCanceled = true;
                CopilotChatMessage canceledMessage = CopilotChatMessage.CreateAssistant("已取消", isPresetInfo: true);
                await AppendMessageAsync(currentSession, canceledMessage);
                return new SendMessageRunState(IsSuccess: false, WasCanceled: true);
            }
            catch (Exception exception)
            {
                CopilotChatMessage exceptionMessage = CopilotChatMessage.CreateAssistant(exception.ToString(), isPresetInfo: true);
                await AppendMessageAsync(currentSession, exceptionMessage);
                return new SendMessageRunState(IsSuccess: false, WasCanceled: false);
            }
            finally
            {
                if (ReferenceEquals(_currentChatCancellationTokenSource, currentChatCancellationTokenSource))
                {
                    _currentChatCancellationTokenSource = null;
                }

                IsChatting = false;
                currentChatCancellationTokenSource.Dispose();
            }
        }
    }

    protected virtual void OnSessionCreated(CopilotChatSession session)
    {
    }

    protected virtual void OnBeforeSendStreaming(CopilotChatSession currentSession, CopilotChatMessage assistantMessage)
    {
    }

    /// <summary>
    /// 创建手动发送消息的上下文。不修改 <see cref="IsChatting"/> 状态，不追加消息到会话，不创建 CTS。
    /// 调用方完全自行控制 AgentFramework 的调用流程。
    /// 返回的上下文包含裸 <see cref="IChatClient"/>、默认工具列表，
    /// 以及两个空壳 <see cref="CopilotChatMessage"/> 供调用方填充和流式追加。
    /// <see cref="ChatClientAgent"/> 和 <see cref="AgentSession"/> 延迟创建，首次调用对应异步方法时才初始化。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>手动发送消息的上下文。</returns>
    public async Task<IManualSendMessageContext> CreateManualSendMessageContextAsync(CancellationToken cancellationToken = default)
    {
        CopilotChatSession currentSession = SelectedSession;

        // 默认工具不经过 HumanApprovalTool 包装，调用方自行决定是否包装
        IReadOnlyList<AITool> defaultTools = _toolManager.CreateDefaultTools(chatContext: null);

        CopilotChatMessage userChatMessage = CopilotChatMessage.CreateUser(string.Empty);
        CopilotChatMessage assistantChatMessage = CopilotChatMessage.CreateAssistant(CopilotChatMessage.PlaceholderContent, isPresetInfo: false);

        IChatClient chatClient = await AgentApiEndpointManager.PrimaryModel.GetChatClientAsync().ConfigureAwait(false);

        return new ManualSendMessageContext
        {
            ChatManager = this,
            AIContextProviders = AIContextProviders,
            Session = currentSession,
            DefaultTools = defaultTools,
            ChatClient = chatClient,
            MainThreadDispatcher = MainThreadDispatcher,
            UserChatMessage = userChatMessage,
            AssistantChatMessage = assistantChatMessage,
        };
    }

    /// <summary>
    /// 手动触发 LLM 标题生成。调用方负责决定调用时机。
    /// 对标为 <see cref="TitleSource.AutoTruncated"/> 或 <see cref="TitleSource.Generated"/> 的会话不会重复生成。
    /// </summary>
    /// <param name="session">
    /// 目标会话。为 <see langword="null"/> 时使用 <see cref="SelectedSession"/>。
    /// </param>
    /// <param name="systemPrompt">
    /// 自定义 System Prompt。为 <see langword="null"/> 时使用默认 Prompt。
    /// </param>
    /// <param name="cancellationToken">取消令牌。</param>
    public Task GenerateSessionTitleAsync(CopilotChatSession? session = null, string? systemPrompt = null, CancellationToken cancellationToken = default)
    {
        return _titleGenerator.GenerateTitleAsync(session ?? SelectedSession, systemPrompt, cancellationToken);
    }

    /// <summary>
    /// 压缩对话，如果没有传压缩器，则采用内置的压缩逻辑
    /// </summary>
    /// <param name="chatReducer"></param>
    /// <returns></returns>
    public async Task ReduceSessionAsync(IChatReducer? chatReducer = null)
    {
        CopilotChatSession currentSession = SelectedSession;
        AgentSession? agentSession = currentSession.AgentSession;
        if (agentSession is null)
        {
            return;
        }

        if (!agentSession.TryGetInMemoryChatHistory(out List<ChatMessage>? messages))
        {
            return;
        }

        if (chatReducer is null)
        {
            IChatClient chatClient = await AgentApiEndpointManager.PrimaryModel.GetChatClientAsync();

            //#pragma warning disable MEAI001
            //            chatReducer = new SummarizingChatReducer(chatClient,2, 3);
            //#pragma warning restore MEAI001
            chatReducer = new CopilotChatManagerChatReducer(chatClient);
        }

        IEnumerable<ChatMessage> result = await chatReducer.ReduceAsync(messages, CancellationToken.None);
        var resultList = result.ToList();
        agentSession.SetInMemoryChatHistory(resultList);

        // 从压缩结果中提取 Assistant 角色的完整内容（含文本、图片、音频等多模态），保留原始 AIContent
        List<AIContent> assistantContents = resultList
            .Where(m => m.Role == ChatRole.Assistant)
            .SelectMany(m => m.Contents)
            .ToList();

        if (assistantContents.Count > 0)
        {
            var userMessage = CopilotChatMessage.CreateUser("总结对话");
            userMessage.IsPresetInfo = true;
            await AppendMessageAsync(currentSession, userMessage);

            var assistantMessage = new CopilotChatMessage(ChatRole.Assistant, assistantContents)
            {
                IsPresetInfo = true
            };
            await AppendMessageAsync(currentSession, assistantMessage);
        }
    }

    private List<AITool> ResolveTools(IReadOnlyList<AITool> tools, CopilotChatContext? chatContext = null,
        CancellationToken cancellationToken = default)
    {
        List<AITool> toolList = new(tools.Count);
        foreach (AITool tool in tools)
        {
            toolList.Add(HumanApprovalTool.BindRuntimeTool(tool, chatContext, cancellationToken));
        }

        toolList.AddRange(_toolManager.CreateDefaultTools(chatContext, cancellationToken)
            .Select(tool => HumanApprovalTool.BindRuntimeTool(tool, chatContext, cancellationToken)));

        if (chatContext is not null)
        {
            foreach (AITool tool in toolList)
            {
                if (HumanApprovalTool.TryGetApprovalDescription(tool, out string? approvalDescription))
                {
                    chatContext.CurrentContent.RegisterApprovalTool(tool.Name, approvalDescription);
                }
            }
        }

        return toolList;
    }

    private static ICopilotChatSessionStateProvider? CreateAgentSessionStateProvider(ChatClientAgent chatClientAgent, AgentSession? agentSession)
    {
        ArgumentNullException.ThrowIfNull(chatClientAgent);

        return agentSession is null ? null : new AgentSessionStateProvider(chatClientAgent, agentSession);
    }

    internal static void AppendAssistantResponseUpdate(CopilotChatMessage copilotChatMessage, AgentResponseUpdate responseUpdate)
    {
        ArgumentNullException.ThrowIfNull(copilotChatMessage);
        ArgumentNullException.ThrowIfNull(responseUpdate);

        foreach (AIContent content in responseUpdate.Contents)
        {
            switch (content)
            {
                case TextReasoningContent textReasoningContent when !string.IsNullOrEmpty(textReasoningContent.Text):
                    copilotChatMessage.AppendReasoning(textReasoningContent.Text);
                    break;
                case TextContent textContent when !string.IsNullOrEmpty(textContent.Text):
                    copilotChatMessage.AppendText(textContent.Text);
                    break;
                case FunctionCallContent functionCallContent:
                    copilotChatMessage.AppendFunctionCall(functionCallContent);
                    break;
                case FunctionResultContent functionResultContent:
                    copilotChatMessage.AppendFunctionResult(functionResultContent);
                    break;
            }
        }

        copilotChatMessage.AppendUsageDetails(responseUpdate.Contents);
    }

    private CopilotChatSession CreateSession()
    {
        var session = new CopilotChatSession(Guid.NewGuid(), DateTimeOffset.Now)
        {
            MainThreadDispatcher = MainThreadDispatcher,
        };
        AddAssistantWelcomeMessage(session);
        ChatSessions.Insert(0, session);
        OnSessionCreated(session);
        return session;
    }

    private CopilotChatSession? FindReusableEmptySession()
    {
        return ChatSessions.FirstOrDefault(IsEmptySession);
    }

    private static bool IsEmptySession(CopilotChatSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return session.ChatMessages.Count < 2 && session.ChatMessages.All(chatMessage => chatMessage.IsPresetInfo);
    }

    /// <summary>
    /// 向当前选中会话追加一条聊天消息。
    /// 如果设置了 <see cref="MainThreadDispatcher"/>，消息将通过调度器在主线程上添加，确保 UI 绑定集合的线程安全。
    /// </summary>
    /// <param name="chatMessage">要追加的聊天消息。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public Task AppendMessageAsync(CopilotChatMessage chatMessage, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chatMessage);
        return AppendMessageAsync(SelectedSession, chatMessage, cancellationToken);
    }

    /// <summary>
    /// 标记进入聊天状态（<see cref="IsChatting"/> = <see langword="true"/>），返回一个可释放对象。
    /// 调用方使用 <c>using</c> 语句包裹流式调用过程，<see cref="IDisposable.Dispose"/> 时自动恢复 <see cref="IsChatting"/> = <see langword="false"/>。
    /// </summary>
    /// <returns>可释放对象，dispose 时恢复 <see cref="IsChatting"/> = <see langword="false"/>。</returns>
    public IDisposable StartChatting()
    {
        return new ChattingScope(this);
    }

    /// <summary>
    /// 向指定会话追加一条聊天消息。
    /// </summary>
    /// <param name="session">目标会话。</param>
    /// <param name="chatMessage">要追加的聊天消息。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    internal async Task AppendMessageAsync(CopilotChatSession session, CopilotChatMessage chatMessage, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await session.AddMessageAsync(chatMessage);
        await ChatLogger.LogMessageAsync(session.SessionId, chatMessage);
    }

    private static async Task<AgentSession> GetOrCreateAgentSessionAsync(ChatClientAgent chatClientAgent, CopilotChatSession currentSession,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(chatClientAgent);
        ArgumentNullException.ThrowIfNull(currentSession);

        AgentSession? agentSession = currentSession.AgentSession;
        if (agentSession is not null)
        {
            return agentSession;
        }

        agentSession = await chatClientAgent.CreateSessionAsync(cancellationToken);
        currentSession.SetAgentSession(agentSession);
        return agentSession;
    }

    private static void AddAssistantWelcomeMessage(CopilotChatSession session)
    {
        session.AddMessage(CopilotChatMessage.CreateAssistant("你好，我是 Copilot。请开始输入你的问题。", isPresetInfo: true));
    }

    private static CancellationTokenSource CreateCurrentChatCancellationTokenSource(CancellationToken cancellationToken)
    {
        return cancellationToken.CanBeCanceled
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
            : new CancellationTokenSource();
    }

    /// <summary>
    /// 聊天状态作用域，构造时设置 <see cref="IsChatting"/> = <see langword="true"/>，
    /// <see cref="Dispose"/> 时恢复 <see cref="IsChatting"/> = <see langword="false"/>。
    /// </summary>
    private sealed class ChattingScope : IDisposable
    {
        private readonly CopilotChatManager _manager;
        private bool _disposed;

        public ChattingScope(CopilotChatManager manager)
        {
            _manager = manager;
            _manager.IsChatting = true;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _manager.IsChatting = false;
        }
    }
}
