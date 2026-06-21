using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using AgentLib.ChatRoom;
using AgentLib.ChatRoom.Model;
using AgentLib.ChatRoom.Services;
using AgentLib.Model;

using Avalonia.Threading;

namespace ChatRoom.AvaloniaShell.ViewModels;

/// <summary>
/// 消息显示项 ViewModel。封装 <see cref="ChatRoomMessage"/> 供 UI 绑定。
/// 当关联了 <see cref="CopilotChatMessage"/> 时，Content 自动跟踪其流式更新。
/// 订阅 <see cref="ChatRoomMessage.PropertyChanged"/> 感知 <see cref="ChatRoomMessage.Content"/>
/// 和 <see cref="ChatRoomMessage.IsStreaming"/> 的实时变更。
/// </summary>
public sealed class MessageItemViewModel : NotifyBase
{
    /// <summary>
    /// 消息唯一标识。
    /// </summary>
    public string MessageId { get; }

    /// <summary>
    /// 发言角色 ID。
    /// </summary>
    public string SenderRoleId { get; }

    /// <summary>
    /// 发言角色显示名。
    /// </summary>
    public string SenderRoleName { get; }

    /// <summary>
    /// 关联的底层 <see cref="CopilotChatMessage"/>。不为 null 时 Content 跟踪其流式更新。
    /// </summary>
    public CopilotChatMessage? CopilotChatMessage { get; }

    /// <summary>
    /// 消息内容。关联了 <see cref="CopilotChatMessage"/> 时返回其实时 Content，否则返回静态内容。
    /// </summary>
    public string Content => Message.Content;

    /// <summary>
    /// 时间戳。
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// 是否人类发送。
    /// </summary>
    public bool IsHumanMessage { get; }

    /// <summary>
    /// 是否系统消息。
    /// </summary>
    public bool IsSystemMessage { get; }

    /// <summary>
    /// 是否 AI 消息。
    /// </summary>
    public bool IsAiMessage => !IsHumanMessage && !IsSystemMessage;

    /// <summary>
    /// 是否正在流式生成。
    /// </summary>
    public bool IsStreaming => Message.IsStreaming;

    /// <summary>
    /// 角色名首字（头像显示）。
    /// </summary>
    public string Initial => string.IsNullOrEmpty(SenderRoleName) ? "?" : SenderRoleName[..1].ToUpperInvariant();

    /// <summary>
    /// 时间显示文本。
    /// </summary>
    public string TimeText => Timestamp.ToString("HH:mm:ss");

    /// <summary>
    /// 底层消息对象。
    /// </summary>
    public ChatRoomMessage Message { get; }

    /// <summary>
    /// 从 <see cref="ChatRoomMessage"/> 创建消息项。
    /// 订阅 <see cref="ChatRoomMessage.PropertyChanged"/> 以感知流式内容更新和状态变更。
    /// </summary>
    public MessageItemViewModel(ChatRoomMessage message)
    {
        Message = message;
        MessageId = message.MessageId;
        SenderRoleId = message.SenderRoleId;
        SenderRoleName = message.SenderRoleName;
        CopilotChatMessage = message.CopilotChatMessage;
        Timestamp = message.Timestamp;
        IsHumanMessage = message.IsHumanMessage;
        IsSystemMessage = message.IsSystemMessage;

        message.PropertyChanged += OnMessagePropertyChanged;
    }

    /// <summary>
    /// 当底层 <see cref="ChatRoomMessage"/> 的 <see cref="ChatRoomMessage.Content"/>
    /// 或 <see cref="ChatRoomMessage.IsStreaming"/> 变更时，桥接到 UI 线程。
    /// </summary>
    private void OnMessagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ChatRoomMessage.Content):
            case nameof(ChatRoomMessage.IsStreaming):
                break;
            default:
                return;
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            OnPropertyChanged(e.PropertyName);
        }
        else
        {
            Dispatcher.UIThread.Post(() => OnPropertyChanged(e.PropertyName));
        }
    }
}

/// <summary>
/// 中栏聊天区 ViewModel。
/// 通过订阅 <see cref="ChatRoomSession.Messages"/> 的 <see cref="INotifyCollectionChanged.CollectionChanged"/>
/// 实时镜像业务层消息集合到 UI 层，无需依赖 <see cref="ChatRoomService.MessageAdded"/> 事件。
/// </summary>
public sealed class ChatViewModel : ViewModelBase
{
    private readonly ChatRoomService _chatRoomService;
    private string _inputText = string.Empty;
    private bool _isRunning;
    private string _currentSpeakerName = string.Empty;
    private CancellationTokenSource? _autoLoopCts;
    private ChatRoomSession? _previousSession;

    /// <summary>
    /// 消息列表。流式消息和历史消息统一在此集合中展示。
    /// 通过 <see cref="INotifyCollectionChanged.CollectionChanged"/> 实时镜像 <see cref="ChatRoomSession.Messages"/>。
    /// </summary>
    public ObservableCollection<MessageItemViewModel> Messages { get; } = [];

    /// <summary>
    /// 输入框文本。
    /// </summary>
    public string InputText
    {
        get => _inputText;
        set => SetField(ref _inputText, value);
    }

    /// <summary>
    /// 是否正在运行自动循环。
    /// </summary>
    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            if (SetField(ref _isRunning, value))
            {
                OnPropertyChanged(nameof(CanSend));
                OnPropertyChanged(nameof(CanStop));
                OnPropertyChanged(nameof(SendButtonText));
                RaiseCommandCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// 当前发言角色名。
    /// </summary>
    public string CurrentSpeakerName
    {
        get => _currentSpeakerName;
        set => SetField(ref _currentSpeakerName, value);
    }

    /// <summary>
    /// 是否可以发送消息。
    /// </summary>
    public bool CanSend => !IsRunning && _chatRoomService.HasActiveSession;

    /// <summary>
    /// 是否可以停止循环。
    /// </summary>
    public bool CanStop => IsRunning;

    /// <summary>
    /// 发送按钮文本。
    /// </summary>
    public string SendButtonText => IsRunning ? "停止" : "发送";

    /// <summary>
    /// 发送命令。
    /// </summary>
    public ICommand SendCommand { get; }

    /// <summary>
    /// 停止命令。
    /// </summary>
    public ICommand StopCommand { get; }

    /// <summary>
    /// 使用指定的服务创建聊天 ViewModel。
    /// </summary>
    public ChatViewModel(ChatRoomService chatRoomService)
    {
        _chatRoomService = chatRoomService;

        SendCommand = new SimpleAsyncCommand(SendAsync, () => CanSend);
        StopCommand = new SimpleCommand(StopAutoLoop, () => CanStop);

        _chatRoomService.SpeakingChanged += OnSpeakingChanged;
        _chatRoomService.SessionChanged += OnSessionChanged;
    }

    private void OnSpeakingChanged(object? sender, (ChatRoomRole? Previous, ChatRoomRole? Current) e)
    {
        Dispatcher.UIThread.Post(() => CurrentSpeakerName = e.Current?.Definition.RoleName ?? string.Empty);
    }

    private void OnSessionChanged(object? sender, ChatRoomManager? manager)
    {
        Dispatcher.UIThread.Post(() =>
        {
            // 退订旧会话的 CollectionChanged
            if (_previousSession is not null)
            {
                _previousSession.Messages.CollectionChanged -= OnMessagesCollectionChanged;
                _previousSession = null;
            }

            Messages.Clear();
            IsRunning = false;

            if (manager is not null)
            {
                _previousSession = manager.Session;

                // 订阅新会话的 CollectionChanged，实时镜像消息
                manager.Session.Messages.CollectionChanged += OnMessagesCollectionChanged;

                // 加载已有消息
                foreach (ChatRoomMessage msg in manager.Session.Messages)
                {
                    Messages.Add(new MessageItemViewModel(msg));
                }
            }
        });
    }

    /// <summary>
    /// 当 <see cref="ChatRoomSession.Messages"/> 集合变更时，实时镜像到 UI 的 <see cref="Messages"/> 集合。
    /// 流式消息加入时立即创建 ViewModel，UI 绑定即可感知实时更新。
    /// </summary>
    private void OnMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Add || e.NewItems is null)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            foreach (ChatRoomMessage msg in e.NewItems)
            {
                Messages.Add(new MessageItemViewModel(msg));
            }
        });
    }

    private void StopAutoLoop()
    {
        _autoLoopCts?.Cancel();
        _chatRoomService.StopAutoLoop();
    }

    private async Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            return;
        }

        string text = InputText.Trim();
        InputText = string.Empty;

        // 人类插话
        await _chatRoomService.HumanInterjectAsync(text, "human", "我");

        // 启动自动循环让 AI 角色回复
        if (!IsRunning)
        {
            IsRunning = true;
            _autoLoopCts = new CancellationTokenSource();
            try
            {
                await _chatRoomService.StartAutoLoopAsync(_autoLoopCts.Token);
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
            finally
            {
                _autoLoopCts?.Dispose();
                _autoLoopCts = null;
                IsRunning = false;

                // 确保持久化到磁盘
                await _chatRoomService.SaveAsync().ConfigureAwait(false);
            }
        }
    }

    private void RaiseCommandCanExecuteChanged()
    {
        if (SendCommand is SimpleAsyncCommand sac)
        {
            sac.RaiseCanExecuteChanged();
        }
        if (StopCommand is SimpleCommand sc)
        {
            sc.RaiseCanExecuteChanged();
        }
    }
}
