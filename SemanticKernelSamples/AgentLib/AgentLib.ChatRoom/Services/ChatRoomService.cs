using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AgentLib.ChatRoom.Configuration;
using AgentLib.ChatRoom.Model;
using AgentLib.ChatRoom.Services;
using AgentLib.ChatRoom.SpeakerSelectors;
using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using AgentLib.Model;

namespace AgentLib.ChatRoom.Services;

/// <summary>
/// 聊天室应用服务。封装 <see cref="ChatRoomManager"/> 生命周期管理，
/// 包括创建/加载/删除会话、注册模型提供商、人类插话、启动/停止循环等操作。
/// </summary>
public sealed class ChatRoomService
{
    private readonly IMainThreadDispatcher _mainThreadDispatcher;
    private readonly ModelProviderService _modelProviderService;
    private readonly string _persistencePath;
    private readonly int _defaultMaxRounds;

    private ChatRoomManager? _currentManager;
    private ChatRoomPersistence? _persistence;

    /// <summary>
    /// 当前活跃的聊天室管理器。
    /// </summary>
    public ChatRoomManager? CurrentManager => _currentManager;

    /// <summary>
    /// 当前是否有活跃会话。
    /// </summary>
    public bool HasActiveSession => _currentManager is not null;

    /// <summary>
    /// 角色发言失败事件。
    /// </summary>
    public event EventHandler<(ChatRoomRole Role, Exception Exception)>? RoleSpeakFailed;

    /// <summary>
    /// 发言角色变更事件。
    /// </summary>
    public event EventHandler<(ChatRoomRole? Previous, ChatRoomRole? Current)>? SpeakingChanged;

    /// <summary>
    /// 会话切换事件。参数为新会话的管理器（可能为 null）。
    /// </summary>
    public event EventHandler<ChatRoomManager?>? SessionChanged;

    /// <summary>
    /// 使用指定的依赖创建聊天室服务。
    /// </summary>
    /// <param name="mainThreadDispatcher">UI 线程调度器。</param>
    /// <param name="modelProviderService">模型提供商服务。</param>
    /// <param name="persistencePath">持久化根目录路径。</param>
    /// <param name="defaultMaxRounds">默认最大轮次。</param>
    public ChatRoomService(
        IMainThreadDispatcher mainThreadDispatcher,
        ModelProviderService modelProviderService,
        string persistencePath,
        int defaultMaxRounds = 10)
    {
        ArgumentNullException.ThrowIfNull(mainThreadDispatcher);
        ArgumentNullException.ThrowIfNull(modelProviderService);
        if (string.IsNullOrWhiteSpace(persistencePath))
        {
            throw new ArgumentException("持久化路径不能为空。", nameof(persistencePath));
        }

        _mainThreadDispatcher = mainThreadDispatcher;
        _modelProviderService = modelProviderService;
        _persistencePath = persistencePath;
        _defaultMaxRounds = defaultMaxRounds;
    }

    /// <summary>
    /// 创建新会话。会先关闭当前会话。
    /// </summary>
    /// <param name="title">会话标题。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>新创建的 <see cref="ChatRoomManager"/>。</returns>
    public async Task<ChatRoomManager> CreateNewSessionAsync(string title = "新聊天室", CancellationToken cancellationToken = default)
    {
        CloseCurrentSession();

        _persistence = new ChatRoomPersistence(_persistencePath);

        var session = new ChatRoomSession
        {
            MainThreadDispatcher = _mainThreadDispatcher,
        };
        session.Title = title;

        _currentManager = new ChatRoomManager(session)
        {
            Persistence = _persistence,
            SpeakerSelector = new RoundRobinSpeakerSelector { MaxRounds = _defaultMaxRounds },
        };

        AttachEvents(_currentManager);

        SessionChanged?.Invoke(this, _currentManager);
        return _currentManager;
    }

    /// <summary>
    /// 加载历史会话。
    /// </summary>
    /// <param name="sessionId">会话 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>恢复的 <see cref="ChatRoomManager"/>。</returns>
    public async Task<ChatRoomManager> LoadSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("会话 ID 不能为空。", nameof(sessionId));
        }

        CloseCurrentSession();

        _persistence = new ChatRoomPersistence(_persistencePath);

        var session = new ChatRoomSession
        {
            MainThreadDispatcher = _mainThreadDispatcher,
        };

        _currentManager = new ChatRoomManager(session)
        {
            Persistence = _persistence,
            SpeakerSelector = new RoundRobinSpeakerSelector { MaxRounds = _defaultMaxRounds },
        };

        AttachEvents(_currentManager);

        await _currentManager.LoadAsync(sessionId, cancellationToken).ConfigureAwait(false);

        // 加载完成后注册模型提供商
        RegisterProviders(_currentManager);

        SessionChanged?.Invoke(this, _currentManager);
        return _currentManager;
    }

    /// <summary>
    /// 关闭当前会话。
    /// </summary>
    public void CloseCurrentSession()
    {
        if (_currentManager is null)
        {
            return;
        }

        _currentManager.Stop();
        DetachEvents(_currentManager);
        _currentManager = null;
        _persistence = null;
        SessionChanged?.Invoke(this, null);
    }

    /// <summary>
    /// 人类插话。
    /// </summary>
    /// <param name="content">消息内容。</param>
    /// <param name="humanRoleId">人类角色 ID。</param>
    /// <param name="humanRoleName">人类角色显示名。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task HumanInterjectAsync(string content, string humanRoleId, string humanRoleName, CancellationToken cancellationToken = default)
    {
        if (_currentManager is null)
        {
            throw new InvalidOperationException("没有活跃的会话。");
        }

        await _currentManager.HumanInterjectAsync(content, humanRoleId, humanRoleName, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 启动自动循环。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task StartAutoLoopAsync(CancellationToken cancellationToken = default)
    {
        if (_currentManager is null)
        {
            throw new InvalidOperationException("没有活跃的会话。");
        }

        RegisterProviders(_currentManager);
        await _currentManager.StartAutoLoopAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 停止自动循环。
    /// </summary>
    public void StopAutoLoop()
    {
        _currentManager?.Stop();
    }

    /// <summary>
    /// 持久化当前会话。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        if (_currentManager is null)
        {
            return;
        }

        await _currentManager.SaveAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 添加角色到当前会话并初始化。
    /// </summary>
    /// <param name="definition">角色定义。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task AddRoleAsync(ChatRoomRoleDefinition definition, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (_currentManager is null)
        {
            throw new InvalidOperationException("没有活跃的会话。");
        }

        var role = new ChatRoomRole(definition)
        {
            MainThreadDispatcher = _mainThreadDispatcher,
        };
        await role.InitializeAsync(cancellationToken).ConfigureAwait(false);
        _currentManager.Roles.Add(role);

        // 注册该角色的模型提供商
        RegisterProvidersForRole(role);

        // 早期校验：确保角色有可用模型，避免发言时才发现问题
        if (!definition.IsHuman)
        {
            role.EnsureModelAvailable();
        }
    }

    /// <summary>
    /// 从当前会话中移除角色。
    /// </summary>
    /// <param name="roleId">角色 ID。</param>
    public void RemoveRole(string roleId)
    {
        if (string.IsNullOrWhiteSpace(roleId))
        {
            throw new ArgumentException("角色 ID 不能为空。", nameof(roleId));
        }

        if (_currentManager is null)
        {
            return;
        }

        ChatRoomRole? role = _currentManager.Roles.FirstOrDefault(r => r.Definition.RoleId == roleId);
        if (role is not null)
        {
            _currentManager.Roles.Remove(role);
        }
    }

    /// <summary>
    /// 设置变更后重新注册模型提供商到当前会话的所有角色。
    /// </summary>
    public void RefreshProviders()
    {
        if (_currentManager is null)
        {
            return;
        }

        RegisterProviders(_currentManager);
    }

    /// <summary>
    /// 设置变更后更新内部 <see cref="ModelProviderService"/> 并重新注册模型提供商。
    /// 应在设置页面保存后调用，确保新配置立即生效。
    /// </summary>
    /// <param name="appSettings">最新的应用设置。</param>
    public void RefreshProviders(AppSettings appSettings)
    {
        _modelProviderService.UpdateSettings(appSettings);

        if (_currentManager is null)
        {
            return;
        }

        RegisterProviders(_currentManager);
    }

    private void RegisterProviders(ChatRoomManager manager)
    {
        IReadOnlyDictionary<string, ILanguageModelProvider> providers = _modelProviderService.GetProviders();
        manager.RegisterRoleModelProviders(providers);
    }

    private void RegisterProvidersForRole(ChatRoomRole role)
    {
        IReadOnlyDictionary<string, ILanguageModelProvider> providers = _modelProviderService.GetProviders();

        if (string.IsNullOrWhiteSpace(role.Definition.ModelProviderId))
        {
            // 未配置特定提供商时，注册所有可用提供商，确保角色能找到模型
            foreach (ILanguageModelProvider provider in providers.Values)
            {
                role.EndpointManager.RegisterLanguageModelProvider(provider);
            }
            return;
        }

        if (providers.TryGetValue(role.Definition.ModelProviderId, out ILanguageModelProvider? specificProvider))
        {
            role.EndpointManager.RegisterLanguageModelProvider(specificProvider);
        }
    }

    private void AttachEvents(ChatRoomManager manager)
    {
        manager.OnRoleSpeakFailed += OnRoleSpeakFailed;
        manager.OnSpeakingChanged += OnSpeakingChanged;
    }

    private void DetachEvents(ChatRoomManager manager)
    {
        manager.OnRoleSpeakFailed -= OnRoleSpeakFailed;
        manager.OnSpeakingChanged -= OnSpeakingChanged;
    }

    private void OnRoleSpeakFailed(object? sender, RoleSpeakFailedEventArgs e)
    {
        RoleSpeakFailed?.Invoke(this, (e.Role, e.Exception));
    }

    private void OnSpeakingChanged(object? sender, SpeakingChangedEventArgs e)
    {
        SpeakingChanged?.Invoke(this, (e.PreviousSpeaker, e.CurrentSpeaker));
    }

    }
