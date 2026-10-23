# ChatRoomRole 操作信号量移除计划

## 背景

`ChatRoomRole` 当前通过 `_operationGate` 串行化以下三类操作：

1. 角色发言：`SpeakAsync` 获取信号量，并把释放责任转移给后台执行的 `RunManualSendAsync`，直到整个流式回复结束才释放。
2. 工作区切换：`PrepareWorkspaceAsync` 获取信号量，成功后跨方法持有，直到 `CommitPreparedWorkspace` 或 `DiscardPreparedWorkspaceAsync` 才释放。
3. 角色释放：`DisposeCoreAsync` 等待信号量，释放运行时后再释放信号量实例。

这套设计把一次可能持续很久的模型流式调用当作临界区，并允许信号量租约跨越多个异步方法和业务阶段。它不仅增加了释放路径的复杂度，而且会在正常业务重入中形成确定性的异步自死锁。

### 已确认的死锁链

角色发言时，`ChatRoomManager.ChatRoomAutoLoopRunner` 会把 `WorkspacePathTools` 创建的 `set_workspace_path` 工具追加到本轮模型工具列表。用户批准该工具后，调用链如下：

1. `ChatRoomRole.SpeakAsync` 已获取 `_operationGate`。
2. `RunManualSendAsync` 正在等待模型工具调用完成，信号量仍由本轮发言持有。
3. `set_workspace_path` 调用 `ChatRoomManager.SetWorkspacePathAsync`。
4. 管理器遍历全部角色，并对当前正在发言的角色调用 `PrepareWorkspaceAsync`。
5. `PrepareWorkspaceAsync` 再次等待当前角色自己的 `_operationGate`。
6. 工具调用必须等待 `PrepareWorkspaceAsync` 返回；模型发言必须等待工具调用返回；信号量则必须等待模型发言结束后才释放。

三者互相等待，取消令牌不能保证打破这个环，最终表现为界面一直处于等待或发言状态。

## 改造目标

- 从 `ChatRoomRole` 中彻底删除 `_operationGate`。
- 删除围绕该信号量形成的等待、租约转移、条件释放和资源释放代码。
- 不引入其他信号量、锁、异步锁、队列或串行执行器作为替代。
- 明确接受 `ChatRoomRole` 的重入和非线程安全行为，不把并发一致性作为该类型的职责。
- 保留工作区两阶段切换、失败回滚、异常聚合、释放幂等和已释放检查等现有业务语义。
- 修复发言中调用 `set_workspace_path` 时同一角色无法重入工作区准备流程的问题。

## 范围边界

本次仅移除 `ChatRoomRole` 自身的 `_operationGate`，不扩大为全局同步机制清理：

- 不删除 `ChatRoomManager._lifecycleGate`。管理器仍负责添加角色、移除角色、加载会话和全聊天室工作区切换的上层生命周期协调。
- 不修改 `ChatRoomAutoLoopRunner` 的运行状态同步。
- 不修改 `CodingWorkspaceRoleRuntime` 的工作区候选状态模型。
- 不为移除信号量后可能出现的重入或并发访问增加补偿性同步代码。
- 不改变公开 API 签名。

## 现有职责拆分

### 应删除的同步职责

`_operationGate` 及其相关代码全部属于待删除范围：

- 字段 `private readonly SemaphoreSlim _operationGate = new(1, 1);`
- `SpeakAsync` 中的 `WaitAsync`。
- `SpeakAsync` 中的 `operationLeaseTransferred` 标记。
- `SpeakAsync` 的条件 `Release` 逻辑。
- `RunManualSendAsync` `finally` 中的 `Release`。
- `PrepareWorkspaceAsync` 中的 `WaitAsync`。
- `PrepareWorkspaceAsync` 准备失败分支中的 `Release`。
- `CommitPreparedWorkspace` 中的 `Release`。
- `DiscardPreparedWorkspaceAsync` 中的 `Release`。
- `CompleteWorkspaceTransitionAsync` 中仅用于串行化的 `WaitAsync`、`try/finally` 和 `Release`。
- `DisposeCoreAsync` 中的 `WaitAsync`、`try/finally`、`Release` 和 `Dispose`。
- `DisposeAsync` XML 注释中“并发协调资源”的过时描述。删除信号量后应改为仅描述角色拥有的扩展运行时和相关资源。

### 必须保留的业务职责

删除信号量时不得顺带删除以下逻辑：

- `ThrowIfDisposed` 的早期检查。
- `DisposeAsync` 通过 `_disposeSync` 和 `_disposeTask` 复用同一个释放任务。
- `DisposeCoreAsync` 通过 `Interlocked.Exchange` 保证释放入口幂等。
- `SpeakAsync` 的入参校验、取消处理、首次系统提示词、消息追加和结果创建。
- `RunManualSendAsync` 的流式更新、会话历史补全、Token 用量更新和空回复判断。
- `PrepareWorkspaceAsync` 的路径规范化、候选状态检查、逐运行时准备、逆序回滚和异常聚合。
- `CommitPreparedWorkspace` 的候选状态检查、运行时提交、工作区路径发布和工具快照刷新。
- `DiscardPreparedWorkspaceAsync` 的逆序丢弃、状态清理和异常聚合。
- `CompleteWorkspaceTransitionAsync` 对各运行时 `DiscardPreparedWorkspaceAsync` 的调用。该调用负责释放提交后进入 pending 状态的旧工作区资源，不是信号量清理代码。
- `DisposeRuntimesAsync` 的逆序释放和异常聚合。

## 代码改造设计

### 1. 简化发言流程

`SpeakAsync` 不再等待任何角色级操作门。完成参数校验和 `ThrowIfDisposed` 后，直接执行模型可用性检查、消息上下文创建和流式任务启动。

由于不再存在租约转移，删除 `operationLeaseTransferred` 以及围绕它的 `try/finally`。保留当前 `OperationCanceledException` 返回 `null` 的行为。

`RunManualSendAsync` 删除仅用于 `_operationGate.Release()` 的 `finally`，保留主体 `try/catch`。这样模型调用工具时可以直接重入同一角色的工作区操作。

### 2. 简化工作区两阶段操作

`PrepareWorkspaceAsync` 不再获取信号量，也不再在成功后跨方法持有任何同步租约。方法仍按原顺序：

1. 检查角色未释放。
2. 规范化路径。
3. 检查当前不存在未处理的候选状态。
4. 依次准备运行时。
5. 成功后记录 `_preparedWorkspacePath` 和 `_hasPreparedWorkspace`。
6. 失败时逆序丢弃已尝试运行时，清空候选状态，并按现有规则抛出原异常或聚合异常。

`CommitPreparedWorkspace` 和 `DiscardPreparedWorkspaceAsync` 只处理候选状态与运行时资源，不再承担释放前序方法所持信号量的隐藏职责。

`CompleteWorkspaceTransitionAsync` 直接遍历运行时并调用 `DiscardPreparedWorkspaceAsync`，用于清理已提交后挂起的旧会话资源。保留 `ThrowIfDisposed`，不添加同步包装。

### 3. 简化释放流程

`DisposeCoreAsync` 在通过 `Interlocked.Exchange` 取得幂等释放权后，直接：

1. 释放全部运行时。
2. 清空运行时工具快照。
3. 清空工作区候选状态。

不再等待正在进行的发言或工作区操作，也不再释放不存在的信号量。若释放与其他操作重叠，行为按本次明确的业务前提处理：类型不保证线程安全，也不保证并发操作间的顺序。

保持现有异常语义：只有 `DisposeRuntimesAsync` 成功返回后才清空运行时工具快照和候选字段，不把这些状态清理移动到 `finally`。

### 4. 保持必要命名空间

删除 `SemaphoreSlim` 不代表可以删除 `System.Threading`。当前文件仍使用 `CancellationToken`、`Interlocked` 和 `Volatile`，因此应保留对应命名空间引用。

## 测试改造

### 替换错误的串行化测试

现有 `ChatRoomRoleTests.WorkspaceSwitchShouldWaitForSpeakingToComplete` 明确断言工作区准备在角色发言结束前不能完成，这正是需要删除的错误行为。

将其改为表达新的业务要求，例如：

- 测试显示名：`角色发言期间准备工作区不应等待发言完成`
- 保持模型流处于阻塞状态。
- 在流尚未释放时调用 `PrepareWorkspaceAsync`。
- 对模型流已开始、准备任务完成和最终发言任务分别使用有限 `WaitAsync`，验证准备任务能及时完成。
- 断言运行时已经收到新工作区候选状态。
- 提交工作区后再释放模型流，最后等待发言任务结束。
- 在 `finally` 中无条件释放模型流，并取消或等待遗留任务，保证断言失败时测试也不会被阻塞的流或旧实现中的释放等待挂住。

测试不得依赖固定的短暂 `Task.Delay` 来证明“仍未完成”，应直接等待预期操作完成，并由测试级超时防止回归时无限挂起。

### 增加工具重入回归覆盖

优先增加能覆盖真实死锁链的测试：

1. 创建带测试运行时的角色和管理器。
2. 让测试模型在发言过程中发起 `set_workspace_path` 工具调用，或用等价的可控工具回调触发 `ChatRoomManager.SetWorkspacePathAsync`。
3. 批准工具执行。
4. 验证工作区切换和最终发言都能在测试超时内完成。
5. 验证管理器、角色和运行时最终发布相同的工作区路径。

如果现有 FakeChatClient 难以稳定模拟审批工具协议，至少保留角色级重入测试：发言流未结束时，当前角色的 `PrepareWorkspaceAsync`、`CommitPreparedWorkspace` 和 `CompleteWorkspaceTransitionAsync` 可以完成。测试名称和说明中应记录它覆盖 `set_workspace_path` 回调同一角色的场景。

角色级受控重入测试只验证关键的同一角色回调路径，不等同于完整验证 `HumanApprovalTool` 的审批协议；文档和测试命名不得把两者混为一谈。

### 增强管理器级旧资源清理覆盖

增加或强化 `ChatRoomManagerTests` 的工作区切换测试，验证 `SetWorkspacePathAsync` 在全部角色提交后，仍会经 `CompleteWorkspaceTransitionsAsync` 调用每个已准备角色的 `CompleteWorkspaceTransitionAsync`，最终触发运行时的 `DiscardPreparedWorkspaceAsync` 清理旧资源。

仅有 `CodingWorkspaceRoleRuntimeTests` 对运行时方法本身的测试不足以覆盖这条管理器调用链。管理器级测试应记录每个运行时的清理次数，防止删除信号量时误删 `CompleteWorkspaceTransitionAsync` 或其调用。

### 保留并运行的现有测试

以下行为与信号量无关，改造后仍必须通过：

- 不存在工作区路径校验。
- 多运行时准备失败后的逆序回滚。
- 提交后默认工作区路径与运行时工具快照同步发布。
- 丢弃候选工作区时保留已提交路径。
- `DisposeAsync` 重复调用只释放一次运行时。
- `ChatRoomManager` 工作区两阶段提交、失败回滚和旧资源清理测试。
- 管理器添加、移除、加载和关闭流程测试。
- `CodingWorkspaceRoleRuntime` 的候选、提交、丢弃和释放测试。

## 风险与接受项

### 明确接受

- 同一 `ChatRoomRole` 的多个发言可重入，内部 AgentSession 的并发修改结果不作线程安全保证。
- 发言、工作区切换和释放可以交叠。
- 工作区候选字段可能在不受同步保护时被重入访问。
- `DisposeAsync` 不再等待正在进行的角色操作自然结束。

这些不是待补救缺陷，而是本次由业务明确接受的行为。不得为了降低这些风险重新引入同步机制。

### 仍需防止

- 删除信号量时误删工作区失败回滚。
- 删除 `CompleteWorkspaceTransitionAsync` 的运行时清理调用，导致旧 `CodingWorkspaceToolSession` 长期不释放。
- 删除 `_disposeTask` 或 `Interlocked.Exchange`，导致重复释放运行时。
- 在运行时释放失败时改用 `finally` 清空工具快照或候选字段，意外改变现有异常语义。
- 把 `System.Threading` 引用误判为未使用。
- 修改 `ChatRoomManager._lifecycleGate`，扩大本次改造范围。

## 验证方式

1. 搜索 `ChatRoomRole.cs`，确认不存在 `_operationGate`、`operationLeaseTransferred`、对应 `WaitAsync`、`Release` 或信号量 `Dispose`，并确认 `DisposeAsync` 注释不再提及并发协调资源。
2. 编译 `AgentLib.ChatRoom` 的 `net6.0` 和 `net9.0` 目标。
3. 运行 `AgentLib.ChatRoom.Tests` 中的 `ChatRoomRoleTests`。
4. 运行工作区相关的 `ChatRoomManagerTests` 和 `CodingWorkspaceRoleRuntimeTests`。
5. 运行完整 `AgentLib.ChatRoom.Tests`，确认没有其他测试隐含依赖角色级串行化。
6. 检查回归测试均设置有限超时，避免信号量类问题再次让测试执行无限等待。

## 实施步骤

1. 修改 `ChatRoomRole.cs`，删除 `_operationGate` 字段。
2. 简化 `SpeakAsync`，删除信号量等待、租约转移和条件释放逻辑。
3. 简化 `RunManualSendAsync`，删除释放信号量的 `finally`。
4. 简化工作区准备、提交、丢弃和完成流程，删除全部角色级信号量操作，同时保留状态转换、回滚和资源清理。
5. 简化 `DisposeCoreAsync`，直接执行幂等运行时释放和状态清理。
6. 将 `WorkspaceSwitchShouldWaitForSpeakingToComplete` 改为不等待发言完成的回归测试。
7. 增加发言期间工作区工具重入不会阻塞的测试覆盖。
8. 编译全部目标框架并运行相关测试与完整测试集。
9. 最后检查本次差异和 `ChatRoomRole` 新增代码，确认没有用其他同步机制替代 `_operationGate`，且没有遗留信号量租约代码；不得因此删除仍有独立职责的 `_disposeSync`、`ChatRoomManager._lifecycleGate` 或 `ChatRoomAutoLoopRunner._stateSync`。
