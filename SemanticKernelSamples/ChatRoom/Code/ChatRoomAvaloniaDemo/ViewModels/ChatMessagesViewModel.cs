using AgentLib.ChatRoom.Model;
using AgentLib.Model;

using ChatRoomAvaloniaDemo.Services;

using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace ChatRoomAvaloniaDemo.ViewModels;

/// <summary>
/// 聊天消息 ViewModel。管理当前聊天室的消息列表显示、自动循环启停和人类插话。
/// </summary>
public sealed class ChatMessagesViewModel : NotifyBase
{
    private readonly ChatRoomService _chatRoomService;
    private string _humanInputText = string.Empty;
    private bool _isRunning;
    private bool _isSpeaking;
    private CancellationTokenSource? _loopCancellationTokenSource;
    private Task? _currentLoopTask;

    /// <summary>
    /// 创建聊天消息 ViewModel。
    /// </summary>
    /// <param name="chatRoomService">聊天室服务。</param>
    public ChatMessagesViewModel(ChatRoomService chatRoomService)
    {
        _chatRoomService = chatRoomService ?? throw new ArgumentNullException(nameof(chatRoomService));

        SendCommand = new DelegateCommand(SendHumanMessage, () => !string.IsNullOrWhiteSpace(HumanInputText));
        StopCommand = new DelegateCommand(StopLoop, () => CanStop);
    }

    /// <summary>
    /// 当前聊天室的消息列表。直接绑定到 <see cref="ChatRoomManager.Session.Messages"/>。
    /// </summary>
    public ObservableCollection<ChatRoomMessage> Messages =>
        _chatRoomService.ChatRoomManager?.Session.Messages ?? new ObservableCollection<ChatRoomMessage>();

    /// <summary>
    /// 人类输入文本（双向绑定）。
    /// </summary>
    public string HumanInputText
    {
        get => _humanInputText;
        set
        {
            if (SetField(ref _humanInputText, value))
            {
                ((DelegateCommand)SendCommand).RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// 是否正在运行自动循环。
    /// </summary>
    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetField(ref _isRunning, value))
            {
                OnPropertyChanged(nameof(CanStop));
                ((DelegateCommand)StopCommand).RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// 是否有角色正在发言。
    /// </summary>
    public bool IsSpeaking
    {
        get => _isSpeaking;
        set => SetField(ref _isSpeaking, value);
    }

    /// <summary>是否可以停止。</summary>
    public bool CanStop => IsRunning;

    /// <summary>发送人类消息命令。</summary>
    public System.Windows.Input.ICommand SendCommand { get; }

    /// <summary>停止自动循环命令。</summary>
    public System.Windows.Input.ICommand StopCommand { get; }

    /// <summary>
    /// 绑定到活跃的 <see cref="ChatRoomManager"/>，监听其事件。
    /// </summary>
    public void BindToManager()
    {
        var manager = _chatRoomService.ChatRoomManager;
        if (manager is null)
        {
            return;
        }

        OnPropertyChanged(nameof(Messages));

        manager.OnMessageAdded += (_, _) =>
        {
            OnPropertyChanged(nameof(Messages));
        };

        manager.OnSpeakingChanged += (_, args) =>
        {
            IsSpeaking = args.CurrentSpeaker is not null;
        };
    }

    private async void SendHumanMessage()
    {
        if (string.IsNullOrWhiteSpace(HumanInputText))
        {
            return;
        }

        string text = HumanInputText;
        HumanInputText = string.Empty;

        // 如果正在循环中，先停止并等待旧循环完全退出
        await StopLoopAsync();

        // 插入人类消息到聊天室
        await _chatRoomService.HumanInterjectAsync(text);
        OnPropertyChanged(nameof(Messages));

        // 自动启动新循环，让 AI 们开始回复
        _currentLoopTask = StartLoopInternalAsync();
    }

    private async Task StartLoopInternalAsync()
    {
        if (IsRunning)
        {
            return;
        }

        _loopCancellationTokenSource = new CancellationTokenSource();
        IsRunning = true;

        try
        {
            await _chatRoomService.StartAutoLoopAsync(_loopCancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            // 正常取消
        }
        finally
        {
            IsRunning = false;
            _loopCancellationTokenSource?.Dispose();
            _loopCancellationTokenSource = null;
        }
    }

    private void StopLoop()
    {
        _loopCancellationTokenSource?.Cancel();
        _chatRoomService.Stop();
    }

    private async Task StopLoopAsync()
    {
        if (!IsRunning && _currentLoopTask is null)
        {
            return;
        }

        StopLoop();

        if (_currentLoopTask is not null)
        {
            try
            {
                await _currentLoopTask;
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
            _currentLoopTask = null;
        }
    }
}