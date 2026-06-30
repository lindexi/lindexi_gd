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
    private IReadOnlyDictionary<string, ILanguageModelProvider>? _languageModelProviders;
    private string? _workspacePath;

    /// <summary>
    /// 人类插话信号。当自动循环运行期间用户插话时设置为 1，
    /// 促使 <see cref="StartAutoLoopAsync"/> 在当前角色发言完成后重启循环，
    /// 使选择器检测到人类消息并让助手立即回话用户。
    /// 使用 int + Interlocked 以保证原子消费，避免多次插话时的竞态。
    /// </summary>
    private int _humanInterjectSignal;

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
    /// 全局默认首选模型 ID。当角色定义未指定模型时，<see cref="TrySetPrimaryModel"/>
    /// 会回退到此值进行匹配。由外部调用方（如 ChatRoomService）在注册提供商时设置。
    /// </summary>
    public string? DefaultPrimaryModelId { get; set; }

    /// <summary>
    /// 当前工作区路径。设置后传播到所有角色的文件系统工具。
    /// </summary>
    public string? WorkspacePath => _workspacePath;

    /// <summary>
    /// 设置工作区路径并传播到所有角色。新添加的角色也会自动应用此路径。
    /// </summary>
    /// <param name="path">工作区路径。</param>
    public void SetWorkspacePath(string? path)
    {
        _workspacePath = string.IsNullOrWhiteSpace(path) ? null : path;
        foreach (ChatRoomRole role in Roles)
        {
            role.WorkspacePath = _workspacePath;
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

        // 连续空回复计数器：防止任何情况下死循环的安全网。
        // 阈值基于可发言的非人类角色数量，至少为 1（即使没有可发言角色也尝试一次后终止）。
        int speakableRoleCount = Math.Max(1, Roles.Count(r =>
            !r.Definition.IsHuman &&
            (r.Definition.ParticipationMode == ChatRoomParticipationMode.AlwaysParticipate ||
             r.Definition.IsManagerRole)));
        int consecutiveEmptyReplies = 0;

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

                // 人类角色不通过 StepAsync 发言，直接跳过
                if (nextSpeaker.Definition.IsHuman)
                {
                    continue;
                }

                ChatRoomMessage? message = await StepAsync(nextSpeaker, loopCancellationToken);

                if (message is not null)
                {
                    consecutiveEmptyReplies = 0;

                    // 通知 Selector 发言成功
                    SpeakerSelector.OnSpeakerResult(nextSpeaker, success: true);

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
                else
                {
                    // 角色未产生有效回复，递增连续空回复计数
                    consecutiveEmptyReplies++;

                    // 通知 Selector 发言失败
                    SpeakerSelector.OnSpeakerResult(nextSpeaker, success: false);

                    // 安全网：连续空回复达到阈值时终止循环，防止死循环
                    if (consecutiveEmptyReplies >= speakableRoleCount)
                    {
                        break;
                    }
                }

                // 人类插话信号：当前角色发言完成且消息处理完毕后，
                // 重启循环让选择器看到人类消息，使助手立即回话用户。
                // 使用 Interlocked.Exchange 原子消费信号，避免多次插话时的竞态。
                if (System.Threading.Interlocked.Exchange(ref _humanInterjectSignal, 0) != 0)
                {
                    consecutiveEmptyReplies = 0;
                    continue;
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

            // 自动循环结束后持久化会话（AI 发言产生的新消息已通过 AppendMessageAsync 持久化，
            // 此处兜底确保角色定义等状态变更被保存）
            _ = SaveAsync();
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
            // 注入聊天室上下文，供角色首次发言时构建系统提示词
            role.ChatRoomContext = BuildChatRoomContext();

            // 构建增量消息：自该角色上次发言之后的公开消息
            string incrementalUserText = BuildIncrementalUserText(role);
            if (string.IsNullOrEmpty(incrementalUserText))
            {
                // 没有新的用户消息，角色无需发言
                return null;
            }

            // 追加角色管理工具和工作区路径审批工具到本次发言
            List<AITool> additionalTools =
            [
                .. ChatRoomRoleManagementTools.CreateTools(this),
                .. WorkspacePathTools.CreateSetWorkspacePathTool(this),
            ];

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
            // 将最终内容回写到 StaticContent，确保持久化序列化时消息内容不丢失
            streamingMessage.StaticContent = assistantContent;
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

        // 自动循环运行期间插话：设置信号，促使当前循环在角色发言完成后重启，
        // 让选择器检测到人类消息并让助手立即回话用户
        if (IsRunning)
        {
            System.Threading.Interlocked.Exchange(ref _humanInterjectSignal, 1);
        }
    }

    /// <summary>
    /// 停止自动循环。
    /// </summary>
    public void Stop()
    {
        _autoLoopCancellationTokenSource?.Cancel();
    }

    /// <summary>
    /// 添加角色到聊天室并完成初始化（技能加载、模型注册）。
    /// 外部应通过此方法添加角色，不直接操作 <see cref="Roles"/> 集合。
    /// </summary>
    /// <param name="role">要添加的角色。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task AddRoleAsync(ChatRoomRole role, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(role);

        // 应用工作区路径到新角色
        role.WorkspacePath = _workspacePath;

        await role.InitializeAsync(cancellationToken).ConfigureAwait(false);
        RegisterModelProvidersForRole(role);
        Roles.Add(role);

        await SaveAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 从聊天室移除指定角色。
    /// </summary>
    /// <param name="roleId">要移除的角色 ID。</param>
    public void RemoveRole(string roleId)
    {
        if (string.IsNullOrWhiteSpace(roleId))
        {
            throw new ArgumentException("角色 ID 不能为空。", nameof(roleId));
        }

        ChatRoomRole? role = Roles.FirstOrDefault(r => r.Definition.RoleId == roleId);
        if (role is not null)
        {
            Roles.Remove(role);
            _ = SaveAsync();
        }
    }

    /// <summary>
    /// 从聊天室移除指定角色，并等待持久化完成。
    /// </summary>
    /// <param name="roleId">要移除的角色 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task RemoveRoleAsync(string roleId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roleId))
        {
            throw new ArgumentException("角色 ID 不能为空。", nameof(roleId));
        }

        ChatRoomRole? role = Roles.FirstOrDefault(r => r.Definition.RoleId == roleId);
        if (role is not null)
        {
            Roles.Remove(role);
            await SaveAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 为单个角色注册所有已存储的模型提供商，并根据角色定义设置首选模型。
    /// 需先通过 <see cref="RegisterRoleModelProviders"/> 注册 providers 字典。
    /// 人类角色跳过注册。
    /// 无论 <see cref="ChatRoomRoleDefinition.ModelProviderId"/> 为何值，都注册所有可用提供商；
    /// <see cref="ChatRoomRoleDefinition.ModelProviderId"/> 和 <see cref="ChatRoomRoleDefinition.ModelId"/>
    /// 用于决定 <see cref="AgentApiEndpointManager.PrimaryModel"/> 首选模型。
    /// 当角色定义未指定模型时，回退到 <see cref="DefaultPrimaryModelId"/>。
    /// 当指定了首选模型但找不到时，抛出 <see cref="PrimaryModelNotFoundException"/>。
    /// </summary>
    /// <param name="role">目标角色。</param>
    /// <exception cref="PrimaryModelNotFoundException">
    /// 指定了首选模型但在已注册的提供商中找不到匹配模型时抛出。
    /// </exception>
    public void RegisterModelProvidersForRole(ChatRoomRole role)
    {
        ArgumentNullException.ThrowIfNull(role);

        if (role.Definition.IsHuman)
        {
            return;
        }

        if (_languageModelProviders is null)
        {
            throw new InvalidOperationException(
                $"模型提供商尚未注册。请先调用 {nameof(RegisterRoleModelProviders)} 注册模型提供商字典。");
        }

        // 无论 ModelProviderId 为何值，都注册所有可用提供商
        foreach (ILanguageModelProvider provider in _languageModelProviders.Values)
        {
            role.EndpointManager.RegisterLanguageModelProvider(provider);
        }

        // 根据角色定义的 ModelProviderId 和 ModelId 设置首选模型
        TrySetPrimaryModel(role);
    }

    /// <summary>
    /// 根据角色定义中的 <see cref="ChatRoomRoleDefinition.ModelProviderId"/> 和
    /// <see cref="ChatRoomRoleDefinition.ModelId"/> 设置首选模型。
    /// 当角色定义未指定模型时，回退到 <see cref="DefaultPrimaryModelId"/> 进行匹配。
    /// 当指定了首选模型但找不到时，抛出 <see cref="PrimaryModelNotFoundException"/>。
    /// </summary>
    private void TrySetPrimaryModel(ChatRoomRole role)
    {
        string? providerId = role.Definition.ModelProviderId;
        string? modelId = role.Definition.ModelId;

        // 角色定义未指定模型时，回退到全局默认首选模型
        if (string.IsNullOrWhiteSpace(providerId) && string.IsNullOrWhiteSpace(modelId))
        {
            modelId = DefaultPrimaryModelId;
            if (string.IsNullOrWhiteSpace(modelId))
            {
                // 全局也未配置，由 EndpointManager 自动选择
                return;
            }
        }

        IReadOnlyList<ILanguageModel> availableModels = role.EndpointManager.GetSupportedModels();

        // 有 modelId 时复用 ResolveModel 进行匹配（支持 "Provider/ModelName" 格式和 provider 过滤）；
        // 仅有 providerId 时取该提供商的第一个模型
        ILanguageModel? matched = !string.IsNullOrWhiteSpace(modelId)
            ? role.EndpointManager.ResolveModel(modelId)
            : availableModels.FirstOrDefault(m => m.ModelDefinition.Provider == providerId);

        // 当角色定义同时指定了 providerId 和 modelId 时，用 GetModel 精确匹配
        if (matched is null && !string.IsNullOrWhiteSpace(modelId) && !string.IsNullOrWhiteSpace(providerId))
        {
            matched = role.EndpointManager.GetModel(modelId, providerId);
        }

        if (matched is null)
        {
            throw new PrimaryModelNotFoundException(providerId, modelId, availableModels);
        }

        role.EndpointManager.PrimaryModel = matched;
    }

    /// <summary>
    /// 注册模型提供商字典并应用到所有现有角色。
    /// 字典会被存储，后续通过 <see cref="AddRoleAsync"/> 添加的角色也会自动注册。
    /// 应在调用 <see cref="StartAutoLoopAsync"/> 前调用此方法。
    /// </summary>
    /// <param name="languageModelProviders">按 Provider ID 索引的语言模型提供商字典。</param>
    public void RegisterRoleModelProviders(IReadOnlyDictionary<string, ILanguageModelProvider> languageModelProviders)
    {
        ArgumentNullException.ThrowIfNull(languageModelProviders);

        _languageModelProviders = languageModelProviders;

        foreach (ChatRoomRole role in Roles)
        {
            RegisterModelProvidersForRole(role);
        }
    }

    /// <summary>
    /// 持久化当前会话。空会话（无消息）不会被保存，避免会话列表出现无聊天记录的空记录。
    /// </summary>
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        if (Persistence is null)
        {
            return;
        }

        // 空会话不持久化，避免会话列表出现无消息的空记录
        if (Session.Messages.Count == 0)
        {
            return;
        }

        var roleDefinitions = Roles.Select(r => r.Definition).ToList();
        ChatRoomSessionData data = Session.ToPersistence(roleDefinitions);
        await Persistence.SaveConfigAsync(data, cancellationToken).ConfigureAwait(false);
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

        // 恢复角色。模型解析失败（如提供商配置已变更）不应阻止会话加载，
        // 否则消息历史将无法还原。失败的角色仍会被添加到 Roles 列表，
        // 用户可在设置中重新配置模型后再使用该角色。
        Roles.Clear();
        foreach (ChatRoomRoleDefinition roleDef in data.Roles)
        {
            var role = new ChatRoomRole(roleDef)
                        {
                            MainThreadDispatcher = Session.MainThreadDispatcher,
                        };

            // 应用工作区路径到恢复的角色
            role.WorkspacePath = _workspacePath;

            await role.InitializeAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                RegisterModelProvidersForRole(role);
            }
            catch (InvalidOperationException)
            {
                // 模型提供商尚未注册或角色定义的模型已不可用（如提供商配置已变更），
                // 跳过首选模型设置但不阻止会话恢复，用户可在设置中重新配置模型后再使用该角色。
            }

            Roles.Add(role);
        }

        // 恢复消息。反序列化后 CopilotChatMessage 为 null（JsonIgnore），
        // 需要从 StaticContent 重建，使 UI 绑定的 MessageItems 能正常渲染历史消息内容。
        Session.Messages.Clear();
        foreach (ChatRoomMessage msg in data.Messages)
        {
            msg.RestoreCopilotChatMessage();
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

    /// <summary>
    /// 构建聊天室上下文提示词，描述当前角色列表、@用法和协作规范。
    /// 注入到角色的系统提示词中，引导角色优先协调而非自己执行。
    /// </summary>
    private string BuildChatRoomContext()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== 聊天室协作指引 ===");

        // 角色列表
        if (Roles.Count > 0)
        {
            sb.AppendLine("当前聊天室的角色：");
            foreach (ChatRoomRole r in Roles)
            {
                string roleInfo = $"- {r.Definition.RoleName}";
                if (r.Definition.IsHuman)
                {
                    roleInfo += "（人类）";
                }
                else if (r.Definition.IsManagerRole)
                {
                    roleInfo += "（管理者）";
                }
                else if (r.Definition.ParticipationMode == ChatRoomParticipationMode.MentionOnly)
                {
                    roleInfo += "（仅被@时参与）";
                }
                sb.AppendLine(roleInfo);
            }
            sb.AppendLine();
        }

        sb.AppendLine("@机制：");
        sb.AppendLine("- 在消息中使用 @【角色名】 或 @角色名 可以指定某角色接下来回复");
        sb.AppendLine("- @角色名 后面必须加一个空格，禁止紧跟任何标点符号（如 ，。：；！等），否则无法正确识别");
        sb.AppendLine("- 被 @ 的角色会在当前发言者之后自动发言");
        sb.AppendLine("- 如果只是提及某个角色而非要求其发言，请勿使用 @ 前缀，否则会导致该角色被触发发言");
        sb.AppendLine();
        sb.AppendLine("协作原则：");
        sb.AppendLine("- 优先通过 @ 其他角色让他们完成各自擅长的工作，而非自己包揽");
        sb.AppendLine("- 如果缺少合适的专业角色，可使用 create_character 工具创建新角色");
        sb.AppendLine("- 创建新角色后，通过 @ 该角色来分配任务，并附上需要它做的事情");

        return sb.ToString().TrimEnd();
    }

    private async Task AppendMessageAsync(ChatRoomMessage message)
    {
        await Session.AddMessageAsync(message);
        OnMessageAdded?.Invoke(this, message);

        // 持久化完整会话配置（含角色和消息列表）到 room.config.json
        await SaveAsync().ConfigureAwait(false);

        // 持久化公开消息到文本日志（fire-and-forget，异常通过 ContinueWith 记录）
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

    /// <summary>
    /// 同意指定审批工具继续执行。遍历所有角色转发审批请求。
    /// </summary>
    /// <param name="approvalToolItem">等待审批的工具片段。</param>
    public void ApproveToolExecution(CopilotChatApprovalToolItem approvalToolItem)
    {
        ArgumentNullException.ThrowIfNull(approvalToolItem);

        foreach (ChatRoomRole role in Roles)
        {
            role.ApproveToolExecution(approvalToolItem);
        }
    }

    /// <summary>
    /// 拒绝指定审批工具继续执行。遍历所有角色转发审批请求。
    /// </summary>
    /// <param name="approvalToolItem">等待审批的工具片段。</param>
    /// <param name="reason">拒绝原因。</param>
    public void RejectToolExecution(CopilotChatApprovalToolItem approvalToolItem, string? reason = null)
    {
        ArgumentNullException.ThrowIfNull(approvalToolItem);

        foreach (ChatRoomRole role in Roles)
        {
            role.RejectToolExecution(approvalToolItem, reason);
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

