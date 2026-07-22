# CodingChatRoom 实现计划

## 文档状态

- 状态：待实施
- 交付类型：实现计划，不包含本轮代码修改
- 目标项目：`SemanticKernelSamples/ChatRoom/Code/CodingChatRoom.AvaloniaShell`
- 关联设计：`CodingChatRoom-架构设计.md`
- 关联细节：`CodingChatRoom-实现细节.md`

## 实施原则

1. `CodingChatRoom.AvaloniaShell` 是单编程助手应用，不是单角色聊天室。
2. 依赖保持 `CodingChatRoom.AvaloniaShell → AgentLib.Coding → AgentLib`。
3. 不引用 `AgentLib.ChatRoom`。
4. UI 可以复制 ChatRoom 的样式和消息展示，但不得复制角色、自动循环、设置和聊天室服务。
5. 配置只使用 `AgentApiManagerConfiguration`。
6. 配置、日志和历史唯一根目录为 `LocalApplicationData/CodingChatRoom`。
7. 启动不允许任何配置或路径回退。
8. 历史恢复必须同时恢复公开消息和 AgentSession。
9. 模型设置工作路径必须经过人工审批。
10. 当前 CodingAgent 运行始终持有单一稳定工作区 Lease。
11. 每个阶段都先补测试，再做最小实现并运行相关测试。

## 阶段 1：建立 Shell 基础结构

### 目标

把模板项目变成可承载 MVVM 和复用消息样式的最小 Avalonia Shell，但暂不接入真实模型发送。

### 修改

1. 在 `CodingChatRoom.AvaloniaShell.csproj` 启用 compiled bindings。
2. 复制并调整 `Styles/Colors.axaml`、`Styles/Controls.axaml`、`Styles/MessageBubble.axaml`。
3. 复制 `AvaloniaMainThreadDispatcher`、`ViewModelBase`、命令类型和必要布尔转换器。
4. 创建 `Views`、`ViewModels`、`Services`、`Infrastructure` 目录。
5. 创建两列 `MainView` 骨架。
6. 创建空的 `SessionListView` 和 `ChatView` 骨架。
7. 调整 `App.axaml`、`MainWindow.axaml` 和 `App.axaml.cs` 使用主视图。

### 删除或避免复制

- 角色颜色/角色首字转换器
- `RoleListView`
- `RoleEditView`
- `RoleLobbyView`
- `SettingsView`
- 页面导航枚举和相关 ViewModel

### 验证

- 项目可编译和启动。
- 主窗口只显示左右两列。
- XAML 不引用 `ChatRoom.AvaloniaShell` 或 `AgentLib.ChatRoom` 类型。

## 阶段 2：固定本地路径与配置启动

### 目标

建立无回退的启动组合根。

### 修改

1. 新增 `CodingChatRoomPaths`。
2. 固定以下路径：
   - `AgentApiManagerConfiguration.json`
   - `Logs`
   - `Sessions`
3. 在 `App.InitializeAppCoreAsync` 中加载 `AgentApiManagerConfiguration`。
4. 创建 `AgentApiEndpointManager` 并加载配置。
5. 启动时读取 `PrimaryModel` 完成可用性校验。
6. 新增启动失败窗口或错误视图。
7. 创建显式目录参数的日志/历史组件。

### 测试

1. 固定路径计算。
2. 配置文件缺失失败。
3. JSON 损坏失败。
4. 无有效模型失败。
5. 未知 `PrimaryModel` 失败。
6. 不访问当前目录、仓库目录和 ChatRoom 环境变量。
7. 日志路径不落入 `LocalApplicationData/AgentLib`。

### 验证

- 有效配置可启动。
- 无效配置只显示明确错误，不进入聊天主界面。
- 不自动生成或加载替代配置。

## 阶段 3：补齐 AgentLib 会话历史存储

### 目标

让 `CopilotChatSession` 可被列表、保存、加载和删除，并恢复 AgentSession。

### 修改

1. 在 AgentLib 中提取或新增文件会话存储类型。
2. 复用 `FileCopilotChatLogger` 现有消息片段 XML 编解码。
3. 增加会话摘要模型。
4. 增加会话快照模型。
5. 增加标题和格式版本字段。
6. 支持读取现有无版本 XML。
7. 增加历史列表、加载和删除 API。
8. 增加把 `AgentSessionState` 反序列化回 `AgentSession` 的恢复流程。
9. 为 `CopilotChatManager` 增加必要的会话添加、移除或恢复入口，避免 Shell 分散修改集合。
10. 调整日志写入时机，确保助手消息完成后保存最新 AgentSessionState。

### 测试

1. 文本、思考、工具、审批、子代理、图片、音频和用量往返。
2. SessionId、StartedTime、Title 往返。
3. AgentSessionState 往返并可继续第二轮发送。
4. 旧格式兼容。
5. 损坏文件隔离。
6. 会话删除同时删除历史和对应日志。
7. 同一会话连续追加不产生重复文件。

### 验证

- 关闭并重启测试流程后，会话消息与模型上下文一致。
- 历史恢复不产生额外欢迎消息或重复日志。

## 阶段 4：实现 Shell 会话管理

### 目标

完成左栏历史与当前会话切换。

### 修改

1. 创建 `CodingChatApplication`。
2. 创建 `SessionItemViewModel` 和 `SessionListViewModel`。
3. 实现启动加载摘要。
4. 无历史时创建新会话；有历史时打开最近会话。
5. 实现新建、打开和删除。
6. 实现空会话复用规则。
7. 创建最小 `MainViewModel`，只组合会话列表与聊天 ViewModel。
8. 发送期间禁用会话变更。

### 测试

1. 无历史创建空会话。
2. 有历史打开最近项。
3. 真正空会话复用。
4. 非空会话新建后插入列表顶部。
5. 打开失败恢复旧选择。
6. 删除失败不移除 UI 项。
7. 活动发送期间命令禁用。

### 验证

- 左侧列表只显示标题、消息数和时间。
- 没有角色数、设置、角色大厅入口。

## 阶段 5：实现 ChatView 消息投影

### 目标

直接展示 `CopilotChatMessage`，复用 ChatView 的片段展示能力。

### 修改

1. 创建 `MessageItemViewModel` 包装 `CopilotChatMessage`。
2. 复制并改造 `ChatMessageItemTemplateSelector`。
3. 迁移文本、思考、工具、审批和子代理模板。
4. 迁移复制正文和复制整条消息。
5. 迁移审批按钮事件。
6. 移除角色头像颜色、@mention 菜单和当前发言角色状态。
7. 作者固定由 `CopilotChatMessage.Author` 提供。
8. 实现会话切换时的集合退订和重订。
9. 实现自动滚动但尊重用户向上浏览。

### 测试

1. 用户消息靠右，Copilot 消息靠左。
2. 工具和审批片段正确选择模板。
3. 流式文本和用量更新触发属性刷新。
4. 复制正文/整条使用正确内容。
5. 切换会话后旧消息不再更新当前 UI。

### 验证

- 聊天历史视觉接近 ChatRoom ChatView。
- UI 中不出现“角色”“聊天室”“@角色名”等文案。

## 阶段 6：实现直接 CodingAgent 发送

### 目标

用户发送后直接进入 `CodingAgent`，不经过 ChatRoom。

### 修改

1. 在组合根创建 `CopilotChatManager` 和 `CodingAgent`。
2. 在 `CodingChatApplication` 实现单活动发送。
3. 调用 `CreateManualSendMessageContextAsync`。
4. 调用 `CodingAgent.RunAsync`。
5. 把 `CompletionTask` 作为完整运行生命周期观察。
6. 实现发送、停止和状态恢复。
7. 完成后刷新会话标题和左侧摘要。
8. 实现 `Ctrl+Enter`。
9. 处理取消、异常和空回复。

### 测试

1. 单次点击只运行一次。
2. 不调用任何 ChatRoom 自动循环 API。
3. 用户/助手消息进入当前 `CopilotChatSession`。
4. 流式响应绑定同一助手对象。
5. 重复发送被拒绝。
6. 停止触发取消。
7. 异常后恢复可发送状态。
8. 空回复清除占位符。

### 验证

- 连续两轮复用同一个 AgentSession。
- 消息和历史文件在运行完成后可恢复。

## 阶段 7：实现用户工作路径输入

### 目标

让用户在 ChatView 中直接设置 CodingAgent 工作区。

### 修改

1. 创建 `CodingWorkspaceController`。
2. 在 ChatView 顶部增加工作路径输入框、应用按钮和状态文本。
3. 使用 `CodingAgent.PrepareWorkspaceChangeAsync` 完成候选准备。
4. 实现 Apply、发布 UI 状态、CommitAfterPublish。
5. 实现失败回滚和路径规范化。
6. 把 `CommittedWorkspacePath` 传给每轮 `CodingAgent.RunAsync`。
7. 应用关闭时由 CodingAgent 释放工作区资源。

### 测试

1. 有效目录成功。
2. 不存在目录失败。
3. Roslyn 初始化降级不影响文件/CLI 工具可用性。
4. 候选失败保留旧路径。
5. 相同规范化路径不重复切换。
6. Windows 路径大小写比较正确。
7. 清除路径行为（如保留该能力）。

### 验证

- 工作路径不进入 AgentApi 配置或会话持久化。
- 重启后工作路径为空。

## 阶段 8：增加手动发送审批工具绑定

### 目标

让 CodingAgent 安全接收 Shell 的宿主控制工具。

### 修改

1. 在 AgentLib 新增可选的 `IManualSendRuntimeToolBinder`。
2. 由 `ManualSendMessageContext` 实现。
3. 重构 `CopilotChatManager`，提取“只绑定指定工具”的共享逻辑。
4. 保持标准发送的默认工具和审批行为不变。
5. 对有宿主工具但不支持绑定的上下文抛出明确异常。

### 测试

1. 配置态审批工具绑定后创建审批项。
2. 审批前内部函数不执行。
3. 同意后只执行一次。
4. 拒绝后不执行。
5. 等待时取消不执行。
6. 普通工具原样返回。
7. 绑定指定工具不追加默认工具。
8. 旧手动上下文在无宿主工具时兼容。

### 验证

- 不存在配置态审批工具直接透传执行的路径。

## 阶段 9：扩展 CodingAgent 宿主控制工具

### 目标

把 `set_workspace_path` 与 Coding 工作区工具安全合并。

### 修改

1. 为纯文本和多模态 `RunAsync` 增加 `hostControlTools` 重载。
2. 旧重载委托空宿主工具。
3. 快照并绑定宿主工具。
4. 校验工具名称冲突。
5. 合并 Lease 工具与宿主控制工具。
6. 保持 `context.DefaultTools` 和 `AIContextProviders` 不进入 Coding 流程。
7. 失败时在模型启动前释放 Lease 和 CTS。

### 测试

1. Lease 工具和宿主工具同时可见。
2. 默认工具仍不可见。
3. 审批工具正确绑定。
4. 工具同名立即失败。
5. 空工作区时只有宿主工作区工具。
6. 异常、取消时正确释放。
7. 当前运行旧 Lease，下一轮新 Lease。

### 验证

- `AgentLib.Coding` 不引用 Shell 或 ChatRoom 类型。

## 阶段 10：实现模型设置工作路径工具

### 目标

允许编程助手在对话中请求工作路径，并显示审批面板。

### 修改

1. 创建 `WorkspacePathToolFactory`。
2. 创建 `set_workspace_path` AIFunction。
3. 使用 `HumanApprovalTool.Wrap` 和中文审批展示。
4. 工具执行复用 `CodingWorkspaceController.ChangeWorkspaceAsync`。
5. 把该工具作为唯一 `hostControlTools` 传给 CodingAgent。
6. 成功结果明确说明从下一条消息生效。
7. 拒绝、取消和路径失败保持旧路径。

### 测试

1. 模型可看到工具。
2. 审批面板显示路径和说明。
3. 审批前路径不变。
4. 同意后 Controller 与 CodingAgent 已提交路径一致。
5. 拒绝后路径不变。
6. 当前运行不能调用新工作区代码工具。
7. 下一轮获得新工作区工具。
8. 空工作区引导流程可用。

### 验证

- 用户手动设置和模型工具共用同一事务核心。
- 没有两套路径校验或资源切换逻辑。

## 阶段 11：实现可靠关闭

### 目标

窗口关闭时等待运行、历史和 CodingAgent 资源完成清理。

### 修改

1. 创建 `AppLifetimeCoordinator`。
2. 关闭时拒绝新操作。
3. 取消并等待活动发送。
4. 等待最后历史持久化。
5. 异步释放 `CodingAgent`。
6. 解除集合和属性事件订阅。
7. 释放完成后关闭窗口。

### 测试

1. 无活动发送时关闭。
2. 活动发送时取消并等待。
3. 审批等待时关闭。
4. 工作区候选准备时关闭。
5. 重复关闭只执行一次。
6. Roslyn 资源只释放一次。
7. 释放异常被记录且不形成后台未观察异常。

### 验证

- 测试结束后无悬挂任务和外部进程。

## 阶段 12：完整验证与清理

### 构建与测试

按顺序运行：

1. AgentLib 相关会话历史与审批测试。
2. `AgentLib.Coding.Tests`。
3. `CodingChatRoom.Shell.Tests`。
4. `ChatRoom.Shell.Tests`，确认复制/重构 AgentLib 后无回归。
5. `AgentLib.ChatRoom.Tests`，确认审批共享逻辑无回归。
6. 构建完整解决方案。

### 手工验收

1. 删除或暂时重命名固定配置文件，确认启动失败且无回退。
2. 使用有效配置启动。
3. 新建会话并发送代码问题。
4. 设置一个工作目录并执行读取、编辑、构建或测试任务。
5. 让模型请求切换目录，分别验证同意与拒绝。
6. 关闭应用并重启，确认历史与 AgentSession 恢复。
7. 删除历史会话，确认日志与历史文件处理符合设计。
8. 发送中停止和关闭，确认资源清理。

### Git 审查

确认：

- 没有 `AgentLib.ChatRoom` 引用。
- 没有设置页、角色页和自动循环残留。
- 没有配置路径回退。
- 没有把工作区路径持久化。
- 没有直接执行未绑定审批工具。
- 没有把当前运行工具热切换到新工作区。
- 没有复制一份独立的消息历史 XML 编解码到 Shell。
- 没有新增任意 Shell 命令能力。

## 主要文件清单

### CodingChatRoom.AvaloniaShell

- `CodingChatRoom.AvaloniaShell.csproj`
- `App.axaml`
- `App.axaml.cs`
- `MainWindow.axaml`
- `Infrastructure/CodingChatRoomPaths.cs`
- `Infrastructure/AvaloniaMainThreadDispatcher.cs`
- `Infrastructure/AppLifetimeCoordinator.cs`
- `Services/CodingChatApplication.cs`
- `Services/CodingWorkspaceController.cs`
- `Services/WorkspacePathToolFactory.cs`
- `ViewModels/MainViewModel.cs`
- `ViewModels/SessionListViewModel.cs`
- `ViewModels/ChatViewModel.cs`
- `ViewModels/MessageItemViewModel.cs`
- `Views/MainView.axaml`
- `Views/SessionListView.axaml`
- `Views/ChatView.axaml`
- `Views/ChatView.axaml.cs`
- `Views/ChatMessageItemTemplateSelector.cs`
- `Styles/*.axaml`

### AgentLib

- `CopilotChatManager.cs`
- `Model/CopilotChatSession.cs`
- `Model/SendMessages_/ManualSendMessageContext.cs`
- 新增 `Model/SendMessages_/IManualSendRuntimeToolBinder.cs`
- `Logging/FileCopilotChatLogger.cs`
- 新增或提取文件会话存储类型及 DTO
- 对应 AgentLib 测试

### AgentLib.Coding

- `CodingAgent.cs`
- 必要时仅内部调整工具快照辅助代码
- `AgentLib.Coding.Tests/CodingAgentTests.cs`

### 测试项目

- 新建 `CodingChatRoom.Shell.Tests`
- 必要时更新解决方案文件

## 风险与处理顺序

### 风险 1：历史格式与运行时状态不完整

优先在阶段 3 解决，再实现 Shell 历史 UI。禁止先做只能显示文本、不能恢复 AgentSession 的伪历史功能。

### 风险 2：审批绕过

优先在 AgentLib 建立可测试的运行时绑定能力，再把工具交给 CodingAgent。禁止在 Shell 或 CodingAgent 中通过反射、复制 private 实现或直接执行配置态包装器规避。

### 风险 3：当前运行跨工作区

通过 Lease 不变量和测试守住。工具结果只发布下一轮路径，不修改当前最终工具数组。

### 风险 4：日志重复或状态落后

明确用户消息、助手消息和 AgentSessionState 的提交时机。必要时把“日志”和“可恢复会话快照”从同一个逐消息方法中拆成明确的追加与检查点操作。

### 风险 5：关闭竞态

活动运行任务、审批等待、工作区事务和 CodingAgent Dispose 必须有统一关闭所有权，不能使用 fire-and-forget。

## 最终验收标准

1. `CodingChatRoom.AvaloniaShell` 启动后直接显示两列聊天界面。
2. 代码中无聊天室角色领域依赖。
3. 用户消息直接运行 `CodingAgent`。
4. 聊天历史展示覆盖文本、思考、工具、审批、子代理和用量。
5. 历史会话可新建、打开、删除并在重启后继续上下文。
6. 配置只从固定 LocalAppData 文件加载，缺失或损坏时不回退。
7. 日志和历史只存在于 `LocalApplicationData/CodingChatRoom`。
8. 用户可直接应用工作路径。
9. CodingAgent 可通过审批工具请求设置工作路径。
10. 审批前、拒绝后和取消后路径不变。
11. 同意后新路径只对下一次消息生效。
12. 当前运行保持稳定 Lease。
13. 关闭会等待运行、持久化和外部资源释放。
14. AgentLib、AgentLib.Coding、Shell、ChatRoom 回归测试和完整构建全部通过。
