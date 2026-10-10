using System;
using System.ComponentModel;
using CodingChatRoom.AvaloniaShell.Services;

namespace CodingChatRoom.AvaloniaShell.ViewModels;

/// <summary>
/// 保存独立任务的界面上下文；切换导航不会销毁聊天对象。
/// </summary>
public sealed class WorkTaskItemViewModel : ViewModelBase
{
    private string _displayName;
    private bool _isActive;
    private bool _isEditing;
    private string _editedDisplayName = string.Empty;

    internal WorkTaskItemViewModel(string name, ChatViewModel chat, SessionListViewModel sessions, CodingWorkTaskRuntime? runtime = null, Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        _displayName = name;
        Chat = chat;
        Sessions = sessions;
        Runtime = runtime;
        chat.PropertyChanged += OnChatChanged;
    }

    /// <summary>获取任务标识。</summary>
    public Guid Id { get; }
    /// <summary>获取或设置独立于会话标题的任务名称。</summary>
    public string DisplayName
    {
        get => _displayName;
        set
        {
            if (!string.IsNullOrWhiteSpace(value) && SetField(ref _displayName, value.Trim()))
            {
                Runtime?.Controller.SetWorkTask(Id, _displayName);
            }
        }
    }
    /// <summary>获取任务聊天上下文。</summary>
    public ChatViewModel Chat { get; }
    /// <summary>获取任务历史导航上下文。</summary>
    public SessionListViewModel Sessions { get; }
    /// <summary>获取或设置导航选中状态。</summary>
    public bool IsActive { get => _isActive; internal set => SetField(ref _isActive, value); }
    /// <summary>获取或设置名称编辑状态。</summary>
    public bool IsEditing { get => _isEditing; set => SetField(ref _isEditing, value); }
    /// <summary>获取或设置待确认的任务名称。</summary>
    public string EditedDisplayName { get => _editedDisplayName; set => SetField(ref _editedDisplayName, value); }
    /// <summary>获取包含压缩阶段的任务活动状态。</summary>
    public bool IsWorking => Chat.IsRunning || Chat.IsCompressing || Chat.IsFinalizing || Chat.IsChangingWorkspace;
    /// <summary>获取任务当前状态文本。</summary>
    public string StatusText => IsWorking ? "工作中" : "空闲";
    internal CodingWorkTaskRuntime? Runtime { get; }

    internal void Detach() => Chat.PropertyChanged -= OnChatChanged;

    private void OnChatChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsWorking));
        OnPropertyChanged(nameof(StatusText));
        if (e.PropertyName is nameof(ChatViewModel.NextRunWorkspacePath)
            or nameof(ChatViewModel.SelectedModel)
            or nameof(ChatViewModel.SelectedReasoningEffort))
        {
            OnPropertyChanged(e.PropertyName);
        }
    }
}
