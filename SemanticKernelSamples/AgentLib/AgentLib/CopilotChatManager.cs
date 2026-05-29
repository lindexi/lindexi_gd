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

    public Task SendMessageAsync(string? inputText, bool withHistory = true, CancellationToken cancellationToken = default)
    {
        return SendMessageAsync(inputText, withHistory, createNewSession: false, tools: null, toolMode: null, cancellationToken);
    }

    public Task SendMessageInNewSessionAsync(string? inputText, CancellationToken cancellationToken = default)
    {
        return SendMessageAsync(inputText, withHistory: false, createNewSession: true, tools: null, toolMode: null, cancellationToken);
    }

    public async Task SendMessageAsync(string? inputText, bool withHistory, bool createNewSession, IEnumerable<AITool>? tools,
        ChatToolMode? toolMode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(inputText))
        {
            return;
        }

        using CancellationTokenSource currentChatCancellationTokenSource = CreateCurrentChatCancellationTokenSource(cancellationToken);
        CancellationToken currentChatCancellationToken = currentChatCancellationTokenSource.Token;
        _currentChatCancellationTokenSource = currentChatCancellationTokenSource;
        WasLastChatCanceled = false;

        if (createNewSession)
        {
            CreateNewSession();
        }

        CopilotChatSession currentSession = SelectedSession;
        IsChatting = true;

        try
        {
            currentChatCancellationToken.ThrowIfCancellationRequested();

            var userChatMessage = CopilotChatMessage.CreateUser(inputText);
            await AppendMessageAsync(currentSession, userChatMessage, currentChatCancellationToken);

            var copilotChatMessage = CopilotChatMessage.CreateAssistant("...", isPresetInfo: false);
            OnBeforeSendStreaming(currentSession, copilotChatMessage);

            var chatClient = await AgentApiEndpointManager.PrimaryModel.GetChatClientAsync();
            CopilotChatContext chatContext = new(currentSession.ChatMessages, copilotChatMessage);
            List<AITool> toolList = ResolveTools(tools, chatContext, cancellationToken);
            ChatClientAgent chatClientAgent = chatClient.AsAIAgent(new ChatClientAgentOptions()
            {
                ChatOptions = new ChatOptions()
                {
                    Tools = toolList,
                    ToolMode = toolList is { Count: > 0 } ? toolMode : null,
                }
            });

            AgentSession agentSession = await GetOrCreateAgentSessionAsync(chatClientAgent, currentSession, currentChatCancellationToken);
            currentSession.SetAgentSession(agentSession, SerializeAgentSession(agentSession));

            IReadOnlyList<ChatMessage> messages = ShouldSendOnlyCurrentMessage(currentSession, withHistory)
                ? [userChatMessage.ToChatMessage()]
                : [];

            currentSession.AddMessage(copilotChatMessage);
           

            bool isFirst = true;
            await foreach (AgentResponseUpdate agentRunResponseUpdate in chatClientAgent.RunStreamingAsync(messages, agentSession, cancellationToken: currentChatCancellationToken))
            {
                if (isFirst)
                {
                    copilotChatMessage.ClearMessageItems();
                }

                isFirst = false;
                AppendAssistantResponseUpdate(copilotChatMessage, agentRunResponseUpdate);
            }

            currentSession.SetAgentSession(agentSession, SerializeAgentSession(agentSession));
            await ChatLogger.LogMessageAsync(currentSession.SessionId, copilotChatMessage, currentSession.SerializedAgentSessionState);
        }
        catch (OperationCanceledException) when (currentChatCancellationToken.IsCancellationRequested)
        {
            WasLastChatCanceled = true;
            var canceledMessage = CopilotChatMessage.CreateAssistant("已取消", isPresetInfo: true);
            await AppendMessageAsync(currentSession, canceledMessage);
        }
        catch (Exception exception)
        {
            var exceptionMessage = CopilotChatMessage.CreateAssistant(exception.ToString(), isPresetInfo: true);
            await AppendMessageAsync(currentSession, exceptionMessage);
        }
        finally
        {
            if (ReferenceEquals(_currentChatCancellationTokenSource, currentChatCancellationTokenSource))
            {
                _currentChatCancellationTokenSource = null;
            }

            IsChatting = false;
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

    private static bool ShouldSendOnlyCurrentMessage(CopilotChatSession currentSession, bool withHistory)
    {
        if (!withHistory)
        {
            return true;
        }

        return !currentSession.HasSerializedAgentSessionState;
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
        await ChatLogger.LogMessageAsync(session.SessionId, chatMessage, session.SerializedAgentSessionState);
    }

    private static string? SerializeAgentSession(AgentSession? agentSession)
    {
        return agentSession is ChatClientAgentSession { ConversationId: { Length: > 0 } conversationId }
            ? conversationId
            : null;
    }

    private static Task<AgentSession?> DeserializeAgentSessionAsync(ChatClientAgent chatClientAgent, string? serializedAgentSessionState,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(chatClientAgent);

        if (string.IsNullOrWhiteSpace(serializedAgentSessionState))
        {
            return Task.FromResult<AgentSession?>(null);
        }

        return chatClientAgent.CreateSessionAsync(serializedAgentSessionState, cancellationToken).AsTask();
    }

    private static async Task<AgentSession> GetOrCreateAgentSessionAsync(ChatClientAgent chatClientAgent, CopilotChatSession currentSession,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(chatClientAgent);
        ArgumentNullException.ThrowIfNull(currentSession);

        AgentSession? agentSession = currentSession.AgentSession;
        if (agentSession is null)
        {
            agentSession = await DeserializeAgentSessionAsync(chatClientAgent, currentSession.SerializedAgentSessionState, cancellationToken);
        }

        if (agentSession is not null)
        {
            currentSession.SetAgentSession(agentSession, currentSession.SerializedAgentSessionState);
            return agentSession;
        }

        agentSession = await chatClientAgent.CreateSessionAsync(cancellationToken);
        currentSession.SetAgentSession(agentSession, SerializeAgentSession(agentSession));
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
