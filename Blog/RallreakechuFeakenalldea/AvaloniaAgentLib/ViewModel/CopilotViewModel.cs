using AvaloniaAgentLib.Core;
using AvaloniaAgentLib.Logging;
using AvaloniaAgentLib.Model;

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

    public void CreateNewSession()
    {
        var session = new CopilotChatSession(Guid.NewGuid(), DateTimeOffset.Now);
        AddAssistantWelcomeMessage(session);
        ChatSessions.Insert(0, session);
        SelectedSession = session;
    }

    public void SetChatLogFolder(string? chatLogFolder)
    {
        ChatLogger = string.IsNullOrWhiteSpace(chatLogFolder)
            ? new FileCopilotChatLogger()
            : new FileCopilotChatLogger(chatLogFolder);
    }

    public async Task SendMessageAsync(string? inputText, bool withHistory = true, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(inputText))
        {
            return;
        }

        CopilotChatSession currentSession = SelectedSession;
        IsChatting = true;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var userChatMessage = CopilotChatMessage.CreateUser(inputText);
            currentSession.AddMessage(userChatMessage);
            await ChatLogger.LogMessageAsync(currentSession.SessionId, userChatMessage);

            var chatClient = AgentApiEndpointManager.CreateOpenAIClient();
            ChatClientAgent chatClientAgent = chatClient.AsAIAgent(new ChatClientAgentOptions()
            {
                ChatOptions = new ChatOptions()
                {

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

            await foreach (var agentRunResponseUpdate in chatClientAgent.RunReasoningStreamingAsync(messages, cancellationToken: cancellationToken))
            {
                if (isFirst)
                {
                    copilotChatMessage.Content = "";
                }

                isFirst = false;

                if (agentRunResponseUpdate.IsFirstThinking)
                {
                    copilotChatMessage.Content = "思考：";
                }

                if (agentRunResponseUpdate.Reasoning is not null)
                {
                    copilotChatMessage.Content += agentRunResponseUpdate.Reasoning;
                }

                if (agentRunResponseUpdate.IsThinkingEnd)
                {
                    copilotChatMessage.Content += "\r\n--------\r\n";
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
            currentSession.AddMessage(canceledMessage);
            await ChatLogger.LogMessageAsync(currentSession.SessionId, canceledMessage);
        }
        catch (Exception exception)
        {
            var exceptionMessage = CopilotChatMessage.CreateAssistant(exception.ToString(), isPresetInfo: true);
            currentSession.AddMessage(exceptionMessage);
            await ChatLogger.LogMessageAsync(currentSession.SessionId, exceptionMessage);
        }
        finally
        {
            IsChatting = false;
        }
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
