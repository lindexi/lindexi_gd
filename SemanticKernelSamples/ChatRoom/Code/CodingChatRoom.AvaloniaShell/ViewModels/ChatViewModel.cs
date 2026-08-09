using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;

using AgentLib;
using AgentLib.Model;

using CodingChatRoom.AvaloniaShell.Services;

using Microsoft.Extensions.AI;

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
    private bool _isLoopIterationEnabled;
    private bool _isDisposed;

    /// <summary>
    /// 初始化尚未接入模型发送的聊天视图骨架。
    /// </summary>
    public ChatViewModel()
    {
        _modelStatusText = "等待应用初始化";
        SendCommand = new SimpleCommand(static () => { }, static () => false);
        CompressConversationCommand = new SimpleCommand(static () => { }, static () => false);
        StopCommand = new SimpleCommand(static () => { }, static () => false);
        ApplyWorkspaceCommand = new SimpleCommand(static () => { }, static () => false);
        PendingImages.CollectionChanged += OnPendingImagesCollectionChanged;
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
        CompressConversationCommand = new SimpleAsyncCommand(CompressConversationAsync, () => CanCompressConversation);
        StopCommand = new SimpleCommand(application.StopActiveRun, () => IsRunning);
        ApplyWorkspaceCommand = new SimpleCommand(static () => { }, static () => false);
        _chatManager.PropertyChanged += OnChatManagerPropertyChanged;
        _application.StateChanged += OnApplicationStateChanged;
        PendingImages.CollectionChanged += OnPendingImagesCollectionChanged;
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
        CompressConversationCommand = new SimpleAsyncCommand(CompressConversationAsync, () => CanCompressConversation);
        StopCommand = new SimpleCommand(application.StopActiveRun, () => IsRunning);
        ApplyWorkspaceCommand = new SimpleAsyncCommand(ApplyWorkspaceAsync, () => CanApplyWorkspace);
        _chatManager.PropertyChanged += OnChatManagerPropertyChanged;
        _application.StateChanged += OnApplicationStateChanged;
        _workspaceController.PropertyChanged += OnWorkspaceControllerPropertyChanged;
        PendingImages.CollectionChanged += OnPendingImagesCollectionChanged;
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
    /// 获取待随下一条消息发送的图片附件。
    /// </summary>
    public ObservableCollection<ImageAttachmentViewModel> PendingImages { get; } = [];

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
    /// 获取或设置下一次发送是否启用循环迭代。
    /// </summary>
    public bool IsLoopIterationEnabled
    {
        get => _isLoopIterationEnabled;
        set
        {
            if (SetField(ref _isLoopIterationEnabled, value))
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
    /// 获取压缩当前对话命令。
    /// </summary>
    public ICommand CompressConversationCommand { get; }

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
    public bool CanSend => _application?.CanSend == true
        && (IsLoopIterationEnabled
            ? !string.IsNullOrWhiteSpace(InputText)
            : !string.IsNullOrWhiteSpace(InputText) || PendingImages.Count > 0);

    /// <summary>
    /// 获取当前对话是否可以压缩。
    /// </summary>
    public bool CanCompressConversation => _application?.CanCompressConversation == true;

    /// <summary>
    /// 获取是否存在待发送图片。
    /// </summary>
    public bool HasPendingImages => PendingImages.Count > 0;

    /// <summary>
    /// 获取是否正在运行。
    /// </summary>
    public bool IsRunning => _application?.IsRunActive == true;

    /// <summary>
    /// 获取当前是否正在压缩对话。
    /// </summary>
    public bool IsCompressing => _application?.IsCompressionActive == true;

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
    /// 尝试添加一张待发送图片。
    /// </summary>
    /// <param name="fileName">图片文件名。</param>
    /// <param name="data">图片二进制数据。</param>
    /// <returns>图片格式受支持且数据非空时返回 <see langword="true"/>。</returns>
    public bool TryAddImageAttachment(string fileName, ReadOnlyMemory<byte> data)
    {
        if (!ImageAttachmentViewModel.TryCreate(fileName, data, out ImageAttachmentViewModel? attachment))
        {
            return false;
        }

        PendingImages.Add(attachment);
        return true;
    }

    /// <summary>
    /// 移除一张待发送图片。
    /// </summary>
    /// <param name="attachment">要移除的图片附件。</param>
    public void RemoveImageAttachment(ImageAttachmentViewModel attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);
        PendingImages.Remove(attachment);
    }

    internal Task AddSystemNoticeAsync(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        return AddSystemMessageAsync(_subscribedSession, content);
    }

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

        CopilotChatSession? session = _subscribedSession;
        try
        {
            await _workspaceController.ChangeWorkspaceAsync(WorkspaceInput).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            const string message = "工作路径切换已取消。";
            Trace.TraceInformation(message);
            await AddSystemMessageAsync(session, message).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            Trace.TraceError($"工作路径切换失败：{exception}");
            await AddSystemMessageAsync(session, $"工作路径切换失败：{exception.Message}").ConfigureAwait(true);
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
        PendingImages.CollectionChanged -= OnPendingImagesCollectionChanged;
        PendingImages.Clear();
        _isDisposed = true;
    }

    private void OnApplicationStateChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(CanSend));
        OnPropertyChanged(nameof(CanCompressConversation));
        OnPropertyChanged(nameof(IsRunning));
        OnPropertyChanged(nameof(IsCompressing));
        RaiseCommandCanExecuteChanged();
    }

    private async Task CompressConversationAsync()
    {
        if (_application is null || !CanCompressConversation)
        {
            return;
        }

        CopilotChatSession? session = _subscribedSession;
        _runStatusText = "正在压缩对话";
        OnPropertyChanged(nameof(StatusText));
        try
        {
            await _application.CompressConversationAsync().ConfigureAwait(true);
            _runStatusText = "对话压缩完成";
            await AddSystemMessageAsync(session, "对话压缩完成。").ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            _runStatusText = "对话压缩已取消";
            await AddSystemMessageAsync(session, "对话压缩已取消。").ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            _runStatusText = $"对话压缩失败：{exception.Message}";
            await AddSystemMessageAsync(session, _runStatusText).ConfigureAwait(true);
        }
        finally
        {
            OnPropertyChanged(nameof(StatusText));
        }
    }

    private async Task SendAsync()
    {
        if (_application is null || !CanSend)
        {
            return;
        }

        var contents = new List<AIContent>(PendingImages.Count + 1);
        if (!string.IsNullOrWhiteSpace(InputText))
        {
            contents.Add(new TextContent(InputText));
        }

        foreach (ImageAttachmentViewModel attachment in PendingImages)
        {
            contents.Add(new DataContent(attachment.Data.ToMemory(), attachment.MimeType));
        }

        CopilotChatSession? session = _subscribedSession;
        string loopPrompt = InputText;
        bool runLoopIteration = IsLoopIterationEnabled;
        if (runLoopIteration)
        {
            IsLoopIterationEnabled = false;
        }

        InputText = string.Empty;
        PendingImages.Clear();
        _runStatusText = "正在运行";
        OnPropertyChanged(nameof(StatusText));
        try
        {
            if (runLoopIteration)
            {
                await _application.RunLoopIterationAsync(loopPrompt).ConfigureAwait(true);
            }
            else
            {
                await _application.SendMessageAsync(contents).ConfigureAwait(true);
            }

            _runStatusText = null;
        }
        catch (OperationCanceledException)
        {
            _runStatusText = "已停止";
            await AddSystemMessageAsync(session, "运行已停止。").ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            _runStatusText = $"运行失败：{exception.Message}";
            await AddSystemMessageAsync(session, _runStatusText).ConfigureAwait(true);
        }
        finally
        {
            OnPropertyChanged(nameof(StatusText));
        }
    }

    private void OnPendingImagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasPendingImages));
        OnPropertyChanged(nameof(CanSend));
        RaiseCommandCanExecuteChanged();
    }

    private void RaiseCommandCanExecuteChanged()
    {
        if (SendCommand is SimpleAsyncCommand sendCommand)
        {
            sendCommand.RaiseCanExecuteChanged();
        }

        if (CompressConversationCommand is SimpleAsyncCommand compressConversationCommand)
        {
            compressConversationCommand.RaiseCanExecuteChanged();
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
            OnPropertyChanged(nameof(CanCompressConversation));
            RaiseCommandCanExecuteChanged();
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

    private static Task AddSystemMessageAsync(CopilotChatSession? session, string content)
    {
        if (session is null)
        {
            return Task.CompletedTask;
        }

        var message = new CopilotChatMessage(ChatRole.System, content)
        {
            IsPresetInfo = true,
        };
        return session.AddMessageAsync(message);
    }
}
