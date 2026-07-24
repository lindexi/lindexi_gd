# CopilotChatManager 会话职责收敛方案

## 文档状态

- 状态：已实施
- 结论来源：对当前工作区相对 `HEAD` 的变更、调用链和线程模型进行复核
- 目标：撤回会话持久化对 `CopilotChatManager` 运行路径的侵入，让完整会话的加载与保存回到业务协调层
- 本文只输出方案，不修改源码

## 一、结论

用户提出的方向整体合理。

需要保留的不是 `CopilotChatManager` 上的恢复与检查点 API，而是“历史会话能够完整恢复并继续对话”这一业务能力。当前实现把这项业务能力错误地下沉到了 Manager，并为此引入第二套会话运行状态机，导致持久化需求反向污染正常发送流程。

建议最终调整为：

1. 从 `CopilotChatManager` 删除 `RestoreSessionAsync`。
2. 从 `CopilotChatManager` 删除 `CreateSessionCheckpointAsync` 及其私有序列化辅助方法。
3. 保留 `AddSession(CopilotChatSession, bool)`，把它作为“完整会话已经准备好后加入 Manager”的唯一入口。
4. 把 `RemoveSession` 改为接收 `CopilotChatSession`，与 `AddSession` 形成对象级对称 API。
5. `StartChatting` 只表达 Manager 的全局聊天 UI 状态，不接收、不捕获也不校验 `CopilotChatSession`。
6. 删除 `EnterSessionRun`、会话级 `SemaphoreSlim`、可恢复配置标记以及关联作用域类型。
7. 会话恢复由业务层在启动或打开历史时一次性完成：读取公开消息、反序列化 `AgentSession`、构造完整 `CopilotChatSession`，最后调用 `AddSession`。
8. 会话保存由业务层在本轮运行完成后一次性完成：序列化当前会话的 `AgentSession`，随后把当前会话和序列化状态交给文件 Store。
9. CodingChatRoom 的运行互斥继续由业务状态表达；在明确所有入口都位于 UI 线程后，不再额外使用锁维护同一事实。

这里的关键判断是：

> `RestoreSessionAsync` 和 `CreateSessionCheckpointAsync` 所代表的业务能力有意义，但把它们放在 `CopilotChatManager` 上没有必要。

## 二、调查依据

### 1. CopilotChatManager 的增量主要来自持久化侵入

当前 `CopilotChatManager.cs` 相对 `HEAD` 增加 239 行、删除 18 行。主要新增内容包括：

- `AddSession`
- `RemoveSession`
- `RestoreSessionAsync`
- `CreateSessionCheckpointAsync`
- `SerializeAgentSessionStateCoreAsync`
- 指定会话创建手动发送上下文的内部重载
- `_sessionRuntimeSync`
- `_sessionRuntimeStates`
- `SessionRuntimeState`
- `EnterSessionRun`
- `SessionRunScope`
- `CompositeScope`
- `IsDefaultRestorableConfiguration`
- `StartChatting(CopilotChatSession)`

其中只有会话集合操作本身属于 Manager 的直接职责。恢复、检查点和运行状态字典都是为了新持久化方案附加的逻辑。

### 2. Shell 已经拥有完整的业务运行状态

`CodingChatApplication` 已经维护：

- `_isRunActive`
- `_activeRunCancellationTokenSource`
- `CanChangeSession`
- `CanSend`
- 发送期间拒绝新建、打开、删除和重复发送

因此在 CodingChatRoom 的正常流程中：

- 发送期间不能切换当前会话。
- 发送期间不能删除当前会话。
- 同一时刻不能启动第二次发送。
- 保存发生在本轮 `CompletionTask` 收束之后。

这已经构成了业务层所需的顺序保证。Manager 再为每个会话维护一个运行门，是对同一事实的重复建模。

### 3. 新增的 SessionRuntimeState 没有形成可靠契约

当前实现存在两个直接问题。

第一，`SessionRuntimeState.IsRunActive` 只有写入，没有任何读取方。真正发挥互斥作用的是 `SemaphoreSlim`，该布尔值是无效状态。

第二，`UsesDefaultRestorableAgentConfiguration` 不能可靠判断会话是否可恢复：

- 标准 `SendMessage` 会根据部分请求参数更新该标记。
- 手动发送允许调用方通过 `GetChatClientAgentAsync(configure)` 任意修改 `ChatClientAgentOptions`。
- 手动发送的 `StartChatting(session)` 却总是把配置视为可恢复。

因此这套状态机不仅侵入正常流程，也没有真正覆盖它试图保证的配置兼容性。

### 4. StartChatting 的 session 参数只为运行门服务

`IManualSendMessageContext.StartChatting()` 的原始含义是切换 `CopilotChatManager.IsChatting`，供 UI 显示“正在运行”和“停止”状态。

本次变更把它改为转发 `ChatManager.StartChatting(Session)`，而内部 session 参数只用于调用 `EnterSessionRun`。没有其他业务语义依赖该参数。

删除运行门后，`StartChatting(CopilotChatSession)` 没有保留价值，应恢复为纯粹的无参状态作用域。

### 5. 文件 Store 已经能恢复公开会话数据

`FileCopilotChatSessionStore` 与 `CopilotChatHistoryXmlCodec` 已经能够读取：

- `SessionId`
- `StartedTime`
- `Title`
- 完整公开消息
- `AgentSessionState` JSON

`CopilotChatSession.SetAgentSession` 也是公开方法。因此业务层具备以下组合条件：

1. 从 Store 读取持久化数据。
2. 创建带 `MainThreadDispatcher` 的运行时 `CopilotChatSession`。
3. 恢复标题和消息。
4. 使用与 CodingAgent 匹配的 `ChatClientAgent` 反序列化框架状态。
5. 调用 `SetAgentSession`。
6. 最后把完整会话交给 Manager。

Manager 不需要参与这段编排。

### 6. 仓库中已有由业务对象负责 AgentSession 编解码的先例

`AgentLib.ChatRoom.ChatRoomRole` 已经自行调用：

- `ChatClientAgent.SerializeSessionAsync`
- `ChatClientAgent.DeserializeSessionAsync`

这说明框架状态的编解码不必集中在 `CopilotChatManager`。真正需要知道“用什么 Agent 配置恢复”的组件，应当是使用该 Agent 的业务层。

## 三、对各项意见的逐条判断

| 意见 | 判断 | 说明 |
| --- | --- | --- |
| 从 Manager 删除 `RestoreSessionAsync` | 合理 | 恢复是“读取持久化数据并建立完整运行时对象”的业务用例，不是内存会话集合管理职责 |
| 从 Manager 删除 `CreateSessionCheckpointAsync` | 合理 | 保存时机、Agent 配置和文件 Store 都由宿主掌握，Manager 不需要制造检查点概念 |
| `RemoveSession` 接收 `CopilotChatSession` | 合理 | 与 `AddSession` 形成对象级对称，也避免 Manager 再承担按 ID 查找的业务适配 |
| `StartChatting` 不应带 session 含义 | 合理 | 它只应表达全局 UI 聊天状态；具体会话已经由手动发送上下文持有 |
| 删除 `EnterSessionRun` | 合理 | 当前宿主已经禁止并发运行和运行期切换；该门是为检查点竞态额外引入的重复机制 |
| 不需要 Manager 保存可恢复性状态 | 合理 | Manager 无法完整观察手动配置，状态既不可靠也不属于其职责 |
| 所有线程机制都可以删除 | 需要限定 | 可删除持久化引入的状态锁；流式回调更新 UI 集合所需的 `MainThreadDispatcher` 仍应保留，文件 Store 的 I/O 门也不在本次删除范围内 |

## 四、目标职责边界

### CopilotChatManager

保留职责：

- 管理 `ChatSessions`。
- 管理 `SelectedSession`。
- 创建普通新会话和欢迎消息。
- 发送标准聊天请求。
- 创建手动发送上下文。
- 维护全局 `IsChatting`、取消和 UI 通知。
- 管理工具、Reducer 和流式消息投影。

不再负责：

- 读取或解释持久化数据。
- 创建会话检查点。
- 克隆会话用于保存。
- 序列化或反序列化持久化用的 `AgentSession`。
- 判断某个历史会话是否“可恢复”。
- 为保存操作维护会话级运行门。
- 记录每个会话使用过什么 Agent 配置。

### CodingChatSessionRepository（业务层）

建议在 CodingChatRoom Shell 中建立或改造一个业务仓储，负责完整运行时会话与文件数据之间的转换。

职责：

- 调用低层文件 Store 加载持久化数据。
- 使用 `IMainThreadDispatcher` 构造运行时 `CopilotChatSession`。
- 恢复标题和完整消息。
- 使用 CodingChatRoom 的固定 Agent 配置反序列化 `AgentSession`。
- 在完整恢复成功后返回 `CopilotChatSession`。
- 保存时序列化当前 `AgentSession`。
- 把当前 `CopilotChatSession` 和序列化状态交给文件 Store。

它不修改 `CopilotChatManager.ChatSessions`。是否把返回的会话加入 Manager，由 `CodingChatApplication` 决定。

### CodingChatAgentSessionSerializer（业务层）

建议用一个很薄的业务服务封装框架状态编解码：

```text
SerializeAsync(CopilotChatSession session)
DeserializeIntoAsync(CopilotChatSession session, JsonElement state)
```

实现使用 CodingChatRoom 已知且固定的 Agent 创建方式调用框架 API。它不维护“可恢复配置”标志；如果未来某个宿主允许每轮使用不同配置，该宿主需要自己定义持久化能力，而不是让公共 Manager 猜测。

首轮实现可以复用现有手动发送上下文取得兼容的 `ChatClientAgent`。若后续发现多个业务都需要同一创建逻辑，再单独提取 Agent 工厂；本次不应为了删除两个 Manager API 再引入一个过度通用的公共抽象。

### FileCopilotChatSessionStore

保留职责：

- XML 编解码。
- 格式版本兼容。
- 列表、加载、保存和删除文件。
- 原子替换。
- 删除关联文本日志。
- 必要的文件 I/O 串行化。

不负责：

- 创建 `ChatClientAgent`。
- 调用框架的 AgentSession 编解码 API。
- 修改 Manager。
- 管理 UI 运行状态。

### CodingChatApplication

保留业务编排：

- 启动时选择要恢复的历史会话。
- 打开、新建和删除会话。
- 启动、取消并等待 CodingAgent 运行。
- 运行完成后保存当前会话。
- 更新会话摘要和 UI 状态。

所有涉及 `Sessions`、`SelectedSession`、`StateChanged` 和 `_isRunActive` 的操作都应遵守 UI 线程约束。

## 五、目标 API

### 1. CopilotChatManager

保留：

```text
CopilotChatSession AddSession(
    CopilotChatSession session,
    bool select = false)
```

调整：

```text
bool RemoveSession(CopilotChatSession session)
```

`RemoveSession` 建议采用实例语义：

1. 参数为空时抛出 `ArgumentNullException`。
2. 只移除集合中实际存在的该对象实例。
3. 不因为传入另一个具有相同 `SessionId` 的对象就移除现有实例。
4. 若移除的是当前选中会话，选择剩余首个会话；集合为空时创建新的欢迎会话。
5. 不再清理任何持久化运行状态，因为这些状态将被删除。

保留无参：

```text
IDisposable StartChatting()
```

删除：

```text
StartChatting(CopilotChatSession session)
RestoreSessionAsync(...)
CreateSessionCheckpointAsync(...)
```

### 2. 低层持久化数据

不再使用带“运行中检查点”含义的 `CopilotChatSessionCheckpoint`。

加载需要同时返回公开会话数据与框架 JSON，可使用一个纯数据契约，例如：

```text
CopilotChatSessionPersistenceData
- SessionId
- StartedTime
- Title
- IReadOnlyList<CopilotChatMessage> Messages
- JsonElement? AgentSessionState
```

该类型只是文件数据载体，不包含：

- 运行门。
- 活动状态。
- 可恢复性标记。
- Manager 引用。
- UI 选择状态。

现有 XML 版本 2 未保存 `TitleSource`，本次保持格式不变，不借本次职责调整扩大格式迁移范围。

### 3. FileCopilotChatSessionStore

建议契约：

```text
Task<CopilotChatSessionPersistenceData> LoadSessionAsync(
    Guid sessionId,
    CancellationToken cancellationToken = default)

Task SaveSessionAsync(
    CopilotChatSession session,
    JsonElement? agentSessionState,
    CancellationToken cancellationToken = default)
```

保存直接读取调用方传入的完整会话，不再先让 Manager 深拷贝成 Checkpoint。业务层保证保存发生在发送收束且会话不可切换的时段。

### 4. CodingChatSessionRepository

建议对 `CodingChatApplication` 暴露更直接的运行时契约：

```text
Task<CopilotChatSession> LoadSessionAsync(Guid sessionId, ...)
Task SaveSessionAsync(CopilotChatSession session, ...)
Task<IReadOnlyList<CopilotChatSessionSummary>> ListSessionsAsync(...)
Task<bool> DeleteSessionAsync(Guid sessionId, ...)
```

这样 `CodingChatApplication` 不需要接触 `JsonElement`，但这项封装位于业务仓储，而不是 Manager。

## 六、目标流程

### 1. 应用启动恢复

```text
File Store 列出历史摘要
  → 业务仓储读取某个会话的持久化数据
  → 创建带 MainThreadDispatcher 的 CopilotChatSession
  → 恢复全部公开消息和标题
  → 反序列化 AgentSessionState
  → SetAgentSession
  → 得到完整 CopilotChatSession
  → CopilotChatManager.AddSession(session, select: true)
  → 移除启动时创建的初始空会话实例
```

关键不变量：

> 在公开消息和 AgentSession 都恢复成功之前，不把会话加入 Manager。

因此反序列化失败时，Manager 不会出现半恢复会话，也不需要在 `catch` 中按 ID 尝试清理。

### 2. 用户打开历史会话

```text
若 Manager 已加载该实例
  → 直接设置 SelectedSession
否则
  → 业务仓储完整恢复会话
  → 恢复成功后 AddSession(session, select: true)
```

失败时只恢复原选择，不调用 `RemoveSession(sessionId)`，因为失败对象从未加入 Manager。

### 3. 发送完成后保存

```text
发送开始前捕获当前 CopilotChatSession 实例
  → CodingAgent 完整运行收束
  → 业务仓储序列化该 session.AgentSession
  → File Store 保存 session + agentSessionState
  → 更新会话摘要
```

不再执行：

- 创建 Checkpoint。
- 克隆公开消息。
- 获取会话运行门。
- 检查可恢复配置标记。
- 临时切换 `SelectedSession`。

### 4. 删除会话

```text
通过 sessionId 从 Manager 集合中解析实际 CopilotChatSession 实例
  → File Store 删除持久化数据
  → CopilotChatManager.RemoveSession(session)
  → 删除摘要
```

ID 是持久化和列表层的标识；Manager 的增删 API 使用运行时对象。

## 七、线程与状态约束

### 应删除的状态安全逻辑

从 Manager 删除：

- `_sessionRuntimeSync`
- `_sessionRuntimeStates`
- `SessionRuntimeState`
- 每会话 `SemaphoreSlim`
- `SessionRunScope`
- `CompositeScope`
- `EnterSessionRun`
- `IsDefaultRestorableConfiguration`
- `UsesDefaultRestorableAgentConfiguration`
- `IsRunActive` 会话运行标记

从 `CodingChatApplication` 可同步清理：

- 仅为重复并发保护存在的 `_runSync`
- 只被测试调用、生产流程未使用的 `EnterActiveRun`
- 对应 `ActiveRunScope`

但应保留业务状态：

- `_isRunActive`：驱动命令可用性和 UI 状态。
- `_activeRunCancellationTokenSource`：支持停止当前运行。

这些字段是业务状态，不是并发控制原语。

### 必须明确的 UI 线程契约

删除锁的前提是把以下入口明确限定在 UI 线程：

- `InitializeAsync` 中发布已加载会话。
- `CreateNewSessionAsync`。
- `OpenSessionAsync`。
- `DeleteSessionAsync`。
- `SendMessageAsync` 的状态发布和最终清理。
- `StopActiveRun`。

同时需要审查业务层的 `ConfigureAwait(false)`：

- 低层文件 I/O 和框架状态编解码可以继续使用。
- 回到 `CodingChatApplication` 后，只要要修改 `ObservableCollection`、Manager 选择、`_isRunActive` 或触发 `StateChanged`，就必须回到 UI 线程。

### 不在本次删除范围内的线程设施

以下逻辑有独立用途，应保留：

- `CopilotChatSession.MainThreadDispatcher`：模型流式回调可能位于后台线程，UI 绑定集合仍需正确调度。
- `CopilotChatManager.TryRunInMainThread`：用于属性通知和流式消息更新。
- `FileCopilotChatSessionStore` 的文件写入门：用于避免同一存储实例发生文件级冲突，与 Manager 会话状态无关。
- CodingAgent 工作区资源自己的生命周期锁：保护 Roslyn/工作区资源，不属于会话检查点逻辑。

## 八、具体代码清理范围

### CopilotChatManager.cs

删除：

- `_sessionRuntimeSync`
- `_sessionRuntimeStates`
- `RestoreSessionAsync`
- `CreateSessionCheckpointAsync`
- `SerializeAgentSessionStateCoreAsync`
- `CreateManualSendMessageContextAsync(CopilotChatSession, ...)` 内部重载
- `StartChatting(CopilotChatSession)`
- `GetSessionRuntimeState`
- `IsDefaultRestorableConfiguration`
- `EnterSessionRun`
- `SessionRuntimeState`
- `SessionRunScope`
- `CompositeScope`
- 标准发送路径中的 `sessionRunScope` 获取与释放

调整：

- `RemoveSession(Guid)` 改为 `RemoveSession(CopilotChatSession)`。
- 公开 `CreateManualSendMessageContextAsync` 直接使用当前 `SelectedSession` 创建上下文。
- `StartChatting()` 恢复为只创建 `ChattingScope`。

保留：

- `AddSession`。
- `CreateNewSession`。
- `SelectedSession`。
- 全局 `IsChatting`。
- 现有主线程调度。

### ManualSendMessageContext.cs

把：

```text
ChatManager.StartChatting(Session)
```

恢复为：

```text
ChatManager.StartChatting()
```

`Session` 仍用于：

- 追加公开消息。
- 获取或创建 `AgentSession`。

但不再参与 Manager 的聊天状态作用域。

### CopilotChatSession.cs

`Clone` 当前只有 `CreateSessionCheckpointAsync` 使用。删除 Checkpoint API 后若无其他调用，应一并删除，避免为不存在的快照流程保留深拷贝入口。

### 持久化模型

删除或替换：

- `CopilotChatSessionCheckpoint`
- 当前只用于包装 Checkpoint 的 `CopilotChatSessionSnapshot`

新增或改造：

- 纯数据型 `CopilotChatSessionPersistenceData`
- XML Codec 直接读写该数据

磁盘 XML 格式和版本号保持不变。

### CodingChatApplication.cs

调整：

- 初始化时调用业务仓储获得完整 `CopilotChatSession`，再调用 `AddSession`。
- 打开历史时同样先完整恢复，再加入 Manager。
- 删除时先按 ID 找到 Manager 中的实际 session，再传给 `RemoveSession`。
- 保存时直接调用业务仓储 `SaveSessionAsync(session)`。
- 移除 Checkpoint 创建。
- 移除恢复失败后的按 ID 清理。
- 在 UI 线程契约成立后移除 `_runSync`、`EnterActiveRun` 和 `ActiveRunScope`。

### ICodingChatSessionStore.cs

建议升级为业务仓储契约，或重命名为 `ICodingChatSessionRepository`，对上层只暴露完整运行时会话，不暴露 Checkpoint。

### 测试

删除或迁移只验证旧架构的测试：

- Manager 创建 Checkpoint。
- Manager 恢复 Checkpoint。
- Checkpoint 冻结消息。
- 活动运行与 Checkpoint 竞争。
- 自定义配置导致 Manager 拒绝持久化。

这些测试不应简单丢失业务覆盖，而应迁移到：

- 业务仓储的完整保存/恢复测试。
- CodingChatApplication 的启动、打开和发送后保存测试。

## 九、实施步骤

### 阶段 1：建立业务层完整会话仓储

1. 定义不带运行状态的 `CopilotChatSessionPersistenceData`。
2. 调整 XML Codec 和 `FileCopilotChatSessionStore.LoadSessionAsync` 返回该数据。
3. 调整文件 Store 保存接口，直接接收 `CopilotChatSession` 与可选 `AgentSessionState`。
4. 保持现有 XML 版本 1/2 读取兼容和版本校验。
5. 增加 CodingChatRoom 业务仓储，注入文件 Store、主线程调度器和 AgentSession 编解码服务。

### 阶段 2：迁移恢复流程

1. 在业务仓储中构造带调度器的 `CopilotChatSession`。
2. 恢复标题和全部消息。
3. 反序列化并设置 `AgentSession`。
4. 修改应用启动流程，在恢复成功后调用 `AddSession`。
5. 修改打开历史流程，在恢复成功后调用 `AddSession`。
6. 删除失败路径中的按 ID 清理逻辑。

### 阶段 3：迁移保存流程

1. 在业务仓储中序列化目标 session 的 `AgentSession`。
2. 修改发送完成流程为直接保存捕获的 session。
3. 删除 `CreateSessionCheckpointAsync` 调用。
4. 保持成功、取消和异常收束后的既有保存策略。
5. 保持当前持久化错误优先级，不在本次职责调整中另行改变产品行为。

### 阶段 4：收敛 CopilotChatManager

1. 修改 `RemoveSession` 为对象参数。
2. 修改所有调用方先解析 Manager 中的实际实例。
3. 删除 `RestoreSessionAsync`。
4. 删除 `CreateSessionCheckpointAsync` 及序列化辅助方法。
5. 删除指定 session 的手动上下文内部重载。
6. 删除 `EnterSessionRun` 和全部 SessionRuntimeState 代码。
7. 恢复无 session 含义的 `StartChatting` 调用链。
8. 删除 `CopilotChatSession.Clone` 等失去调用方的代码。

### 阶段 5：简化业务状态同步

1. 明确 CodingChatApplication 的 UI 线程入口契约。
2. 调整会修改 UI 绑定状态的异步续体回到 UI 线程。
3. 删除 `_runSync`。
4. 删除未被生产流程使用的 `EnterActiveRun` 和 `ActiveRunScope`。
5. 保留 `_isRunActive` 与当前运行 CTS 作为业务状态。

### 阶段 6：迁移测试与文档

1. 把 Manager 恢复测试迁移为业务仓储恢复测试。
2. 保留恢复后继续第二轮对话的端到端测试。
3. 增加“恢复失败时 Manager 集合不变”的测试。
4. 修改 Add/Remove 对称性测试。
5. 修改 StartChatting 测试，确认不再绑定 session。
6. 修改文件 Store 往返测试以使用新的纯数据契约。
7. 更新架构设计、实现细节和实施计划中的旧 Checkpoint 描述。

## 十、验证计划

### CopilotChatManager

至少验证：

1. `AddSession` 可加入完整会话并按需选中。
2. 相同 ID 已存在时不重复加入。
3. `RemoveSession(session)` 移除实际实例。
4. 传入未被 Manager 持有的实例时返回 `false`。
5. 移除当前会话后正确选择剩余会话。
6. 移除最后会话后创建新的欢迎会话。
7. `StartChatting` 只切换全局 `IsChatting`。
8. 标准发送和手动发送不再访问任何持久化运行状态。

### 业务仓储

至少验证：

1. 加载后保留 SessionId、StartedTime、Title 和完整富消息。
2. 加载后会话携带正确的 `MainThreadDispatcher`。
3. 加载后恢复 `AgentSession`。
4. 恢复结果可继续第二轮对话并看到上一轮模型历史。
5. AgentSession 反序列化失败时不返回半成品会话。
6. 保存直接使用传入会话的最终消息和最新 AgentSession 状态。
7. 不创建 Manager Checkpoint，也不切换 Manager 当前选择。

### CodingChatApplication

至少验证：

1. 启动时完整恢复成功后才加入 Manager。
2. 启动时损坏历史不会污染 Manager 集合。
3. 打开历史失败时保留原选中会话。
4. 删除时向 Manager 传入实际 session 实例。
5. 发送完成后保存启动该轮时捕获的 session。
6. 活动发送期间 UI 命令仍禁止切换、删除、新建和重复发送。
7. 状态通知与集合更新发生在 UI 线程。

### FileCopilotChatSessionStore

至少验证：

1. 现有 XML 版本 1/2 继续可读。
2. 富消息、用量和 AgentSession JSON 往返不丢失。
3. 同一 SessionId 仍更新同一文件。
4. 损坏或不支持版本文件继续被隔离。
5. 删除历史时继续清理对应日志。
6. 原子写入行为不退化。

### 最终验证

1. 运行 AgentLib 相关测试。
2. 运行 AgentLib.Coding 相关测试。
3. 运行 CodingChatRoom.AvaloniaShell.Tests。
4. 运行 AgentLib.ChatRoom 回归测试。
5. 构建完整解决方案。
6. 搜索确认 `CopilotChatManager` 不再引用 Checkpoint、Snapshot、`SerializeSessionAsync`、`DeserializeSessionAsync` 或会话运行门。

## 十一、风险与处理

### 1. Agent 配置必须与持久化状态兼容

这项约束仍然存在，但应由 CodingChatRoom 的业务编解码服务负责。当前 CodingChatRoom 使用固定的 CodingAgent 运行方式，能够明确选择兼容的 `ChatClientAgent`。

不再尝试让 Manager 通过观察部分调用参数推断历史配置。

### 2. 保存期间会话必须稳定

本方案不再深拷贝 Checkpoint，而是依赖业务时序：只有本轮运行完成且会话切换仍被禁用时才保存。

如果未来出现后台自动保存或允许并行编辑，再在业务仓储层设计明确的快照策略；不能提前把这类边缘需求固化进 Manager 的正常发送路径。

### 3. UI 线程契约必须真实成立

当前部分业务方法使用 `ConfigureAwait(false)`。实施时不能一边删除锁，一边继续从任意线程修改 `ObservableCollection` 或发布 UI 状态。

正确做法是先让业务状态发布回到 UI 线程，再删除重复同步原语。

### 4. Manager 是公共库类型

删除会话运行门意味着 Manager 不承诺支持同一会话的并行发送。该约束应写入 API 文档：

> 一个 Manager 的运行顺序由宿主协调；Manager 提供聊天能力，不提供业务事务调度。

这与当前 `IsChatting`、当前 CTS 和 `SelectedSession` 本身就是单活动运行模型相一致。

### 5. 不扩大磁盘格式变更

本次只调整职责和 API，不改 XML 格式版本，不新增 TitleSource 持久化，不切换 JSON 文件格式。

## 十二、最终结构

```text
CodingChatApplication
├── 启动/打开
│   ├── CodingChatSessionRepository.LoadSessionAsync
│   │   ├── FileCopilotChatSessionStore.LoadSessionAsync
│   │   ├── 构造完整 CopilotChatSession
│   │   └── CodingChatAgentSessionSerializer.DeserializeIntoAsync
│   └── CopilotChatManager.AddSession
├── 发送
│   └── CodingAgent.RunAsync
├── 保存
│   └── CodingChatSessionRepository.SaveSessionAsync
│       ├── CodingChatAgentSessionSerializer.SerializeAsync
│       └── FileCopilotChatSessionStore.SaveSessionAsync
└── 删除
    ├── FileCopilotChatSessionStore.DeleteSessionAsync
    └── CopilotChatManager.RemoveSession(session)

CopilotChatManager
├── ChatSessions / SelectedSession
├── AddSession / RemoveSession
├── CreateNewSession
├── SendMessage / ManualSendMessageContext
├── StartChatting（仅全局 UI 状态）
└── 主线程消息调度
```

最终不变量：

> 业务层先完成一整个会话的反序列化，再把完整对象加入 Manager；业务层在一轮运行收束后直接保存当前会话。Manager 不知道文件、Checkpoint、恢复事务、会话运行门或历史配置标记。
