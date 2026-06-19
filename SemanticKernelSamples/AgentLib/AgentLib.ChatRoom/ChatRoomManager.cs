using AgentLib.ChatRoom.Model;
using AgentLib.ChatRoom.Tools;
using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using AgentLib.Model;

using Microsoft.Extensions.AI;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AgentLib.ChatRoom;

/// <summary>
/// 聊天室核心管理器（导演）。负责编排多角色的发言顺序、管理共享对话历史、
/// 处理人类插话和持久化。
/// </summary>
public sealed class ChatRoomManager : NotifyBase
{
    private bool _isRunning;
    private ChatRoomRole? _currentSpeaker;
    private CancellationTokenSource? _autoLoopCancellationTokenSource;

    /// <summary>
    /// 使用指定的会话创建聊天室管理器。
    /// </summary>
    /// <param name="session">聊天室会话。</param>
    public ChatRoomManager(ChatRoomSession session)
    {
        Session = session ?? throw new ArgumentNullException(nameof(session));
    }

    /// <summary>
    /// 使用新生成的会话创建聊天室管理器。
    /// </summary>
    public ChatRoomManager()
        : this(new ChatRoomSession())
    {
    }

    /// <summary>
    /// 聊天室会话（共享对话历史）。
    /// </summary>
    public ChatRoomSession Session { get; }

    /// <summary>
    /// 聊天室中的角色列表。
    /// </summary>
    public ObservableCollection<ChatRoomRole> Roles { get; } = [];

    /// <summary>
    /// 发言选择策略。默认为 <see cref="SpeakerSelectors.RoundRobinSpeakerSelector"/>。
    /// </summary>
    public ISpeakerSelector SpeakerSelector { get; set; } = new SpeakerSelectors.RoundRobinSpeakerSelector();

    /// <summary>
    /// 持久化管理器。为 <see langword="null"/> 时不持久化。
    /// </summary>
    public ChatRoomPersistence? Persistence { get; set; }

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
                OnPropertyChanged(nameof(CanStartLoop));
                OnPropertyChanged(nameof(CanStop));
            }
        }
    }

    /// <summary>
    /// 是否可以启动自动循环。
    /// </summary>
    public bool CanStartLoop => !IsRunning && Roles.Count > 0;

    /// <summary>
    /// 是否可以停止。
    /// </summary>
    public bool CanStop => IsRunning;

    /// <summary>
    /// 当前正在发言的角色。为 <see langword="null"/> 时没有角色在发言。
    /// </summary>
    public ChatRoomRole? CurrentSpeaker
    {
        get => _currentSpeaker;
        private set
        {
            ChatRoomRole? previous = _currentSpeaker;
            if (SetField(ref _currentSpeaker, value))
            {
                OnPropertyChanged(nameof(IsSpeaking));
                OnSpeakingChanged?.Invoke(this, new SpeakingChangedEventArgs(previous, value));
            }
        }
    }

    /// <summary>
    /// 是否有角色正在发言。
    /// </summary>
    public bool IsSpeaking => CurrentSpeaker is not null;

    /// <summary>
    /// 当前发言角色变更事件。
    /// </summary>
    public event EventHandler<SpeakingChangedEventArgs>? OnSpeakingChanged;

    /// <summary>
    /// 收到新公开消息的事件。
    /// </summary>
    public event EventHandler<ChatRoomMessage>? OnMessageAdded;

    /// <summary>
    /// 角色发言失败的事件。
    /// </summary>
    public event EventHandler<RoleSpeakFailedEventArgs>? OnRoleSpeakFailed;

    /// <summary>
    /// 启动自动循环。由 <see cref="SpeakerSelector"/> 决定每次发言的角色。
    /// 流式内容直接追加到 <see cref="ChatRoomSession.Messages"/> 集合，UI 绑定感知实时更新。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task StartAutoLoopAsync(CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            return;
        }

        _autoLoopCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        CancellationToken loopCancellationToken = _autoLoopCancellationTokenSource.Token;

        IsRunning = true;

        try
        {
            while (!loopCancellationToken.IsCancellationRequested)
            {
                // 选择下一个发言者（Selector 内部管理 @ 队列和暂停逻辑）
                ChatRoomRole? nextSpeaker = await SpeakerSelector.SelectNextSpeakerAsync(
                    Roles,
                    Session.Messages,
                    loopCancellationToken);

                if (nextSpeaker is null)
                {
                    // 对话暂停或自然结束
                    break;
                }

                ChatRoomMessage? message = await StepAsync(nextSpeaker, loopCancellationToken);

                if (message is not null)
                {
                    // 解析消息中的 @mention，填充 MentionedRoleIds
                    IReadOnlyList<string> mentionedRoleIds = MentionParser.ParseMentions(message.Content, Roles);
                    if (mentionedRoleIds.Count > 0)
                    {
                        message.MentionedRoleIds = mentionedRoleIds;
                    }

                    // 系统消息等非流式消息不在 Messages 集合中，需要追加
                    if (!Session.Messages.Contains(message))
                    {
                        await AppendMessageAsync(message).ConfigureAwait(false);
                    }
                    else
                    {
                        // 流式消息已在 Messages 集合中，UI 通过 CollectionChanged 感知；
                        // 无需重复触发 OnMessageAdded，仅持久化
                        if (Persistence is not null)
                        {
                            _ = Persistence.SavePublicMessageAsync(Session.SessionId, message)
                                .ContinueWith(static t =>
                                {
                                    if (t.IsFaulted && t.Exception is not null)
                                    {
                                        Debug.Fail($"持久化公开消息失败: {t.Exception.InnerException?.Message}");
                                    }
                                }, TaskContinuationOptions.OnlyOnFaulted);
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException) when (loopCancellationToken.IsCancellationRequested)
        {
            // 正常取消
        }
        finally
        {
            IsRunning = false;
            CurrentSpeaker = null;
            _autoLoopCancellationTokenSource?.Dispose();
            _autoLoopCancellationTokenSource = null;
        }
    }

    /// <summary>
    /// 让指定角色发言一次。流式消息在发言开始时即追加到 <see cref="ChatRoomSession.Messages"/> 集合，
    /// UI 通过绑定 <see cref="ChatRoomMessage.CopilotChatMessage"/> 感知实时更新。
    /// 发言完成后若内容为空则移除该消息，否则标记 <see cref="ChatRoomMessage.IsStreaming"/> 为 <see langword="false"/>。
    /// </summary>
    /// <param name="role">要发言的角色。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>角色产生的公开消息（已在 Messages 集合中）。如果角色未产生有效回复，返回 <see langword="null"/>。</returns>
    public async Task<ChatRoomMessage?> StepAsync(
        ChatRoomRole role,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(role);

        if (role.Definition.IsHuman)
        {
            // 人类角色不通过 StepAsync 发言
            return null;
        }

        CurrentSpeaker = role;

        try
        {
            // 构建增量消息：自该角色上次发言之后的公开消息
            string incrementalUserText = BuildIncrementalUserText(role);

            // 追加角色管理工具到本次发言
            IReadOnlyList<AITool> additionalTools = ChatRoomRoleManagementTools.CreateTools(this);

            // 调用角色发言，获取包含流式 CopilotChatMessage 的结果
            ChatRoomSpeakResult? speakResult = role.SpeakAsync(
                incrementalUserText,
                additionalTools,
                cancellationToken);

            if (speakResult is null)
            {
                return null;
            }

            // 创建流式消息并立即追加到 Messages 集合，UI 通过绑定感知实时更新
            var streamingMessage = new ChatRoomMessage
            {
                SenderRoleId = role.Definition.RoleId,
                SenderRoleName = role.Definition.RoleName,
                CopilotChatMessage = speakResult.AssistantChatMessage,
                IsStreaming = true,
            };
            await Session.AddMessageAsync(streamingMessage).ConfigureAwait(false);

            // 等待发言完成，获取最终文本
            string? assistantContent = await speakResult.FinalContentTask.ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(assistantContent))
            {
                // 空回复，从 Messages 移除流式消息
                Session.Messages.Remove(streamingMessage);
                return null;
            }

            // 发言完成，标记流式结束（CopilotChatMessage 引用不变，Content 已流式更新到位）
            streamingMessage.IsStreaming = false;
            return streamingMessage;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            // 角色发言失败，引发事件
            OnRoleSpeakFailed?.Invoke(this, new RoleSpeakFailedEventArgs(role, ex));

            // 返回系统消息
            return ChatRoomMessage.CreateSystem($"角色 「{role.Definition.RoleName}」发言失败：{ex.Message}");
        }
        finally
        {
            CurrentSpeaker = null;
        }
    }

    /// <summary>
    /// 人类角色插话。消息直接追加到共享会话，不使用 LLM。
    /// </summary>
    /// <param name="content">人类输入的内容。</param>
    /// <param name="humanRoleId">人类角色 Id。</param>
    /// <param name="humanRoleName">人类角色显示名。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task HumanInterjectAsync(string content, string humanRoleId, string humanRoleName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var message = ChatRoomMessage.CreateHuman(content, humanRoleId, humanRoleName);

        // 解析人类消息中的 @mention
        IReadOnlyList<string> mentionedRoleIds = MentionParser.ParseMentions(message.Content, Roles);
        if (mentionedRoleIds.Count > 0)
        {
            message.MentionedRoleIds = mentionedRoleIds;
        }

        await AppendMessageAsync(message);
    }

    /// <summary>
    /// 停止自动循环。
    /// </summary>
    public void Stop()
    {
        _autoLoopCancellationTokenSource?.Cancel();
    }

    /// <summary>
    /// 为所有配置了独立模型提供商 ID 的角色注册模型提供商。
    /// 应在调用 <see cref="StartAutoLoopAsync"/> 前由外部完成模型提供商的注册并调用此方法。
    /// </summary>
    /// <param name="languageModelProviders">按 Provider ID 索引的语言模型提供商字典。</param>
    public void RegisterRoleModelProviders(IReadOnlyDictionary<string, ILanguageModelProvider> languageModelProviders)
    {
        ArgumentNullException.ThrowIfNull(languageModelProviders);

        foreach (ChatRoomRole role in Roles)
        {
            if (string.IsNullOrWhiteSpace(role.Definition.ModelProviderId))
            {
                // 未配置特定提供商时，注册所有可用提供商
                foreach (ILanguageModelProvider provider in languageModelProviders.Values)
                {
                    role.EndpointManager.RegisterLanguageModelProvider(provider);
                }
            }
            else if (languageModelProviders.TryGetValue(role.Definition.ModelProviderId, out ILanguageModelProvider? provider))
            {
                role.EndpointManager.RegisterLanguageModelProvider(provider);
            }
        }
    }

    /// <summary>
    /// 持久化当前会话。
    /// </summary>
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        if (Persistence is null)
        {
            return;
        }

        var roleDefinitions = Roles.Select(r => r.Definition).ToList();
        ChatRoomSessionData data = Session.ToPersistence(roleDefinitions);
        await Persistence.SaveConfigAsync(data, cancellationToken);
    }

    /// <summary>
    /// 从持久化恢复聊天室。
    /// </summary>
    /// <param name="sessionId">要恢复的会话 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task LoadAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (Persistence is null)
        {
            return;
        }

        ChatRoomSessionData? data = await Persistence.LoadConfigAsync(sessionId, cancellationToken);
        if (data is null)
        {
            return;
        }

        // 用持久化数据替换当前 Session 的状态
        Session.Title = data.Title;

        // 恢复角色
        Roles.Clear();
        foreach (ChatRoomRoleDefinition roleDef in data.Roles)
        {
            var role = new ChatRoomRole(roleDef)
                        {
                            MainThreadDispatcher = Session.MainThreadDispatcher,
                        };
            await role.InitializeAsync(cancellationToken);
            Roles.Add(role);
        }

        // 恢复消息
        Session.Messages.Clear();
        foreach (ChatRoomMessage msg in data.Messages)
        {
            await Session.AddMessageAsync(msg);
        }
    }

    private string BuildIncrementalUserText(ChatRoomRole role)
    {
        var sb = new StringBuilder();

        // 获取自该角色上次发言后的增量消息
        IReadOnlyList<ChatRoomMessage> incrementalMessages = Session.GetMessagesSinceLastSpeak(role.Definition.RoleId);

        if (incrementalMessages.Count == 0)
        {
            // 该角色从未发言过，且没有其他消息（首次发言，无上下文）
            return string.Empty;
        }

        // 将增量消息拼接为 User 文本
        foreach (ChatRoomMessage message in incrementalMessages)
        {
            // 跳过系统消息
            if (message.IsSystemMessage)
            {
                continue;
            }

            string prefix = message.SenderRoleName;
            if (string.IsNullOrEmpty(prefix))
            {
                prefix = message.IsHumanMessage ? "用户" : "另一位参与者";
            }

            sb.AppendLine($"{prefix}说：{message.Content}");
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    private async Task AppendMessageAsync(ChatRoomMessage message)
    {
        await Session.AddMessageAsync(message);
        OnMessageAdded?.Invoke(this, message);

        // 持久化公开消息（fire-and-forget，异常通过 ContinueWith 记录）
        if (Persistence is not null)
        {
            _ = Persistence.SavePublicMessageAsync(Session.SessionId, message)
                .ContinueWith(static t =>
                {
                    if (t.IsFaulted && t.Exception is not null)
                    {
                        Debug.Fail($"持久化公开消息失败: {t.Exception.InnerException?.Message}");
                    }
                }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}

/// <summary>
/// 发言角色变更事件参数。
/// </summary>
public sealed class SpeakingChangedEventArgs : EventArgs
{
    /// <summary>
    /// 之前的发言角色。为 <see langword="null"/> 表示之前没有角色在发言。
    /// </summary>
    public ChatRoomRole? PreviousSpeaker { get; }

    /// <summary>
    /// 当前的发言角色。为 <see langword="null"/> 表示没有角色正在发言。
    /// </summary>
    public ChatRoomRole? CurrentSpeaker { get; }

    /// <summary>
    /// 创建发言角色变更事件参数。
    /// </summary>
    public SpeakingChangedEventArgs(ChatRoomRole? previousSpeaker, ChatRoomRole? currentSpeaker)
    {
        PreviousSpeaker = previousSpeaker;
        CurrentSpeaker = currentSpeaker;
    }
}

/// <summary>
/// 角色发言失败事件参数。
/// </summary>
public sealed class RoleSpeakFailedEventArgs : EventArgs
{
    /// <summary>
    /// 发言失败的角色。
    /// </summary>
    public ChatRoomRole Role { get; }

    /// <summary>
    /// 失败异常。
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// 创建角色发言失败事件参数。
    /// </summary>
    public RoleSpeakFailedEventArgs(ChatRoomRole role, Exception exception)
    {
        Role = role ?? throw new ArgumentNullException(nameof(role));
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
    }
}

