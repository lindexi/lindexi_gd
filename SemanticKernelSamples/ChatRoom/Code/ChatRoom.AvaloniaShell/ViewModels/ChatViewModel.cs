using System;
using System.Collections.ObjectModel;
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
/// </summary>
public sealed class MessageItemViewModel : NotifyBase
{
    private readonly string _staticContent;
    private CopilotChatMessage? _copilotChatMessage;

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
    public CopilotChatMessage? CopilotChatMessage
    {
        get => _copilotChatMessage;
        private set
        {
            if (_copilotChatMessage is not null)
            {
                _copilotChatMessage.PropertyChanged -= OnCopilotChatMessagePropertyChanged;
            }

            _copilotChatMessage = value;

            if (_copilotChatMessage is not null)
            {
                _copilotChatMessage.PropertyChanged += OnCopilotChatMessagePropertyChanged;
            }

            OnPropertyChanged(nameof(Content));
        }
    }

    /// <summary>
    /// 消息内容。关联了 <see cref="CopilotChatMessage"/> 时返回其实时 Content，否则返回静态内容。
    /// </summary>
    public string Content => CopilotChatMessage?.Content ?? _staticContent;

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
    /// 角色名首字（头像显示）。
    /// </summary>
    public string Initial => string.IsNullOrEmpty(SenderRoleName) ? "?" : SenderRoleName[..1].ToUpperInvariant();

    /// <summary>
    /// 时间显示文本。
    /// </summary>
    public string TimeText => Timestamp.ToString("HH:mm:ss");

    /// <summary>
    /// 从 <see cref="ChatRoomMessage"/> 创建消息项。
    /// </summary>
    public MessageItemViewModel(ChatRoomMessage message)
    {
        MessageId = message.MessageId;
        SenderRoleId = message.SenderRoleId;
        SenderRoleName = message.SenderRoleName;
        _staticContent = message.Content;
        Timestamp = message.Timestamp;
        IsHumanMessage = message.IsHumanMessage;
        IsSystemMessage = message.IsSystemMessage;
        CopilotChatMessage = message.CopilotChatMessage;
    }

    private void OnCopilotChatMessagePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(CopilotChatMessage.Content))
        {
            return;
        }

        // CopilotChatMessage 的 PropertyChanged 在后台线程触发（RunStreamingAsync 循环），
        // 需要调度到 UI 线程才能让 Avalonia 绑定更新界面。
        if (Dispatcher.UIThread.CheckAccess())
        {
            OnPropertyChanged(nameof(Content));
        }
        else
        {
            Dispatcher.UIThread.Post(() => OnPropertyChanged(nameof(Content)));
        }
    }
}

/// <summary>
/// 中栏聊天区 ViewModel。
/// </summary>
public sealed class ChatViewModel : ViewModelBase
{
    private readonly ChatRoomService _chatRoomService;
    private string _inputText = string.Empty;
    private bool _isRunning;
    private string _currentSpeakerName = string.Empty;
    private CancellationTokenSource? _autoLoopCts;
    private ChatRoomSession? _session;
    private MessageItemViewModel? _streamingMessageItem;

    /// <summary>
    /// 消息列表。
    /// </summary>
    public ObservableCollection<MessageItemViewModel> Messages { get; } = [];

    /// <summary>
    /// 当前聊天室会话。UI 可绑定到 <see cref="ChatRoomSession.StreamingMessage"/> 感知流式更新。
    /// </summary>
    public ChatRoomSession? Session
    {
        get => _session;
        private set
        {
            if (_session is not null)
            {
                _session.PropertyChanged -= OnSessionPropertyChanged;
            }

            if (SetField(ref _session, value))
            {
                if (_session is not null)
                {
                    _session.PropertyChanged += OnSessionPropertyChanged;
                }

                UpdateStreamingMessageItem();
            }
        }
    }

    /// <summary>
    /// 流式消息的 ViewModel 表示。为 <see langword="null"/> 时没有角色在流式发言。
    /// UI 将此项追加到消息列表末尾显示。
    /// </summary>
    public MessageItemViewModel? StreamingMessageItem
    {
        get => _streamingMessageItem;
        private set => SetField(ref _streamingMessageItem, value);
    }

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

        _chatRoomService.MessageAdded += OnMessageAdded;
        _chatRoomService.SpeakingChanged += OnSpeakingChanged;
        _chatRoomService.SessionChanged += OnSessionChanged;
    }

    private void OnMessageAdded(object? sender, ChatRoomMessage e)
    {
        Dispatcher.UIThread.Post(() => Messages.Add(new MessageItemViewModel(e)));
    }

    private void OnSpeakingChanged(object? sender, (ChatRoomRole? Previous, ChatRoomRole? Current) e)
    {
        Dispatcher.UIThread.Post(() => CurrentSpeakerName = e.Current?.Definition.RoleName ?? string.Empty);
    }

    private void OnSessionChanged(object? sender, ChatRoomManager? manager)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Messages.Clear();
            StreamingMessageItem = null;
            IsRunning = false;
            Session = manager?.Session;

            if (manager is not null)
            {
                foreach (ChatRoomMessage msg in manager.Session.Messages)
                {
                    Messages.Add(new MessageItemViewModel(msg));
                }
            }
        });
    }

    /// <summary>
    /// 监听 Session 的 StreamingMessage 属性变更，自动创建/清除 StreamingMessageItem。
    /// </summary>
    private void OnSessionPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChatRoomSession.StreamingMessage))
        {
            Dispatcher.UIThread.Post(UpdateStreamingMessageItem);
        }
    }

    /// <summary>
    /// 根据 Session.StreamingMessage 同步 StreamingMessageItem。
    /// </summary>
    private void UpdateStreamingMessageItem()
    {
        ChatRoomMessage? streaming = _session?.StreamingMessage;
        StreamingMessageItem = streaming is not null
            ? new MessageItemViewModel(streaming)
            : null;
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
        await _chatRoomService.HumanInterjectAsync(text, "human", "我").ConfigureAwait(false);

        // 启动自动循环让 AI 角色回复
        if (!IsRunning)
        {
            IsRunning = true;
            _autoLoopCts = new CancellationTokenSource();
            try
            {
                await _chatRoomService.StartAutoLoopAsync(_autoLoopCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
            finally
            {
                _autoLoopCts?.Dispose();
                _autoLoopCts = null;
                Dispatcher.UIThread.Post(() => IsRunning = false);
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
