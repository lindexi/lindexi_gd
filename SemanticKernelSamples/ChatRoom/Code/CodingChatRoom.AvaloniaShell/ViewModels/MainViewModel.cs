using System;

namespace CodingChatRoom.AvaloniaShell.ViewModels;

/// <summary>
/// 组合历史会话与聊天区域。
/// </summary>
public sealed class MainViewModel : ViewModelBase
{
    /// <summary>
    /// 初始化 Shell 视图模型。
    /// </summary>
    public MainViewModel()
        : this(new SessionListViewModel(), new ChatViewModel())
    {
    }

    /// <summary>
    /// 使用指定子 ViewModel 初始化 Shell。
    /// </summary>
    public MainViewModel(SessionListViewModel sessionListViewModel, ChatViewModel chatViewModel)
    {
        ArgumentNullException.ThrowIfNull(sessionListViewModel);
        ArgumentNullException.ThrowIfNull(chatViewModel);

        SessionListViewModel = sessionListViewModel;
        ChatViewModel = chatViewModel;
    }

    /// <summary>
    /// 获取历史会话列表 ViewModel。
    /// </summary>
    public SessionListViewModel SessionListViewModel { get; }

    /// <summary>
    /// 获取聊天区域 ViewModel。
    /// </summary>
    public ChatViewModel ChatViewModel { get; }
}
