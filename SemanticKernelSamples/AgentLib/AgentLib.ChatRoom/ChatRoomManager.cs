using AgentLib.ChatRoom.Model;
using AgentLib.ChatRoom.Services;
using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using AgentLib.Model;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AgentLib.ChatRoom;

/// <summary>
/// 聊天室核心管理器（导演）。负责编排多角色的发言顺序、管理共享对话历史、
/// 处理人类插话和持久化。
/// </summary>
public sealed partial class ChatRoomManager : NotifyBase, IAsyncDisposable
{
    private bool _isRunning;
    private ChatRoomRole? _currentSpeaker;
    private readonly ChatRoomAutoLoopRunner _autoLoopRunner;
    private readonly IChatRoomRoleFactory _roleFactory;
    private readonly SemaphoreSlim _workspaceChangeLock = new(1, 1);
    private IReadOnlyDictionary<string, ILanguageModelProvider>? _languageModelProviders;
    private string? _workspacePath;
    private Guid? _persistenceSessionId;
    private DateTimeOffset? _persistenceCreatedAt;
    private readonly object _closeSync = new();
    private Task? _closeTask;
    private int _isClosingOrClosed;

    /// <summary>
    /// 使用指定的会话创建聊天室管理器。
    /// </summary>
    /// <param name="session">聊天室会话。</param>
    public ChatRoomManager(ChatRoomSession session)
        : this(session, new ChatRoomRoleFactory(session?.MainThreadDispatcher))
    {
    }

    internal ChatRoomManager(
        ChatRoomSession session,
        IChatRoomRoleFactory roleFactory)
    {
        Session = session ?? throw new ArgumentNullException(nameof(session));
        _roleFactory = roleFactory ?? throw new ArgumentNullException(nameof(roleFactory));
        _autoLoopRunner = new ChatRoomAutoLoopRunner(this);
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
    /// 持久化管理器。为 <see langword="null"/> 时不持久化。
    /// </summary>
    public ChatRoomPersistence? Persistence { get; set; }

    /// <summary>
    /// 全局默认首选模型 ID。当角色定义未指定模型时，<see cref="TrySetPrimaryModel"/>
    /// 会回退到此值进行匹配。由外部调用方（如 ChatRoomService）在注册提供商时设置。
    /// </summary>
    public string? DefaultPrimaryModelId { get; set; }

    /// <summary>
    /// 当前工作区路径。此属性是管理器及全部角色工作区状态的唯一权威来源。
    /// </summary>
    public string? WorkspacePath => _workspacePath;

    private Guid CurrentPersistenceSessionId => _persistenceSessionId ?? Session.SessionId;

    private DateTimeOffset CurrentPersistenceCreatedAt => _persistenceCreatedAt ?? Session.CreatedAt;

    /// <summary>
    /// 异步设置工作区路径并向全部角色发布共享工作区会话中的授权工具。
    /// </summary>
    /// <param name="path">工作区路径。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task SetWorkspacePathAsync(string? path, CancellationToken cancellationToken = default)
    {
        ThrowIfClosingOrClosed();
        string? newPath = NormalizeWorkspacePath(path);
        await _workspaceChangeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfClosingOrClosed();
            if (PathsEqual(_workspacePath, newPath))
            {
                return;
            }

            string? oldPath = _workspacePath;
            ChatRoomRole[] roles = Roles.ToArray();
            var attemptedRoles = new List<ChatRoomRole>(roles.Length);
            try
            {
                foreach (ChatRoomRole role in roles)
                {
                    attemptedRoles.Add(role);
                    await role.SetWorkspacePathAsync(newPath, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception changeException)
            {
                List<Exception>? rollbackExceptions = null;
                for (int i = attemptedRoles.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        await attemptedRoles[i].SetWorkspacePathAsync(oldPath, CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception rollbackException)
                    {
                        (rollbackExceptions ??= []).Add(rollbackException);
                    }
                }

                if (rollbackExceptions is { Count: > 0 })
                {
                    var exceptions = new List<Exception>(rollbackExceptions.Count + 1)
                    {
                        changeException,
                    };
                    exceptions.AddRange(rollbackExceptions);
                    throw new AggregateException("工作区切换失败，且一个或多个角色回滚失败。", exceptions);
                }

                throw;
            }

            ThrowIfClosingOrClosed();
            _workspacePath = newPath;
        }
        finally
        {
            _workspaceChangeLock.Release();
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
    /// 角色从聊天室移除后的通知。
    /// </summary>
    public event EventHandler<ChatRoomRole>? RoleRemoved;

    /// <summary>
    /// 角色的可编辑定义已原位更新后的通知。
    /// </summary>
    public event EventHandler<ChatRoomRole>? RoleUpdated;

    /// <summary>
    /// 启动自动循环。自动循环按当前消息上下文调度待发言角色，并在普通角色无法继续推进时让管理者介入。
    /// 流式内容直接追加到 <see cref="ChatRoomSession.Messages"/> 集合，UI 绑定感知实时更新。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task StartAutoLoopAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfClosingOrClosed();
        await _autoLoopRunner.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 让指定角色发言一次。流式消息在发言开始时即追加到 <see cref="ChatRoomSession.Messages"/> 集合，
    /// UI 通过绑定 <see cref="ChatRoomMessage.CopilotChatMessage"/> 感知实时更新。
    /// 发言完成后若内容为空则移除该消息，否则标记 <see cref="ChatRoomMessage.IsStreaming"/> 为 <see langword="false"/>。
    /// </summary>
    /// <param name="role">要发言的角色。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>角色产生的公开消息（已在 Messages 集合中）。如果角色未产生有效回复，返回 <see langword="null"/>。</returns>
    public async Task<ChatRoomMessage?> StepAsync(ChatRoomRole role,
        CancellationToken cancellationToken = default)
    {
        ThrowIfClosingOrClosed();
        return await _autoLoopRunner.StepAsync(role, cancellationToken).ConfigureAwait(false);
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
        ThrowIfClosingOrClosed();

        var message = ChatRoomMessage.CreateHuman(content, humanRoleId, humanRoleName);

        // 解析人类消息中的 @mention
        IReadOnlyList<string> mentionedRoleIds = MentionParser.ParseMentions(message.Content, Roles);
        if (mentionedRoleIds.Count > 0)
        {
            message.MentionedRoleIds = mentionedRoleIds;
        }

        await AppendMessageAsync(message);

        _autoLoopRunner.NotifyHumanInterjected();
    }

    /// <summary>
    /// 停止自动循环。
    /// </summary>
    public void Stop()
    {
        _autoLoopRunner.Stop();
    }

    /// <summary>
    /// 停止自动循环并等待当前循环退出。
    /// </summary>
    public async Task StopAsync()
    {
        await _autoLoopRunner.StopAsync().ConfigureAwait(false);
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
        ThrowIfClosingOrClosed();
        bool added = false;
        await _workspaceChangeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfClosingOrClosed();
            ValidateRoleCanBeAdded(role);
            await role.InitializeAsync(cancellationToken).ConfigureAwait(false);
            await role.SetWorkspacePathAsync(_workspacePath, cancellationToken).ConfigureAwait(false);
            RegisterModelProvidersForRole(role);
            Roles.Add(role);
            added = true;
            await SaveAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            if (added)
            {
                Roles.Remove(role);
            }
            await role.DisposeAsync().ConfigureAwait(false);
            throw;
        }
        finally
        {
            _workspaceChangeLock.Release();
        }
    }

    /// <summary>
    /// 原位更新指定角色的可编辑定义字段，保留角色执行器、工作区资源和 AgentSession。
    /// </summary>
    /// <param name="roleId">目标角色 ID。</param>
    /// <param name="roleName">新的角色显示名。</param>
    /// <param name="systemPrompt">新的系统提示词。</param>
    /// <param name="isHuman">是否为人类角色。</param>
    /// <param name="modelProviderId">新的模型提供商 ID。</param>
    /// <param name="modelId">新的模型 ID。</param>
    /// <param name="memoryContent">新的角色记忆。</param>
    /// <param name="participationMode">新的参与模式。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task UpdateRoleAsync(
        string roleId,
        string roleName,
        string systemPrompt,
        bool isHuman,
        string? modelProviderId,
        string? modelId,
        string? memoryContent,
        ChatRoomParticipationMode participationMode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roleId))
        {
            throw new ArgumentException("角色 ID 不能为空。", nameof(roleId));
        }
        if (string.IsNullOrWhiteSpace(roleName))
        {
            throw new ArgumentException("角色名称不能为空。", nameof(roleName));
        }
        ArgumentNullException.ThrowIfNull(systemPrompt);

        ThrowIfClosingOrClosed();
        ChatRoomRole role = Roles.FirstOrDefault(candidate => candidate.Definition.RoleId == roleId)
            ?? throw new InvalidOperationException($"找不到角色：{roleId}");
        ChatRoomRoleDefinition definition = role.Definition;
        string normalizedRoleName = roleName.Trim();
        if (definition.ExecutionKind != ChatRoomRoleExecutionKind.Standard && isHuman)
        {
            throw new InvalidOperationException("非 Standard 执行角色不能改为人类角色。");
        }
        if (Roles.Any(candidate =>
                !ReferenceEquals(candidate, role)
                && string.Equals(candidate.Definition.RoleName, normalizedRoleName, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException($"角色名称已存在：{normalizedRoleName}");
        }

        string oldRoleName = definition.RoleName;
        string oldSystemPrompt = definition.SystemPrompt;
        bool oldIsHuman = definition.IsHuman;
        string? oldModelProviderId = definition.ModelProviderId;
        string? oldModelId = definition.ModelId;
        string? oldMemoryContent = definition.MemoryContent;
        ChatRoomParticipationMode oldParticipationMode = definition.ParticipationMode;
        var newlyRegisteredProviders = new List<ILanguageModelProvider>();
        try
        {
            definition.RoleName = normalizedRoleName;
            definition.SystemPrompt = systemPrompt;
            definition.IsHuman = isHuman;
            definition.ModelProviderId = NormalizeOptionalValue(modelProviderId);
            definition.ModelId = NormalizeOptionalValue(modelId);
            definition.MemoryContent = NormalizeOptionalValue(memoryContent);
            definition.ParticipationMode = participationMode;

            if (!isHuman && _languageModelProviders is not null)
            {
                foreach (ILanguageModelProvider provider in _languageModelProviders.Values)
                {
                    if (role.RegisterLanguageModelProvider(provider))
                    {
                        newlyRegisteredProviders.Add(provider);
                    }
                }

                TrySetPrimaryModel(role);
            }

            await SaveAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            definition.RoleName = oldRoleName;
            definition.SystemPrompt = oldSystemPrompt;
            definition.IsHuman = oldIsHuman;
            definition.ModelProviderId = oldModelProviderId;
            definition.ModelId = oldModelId;
            definition.MemoryContent = oldMemoryContent;
            definition.ParticipationMode = oldParticipationMode;
            foreach (ILanguageModelProvider provider in newlyRegisteredProviders)
            {
                role.UnregisterLanguageModelProvider(provider);
            }
            if (!oldIsHuman && _languageModelProviders is not null)
            {
                TrySetPrimaryModel(role);
            }

            throw;
        }

        await RaiseRoleUpdatedAsync(role).ConfigureAwait(false);
    }

    private static string? NormalizeOptionalValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// 使用管理器持有的统一角色工厂创建角色并添加到聊天室。
    /// </summary>
    /// <param name="definition">角色定义。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>已添加并完成初始化的聊天室角色。</returns>
    public async Task<ChatRoomRole> AddRoleAsync(
        ChatRoomRoleDefinition definition,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);

        ChatRoomRole role = _roleFactory.CreateRole(definition);
        await AddRoleAsync(role, cancellationToken).ConfigureAwait(false);
        return role;
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

        ThrowIfClosingOrClosed();
        ThrowIfClosingOrClosed();
        ChatRoomRole? role = Roles.FirstOrDefault(r => r.Definition.RoleId == roleId);
        if (role is null)
        {
            return;
        }

        Roles.Remove(role);
        RoleRemoved?.Invoke(this, role);
        List<Exception>? exceptions = null;
        try
        {
            await SaveAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            (exceptions ??= []).Add(ex);
        }

        try
        {
            await role.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            (exceptions ??= []).Add(ex);
        }

        if (exceptions is { Count: 1 })
        {
            throw exceptions[0];
        }
        if (exceptions is { Count: > 1 })
        {
            throw new AggregateException("移除角色后持久化或释放资源时发生错误。", exceptions);
        }
    }

    /// <summary>
    /// 清空指定角色的私有 AgentSession 记忆，不修改聊天室公开消息和增量发言进度。
    /// </summary>
    /// <param name="roleId">角色 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task ClearRoleSessionMemoryAsync(string roleId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roleId))
        {
            throw new ArgumentException("角色 ID 不能为空。", nameof(roleId));
        }

        ThrowIfClosingOrClosed();
        ChatRoomRole? role = Roles.FirstOrDefault(r => r.Definition.RoleId == roleId);
        if (role is null)
        {
            return;
        }

        role.ClearSessionMemory();

        if (Persistence is not null)
        {
            await Persistence
                .DeleteRoleAgentSessionStateAsync(CurrentPersistenceSessionId, roleId, cancellationToken)
                .ConfigureAwait(false);
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
            role.RegisterLanguageModelProvider(provider);
        }

        // 根据角色定义的 ModelProviderId 和 ModelId 设置首选模型
        TrySetPrimaryModel(role);
    }

    private async Task RaiseRoleUpdatedAsync(ChatRoomRole role)
    {
        IMainThreadDispatcher? dispatcher = role.MainThreadDispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            RoleUpdated?.Invoke(this, role);
            return;
        }

        await dispatcher.InvokeAsync(() =>
        {
            RoleUpdated?.Invoke(this, role);
            return Task.CompletedTask;
        }).ConfigureAwait(false);
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
    /// 持久化当前会话。空会话（无消息且无角色）不会被保存，避免会话列表出现无内容的空记录。
    /// </summary>
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        if (Persistence is null)
        {
            return;
        }

        // 空会话不持久化，但仅修改角色配置时仍需要保存角色定义。
        if (Session.Messages.Count == 0 && Roles.Count == 0)
        {
            return;
        }

        List<ChatRoomRoleDefinition> roleDefinitions = Roles.Select(r => r.Definition).ToList();
        ChatRoomSessionData data = new()
        {
            SessionId = CurrentPersistenceSessionId,
            Title = Session.Title,
            CreatedAt = CurrentPersistenceCreatedAt,
            LastActivityAt = Session.Messages.Count > 0
                ? Session.Messages[^1].Timestamp
                : CurrentPersistenceCreatedAt,
            Roles = roleDefinitions,
            Messages = Session.Messages.ToList(),
        };
        await Persistence.SaveConfigAsync(data, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 从持久化恢复聊天室。
    /// </summary>
    /// <param name="sessionId">要恢复的会话 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task LoadAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ThrowIfClosingOrClosed();
        if (Persistence is null)
        {
            return;
        }

        ThrowIfClosingOrClosed();
        ChatRoomSessionData? data = await Persistence.LoadConfigAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (data is null)
        {
            return;
        }

        await _workspaceChangeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        var candidateRoles = new List<ChatRoomRole>(data.Roles.Count);
        var candidateMessages = new List<ChatRoomMessage>(data.Messages.Count);
        try
        {
            foreach (ChatRoomRoleDefinition roleDef in data.Roles)
            {
                ChatRoomRole role = _roleFactory.CreateRole(roleDef);
                candidateRoles.Add(role);
                await role.InitializeAsync(cancellationToken).ConfigureAwait(false);
                await role.SetWorkspacePathAsync(_workspacePath, cancellationToken).ConfigureAwait(false);

                try
                {
                    RegisterModelProvidersForRole(role);
                }
                catch (InvalidOperationException)
                {
                    // 模型配置变化不阻止恢复公开历史，后续可重新注册提供商。
                }

                JsonElement? agentSessionState = await Persistence
                    .LoadRoleAgentSessionStateAsync(data.SessionId, role.Definition.RoleId, cancellationToken)
                    .ConfigureAwait(false);
                if (agentSessionState is JsonElement serializedState)
                {
                    await role.RestoreAgentSessionStateAsync(serializedState, cancellationToken).ConfigureAwait(false);
                }
            }

            foreach (ChatRoomMessage message in data.Messages)
            {
                message.RestoreCopilotChatMessage();
                candidateMessages.Add(message);
            }
        }
        catch
        {
            await DisposeRolesBestEffortAsync(candidateRoles).ConfigureAwait(false);
            _workspaceChangeLock.Release();
            throw;
        }

        ChatRoomRole[] oldRoles;
        try
        {
            oldRoles = Roles.ToArray();
            _persistenceSessionId = data.SessionId;
            _persistenceCreatedAt = data.CreatedAt;
            Session.Title = data.Title;
            Roles.Clear();
            foreach (ChatRoomRole role in candidateRoles)
            {
                Roles.Add(role);
            }

            Session.Messages.Clear();
            foreach (ChatRoomMessage message in candidateMessages)
            {
                await Session.AddMessageAsync(message).ConfigureAwait(false);
            }
        }
        finally
        {
            _workspaceChangeLock.Release();
        }

        await DisposeRolesBestEffortAsync(oldRoles).ConfigureAwait(false);
    }

    /// <summary>
    /// 异步关闭聊天室，停止自动循环并尽力释放全部角色。重复调用不会重复释放。
    /// </summary>
    /// <param name="cancellationToken">仅在进入实际释放阶段前生效的取消令牌。</param>
    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_closeSync)
        {
            return _closeTask ??= CloseCoreAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await CloseAsync().ConfigureAwait(false);
    }

    private async Task CloseCoreAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();
        if (Interlocked.Exchange(ref _isClosingOrClosed, 1) != 0)
        {
            return;
        }

        try
        {
            _autoLoopRunner.Stop();
            bool stopped = await _autoLoopRunner.TryStopAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            ChatRoomRole[] roles = Roles.ToArray();
            Roles.Clear();
            CurrentSpeaker = null;
            _workspacePath = null;

            if (!stopped && _autoLoopRunner.GetRunningTask() is { } runningTask)
            {
                _ = DisposeRolesAfterTaskAsync(runningTask, roles);
                return;
            }

            await DisposeRolesBestEffortAsync(roles).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Volatile.Write(ref _isClosingOrClosed, 0);
            lock (_closeSync)
            {
                _closeTask = null;
            }
            throw;
        }
    }

    private static async Task DisposeRolesAfterTaskAsync(Task runningTask, IEnumerable<ChatRoomRole> roles)
    {
        try
        {
            await runningTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"自动循环结束时发生错误：{ex}");
        }

        try
        {
            await DisposeRolesBestEffortAsync(roles).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"延迟释放聊天室角色时发生错误：{ex}");
        }
    }

    private void ValidateRoleCanBeAdded(ChatRoomRole role)
    {
        if (Roles.Any(existing => string.Equals(existing.Definition.RoleId, role.Definition.RoleId, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException($"角色 ID 已存在：{role.Definition.RoleId}");
        }

        if (Roles.Any(existing => string.Equals(existing.Definition.RoleName, role.Definition.RoleName, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException($"角色名称已存在：{role.Definition.RoleName}");
        }
    }

    private static async Task DisposeRolesBestEffortAsync(IEnumerable<ChatRoomRole> roles)
    {
        List<Exception>? exceptions = null;
        foreach (ChatRoomRole role in roles)
        {
            try
            {
                await role.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                (exceptions ??= []).Add(ex);
            }
        }

        if (exceptions is { Count: > 0 })
        {
            throw new AggregateException("释放聊天室角色时发生错误。", exceptions);
        }
    }

    private static string? NormalizeWorkspacePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        string normalizedPath = Path.GetFullPath(path);
        if (!Directory.Exists(normalizedPath))
        {
            throw new DirectoryNotFoundException($"指定的工作区目录不存在: {normalizedPath}");
        }

        return normalizedPath;
    }

    private static bool PathsEqual(string? left, string? right) => string.Equals(
        left,
        right,
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    private void ThrowIfClosingOrClosed()
    {
        if (Volatile.Read(ref _isClosingOrClosed) != 0)
        {
            throw new ObjectDisposedException(nameof(ChatRoomManager));
        }
    }

    private async Task AppendMessageAsync(ChatRoomMessage message)
    {
        await Session.AddMessageAsync(message);
        OnMessageAdded?.Invoke(this, message);

        // 持久化完整会话配置（含角色和消息列表）到 room.config.json
        await SaveAsync().ConfigureAwait(false);

        // 持久化公开消息到文本日志。
        if (Persistence is not null)
        {
            await Persistence.SavePublicMessageAsync(CurrentPersistenceSessionId, message).ConfigureAwait(false);
        }
    }

    private async Task SaveRoleAgentSessionStateAsync(ChatRoomRole role, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(role);

        if (Persistence is null || role.Definition.IsHuman)
        {
            return;
        }

        JsonElement? agentSessionState = await role
            .SerializeAgentSessionStateAsync(cancellationToken)
            .ConfigureAwait(false);
        if (agentSessionState is JsonElement serializedState)
        {
            await Persistence
                .SaveRoleAgentSessionStateAsync(CurrentPersistenceSessionId, role.Definition.RoleId, serializedState, cancellationToken)
                .ConfigureAwait(false);
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
/// 发言角色变更事件。
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

