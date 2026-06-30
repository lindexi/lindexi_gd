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

using Avalonia.Controls;
using Avalonia.Threading;

using Microsoft.Extensions.AI;

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
    /// 是否关联了底层 CopilotChatMessage，可以显示片段（思考/工具/子代理等）。
    /// </summary>
    public bool HasCopilotChatMessage => CopilotChatMessage is not null;

    /// <summary>
    /// 直接暴露底层消息的片段集合，供 XAML 的 ItemsControl 绑定。
    /// 可能为 null（人类/系统消息或未关联 CopilotChatMessage）。
    /// </summary>
    public ObservableCollection<ICopilotChatMessageItem>? MessageItems => CopilotChatMessage?.MessageItems;

    /// <summary>
    /// 是否有用量详情。
    /// </summary>
    public bool HasUsageDetails => CopilotChatMessage?.HasUsageDetails ?? false;

    /// <summary>
    /// 用量摘要文本（如：用量 总计 123 输入 45 输出 78）。
    /// </summary>
    public string UsageSummaryText => CopilotChatMessage?.UsageSummaryText ?? string.Empty;

    /// <summary>
    /// 消息的完整内容（包含思考/工具调用等片段），用于右键复制整条消息。
    /// 回退到公开可见的 Content。
    /// </summary>
    public string FullContent => CopilotChatMessage?.FullContent ?? Message.Content;

    /// <summary>
    /// 设计时无参构造函数。用于 Avalonia 设计器预览。
    /// </summary>
    public MessageItemViewModel()
        : this(new ChatRoomMessage())
    {
    }

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

        if (CopilotChatMessage is not null)
        {
            CopilotChatMessage.PropertyChanged += OnCopilotChatMessagePropertyChanged;
            try
            {
                CopilotChatMessage.MessageItems.CollectionChanged += OnCopilotMessageItemsChanged;
            }
            catch
            {
                // ignore if underlying collection doesn't support notifications
            }
        }
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

    private void OnCopilotChatMessagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Bridge important properties to the UI
        switch (e.PropertyName)
        {
            case nameof(CopilotChatMessage.CurrentUsageDetails):
            case nameof(CopilotChatMessage.TotalUsageDetails):
            case nameof(CopilotChatMessage.HasUsageDetails):
            case nameof(CopilotChatMessage.Content):
                break;
            default:
                break;
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            OnPropertyChanged(nameof(UsageSummaryText));
            OnPropertyChanged(nameof(HasUsageDetails));
            OnPropertyChanged(nameof(FullContent));
            OnPropertyChanged(nameof(Content));
        }
        else
        {
            Dispatcher.UIThread.Post(() =>
            {
                OnPropertyChanged(nameof(UsageSummaryText));
                OnPropertyChanged(nameof(HasUsageDetails));
                OnPropertyChanged(nameof(FullContent));
                OnPropertyChanged(nameof(Content));
            });
        }
    }

    private void OnCopilotMessageItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            OnPropertyChanged(nameof(FullContent));
        }
        else
        {
            Dispatcher.UIThread.Post(() => OnPropertyChanged(nameof(FullContent)));
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
                OnPropertyChanged(nameof(CanStop));
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
    /// 是否可以发送消息。允许在自动循环运行时插话，当前正在发言的角色继续说完，
    /// 随后助手立即回话用户，并将助手 @ 的角色加入排队发言。
    /// </summary>
    public bool CanSend => _chatRoomService?.HasActiveSession ?? false;

    /// <summary>
    /// 是否可以停止循环。
    /// </summary>
    public bool CanStop => IsRunning;

    /// <summary>
    /// 发送按钮文本。
    /// </summary>
    public string SendButtonText => "发送";

    /// <summary>
    /// 发送命令。
    /// </summary>
    public ICommand SendCommand { get; }

    /// <summary>
    /// 停止命令。
    /// </summary>
    public ICommand StopCommand { get; }

    /// <summary>
    /// 设计时无参构造函数。用于 Avalonia 设计器预览，填充示例消息数据。
    /// </summary>
    public ChatViewModel()
    {
        _chatRoomService = null!;
        SendCommand = new SimpleCommand(() => { }, () => false);
        StopCommand = new SimpleCommand(() => { }, () => false);

        if (Design.IsDesignMode)
        {
            PopulateDesignTimeData();
        }
    }

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

        // 人类插话：消息立即追加到界面，正在发言的角色继续说完
        await _chatRoomService.HumanInterjectAsync(text, "human", "我");

        // 自动循环未运行时，启动循环让 AI 角色回复
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

    /// <summary>
    /// 同意指定审批工具继续执行。
    /// </summary>
    /// <param name="approvalToolItem">等待审批的工具片段。</param>
    public void ApproveTool(CopilotChatApprovalToolItem approvalToolItem)
    {
        ArgumentNullException.ThrowIfNull(approvalToolItem);
        _chatRoomService.ApproveToolExecution(approvalToolItem);
    }

    /// <summary>
    /// 拒绝指定审批工具继续执行。
    /// </summary>
    /// <param name="approvalToolItem">等待审批的工具片段。</param>
    public void RejectTool(CopilotChatApprovalToolItem approvalToolItem)
    {
        ArgumentNullException.ThrowIfNull(approvalToolItem);
        _chatRoomService.RejectToolExecution(approvalToolItem);
    }

    /// <summary>
    /// 填充设计时示例数据，覆盖所有消息片段类型，方便设计器预览。
    /// </summary>
    private void PopulateDesignTimeData()
    {
        // ---- 人类消息 ----
        var humanMsg = new ChatRoomMessage
        {
            SenderRoleId = "human",
            SenderRoleName = "我",
            IsHumanMessage = true,
            StaticContent = "请帮我分析一下这个项目的架构，并给出优化建议。",
        };
        Messages.Add(new MessageItemViewModel(humanMsg));

        // ---- AI 消息（含思考、工具调用、审批工具、子代理、用量摘要） ----
        var copilotMsg = new CopilotChatMessage(ChatRole.Assistant, "我来分析项目结构。");
        // 思考片段
        copilotMsg.MessageItems.Add(new CopilotChatReasoningItem("用户想了解项目架构，我需要先查看目录结构，然后分析各模块的职责和依赖关系。"));
        // 工具调用片段
        copilotMsg.MessageItems.Add(new CopilotChatToolItem("tool-1", "ListFiles", "/src", "Controllers/\nModels/\nServices/\nViews/"));
        // 审批工具片段（等待审批状态）：设置工作区路径
        copilotMsg.MessageItems.Add(new CopilotChatApprovalToolItem("approval-1", "set_workspace_path", "{\n  \"path\": \"C:\\\\Projects\\\\MyApp\"\n}", "AI 请求设置工作区路径，请确认路径是否安全。"));
        // 审批工具片段（等待审批状态）：删除文件
        copilotMsg.MessageItems.Add(new CopilotChatApprovalToolItem("approval-2", "DeleteFile", "{\n  \"path\": \"/temp/old_config.json\"\n}", "该文件是旧版配置文件，删除后不可恢复，请确认。"));
        // 子代理片段
        var subAgent = new CopilotChatSubAgentItem("sub-1", "代码分析子智能体", "请分析 Controllers 目录下的代码质量", "Controllers 目录共 5 个文件，代码行数约 1200 行。");
        subAgent.MessageItems.Add(new CopilotChatReasoningItem("先统计文件数量，再逐个分析代码规范。"));
        subAgent.MessageItems.Add(new CopilotChatTextItem("正在读取 Controllers 目录..."));
        subAgent.MessageItems.Add(new CopilotChatToolItem("tool-2", "ReadFile", "HomeController.cs", "已读取 HomeController.cs，共 350 行。"));
        subAgent.MessageItems.Add(new CopilotChatToolItem("tool-3", "ReadFile", "ApiController.cs", "已读取 ApiController.cs，共 280 行。"));
        // 嵌套子代理
        var nestedSubAgent = new CopilotChatSubAgentItem("sub-2", "安全审计子智能体", "检查 ApiController 的认证逻辑", "ApiController 使用了 JWT 认证，配置正确。");
        nestedSubAgent.MessageItems.Add(new CopilotChatTextItem("认证中间件配置检查通过。"));
        subAgent.MessageItems.Add(nestedSubAgent);
        copilotMsg.MessageItems.Add(subAgent);
        // 文本片段
        copilotMsg.MessageItems.Add(new CopilotChatTextItem("项目整体架构清晰，采用 MVC 分层设计。建议将 Services 层进一步拆分为 Application 和 Domain 两层以提高可维护性。"));

        // 设置用量摘要
        copilotMsg.AppendUsageDetails(new UsageDetails
        {
            InputTokenCount = 456,
            OutputTokenCount = 1289,
            TotalTokenCount = 1745,
        });

        var aiMsg = new ChatRoomMessage
        {
            SenderRoleId = "assistant",
            SenderRoleName = "助手",
            CopilotChatMessage = copilotMsg,
            StaticContent = copilotMsg.Content,
        };
        Messages.Add(new MessageItemViewModel(aiMsg));

        // ---- 系统消息 ----
        var sysMsg = ChatRoomMessage.CreateSystem("角色「代码审查员」发言失败：模型不可用，已跳过。");
        Messages.Add(new MessageItemViewModel(sysMsg));

        // ---- 流式消息（模拟正在输入） ----
        var streamingCopilotMsg = new CopilotChatMessage(ChatRole.Assistant, "正在生成详细报告...");
        streamingCopilotMsg.MessageItems.Add(new CopilotChatReasoningItem("需要整理之前的分析结果，生成一份结构化的报告..."));
        var streamingMsg = new ChatRoomMessage
        {
            SenderRoleId = "reviewer",
            SenderRoleName = "代码审查员",
            CopilotChatMessage = streamingCopilotMsg,
            StaticContent = streamingCopilotMsg.Content,
            IsStreaming = true,
        };
        Messages.Add(new MessageItemViewModel(streamingMsg));

        // 设置设计时运行状态
        _isRunning = true;
        _currentSpeakerName = "代码审查员";
    }
}
