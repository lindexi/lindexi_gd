using AvaloniaAgentLib.Core;
using AvaloniaAgentLib.Logging;
using AvaloniaAgentLib.Model;
using AvaloniaAgentLib.Tools;

using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Reasoning;
using Microsoft.Extensions.AI;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace AvaloniaAgentLib.ViewModel;

public class CopilotViewModel : INotifyPropertyChanged
{
    public CopilotViewModel()
        : this(new FileCopilotChatLogger())
    {
    }

    public CopilotViewModel(ICopilotChatLogger chatLogger)
    {
        ChatLogger = chatLogger;
        _toolManager = new CopilotToolManager();
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

            OnPropertyChanged();
            OnPropertyChanged(nameof(CanEditInput));
            OnPropertyChanged(nameof(SendButtonText));
        }
    }

    private bool _isChatting;
    private ICopilotChatLogger _chatLogger = null!;
    private CopilotChatSession _selectedSession = null!;
    private readonly CopilotToolManager _toolManager;

    /// <summary>
    /// 能否编辑输入
    /// </summary>
    public bool CanEditInput => !IsChatting;

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
            string? normalizedPath = string.IsNullOrWhiteSpace(value)
                ? null
                : value;

            if (string.Equals(_toolManager.WorkspacePath, normalizedPath, StringComparison.Ordinal))
            {
                return;
            }

            _toolManager.WorkspacePath = normalizedPath;
            OnPropertyChanged();
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

    public async Task SendMessageAsync(string? inputText, bool withHistory = true, CancellationToken cancellationToken = default)
    {
        await SendMessageAsync(inputText, withHistory, createNewSession: false, null, null, cancellationToken);
    }

    public Task SendMessageInNewSessionAsync(string? inputText, CancellationToken cancellationToken = default)
    {
        return SendMessageAsync(inputText, withHistory: false, createNewSession: true, tools: null, toolMode: null,
            cancellationToken);
    }

    /// <summary>
    /// 发送消息并允许附加 Agent 工具调用。
    /// </summary>
    public async Task SendMessageAsync(string? inputText, bool withHistory, bool createNewSession, IEnumerable<AITool>? tools,
        ChatToolMode? toolMode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(inputText))
        {
            return;
        }

        if (createNewSession)
        {
            CreateNewSession();
        }

        CopilotChatSession currentSession = SelectedSession;
        IsChatting = true;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var userChatMessage = CopilotChatMessage.CreateUser(inputText);
            await AppendMessageAsync(currentSession, userChatMessage, cancellationToken);

            var chatClient = AgentApiEndpointManager.CreateOpenAIClient();
            List<AITool> toolList = ResolveTools(tools);
            ChatClientAgent chatClientAgent = chatClient.AsAIAgent(new ChatClientAgentOptions()
            {
                ChatOptions = new ChatOptions()
                {
                    Tools = toolList,
                    ToolMode = toolList is { Count: > 0 } ? toolMode : null,
                }
            });

            ChatMessage[] messages = withHistory
                ?
                // 需要带历史的情况
                [
                    ..currentSession.ChatMessages
                        .Where(chatMessage => !chatMessage.IsPresetInfo)
                        .Select(chatMessage => chatMessage.ToChatMessage()),
                ]
                // 无需历史的情况
                : [userChatMessage.ToChatMessage()];

            var copilotChatMessage = CopilotChatMessage.CreateAssistant("...", isPresetInfo: false);
            currentSession.AddMessage(copilotChatMessage);

            bool isFirst = true;
            bool isFirstReasoning = true;

            await foreach (var agentRunResponseUpdate in chatClientAgent.RunReasoningStreamingAsync(messages, cancellationToken: cancellationToken))
            {
                if (isFirst)
                {
                    copilotChatMessage.Content = "";
                }

                isFirst = false;
                copilotChatMessage.AppendUsageDetails(agentRunResponseUpdate.Origin.Contents);

                if (agentRunResponseUpdate.Reasoning is not null)
                {
                    string reasoning = agentRunResponseUpdate.Reasoning;
                    if (isFirstReasoning)
                    {
                        reasoning = reasoning.TrimStart();
                        isFirstReasoning = false;
                    }

                    copilotChatMessage.Reason += reasoning;
                }

                var text = agentRunResponseUpdate.Text;
                if (!string.IsNullOrEmpty(text))
                {
                    copilotChatMessage.Content += text;
                }
                else
                {
                    foreach (var content in agentRunResponseUpdate.Origin.Contents)
                    {
                        if (content is FunctionCallContent functionCallContent)
                        {
                            copilotChatMessage.Content += $"Function Call: {functionCallContent}\r\n";
                        }

                        /*
                         * TextContent	文本内容可以是输入，例如，来自用户或开发人员，以及代理的输出。 通常包含代理的文本结果。
                           DataContent	可以是输入和输出的二进制内容。 可用于向代理传入和传出图像、音频或视频数据（其中受支持）。
                           UriContent	通常指向托管内容（如图像、音频或视频）的 URL。
                           FunctionCallContent	推理服务调用函数工具的请求。
                           FunctionResultContent	函数工具调用的结果。
                         */
                    }
                }
            }

            await ChatLogger.LogMessageAsync(currentSession.SessionId, copilotChatMessage);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            var canceledMessage = CopilotChatMessage.CreateAssistant("已取消", isPresetInfo: true);
            await AppendMessageAsync(currentSession, canceledMessage, cancellationToken);
        }
        catch (Exception exception)
        {
            var exceptionMessage = CopilotChatMessage.CreateAssistant(exception.ToString(), isPresetInfo: true);
            await AppendMessageAsync(currentSession, exceptionMessage, cancellationToken);
        }
        finally
        {
            IsChatting = false;
        }
    }

    private List<AITool> ResolveTools(IEnumerable<AITool>? tools)
    {
        List<AITool> toolList = [];
        if (tools != null)
        {
            toolList.AddRange(tools);
        }
        toolList.AddRange(_toolManager.CreateDefaultTools());

        return toolList;
    }

    private CopilotChatSession CreateSession()
    {
        var session = new CopilotChatSession(Guid.NewGuid(), DateTimeOffset.Now);
        AddAssistantWelcomeMessage(session);
        ChatSessions.Insert(0, session);
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

    private async Task AppendMessageAsync(CopilotChatSession session, CopilotChatMessage chatMessage,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        session.AddMessage(chatMessage);
        await ChatLogger.LogMessageAsync(session.SessionId, chatMessage);
    }

    private static void AddAssistantWelcomeMessage(CopilotChatSession session)
    {
        session.AddMessage(CopilotChatMessage.CreateAssistant("你好，我是 Copilot。请开始输入你的问题。", isPresetInfo: true));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    public void OpenSetting()
    {
        SettingOpened?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? SettingOpened;
}
