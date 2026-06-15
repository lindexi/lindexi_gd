using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AgentLib.Model;

/// <summary>
/// 表示一个 Copilot 聊天会话，包含会话元数据和消息列表。
/// </summary>
public sealed class CopilotChatSession : NotifyBase
{
    private const int MaxTitleLength = 20;
    private string _title = "新会话";
    private TitleSource _titleSource;
    private AgentSession? _agentSession;
    private IMainThreadDispatcher? _mainThreadDispatcher;

    /// <summary>
    /// 当前标题的来源。
    /// </summary>
    internal TitleSource TitleSourceValue => _titleSource;

    /// <summary>
    /// 使用新的会话 ID 和当前时间创建会话。
    /// </summary>
    public CopilotChatSession()
        : this(Guid.NewGuid(), DateTimeOffset.Now)
    {
    }

    /// <summary>
    /// 使用指定的会话 ID 和开始时间创建会话。
    /// </summary>
    /// <param name="sessionId">会话唯一标识符。</param>
    /// <param name="startedTime">会话开始时间。</param>
    public CopilotChatSession(Guid sessionId, DateTimeOffset startedTime)
    {
        SessionId = sessionId;
        StartedTime = startedTime;
    }

    /// <summary>
    /// 会话唯一标识符。
    /// </summary>
    public Guid SessionId { get; }

    /// <summary>
    /// 会话开始时间。
    /// </summary>
    public DateTimeOffset StartedTime { get; }

    /// <summary>
    /// 会话中的聊天消息列表。
    /// </summary>
    public ObservableCollection<CopilotChatMessage> ChatMessages { get; } = [];

    /// <summary>
    /// 当前会话关联的代理会话。当不携带历史时为 <see langword="null"/>。
    /// </summary>
    public AgentSession? AgentSession
    {
        get => _agentSession;
        private set => SetField(ref _agentSession, value);
    }

    /// <summary>
    /// 主线程调度器。设置后，<see cref="AddMessage"/> 将通过调度器回到主线程修改 <see cref="ChatMessages"/>。
    /// 为 <see langword="null"/> 时直接在当前线程执行。仅在构造期可设置。
    /// </summary>
    public IMainThreadDispatcher? MainThreadDispatcher
    {
        get => _mainThreadDispatcher;
        init => _mainThreadDispatcher = value;
    }

    /// <summary>
    /// 会话标题。默认为"新会话"，在收到第一条用户消息后自动生成。
    /// </summary>
    public string Title
    {
        get => _title;
        private set
        {
            if (!SetField(ref _title, value))
            {
                return;
            }

            OnPropertyChanged(nameof(DisplayText));
        }
    }

    /// <summary>
    /// 用于显示的文本，包含标题和开始时间。
    /// </summary>
    public string DisplayText => $"{Title} {StartedTime:MM-dd HH:mm}";

    /// <summary>
    /// 向会话中添加一条聊天消息，并尝试更新会话标题。
    /// 如果设置了 <see cref="MainThreadDispatcher"/>，将调度到主线程执行。
    /// </summary>
    /// <param name="chatMessage">要添加的聊天消息。</param>
    public async Task AddMessageAsync(CopilotChatMessage chatMessage)
    {
        if (_mainThreadDispatcher is not null)
        {
            await _mainThreadDispatcher.InvokeAsync(() =>
            {
                AddMessageCore(chatMessage);
                return Task.CompletedTask;
            });
            return;
        }

        AddMessageCore(chatMessage);
    }

    /// <summary>
    /// 同步添加消息，仅在确定无 dispatcher 的构造期使用。
    /// </summary>
    internal void AddMessage(CopilotChatMessage chatMessage)
    {
        AddMessageCore(chatMessage);
    }

    private void AddMessageCore(CopilotChatMessage chatMessage)
    {
        ArgumentNullException.ThrowIfNull(chatMessage);

#if DEBUG
        if (_mainThreadDispatcher is not null && !_mainThreadDispatcher.CheckAccess())
        {
            Debug.Fail("CopilotChatSession.AddMessageAsync 不在主线程上执行，但已设置 MainThreadDispatcher。");
        }
#endif

        ChatMessages.Add(chatMessage);
        TryUpdateTitle(chatMessage);
    }

    /// <summary>
    /// 设置当前会话关联的代理会话。
    /// </summary>
    /// <param name="agentSession">代理会话，可为 <see langword="null"/>。</param>
    public void SetAgentSession(AgentSession? agentSession)
    {
        AgentSession = agentSession;
    }

    /// <summary>
    /// 设置会话标题并标记来源。设置后 <see cref="TryUpdateTitle"/> 将不再自动覆盖。
    /// </summary>
    /// <param name="title">标题文本。</param>
    /// <param name="source">标题来源，默认为 <see cref="TitleSource.UserSet"/>。</param>
    public void SetTitle(string title, TitleSource source = TitleSource.UserSet)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        _titleSource = source;
        Title = title.Length <= MaxTitleLength
            ? title
            : $"{title[..MaxTitleLength]}...";
    }

    private void TryUpdateTitle(CopilotChatMessage chatMessage)
    {
        if (_titleSource != TitleSource.Default || chatMessage.Role != ChatRole.User || chatMessage.IsPresetInfo)
        {
            return;
        }

        string title = CreateTitle(chatMessage.Content);
        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        _titleSource = TitleSource.AutoTruncated;
        Title = title;
    }

    private static string CreateTitle(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        string title = string.Join(" ", content.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (title.Length <= MaxTitleLength)
        {
            return title;
        }

        return $"{title[..MaxTitleLength]}...";
    }
}
