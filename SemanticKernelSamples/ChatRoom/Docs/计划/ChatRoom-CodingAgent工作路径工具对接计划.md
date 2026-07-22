# ChatRoom CodingAgent 工作路径工具对接计划

## 文档定位

本文解决以下问题：

1. 为什么当前 `CodingAgent` 没有模型可调用的“设置工作路径”工具
2. 为什么不能简单把聊天室现有的 `set_workspace_path` 直接塞进 `CodingAgent`
3. 如何在不破坏人工审批、工作区事务、运行租约和程序集依赖方向的前提下，让 Coding 角色也能请求设置聊天室工作区

本文只给出修改计划，不实现代码。

## 当前实现结论

### `CodingAgent` 并非完全不知道工作区

`AgentLib.Coding.CodingAgent` 已经具有完整的宿主侧工作区生命周期：

- `RunAsync(..., workspacePath, ...)` 接收本轮期望工作区
- `WorkspacePath` 暴露当前已提交路径
- `PrepareWorkspaceChangeAsync` 提供候选创建、应用、回滚和提交事务
- `CodingWorkspaceToolProvider` 持有当前工作区 Session
- `CodingWorkspaceToolLease` 固定单次运行使用的路径、工具集合和底层资源

因此，缺少的不是“工作区状态”，而是一个允许模型主动请求宿主改变该状态的工具。

### 聊天室已经有 `set_workspace_path`

`WorkspacePathTools.CreateSetWorkspacePathTool` 创建的 `set_workspace_path` 会：

1. 接收绝对路径
2. 通过 `HumanApprovalTool.Wrap` 进入人工审批
3. 审批通过后调用 `ChatRoomManager.SetWorkspacePathAsync`
4. 由管理器向全部角色一致发布新工作区
5. 任一角色更新失败时回滚此前已更新角色

`ChatRoomManager` 仍然是聊天室工作区路径的唯一权威状态所有者。

### Standard 路径能够使用该工具

`ChatRoomAutoLoopRunner.StepAsync` 当前为每轮发言创建：

- 角色管理工具
- `set_workspace_path`

`StandardChatRoomRoleExecutor` 使用 `CopilotChatManager.SendMessage`。标准发送路径会调用 `ResolveTools`，把配置态的审批工具绑定到当前助手消息、聊天上下文和取消令牌，因此审批面板可以正常显示并等待用户决定。

### Coding 路径有意丢弃聊天室工具

`CodingChatRoomRoleExecutor` 虽然收到同一个 `ChatRoomRoleExecutionContext`，但没有读取 `AdditionalTools`。

`CodingAgent.RunCoreAsync` 又会执行：

```text
ChatOptions.Tools = 当前 CodingWorkspaceToolLease.Tools
AIContextProviders = 空集合
```

这意味着 Coding 运行：

- 不使用 `IManualSendMessageContext.DefaultTools`
- 不使用聊天室角色管理工具
- 不使用聊天室 `set_workspace_path`
- 只使用该次工作区租约中的 Roslyn、文件和 .NET CLI 工具

这正是当前 Coding 角色看不到“设置工作路径”工具的直接原因。

## 为什么不能直接追加现有工具

### 原因一：直接执行会绕过审批

`HumanApprovalTool.Wrap` 返回的是配置态包装器。真正的审批等待逻辑只在标准发送路径调用 `HumanApprovalTool.BindRuntimeTool` 后才存在。

当前 `IManualSendMessageContext.GetChatClientAgentAsync` 允许调用方覆盖 `ChatOptions.Tools`，但不会自动把调用方新增的配置态审批工具绑定成运行时工具。

如果 `CodingAgent` 直接把聊天室传入的 `set_workspace_path` 放进 `ChatOptions.Tools`，模型调用时可能直接透传到内部函数，从而绕过审批。

### 原因二：会错误暴露全部聊天室协调工具

当前 `AdditionalTools` 同时包含：

- `list_characters`
- `create_character`
- `edit_character`
- `set_workspace_path`

本需求只要求 Coding 角色获得工作区控制能力，不等于要重新把角色管理、聊天室人设或其他协调能力全部注入固定编程代理。

如果直接复用整个 `AdditionalTools`，会破坏现有的职责隔离，并扩大 CodingAgent 的工具权限。

### 原因三：当前运行已经持有稳定工作区租约

`CodingAgent.RunAsync` 在返回流式结果前已经取得 `CodingWorkspaceToolLease`。该租约必须在整次运行中保持稳定，否则工作区切换可能导致模型后续工具调用访问已释放的 Roslyn Session，或者在同一任务中混用两个代码仓库。

因此，即使本轮通过 `set_workspace_path` 成功切换了聊天室工作区，也不能把当前运行的工具集合原地替换为新工作区工具。

安全语义必须是：

- 当前运行继续使用启动时租约
- 新工作区只影响后续 Coding 运行
- 不在一次运行中跨工作区切换代码工具

## 目标

1. Coding 角色可以看到并调用聊天室提供的 `set_workspace_path`
2. Coding 路径中的设置工作区操作必须继续等待现有人工审批 UI
3. 审批通过后仍由 `ChatRoomManager.SetWorkspacePathAsync` 统一发布和回滚
4. `AgentLib.Coding` 不引用 `AgentLib.ChatRoom`
5. Coding 角色只接收显式允许的宿主控制工具，不接收全部聊天室协调工具
6. 当前运行的工作区租约保持不可变
7. 工作区切换后，下一次 Coding 发言自然使用新工具
8. Standard 路径行为保持不变

## 非目标

1. 不让 `CodingAgent` 自己持久化聊天室工作区
2. 不在 `CodingAgent` 中复制一份 `WorkspacePathTools`
3. 不让 Coding 角色获得 `create_character`、`edit_character` 等工具
4. 不允许一次 Coding 运行在中途无边界地切换代码仓库
5. 首版不自动重放原始任务，也不在工作区设置成功后静默启动第二次模型调用
6. 不修改角色定义来保存工作区路径或工具列表

## 核心设计

### 设计一：增加通用的宿主控制工具通道

`CodingAgent` 只依赖 `Microsoft.Extensions.AI.AITool`，不认识聊天室类型。

建议为 `CodingAgent.RunAsync` 增加可以传入宿主控制工具的重载，现有重载继续委托到新重载并传入空集合：

```text
RunAsync(
  IManualSendMessageContext context,
  IReadOnlyList<AIContent> contents,
  string? workspacePath,
  IReadOnlyList<AITool> hostControlTools,
  CancellationToken cancellationToken)
```

纯文本重载提供同等能力。

`hostControlTools` 的语义应明确为：

- 由宿主按本轮需要显式提供
- 不进入 `CodingWorkspaceToolProvider`
- 不随工作区 Session 持久化
- 不由 `CodingAgent` 创建或解释
- 只在本次运行中可见

不建议把参数命名为 `additionalTools`，因为它不是任意扩展工具集合，而是经过宿主选择的控制面工具。

### 设计二：用可选能力接口完成运行时审批绑定

`IManualSendMessageContext` 是公共接口，直接增加必实现成员会破坏仓库外已有实现。建议保持该接口不变，另增一个可选能力接口，例如：

```text
IManualSendRuntimeToolBinder
  - BindRuntimeTools(
  IReadOnlyList<AITool> tools,
  CancellationToken cancellationToken)
```

内部 `ManualSendMessageContext` 同时实现 `IManualSendMessageContext` 和该能力接口，并使用自己持有的：

- 当前 `CopilotChatSession`
- 当前助手消息
- `CopilotChatManager`
- 本轮取消令牌

创建对应的 `CopilotChatContext`，然后只对传入工具执行 `HumanApprovalTool.BindRuntimeTool` 和审批项注册。

`CodingAgent` 的兼容规则为：

- 没有宿主控制工具时，只要求基础 `IManualSendMessageContext`，现有调用和自定义实现不受影响
- 传入宿主控制工具时，要求上下文同时实现 `IManualSendRuntimeToolBinder`
- 缺少该能力时在模型运行开始前抛出明确的 `NotSupportedException`，绝不能把配置态审批工具原样执行

该接口不得：

- 自动追加默认工具
- 修改 `DefaultTools`
- 创建新会话
- 改变 AgentSession
- 隐式合并其他宿主工具

建议把 `CopilotChatManager.ResolveTools` 中“绑定指定工具”和“追加默认工具”拆成两个内部步骤，标准发送继续组合调用，手动发送只复用前者，避免复制审批注册逻辑。

### 设计三：CodingAgent 合并租约工具和宿主控制工具

CodingAgent 每次运行按以下顺序准备工具：

1. 取得当前 `CodingWorkspaceToolLease`
2. 快照复制 `hostControlTools`
3. 通过可选的 `IManualSendRuntimeToolBinder.BindRuntimeTools` 绑定宿主控制工具
4. 合并租约工具与已绑定宿主工具
5. 校验工具名称冲突
6. 把合并结果一次性写入 `ChatOptions.Tools`

推荐优先级：

```text
工作区租约工具
  + 宿主控制工具
```

出现同名工具时应立即抛出配置异常，不静默覆盖。这样可以避免宿主工具伪装成 `read_file`、`run_build` 等编程工具，也避免未来 Coding 工具新增同名函数后产生不可见行为变化。

### 设计四：聊天室显式区分标准工具和 Coding 宿主工具

不要让 `CodingChatRoomRoleExecutor` 从 `AdditionalTools` 中按名称猜测哪个工具可以传入 CodingAgent。

建议扩展 `ChatRoomRoleExecutionContext`，分别携带：

- `AdditionalTools`：Standard 路径使用的完整本轮工具
- `HostControlTools`：允许专用 Agent 路径使用的宿主控制工具

`ChatRoomAutoLoopRunner.StepAsync` 组装方式为：

```text
roleManagementTools = 角色管理工具
workspacePathTools = 设置工作区工具

AdditionalTools = roleManagementTools + workspacePathTools
HostControlTools = workspacePathTools
```

执行器规则：

- `StandardChatRoomRoleExecutor` 继续使用 `AdditionalTools`
- `CodingChatRoomRoleExecutor` 只把 `HostControlTools` 传给 `CodingAgent`
- 未来其他专用 Agent 自行决定是否接受宿主控制工具

这比根据工具名称过滤更可靠，也不会让 `AgentLib.Coding` 反向引用 ChatRoom。

### 设计五：复用聊天室工作区工具，不复制路径切换逻辑

`set_workspace_path` 的最终执行仍调用：

```text
ChatRoomManager.SetWorkspacePathAsync
```

不要在 `CodingAgent` 中直接调用：

- `CodingWorkspaceToolProvider.SetWorkspacePathAsync`
- `CodingChatRoomRoleExecutor.SetWorkspacePathAsync`
- `CopilotChatManager.WorkspacePath = ...`

否则只能更新单个 Coding 角色，无法保证聊天室内其他角色与管理器权威状态一致。

可以在 `WorkspacePathTools` 中提取共享的路径切换核心，再提供两个展示适配器：

1. Standard 版本：保持现有描述和结果文本
2. Coding 版本：同样调用管理器，但明确提示“当前 Coding 运行仍使用启动时工具快照，新路径从下一次发言生效”

两个版本必须复用同一审批配置和同一路径切换实现，不能形成两套校验逻辑。

## 运行语义

### 已设置工作区时

```text
Coding 角色开始发言
  → CodingAgent 取得工作区 A 的 Lease
  → 工具集合 = A 的编程工具 + set_workspace_path
  → 模型可正常处理 A
```

如果模型请求切换到工作区 B：

```text
调用 set_workspace_path(B)
  → UI 显示审批
  → 用户同意
  → ChatRoomManager 向全部角色发布 B
  → 当前运行继续持有 A 的 Lease
  → 当前运行结束后释放 A 的 Lease
  → 下一次 Coding 发言取得 B 的 Lease
```

当前运行不得在切换成功后继续对 B 执行代码工具。

### 尚未设置工作区时

```text
Coding 角色开始发言
  → 取得空 Lease
  → 工具集合只有 set_workspace_path
  → 模型请求设置路径
  → 用户审批
  → 聊天室发布工作区
  → 本轮结束并提示下一条消息继续任务
```

这是首版推荐的安全引导流程。

若产品后续要求“一次用户消息内自动设置工作区并继续原任务”，应单独设计可观察的两阶段运行：

1. 工作区引导阶段
2. 新 Lease 下的任务继续阶段

不能通过热替换 `ChatOptions.Tools` 或让每个工具调用临时读取最新工作区来规避该设计，因为那会破坏单次运行的工作区一致性。

## 取消、拒绝与失败处理

### 用户拒绝审批

- `ChatRoomManager.WorkspacePath` 不变
- 当前 Coding Lease 不变
- 工具返回现有拒绝信息
- Coding 运行可以正常结束

### 等待审批时取消

- 审批等待使用 Coding 运行的取消令牌
- 取消后不得调用路径切换函数
- Coding 运行按现有取消路径释放 Lease 和运行 CTS

### 候选工作区创建失败

- `ChatRoomManager.SetWorkspacePathAsync` 抛出原始异常
- 已提交工作区保持不变
- 已更新角色按现有逻辑回滚
- 工具输出友好失败信息
- 当前 Coding Lease 不受影响

### 工具名称冲突

- 在创建 `ChatClientAgent` 前失败
- 不启动模型流
- 清理助手占位符
- 释放已取得的 Lease
- 错误中列出冲突工具名

## 需要修改的核心文件

### AgentLib 手动发送与审批绑定

- `SemanticKernelSamples/AgentLib/AgentLib/Model/SendMessages_/IManualSendMessageContext.cs`
  - 保持现有基础契约不变；文档可补充可选能力的关系
- 建议新增 `SemanticKernelSamples/AgentLib/AgentLib/Model/SendMessages_/IManualSendRuntimeToolBinder.cs`
  - 定义“只绑定指定工具”的可选能力，避免破坏现有上下文实现
- `SemanticKernelSamples/AgentLib/AgentLib/Model/SendMessages_/ManualSendMessageContext.cs`
  - 实现可选能力，并基于当前助手消息和会话绑定审批工具
- `SemanticKernelSamples/AgentLib/AgentLib/CopilotChatManager.cs`
  - 拆分“绑定指定工具”和“追加默认工具”逻辑，供标准发送与手动发送复用
- `SemanticKernelSamples/AgentLib/AgentLib/Tools/HumanApprovalTool.cs`
  - 原则上不改变审批语义；仅在需要时调整内部可复用入口或文档

### AgentLib.Coding

- `SemanticKernelSamples/AgentLib/AgentLib.Coding/CodingAgent.cs`
  - 增加宿主控制工具重载
  - 快照、绑定、合并并校验工具
  - 保持 `AIContextProviders = []`
  - 保持 Lease 在完整运行生命周期内有效
- `SemanticKernelSamples/AgentLib/AgentLib.Coding/CodingAgentRunResult.cs`
  - 首版无需修改；若后续增加两阶段自动续跑，再单独评估状态暴露

### AgentLib.ChatRoom

- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom/ChatRoomRoleExecutionContext.cs`
  - 区分 Standard 全量工具和专用 Agent 宿主控制工具
- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom/ChatRoomRole.cs`
  - 将两类工具传入统一执行上下文，不按具体执行器分支
- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom/ChatRoomManager.ChatRoomAutoLoopRunner.cs`
  - 分别组装角色管理工具和工作区控制工具
- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom/CodingChatRoomRoleExecutor.cs`
  - 把 `HostControlTools` 传给 CodingAgent
- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom/StandardChatRoomRoleExecutor.cs`
  - 继续只使用完整 `AdditionalTools`
- `SemanticKernelSamples/AgentLib/AgentLib.ChatRoom/Tools/WorkspacePathTools.cs`
  - 复用同一路径切换核心，为 Coding 场景提供准确的生效时机说明

## 测试计划

### AgentLib 测试

新增或扩展手动发送测试，验证：

1. 配置态审批工具经手动上下文绑定后会创建审批项
2. 审批前内部函数未执行
3. 同意后执行一次
4. 拒绝后不执行
5. 取消审批等待后不执行
6. 绑定指定工具不会自动追加默认工具
7. 普通非审批工具保持原样可调用
8. 仅实现基础 `IManualSendMessageContext` 的旧上下文在无宿主工具时仍可运行
9. 旧上下文收到宿主控制工具时明确失败，不会绕过审批

### AgentLib.Coding.Tests

在 `CodingAgentTests` 增加：

1. 宿主控制工具与 Lease 工具同时可见
2. `context.DefaultTools` 仍不会自动进入 Coding 工具集合
3. 配置态宿主审批工具会被正确绑定
4. 宿主工具名称与 Lease 工具冲突时立即失败
5. 运行期间切换工作区后，本轮仍使用旧 Lease
6. 下一轮使用新工作区工具
7. 空工作区时仍可看到 `set_workspace_path`
8. 取消或异常时宿主工具绑定不会泄漏 Lease

### AgentLib.ChatRoom.Tests

在 `ChatRoomRoleExecutorTests`、`ChatRoomManagerIntegrationTests` 增加：

1. Coding 执行器能看到 `set_workspace_path`
2. Coding 执行器看不到 `create_character`、`edit_character`
3. Coding 路径调用该工具时 UI 审批项出现
4. 同意后 `ChatRoomManager.WorkspacePath` 更新
5. 拒绝或取消后路径不变
6. 路径更新失败时聊天室和全部角色保持旧路径
7. 当前 Coding 运行不热切换工具
8. 下一次 Coding 发言获得新工作区工具
9. Standard 路径现有审批测试继续通过

## 分步实施计划

1. 增加可选的手动发送运行时工具绑定接口，并由内置上下文实现
2. 重构 `CopilotChatManager` 的工具绑定逻辑，使标准发送和手动发送共享同一实现
3. 为 `CodingAgent.RunAsync` 增加宿主控制工具重载，并保持旧重载兼容
4. 在 CodingAgent 中实现工具快照、审批绑定、名称冲突校验和最终合并
5. 扩展角色执行上下文，区分 Standard 全量工具与专用 Agent 宿主控制工具
6. 调整自动循环工具组装，只把工作区控制工具放入 Coding 宿主通道
7. 调整 Coding 执行器，把宿主控制工具传给 CodingAgent
8. 调整工作区工具的 Coding 场景描述，明确新路径从下一次 Coding 发言生效
9. 补充审批、取消、拒绝、失败回滚、空工作区和 Lease 稳定性测试
10. 运行 AgentLib、AgentLib.Coding、AgentLib.ChatRoom 和 ChatRoom Shell 相关测试
11. 构建完整解决方案并审查 Git 变更，确认没有把 ChatRoom 依赖引入 AgentLib.Coding

## 验收标准

1. 用户直接 `@编程助手` 且尚未设置工作区时，Coding 角色能够调用 `set_workspace_path`
2. UI 会显示现有人工审批面板，审批前路径不发生变化
3. 审批同意后，`ChatRoomManager.WorkspacePath` 和全部角色的工作区状态一致更新
4. 审批拒绝、取消或候选创建失败时，旧工作区保持可用
5. Coding 角色看不到角色管理工具
6. CodingAgent 当前运行始终只使用一个稳定工作区 Lease
7. 工作区切换后的下一次 Coding 发言使用新工作区工具
8. Standard 角色的工具和审批行为没有回归
9. `AgentLib.Coding` 不引用任何 ChatRoom 类型
10. 工作区路径和工具实例仍不进入角色定义、模板或会话持久化

## 后续可选增强

如果产品需要“设置工作区后自动继续原任务”，应新增独立计划，设计以下能力：

- 可观察的两阶段 Coding 运行状态
- 原始用户输入的安全重放或继续语义
- 两阶段共用同一个公开流式消息的方式
- AgentSession 中工具调用与继续消息的历史顺序
- 工作区设置成功但第二阶段失败时的错误展示
- 用户取消时同时终止两个阶段

在这些问题明确前，不应以动态工具代理、热替换工具集合或跨工作区工具调用作为快捷实现。
