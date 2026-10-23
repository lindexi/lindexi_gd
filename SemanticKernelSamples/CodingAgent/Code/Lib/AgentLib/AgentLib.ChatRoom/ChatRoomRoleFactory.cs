using AgentLib.ChatRoom.Model;
using AgentLib.ChatRoom.Services;
using AgentLib.Core;

namespace AgentLib.ChatRoom;

/// <summary>
/// 创建聊天室角色。
/// </summary>
public sealed class ChatRoomRoleFactory : IChatRoomRoleFactory
{
    private readonly IMainThreadDispatcher? _mainThreadDispatcher;
    private readonly IReadOnlyDictionary<ChatRoomRoleExecutionKind, IChatRoomRoleExecutorFactory> _executorFactories;

    /// <summary>
    /// 创建通用角色工厂。
    /// </summary>
    /// <param name="mainThreadDispatcher">可选的主线程调度器。</param>
    /// <param name="roslynLanguageServerCommand">Roslyn Language Server 启动命令。</param>
    public ChatRoomRoleFactory(
        IMainThreadDispatcher? mainThreadDispatcher = null,
        string roslynLanguageServerCommand = "roslyn-language-server")
        : this(
            mainThreadDispatcher,
            [
                new StandardChatRoomRoleExecutorFactory(),
                new CodingChatRoomRoleExecutorFactory(roslynLanguageServerCommand),
            ])
    {
    }

    internal ChatRoomRoleFactory(
        IMainThreadDispatcher? mainThreadDispatcher,
        IEnumerable<IChatRoomRoleExecutorFactory> executorFactories)
    {
        ArgumentNullException.ThrowIfNull(executorFactories);
        _mainThreadDispatcher = mainThreadDispatcher;
        var factories = new Dictionary<ChatRoomRoleExecutionKind, IChatRoomRoleExecutorFactory>();
        foreach (IChatRoomRoleExecutorFactory factory in executorFactories)
        {
            ArgumentNullException.ThrowIfNull(factory);
            if (!Enum.IsDefined(typeof(ChatRoomRoleExecutionKind), factory.ExecutionKind))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(executorFactories),
                    factory.ExecutionKind,
                    "执行器工厂声明了未知执行种类。");
            }

            if (!factories.TryAdd(factory.ExecutionKind, factory))
            {
                throw new ArgumentException(
                    $"执行种类 {factory.ExecutionKind} 注册了多个执行器工厂。",
                    nameof(executorFactories));
            }
        }

        _executorFactories = factories;
    }

    /// <summary>
    /// 创建可持久化的编程助手角色定义。
    /// </summary>
    /// <returns>新的聊天室角色定义。</returns>
    public ChatRoomRoleDefinition CreateCodingAssistantDefinition()
    {
        return new CodingAssistantRoleFactory().CreateDefinition();
    }

    /// <summary>
    /// 创建仅在当前进程中存在的编程助手模板。
    /// </summary>
    /// <returns>不会由模板服务写入磁盘的运行时模板。</returns>
    public RoleTemplate CreateCodingAssistantRuntimeTemplate()
    {
        return new CodingAssistantRoleFactory().CreateRuntimeTemplate();
    }

    /// <inheritdoc />
    public ChatRoomRole CreateRole(ChatRoomRoleDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (!Enum.IsDefined(typeof(ChatRoomRoleExecutionKind), definition.ExecutionKind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(definition),
                definition.ExecutionKind,
                "角色定义包含未知执行种类。");
        }

        if (definition.IsHuman && definition.ExecutionKind != ChatRoomRoleExecutionKind.Standard)
        {
            throw new ArgumentException("人类角色只能使用 Standard 执行种类。", nameof(definition));
        }

        if (!_executorFactories.TryGetValue(definition.ExecutionKind, out IChatRoomRoleExecutorFactory? executorFactory))
        {
            throw new InvalidOperationException($"没有为执行种类 {definition.ExecutionKind} 注册角色执行器工厂。");
        }

        IChatRoomRoleExecutor executor = executorFactory.Create(new ChatRoomRoleExecutorCreationContext(definition))
            ?? throw new InvalidOperationException($"执行器工厂 {executorFactory.GetType().Name} 返回了空执行器。");
        if (executor.ExecutionKind != definition.ExecutionKind)
        {
            executor.DisposeAsync().AsTask().GetAwaiter().GetResult();
            throw new InvalidOperationException(
                $"执行器工厂为 {definition.ExecutionKind} 返回了声明为 {executor.ExecutionKind} 的执行器。");
        }

        return new ChatRoomRole(definition, null, executor)
        {
            MainThreadDispatcher = _mainThreadDispatcher,
        };
    }
}
