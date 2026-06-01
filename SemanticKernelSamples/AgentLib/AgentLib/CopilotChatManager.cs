using AgentLib.Core;
using AgentLib.Logging;
using AgentLib.Model;
using AgentLib.Tools;

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AgentLib;

public class CopilotChatManager : NotifyBase
{
    private bool _isChatting;
    private bool _wasLastChatCanceled;
    private ICopilotChatLogger _chatLogger = null!;
    private CopilotChatSession _selectedSession = null!;
    private CancellationTokenSource? _currentChatCancellationTokenSource;
    private readonly CopilotToolManager _toolManager;

    public CopilotChatManager()
        : this(new EmptyCopilotChatLogger())
    {
        // 无参构造，让 XAML 系开森
    }

    public CopilotChatManager(ICopilotChatLogger chatLogger)
    {
        ChatLogger = chatLogger;
        _toolManager = new CopilotToolManager(this.AgentApiEndpointManager);
        CreateNewSession();
    }

    public AgentApiEndpointManager AgentApiEndpointManager { get; } = new();

    public ObservableCollection<CopilotChatSession> ChatSessions { get; } = [];

    public ObservableCollection<CopilotChatMessage> ChatMessages => SelectedSession.ChatMessages;

    public ICopilotChatLogger ChatLogger
    {
        get => _chatLogger;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _chatLogger = value;
        }
    }

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

    public bool CanEditInput => !IsChatting;

    public bool WasLastChatCanceled
    {
        get => _wasLastChatCanceled;
        private set => SetField(ref _wasLastChatCanceled, value);
    }

    public string SendButtonText => IsChatting ? "停止" : "发送";

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

    public Guid CurrentSessionId => SelectedSession.SessionId;

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

    public void CreateNewSession()
    {
        CopilotChatSession session = FindReusableEmptySession() ?? CreateSession();
        SelectedSession = session;
    }

    public void SetChatLogFolder(string? chatLogFolder)
    {
        ChatLogger = string.IsNullOrWhiteSpace(chatLogFolder)
            ? new FileCopilotChatLogger()
            : new FileCopilotChatLogger(chatLogFolder);
    }

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
        ArgumentException.ThrowIfNullOrWhiteSpace(userText);
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
    /// <param name="inputText"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task SendMessageInNewSessionAsync(string? inputText, CancellationToken cancellationToken = default)
    {
        return SendMessageAsync(inputText, withHistory: true, createNewSession: true, tools: null, toolMode: null, cancellationToken);
    }

    /// <summary>
    /// 发送消息并开始聊天。可以选择是否携带历史消息，是否创建新会话，以及使用哪些工具。
    /// </summary>
    /// <param name="inputText"></param>
    /// <param name="withHistory">是否携带历史消息</param>
    /// <param name="createNewSession">是否创建新会话</param>
    /// <param name="tools">使用的工具</param>
    /// <param name="toolMode">工具模式</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task SendMessageAsync(string? inputText, bool withHistory = true, bool createNewSession = false, IEnumerable<AITool>? tools = null,
        ChatToolMode? toolMode = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(inputText))
        {
            return;
        }

        SendMessageResult sendMessageResult = SendMessage(new SendMessageRequest(inputText, withHistory, createNewSession, tools, toolMode, cancellationToken));
        await sendMessageResult.RunTask;
    }

    /// <summary>
    /// 发送消息并开始聊天
    /// </summary>
    /// <param name="request"></param>
    /// <returns>需要等待才能获取结果的任务</returns>
    public SendMessageResult SendMessage(SendMessageRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.InputText);

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

        CopilotChatMessage userChatMessage = CopilotChatMessage.CreateUser(request.InputText);
        CopilotChatMessage assistantChatMessage = CopilotChatMessage.CreateAssistant("...", isPresetInfo: false);
        OnBeforeSendStreaming(currentSession, assistantChatMessage);

        CopilotChatContext chatContext = new(currentSession.ChatMessages, assistantChatMessage);
        List<AITool> toolList = ResolveTools(request.Tools, chatContext, currentChatCancellationToken);

        Task<ChatClientAgentCreatedResult> createChatClientAgentTask = CreateChatClientAgentAsync(request.WithHistory, request.ToolMode);

        async Task<ChatClientAgentCreatedResult> CreateChatClientAgentAsync(
            bool withHistory, ChatToolMode? toolMode)
        {
            currentChatCancellationToken.ThrowIfCancellationRequested();

            IChatClient chatClient = await AgentApiEndpointManager.PrimaryModel.GetChatClientAsync();
            ChatClientAgent chatClientAgent = chatClient.AsAIAgent(new ChatClientAgentOptions()
            {
                ChatOptions = new ChatOptions()
                {
                    Tools = [.. toolList],
                    ToolMode = toolList.Count > 0 ? toolMode : null,
                }
            });

            // 决定是否追加历史消息
            AgentSession? runSession = withHistory
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
                currentSession.AddMessage(assistantChatMessage);

                bool isFirst = true;
                ChatMessage chatMessage = userChatMessage.ToChatMessage();
                await foreach (AgentResponseUpdate agentRunResponseUpdate in chatClientAgentCreatedResult.ChatClientAgent.RunStreamingAsync(
                    chatMessage, chatClientAgentCreatedResult.AgentSession, cancellationToken: currentChatCancellationToken))
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
                throw;
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

    private List<AITool> ResolveTools(IEnumerable<AITool>? tools, CopilotChatContext? chatContext = null,
        CancellationToken cancellationToken = default)
    {
        List<AITool> toolList = [];
        if (tools != null)
        {
            foreach (AITool tool in tools)
            {
                toolList.Add(HumanApprovalTool.BindRuntimeTool(tool, chatContext, cancellationToken));
            }
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

    private static void AppendAssistantResponseUpdate(CopilotChatMessage copilotChatMessage, AgentResponseUpdate responseUpdate)
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
        var session = new CopilotChatSession(Guid.NewGuid(), DateTimeOffset.Now);
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

    private async Task AppendMessageAsync(CopilotChatSession session, CopilotChatMessage chatMessage, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        session.AddMessage(chatMessage);
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
}
