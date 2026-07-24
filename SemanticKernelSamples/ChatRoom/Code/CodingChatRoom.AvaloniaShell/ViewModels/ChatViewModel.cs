using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;

using AgentLib;
using AgentLib.Model;

using CodingChatRoom.AvaloniaShell.Services;

namespace CodingChatRoom.AvaloniaShell.ViewModels;

/// <summary>
/// 表示右侧编程助手聊天区域。
/// </summary>
public sealed class ChatViewModel : ViewModelBase, IDisposable
{
    private readonly CopilotChatManager? _chatManager;
    private readonly CodingChatApplication? _application;
    private readonly CodingWorkspaceController? _workspaceController;
    private readonly string _modelStatusText;
    private CopilotChatSession? _subscribedSession;
    private string _inputText = string.Empty;
    private string? _runStatusText;
    private bool _isDisposed;

    /// <summary>
    /// 初始化尚未接入模型发送的聊天视图骨架。
    /// </summary>
    public ChatViewModel()
    {
        _modelStatusText = "等待应用初始化";
        SendCommand = new SimpleCommand(static () => { }, static () => false);
        StopCommand = new SimpleCommand(static () => { }, static () => false);
        ApplyWorkspaceCommand = new SimpleCommand(static () => { }, static () => false);
    }

    internal ChatViewModel(CopilotChatManager chatManager, string statusText)
    {
        ArgumentNullException.ThrowIfNull(chatManager);
        _chatManager = chatManager;
        _modelStatusText = statusText;
        SendCommand = new SimpleCommand(static () => { }, static () => false);
        StopCommand = new SimpleCommand(static () => { }, static () => false);
        ApplyWorkspaceCommand = new SimpleCommand(static () => { }, static () => false);
        _chatManager.PropertyChanged += OnChatManagerPropertyChanged;
        AttachSession(_chatManager.SelectedSession);
    }

    internal ChatViewModel(
        CopilotChatManager chatManager,
        CodingChatApplication application,
        string statusText)
    {
        ArgumentNullException.ThrowIfNull(chatManager);
        ArgumentNullException.ThrowIfNull(application);
        _chatManager = chatManager;
        _application = application;
        _modelStatusText = statusText;
        SendCommand = new SimpleAsyncCommand(SendAsync, () => CanSend);
        StopCommand = new SimpleCommand(application.StopActiveRun, () => IsRunning);
        ApplyWorkspaceCommand = new SimpleCommand(static () => { }, static () => false);
        _chatManager.PropertyChanged += OnChatManagerPropertyChanged;
        _application.StateChanged += OnApplicationStateChanged;
        AttachSession(_chatManager.SelectedSession);
    }

    internal ChatViewModel(
        CopilotChatManager chatManager,
        CodingChatApplication application,
        CodingWorkspaceController workspaceController,
        string statusText)
    {
        ArgumentNullException.ThrowIfNull(chatManager);
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(workspaceController);
        _chatManager = chatManager;
        _application = application;
        _workspaceController = workspaceController;
        _modelStatusText = statusText;
        SendCommand = new SimpleAsyncCommand(SendAsync, () => CanSend);
        StopCommand = new SimpleCommand(application.StopActiveRun, () => IsRunning);
        ApplyWorkspaceCommand = new SimpleAsyncCommand(ApplyWorkspaceAsync, () => CanApplyWorkspace);
        _chatManager.PropertyChanged += OnChatManagerPropertyChanged;
        _application.StateChanged += OnApplicationStateChanged;
        _workspaceController.PropertyChanged += OnWorkspaceControllerPropertyChanged;
        AttachSession(_chatManager.SelectedSession);
    }

    /// <summary>
    /// 获取当前会话标题。
    /// </summary>
    public string CurrentSessionTitle => _subscribedSession?.Title ?? "编程助手";

    /// <summary>
    /// 获取当前会话 ID。
    /// </summary>
    public Guid CurrentSessionId => _subscribedSession?.SessionId ?? Guid.Empty;

    /// <summary>
    /// 获取当前状态说明。
    /// </summary>
    public string StatusText => _runStatusText ?? _modelStatusText;

    /// <summary>
    /// 获取消息投影集合。
    /// </summary>
    public ObservableCollection<MessageItemViewModel> Messages { get; } = [];

    /// <summary>
    /// 获取或设置输入文本。
    /// </summary>
    public string InputText
    {
        get => _inputText;
        set
        {
            if (SetField(ref _inputText, value))
            {
                OnPropertyChanged(nameof(CanSend));
                RaiseCommandCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// 获取发送命令。
    /// </summary>
    public ICommand SendCommand { get; }

    /// <summary>
    /// 获取停止命令。
    /// </summary>
    public ICommand StopCommand { get; }

    /// <summary>
    /// 获取应用工作路径命令。
    /// </summary>
    public ICommand ApplyWorkspaceCommand { get; }

    /// <summary>
    /// 获取是否可发送消息。
    /// </summary>
    public bool CanSend => _application?.CanSend == true && !string.IsNullOrWhiteSpace(InputText);

    /// <summary>
    /// 获取是否正在运行。
    /// </summary>
    public bool IsRunning => _application?.IsRunActive == true;

    /// <summary>
    /// 获取或设置待应用的工作路径。
    /// </summary>
    public string WorkspaceInput
    {
        get => _workspaceController?.WorkspaceInput ?? string.Empty;
        set
        {
            if (_workspaceController is not null)
            {
                _workspaceController.WorkspaceInput = value;
            }
        }
    }

    /// <summary>
    /// 获取当前已提交的工作路径。
    /// </summary>
    public string? CommittedWorkspacePath => _workspaceController?.CommittedWorkspacePath;

    /// <summary>
    /// 获取工作路径状态文本。
    /// </summary>
    public string WorkspaceStatusText => _workspaceController?.StatusText ?? "工作路径功能尚未初始化";

    /// <summary>
    /// 获取是否正在切换工作路径。
    /// </summary>
    public bool IsChangingWorkspace => _workspaceController?.IsChangingWorkspace == true;

    /// <summary>
    /// 获取当前是否可以应用工作路径。
    /// </summary>
    public bool CanApplyWorkspace => _workspaceController is not null && !IsChangingWorkspace;

    /// <summary>
    /// 同意指定审批工具继续执行。
    /// </summary>
    /// <param name="approvalToolItem">等待审批的工具片段。</param>
    public void ApproveTool(CopilotChatApprovalToolItem approvalToolItem)
    {
        ArgumentNullException.ThrowIfNull(approvalToolItem);
        _chatManager?.ApproveToolExecution(approvalToolItem);
    }

    /// <summary>
    /// 拒绝指定审批工具继续执行。
    /// </summary>
    /// <param name="approvalToolItem">等待审批的工具片段。</param>
    public void RejectTool(CopilotChatApprovalToolItem approvalToolItem)
    {
        ArgumentNullException.ThrowIfNull(approvalToolItem);
        _chatManager?.RejectToolExecution(approvalToolItem);
    }

    private void OnWorkspaceControllerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CodingWorkspaceController.WorkspaceInput))
        {
            OnPropertyChanged(nameof(WorkspaceInput));
        }
        else if (e.PropertyName == nameof(CodingWorkspaceController.CommittedWorkspacePath))
        {
            OnPropertyChanged(nameof(CommittedWorkspacePath));
        }
        else if (e.PropertyName == nameof(CodingWorkspaceController.StatusText))
        {
            OnPropertyChanged(nameof(WorkspaceStatusText));
        }
        else if (e.PropertyName == nameof(CodingWorkspaceController.IsChangingWorkspace))
        {
            OnPropertyChanged(nameof(IsChangingWorkspace));
            OnPropertyChanged(nameof(CanApplyWorkspace));
            RaiseCommandCanExecuteChanged();
        }
    }

    private async Task ApplyWorkspaceAsync()
    {
        if (_workspaceController is null || !CanApplyWorkspace)
        {
            return;
        }

        try
        {
            await _workspaceController.ChangeWorkspaceAsync(WorkspaceInput).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            Trace.TraceInformation("工作路径切换已取消。");
        }
        catch (Exception exception)
        {
            Trace.TraceError($"工作路径切换失败：{exception}");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        if (_chatManager is not null)
        {
            _chatManager.PropertyChanged -= OnChatManagerPropertyChanged;
        }

        if (_application is not null)
        {
            _application.StateChanged -= OnApplicationStateChanged;
        }

        if (_workspaceController is not null)
        {
            _workspaceController.PropertyChanged -= OnWorkspaceControllerPropertyChanged;
        }

        DetachSession();
        ClearMessages();
        _isDisposed = true;
    }

    private void OnApplicationStateChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(CanSend));
        OnPropertyChanged(nameof(IsRunning));
        RaiseCommandCanExecuteChanged();
    }

    private async Task SendAsync()
    {
        if (_application is null || !CanSend)
        {
            return;
        }

        string prompt = InputText;
        InputText = string.Empty;
        _runStatusText = "正在运行";
        OnPropertyChanged(nameof(StatusText));
        try
        {
            await _application.SendMessageAsync(prompt).ConfigureAwait(true);
            _runStatusText = null;
        }
        catch (OperationCanceledException)
        {
            _runStatusText = "已停止";
        }
        catch (Exception exception)
        {
            _runStatusText = $"运行失败：{exception.Message}";
        }
        finally
        {
            OnPropertyChanged(nameof(StatusText));
        }
    }

    private void RaiseCommandCanExecuteChanged()
    {
        if (SendCommand is SimpleAsyncCommand sendCommand)
        {
            sendCommand.RaiseCanExecuteChanged();
        }

        if (StopCommand is SimpleCommand stopCommand)
        {
            stopCommand.RaiseCanExecuteChanged();
        }

        if (ApplyWorkspaceCommand is SimpleAsyncCommand applyWorkspaceCommand)
        {
            applyWorkspaceCommand.RaiseCanExecuteChanged();
        }
    }

    private void OnChatManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CopilotChatManager.SelectedSession) && _chatManager is not null)
        {
            AttachSession(_chatManager.SelectedSession);
        }
    }

    private void AttachSession(CopilotChatSession session)
    {
        if (ReferenceEquals(_subscribedSession, session))
        {
            return;
        }

        DetachSession();
        ClearMessages();
        _subscribedSession = session;
        _subscribedSession.PropertyChanged += OnSessionPropertyChanged;
        _subscribedSession.ChatMessages.CollectionChanged += OnChatMessagesCollectionChanged;
        foreach (CopilotChatMessage message in _subscribedSession.ChatMessages)
        {
            Messages.Add(new MessageItemViewModel(message));
        }

        OnPropertyChanged(nameof(CurrentSessionId));
        OnPropertyChanged(nameof(CurrentSessionTitle));
    }

    private void DetachSession()
    {
        if (_subscribedSession is null)
        {
            return;
        }

        _subscribedSession.PropertyChanged -= OnSessionPropertyChanged;
        _subscribedSession.ChatMessages.CollectionChanged -= OnChatMessagesCollectionChanged;
        _subscribedSession = null;
    }

    private void OnSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CopilotChatSession.Title))
        {
            OnPropertyChanged(nameof(CurrentSessionTitle));
        }
    }

    private void OnChatMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null)
        {
            int insertionIndex = e.NewStartingIndex >= 0 ? e.NewStartingIndex : Messages.Count;
            foreach (CopilotChatMessage message in e.NewItems)
            {
                Messages.Insert(insertionIndex++, new MessageItemViewModel(message));
            }

            return;
        }

        RebuildMessages();
    }

    private void RebuildMessages()
    {
        ClearMessages();
        if (_subscribedSession is null)
        {
            return;
        }

        foreach (CopilotChatMessage message in _subscribedSession.ChatMessages)
        {
            Messages.Add(new MessageItemViewModel(message));
        }
    }

    private void ClearMessages()
    {
        foreach (MessageItemViewModel message in Messages)
        {
            message.Dispose();
        }

        Messages.Clear();
    }
}
