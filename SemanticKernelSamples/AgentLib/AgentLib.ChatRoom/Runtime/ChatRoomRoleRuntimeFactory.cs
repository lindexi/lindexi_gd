using AgentLib.ChatRoom.Domain;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;

using System.Collections.ObjectModel;

namespace AgentLib.ChatRoom.Runtime;

/// <summary>
/// 角色运行时创建时使用的模型提供商快照。
/// </summary>
public sealed class ChatRoomModelProviderSnapshot
{
    /// <summary>
    /// 创建模型提供商快照。
    /// </summary>
    public ChatRoomModelProviderSnapshot(
        IReadOnlyDictionary<string, ILanguageModelProvider> providers,
        string? defaultPrimaryModelId = null)
    {
        ArgumentNullException.ThrowIfNull(providers);
        var copy = new Dictionary<string, ILanguageModelProvider>(providers.Count, StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, ILanguageModelProvider> pair in providers)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
            {
                throw new ArgumentException("模型提供商标识不能为空或空白。", nameof(providers));
            }
            ArgumentNullException.ThrowIfNull(pair.Value);
            copy.Add(pair.Key.Trim(), pair.Value);
        }

        Providers = new ReadOnlyDictionary<string, ILanguageModelProvider>(copy);
        DefaultPrimaryModelId = string.IsNullOrWhiteSpace(defaultPrimaryModelId)
            ? null
            : defaultPrimaryModelId.Trim();
    }

    /// <summary>
    /// 已冻结的模型提供商集合。
    /// </summary>
    public IReadOnlyDictionary<string, ILanguageModelProvider> Providers { get; }

    /// <summary>
    /// 默认首选模型标识。
    /// </summary>
    public string? DefaultPrimaryModelId { get; }
}

/// <summary>
/// 基于隔离角色适配器创建指定执行种类的运行时。
/// </summary>
public sealed class ChatRoomRoleRuntimeFactory : IChatRoomRoleRuntimeFactory
{
    private readonly ChatRoomRoleFactory _roleFactory;
    private readonly ChatRoomModelProviderSnapshot _providerSnapshot;

    /// <summary>
    /// 创建角色运行时工厂。
    /// </summary>
    public ChatRoomRoleRuntimeFactory(
        ChatRoomRoleExecutionKind executionKind,
        ChatRoomRoleFactory roleFactory,
        ChatRoomModelProviderSnapshot providerSnapshot)
    {
        if (!Enum.IsDefined(typeof(ChatRoomRoleExecutionKind), executionKind))
        {
            throw new ArgumentOutOfRangeException(nameof(executionKind));
        }
        ArgumentNullException.ThrowIfNull(roleFactory);
        ArgumentNullException.ThrowIfNull(providerSnapshot);

        ExecutionKind = executionKind;
        _roleFactory = roleFactory;
        _providerSnapshot = providerSnapshot;
    }

    /// <inheritdoc />
    public ChatRoomRoleExecutionKind ExecutionKind { get; }

    /// <inheritdoc />
    public Task<IChatRoomRoleRuntime> CreateAsync(
        ChatRoomRoleDefinition definition,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(definition);
        cancellationToken.ThrowIfCancellationRequested();
        if (definition.ExecutionKind != ExecutionKind)
        {
            throw new ArgumentException("角色定义与运行时工厂执行种类不一致。", nameof(definition));
        }
        if (definition.IsHuman)
        {
            throw new ArgumentException("人类角色不创建模型运行时。", nameof(definition));
        }

        IChatRoomRoleRuntime runtime = new IsolatedChatRoomRoleRuntime(
            definition,
            _roleFactory,
            _providerSnapshot.Providers,
            _providerSnapshot.DefaultPrimaryModelId);
        return Task.FromResult(runtime);
    }
}
