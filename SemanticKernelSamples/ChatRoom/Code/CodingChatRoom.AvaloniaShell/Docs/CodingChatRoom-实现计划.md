# CodingChatRoom 实现计划

## 文档状态

- 状态：实施中（阶段 1-7 已完成，下一步实施阶段 8）
- 交付类型：实现计划与实时实施记录
- 目标项目：`SemanticKernelSamples/ChatRoom/Code/CodingChatRoom.AvaloniaShell`
- 关联设计：`CodingChatRoom-架构设计.md`
- 关联细节：`CodingChatRoom-实现细节.md`

## 实施进度

| 阶段 | 状态 | 最近验证 |
| --- | --- | --- |
| 阶段 1：建立 Shell 基础结构 | 已完成 | 2026-04-13：结构测试 3/3 通过，完整解决方案构建成功 |
| 阶段 2：固定本地路径与配置启动 | 已完成 | 2026-04-13：启动与路径测试 7/7 通过，完整解决方案构建成功 |
| 阶段 3：补齐 AgentLib 会话历史存储 | 已完成 | 2026-04-13：直接相关回归 18/18 通过，恢复后第二轮发送与最新状态检查点通过 |
| 会话持久化职责简化 | 已完成 | File Store 返回纯数据；Shell 仓储装配完整会话；Manager 无恢复/检查点/运行门；Logger 仅文本 |
| 阶段 4：实现 Shell 会话管理 | 已完成 | 2026-04-13：Shell 测试 17/17 通过，完整解决方案构建成功 |
| 阶段 5：实现 ChatView 消息投影 | 已完成 | 2026-04-13：Shell 回归 26/26 通过，完整解决方案构建成功 |
| 阶段 6：实现直接 CodingAgent 发送 | 已完成 | 2026-04-13：Shell 回归 33/33 通过，CodingAgent 关键生命周期 3/3 通过，完整解决方案构建成功 |
| 阶段 7：实现用户工作路径输入 | 已完成 | 2026-04-13：Shell 回归 42/42 通过，工作区工具事务 21/21 通过，完整解决方案构建成功 |
| 阶段 8-12 | 未开始 | - |

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

### 实施记录

- 状态：已完成。
- 已启用 Avalonia compiled bindings，并建立 `Views`、`ViewModels`、`Services`、`Infrastructure`、`Styles` 和必要转换器。
- `MainView` 使用左侧会话列表、分隔条和右侧聊天区域的两列布局。
- Shell 程序集不引用 `AgentLib.ChatRoom`，主 ViewModel 只组合会话列表与聊天区域。
- 2026-04-13 验证：`ShellStructureTests` 3/3 通过，完整解决方案构建成功。

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

### 实施记录

- 状态：已完成。
- `CodingChatRoomPaths` 将配置、日志和会话固定在显式根目录；生产根目录固定为 `LocalApplicationData/CodingChatRoom`。
- 启动只读取固定的 `AgentApiManagerConfiguration.json`，配置缺失、JSON 损坏、无有效模型或未知 `PrimaryModel` 时直接失败。
- 初始化失败时只显示 `StartupFailureWindow`，不会进入聊天主界面，也不会生成或查找替代配置。
- `FileCopilotChatLogger` 只使用显式 `Logs` 目录；`FileCopilotChatSessionStore` 使用显式 `Sessions` 与 `Logs` 目录。
- 2026-04-13 验证：`CodingChatStartupTests` 7/7 通过，源码检查未发现当前目录、仓库目录或 ChatRoom 环境变量回退，完整解决方案构建成功。

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

### 实施记录

- 状态：已完成。
- 2026-04-13 调查结论：`FileCopilotChatLogger` 已能写入文本、思考、工具、审批、子代理、图片、音频、用量及最新 `AgentSessionState`，但尚无读取、列表和删除 API。
- 当前 XML 根节点只有 `SessionId` 与首条消息时间，缺少标题和格式版本；现有无版本 XML 将作为版本 1 兼容读取。
- 将在 AgentLib 内提取共享消息 XML 编解码，并新增文件会话存储、会话摘要和会话快照；Shell 不复制 XML 编解码。
- AgentSession 恢复复用 `ChatClientAgent.DeserializeSessionAsync`，由 `CopilotChatManager` 提供集中恢复入口，避免 Shell 直接操作会话集合。
- `AgentLib.Tests` 已存在但尚未加入 `ChatRoom.slnx`，阶段 3 测试继续放入该项目，并在本阶段补入解决方案以便统一回归。
- 2026-04-13 测试先行：已将 `AgentLib.Tests` 加入 `ChatRoom.slnx`，新增 `FileCopilotChatSessionStoreTests`，覆盖完整快照往返、摘要排序、损坏文件隔离、旧版无版本兼容、历史与日志删除、同会话单文件更新。
- 新增测试当前按预期因 `FileCopilotChatSessionStore`、`CopilotChatSessionSummary` 和 `CopilotChatSessionSnapshot` 尚未实现而编译失败，下一步开始最小实现。
- 2026-04-13 已实现 `FileCopilotChatSessionStore`、`CopilotChatSessionSummary`、`CopilotChatSessionSnapshot` 与共享 `CopilotChatHistoryXmlCodec`。
- 存储支持完整快照保存、摘要列表、按 ID 加载、损坏文件跳过、旧版无版本 XML 兼容、历史与日志删除，以及同会话单文件覆盖更新。
- `FileCopilotChatLogger` 已改用共享 XML 编解码，不再维护独立的消息片段序列化实现；新写入历史包含格式版本。
- 当前验证：会话存储与原日志测试合计 10/10 通过。
- 2026-04-13 曾为 `CopilotChatManager` 增加集中恢复与状态序列化入口；该过渡实现随后被职责简化方案取代。
- 当前恢复流程位于 Shell 完整会话仓储：不会创建欢迎消息或重复写日志，存在状态时通过 `ChatClientAgent.DeserializeSessionAsync` 恢复 `AgentSession`。
- 当前验证：存储、日志与管理器恢复测试合计 13/13 通过。
- 2026-04-13 继续实施审核：现有恢复测试只验证反序列化后的状态可再次序列化，尚未实际执行恢复后的第二轮模型调用；现有日志测试只验证状态提供器结果覆盖 XML，尚未证明标准发送在助手流完成后抓取更新后的 `AgentSessionState`。
- 阶段 3 剩余工作收敛为两项测试先行验收：恢复会话后继续第二轮发送，以及标准发送完成后历史文件保存最新代理状态；若测试暴露时机或恢复缺陷，再做最小修正。
- 2026-04-13 已补充上述两项端到端测试。恢复持久化快照后实际执行第二轮发送的测试通过，确认模型输入同时包含第一轮与第二轮用户消息。
- 最新状态检查点测试按预期失败：标准发送先通过 `AppendMessageAsync` 记录助手占位消息，流完成后再次调用日志器记录同一助手对象，加载历史得到 3 条消息而不是 2 条；下一步将拆分“加入公开消息”和“完成后持久化检查点”的写入职责，消除重复历史消息。
- 2026-04-13 已完成最小修正：标准发送中的助手占位对象只加入当前 `CopilotChatSession`，不在占位阶段写入日志；流式响应完成后再由现有日志调用一次性保存最终助手消息与最新 `AgentSessionState`。
- 修正后恢复与检查点测试 5/5 通过；历史快照只包含用户消息和最终助手消息，不再重复保存占位助手消息，且持久化状态与发送完成后的当前代理状态一致。
- 2026-04-13 阶段 3 直接相关回归：`FileCopilotChatSessionStoreTests`、`FileCopilotChatLoggerTests`、`CopilotChatManagerSessionRestoreTests`、`CopilotChatManagerSessionTests` 合计 18/18 通过，阶段目标完成。
- 后续职责简化已完成：Manager 删除恢复、检查点和会话运行门，只保留会话集合、选择及全局聊天状态；`RemoveSession` 使用对象实例语义。
- File Store 加载返回 `CopilotChatSessionPersistenceData`，保存接收 `CopilotChatSession` 与可选 `JsonElement`；XML 格式版本保持为 2。
- Shell 的 `FileCodingChatSessionStore` 负责完整会话恢复和 AgentSession 序列化，`CodingChatApplication` 直接保存运行开始时捕获的会话实例；启动会跳过无法加载/恢复的最近历史。
- ChatRoom 已删除无生产调用的 `SaveRoleMessageAsync` 及角色 Logger XML 路径，保留公开文本日志和独立 `agent-session-state.json`。
- 异常优先级已固定：运行失败或取消优先于保存失败；运行成功时保存失败向调用方抛出。
- 扩展运行 `CopilotChatManagerSendMessageTests` 时发现既存的 `SendMessage_WhenSubAgentDoesNotReturnOutput_RetriesWithToolRequiredPrompt` 单独复跑仍期望 2 次、实际 4 次；该测试位于子代理重试路径，不经过本次会话存储与日志检查点改动，记录为非阶段 3 阻塞回归，后续在相关功能阶段处理。

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

### 实施记录

- 状态：已完成。
- 2026-04-13 阶段 3 验收完成后进入阶段 4，开始审核 Shell 启动组合根、会话列表骨架、存储接入点与测试接缝。
- 2026-04-13 审核结论：当前 `SessionListViewModel` 只有空集合和永久禁用的新建命令，`App.axaml.cs` 直接创建静态 ViewModel，`CodingChatRuntime` 尚未持有 `FileCopilotChatSessionStore` 或应用协调服务。
- `CopilotChatManager` 构造时会自动创建一个带预设欢迎消息的可复用空会话；阶段 4 将保留该空会话语义，无历史时直接选择它，有历史时恢复最近会话并移除未使用的初始空会话，避免额外欢迎会话残留。
- 为覆盖打开失败恢复旧选择、删除失败保留 UI 项等文件 I/O 失败行为，将增加仅用于 Shell 协调层的薄会话存储契约，生产适配 `FileCopilotChatSessionStore`；XML 编解码仍唯一保留在 AgentLib。
- `CodingChatApplication` 将集中初始化、新建、打开、删除和活动发送门控，`SessionListViewModel` 只负责摘要投影、选择状态和命令可用性，不直接操作 `CopilotChatManager.ChatSessions`。
- 2026-04-13 测试先行：已新增 `CodingChatApplicationTests`，覆盖无历史保留空会话、有历史打开最近项并清理初始空会话、真正空会话复用、非空会话新建置顶、打开失败恢复旧选择、删除失败保留列表项、活动发送期间禁用全部会话命令。
- 新增测试当前按预期因 `ICodingChatSessionStore` 与 `CodingChatApplication` 尚未实现而编译失败，下一步开始最小实现。
- 2026-04-13 已实现薄 `ICodingChatSessionStore` 契约及 `FileCodingChatSessionStore` 生产适配器，文件 XML 编解码仍全部复用 AgentLib 的 `FileCopilotChatSessionStore`。
- 已实现 `CodingChatApplication`，集中负责摘要初始化、最近会话恢复、初始空会话清理、新建与空会话复用、打开失败回滚、删除成功后更新集合以及活动发送门控。
- `SessionListViewModel` 已改为投影应用摘要和当前选择，并提供新建、打开、删除三个异步命令；命令可用性统一跟随 `CanChangeSession`。
- 当前验证：阶段 4 服务层行为测试 7/7 通过。
- 2026-04-13 已将 `FileCodingChatSessionStore` 与 `CodingChatApplication` 接入 `CodingChatStartup` 和 `CodingChatRuntime`；启动成功后先加载会话摘要并恢复最近历史，再创建真实 `SessionListViewModel`。
- 左栏 `ListBox` 已绑定当前会话选择，选择变化显式执行打开命令；每项提供删除命令，显示内容仍只有标题、消息数和开始时间，不包含角色数、设置或角色大厅入口。
- 当前验证：`CodingChatRoom.AvaloniaShell` 项目编译成功，Avalonia compiled bindings 与 XAML 编译通过。
- 2026-04-13 Shell 全量回归：结构、固定启动与会话管理测试合计 17/17 通过，无阶段 4 功能回归；测试输出仅包含现有 MSTEST0045 协作取消建议。
- 2026-04-13 最终验证：完整解决方案构建成功，本地改动审查无新增意见，`git diff --check` 通过；阶段 4 完成，下一步进入阶段 5 消息投影。

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

### 实施记录

- 状态：已完成。
- 2026-04-13 阶段 5 审核结论：当前 `ChatViewModel` 只有空消息集合和永久禁用命令，`ChatView` 仍是占位说明；参考 ChatRoom 已具备文本、思考、工具、审批、子代理、用量、复制和审批交互模板，但包含角色头像、角色颜色、@mention 与自动循环状态，不能直接照搬。
- 阶段 5 将让 `ChatViewModel` 直接订阅 `CopilotChatManager.SelectedSession.ChatMessages`，切换会话时退订旧集合并释放旧 `MessageItemViewModel`；作者固定使用 `CopilotChatMessage.Author`，审批入口复用 `CopilotChatManager.ApproveToolExecution` 与 `RejectToolExecution`。
- 自动滚动由 View 根据用户是否接近底部维护，新增消息或流式内容变化时仅在用户未向上浏览时跟随到底部；阶段 5 不接入真实发送，发送与停止命令继续保持禁用。
- 2026-04-13 测试先行：已新增 `ChatViewModelTests`，覆盖用户消息靠右/Copilot 消息靠左、工具与审批片段模板选择、流式文本与用量属性刷新、复制正文与整条消息的内容来源、切换会话后旧集合不再更新当前 UI，以及审批入口复用聊天管理器。
- 新增测试当前按预期因 `MessageItemViewModel` 实时投影、`ChatMessageItemTemplateSelector`、`ChatViewModel` 会话切换与审批 API 尚未实现而编译失败，下一步开始最小实现。
- 2026-04-13 已新增独立 `MessageItemViewModel`，直接包装 `CopilotChatMessage`，公开作者、正文、完整内容、消息片段、时间、用户/助手/系统方向和用量摘要，并桥接底层消息的流式文本、完整内容与用量属性通知。
- `MessageItemViewModel` 实现显式释放，退订底层消息事件，为后续会话切换时彻底解除旧消息投影订阅提供生命周期边界。
- 已新增精简 `ChatMessageItemTemplateSelector`，只按文本、思考、普通工具、审批工具和子代理片段选择模板，不包含角色或聊天室领域逻辑；相关生产文件通过文件级编译检查。
- 2026-04-13 已实现 `ChatViewModel` 对 `CopilotChatManager.SelectedSession` 的直接投影：启动时加载当前消息，新增消息实时加入 UI，选择变化时退订旧会话和旧消息项、清空投影并订阅新会话。
- 当前会话标题随 `CopilotChatSession.Title` 更新；`CurrentSessionId` 可用于确认投影来源。审批同意与拒绝入口直接委托 `CopilotChatManager`，未复制审批状态逻辑。
- `App.axaml.cs` 已改为把运行时 `CopilotChatManager` 注入 `ChatViewModel`；阶段 5 的发送与停止命令仍保持禁用。
- 当前验证：`ChatViewModelTests` 6/6 通过，确认消息方向、模板选择、流式与用量刷新、复制内容、旧会话退订和审批委托行为。
- 2026-04-13 已将 ChatRoom 的消息展示能力精简迁移到 `CodingChatRoom.AvaloniaShell/Views/ChatView.axaml`：用户消息靠右，Copilot 消息靠左，系统消息居中；支持文本、可折叠思考、普通工具、审批工具、子代理嵌套片段和 Token 用量摘要。
- 迁移时已移除角色头像、角色颜色、@mention、聊天室发言状态和多角色名称逻辑；消息作者直接绑定 `CopilotChatMessage.Author`。
- 已接入“复制正文”“复制整条”菜单及审批同意/拒绝按钮，View 只负责剪贴板和点击事件，业务决策仍由 `ChatViewModel`/`CopilotChatManager` 承担。
- 已补作者、思考、用量和辅助标签样式；完整解决方案构建成功，`ChatViewModelTests` 保持 6/6 通过。
- 2026-04-13 已实现尊重用户浏览位置的自动滚动：新增可测试的 `ChatAutoScrollState`，用户接近底部时在新增消息或流式内容引起布局增长后继续滚到底部；用户向上浏览后暂停强制跟随，重新滚回底部后恢复。
- `ChatView` 在 DataContext 或当前会话 ID 变化时重置到底部，通过 `ScrollViewer.ScrollChanged` 区分用户偏移变化与内容/视口布局变化，滚动动作延后到布局完成阶段执行。
- 当前验证：自动滚动 3 个测试与消息投影 6 个测试合计 9/9 通过。
- 2026-04-13 Shell 全量回归首次运行 25/26，通过项外发现 `CodingChatRoomPaths` 的生产配置文件名漂移为 `AgentConfiguration.json`，与阶段 2 已固定的 `AgentApiManagerConfiguration.json` 契约及现有测试不一致。
- 已恢复生产常量为 `AgentApiManagerConfiguration.json`；`CodingChatRoom.AvaloniaShell.Tests` 全量回归现为 26/26 通过。
- 2026-04-13 最终验证：完整解决方案构建成功，本地改动审查无新增意见，`git diff --check` 通过；生产 Views/ViewModels/Styles 源码未发现角色头像、角色颜色、@mention 或聊天室发言状态残留。
- 阶段 5 完成，下一步进入阶段 6：实现直接 `CodingAgent` 发送、停止、`Ctrl+Enter` 与完整运行生命周期。

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

### 实施记录

- 状态：已完成。
- 2026-04-13 阶段 6 审核结论：组合根已经创建并持有 `CopilotChatManager` 与 `CodingAgent`，但 `CodingChatApplication` 目前只提供会话变更门控，`ChatViewModel` 的发送与停止命令仍永久禁用。
- `CreateManualSendMessageContextAsync` 只创建绑定当前会话的用户/助手消息和延迟代理上下文；`CodingAgent.RunAsync` 会把这两个对象加入当前 `CopilotChatSession`，并将流式响应持续写入同一个助手对象，因此 Shell 不应再次追加消息或创建自己的助手占位对象。
- `CodingAgentRunResult.CompletionTask` 覆盖模型流、空回复占位清理、运行 CTS 释放和工作区 Lease 释放，阶段 6 必须以该任务作为活动发送的完整生命周期，不能只等待 `RunAsync` 返回。
- 当前手动发送完成后没有自动执行标题生成或完整会话检查点保存。阶段 6 将在 `CompletionTask` 成功、取消或异常收束后刷新摘要，并在可序列化代理状态时保存完整会话快照，确保最终助手内容和最新 `AgentSessionState` 可恢复。
- 发送测试需要隔离真实模型流和密封的 `CodingAgent`；将只为 Shell 协调层增加可注入的最薄运行契约，生产适配现有 `CodingAgent`，不复制 Coding 流程，也不引入 ChatRoom 自动循环 API。
- 2026-04-13 测试先行：已新增 `CodingChatSendingTests`，覆盖单次发送只启动一次并复用同一助手对象、活动期间拒绝重复发送、停止取消完整生命周期、异常后恢复可发送状态，以及空回复清除占位符并保存最终会话。
- 新增测试当前按预期因 `ICodingChatRunner`、三参数 `CodingChatApplication` 构造函数、`SendMessageAsync`、`StopActiveRun`、`IsRunActive` 与 `CanSend` 尚未实现而编译失败；下一步开始协调层最小实现。
- 2026-04-13 已新增 Shell 内部最薄 `ICodingChatRunner` 与 `CodingAgentChatRunner` 生产适配器；适配器只负责创建 `IManualSendMessageContext` 并调用现有 `CodingAgent.RunAsync`，未复制 Coding 流程或引入 ChatRoom API。
- `CodingChatApplication` 已实现唯一活动运行 CTS、空白输入校验、重复发送拒绝、停止取消和 `CompletionTask` 完整等待；活动期间继续复用既有会话变更门控，完成、取消或异常后均恢复可发送状态。
- `ICodingChatSessionStore` 已提供完整 `CopilotChatSession` 加载与保存入口。每轮收束时 Shell 仓储序列化当前 `AgentSessionState`、保存最终会话并刷新左侧摘要，空回复保存时不保留助手占位符。
- 当前验证：`CodingChatSendingTests` 5/5 通过，确认单次运行、同一助手对象投影、重复发送拒绝、停止取消、异常恢复和最终快照保存行为。
- 2026-04-13 已将 `CodingChatApplication` 注入 `ChatViewModel`：非空输入且无活动运行时发送命令可用，提交后立即清空已捕获输入；运行期间显示停止命令并统一委托应用活动 CTS 取消。
- `ChatViewModel` 订阅应用状态变化，实时刷新 `CanSend`、`IsRunning` 和命令可执行状态；取消显示“已停止”，异常显示明确失败信息，异常不会从异步命令形成未观察异常，后续仍可继续发送。
- `App.axaml.cs` 已改为使用包含应用协调器的真实 `ChatViewModel` 构造函数。当前验证：`ChatViewModelTests` 8/8 通过，其中新增发送输入清空与停止取消测试 2/2 通过。
- 2026-04-13 已在 `ChatView` 输入框接入 `Ctrl+Enter`：仅当发送命令可执行时触发发送并标记按键已处理，普通 Enter 继续用于多行输入；完整解决方案构建验证通过，Avalonia compiled bindings 与事件绑定编译成功。
- 2026-04-13 阶段 6 针对性回归：发送协调、消息投影和会话管理合计 20/20 通过；`CodingAgent` 连续两轮复用同一 `AgentSession`、模型初始化失败清理占位符、运行取消释放 Lease 并允许后续运行合计 3/3 通过。
- 针对性回归未发现新的生命周期缺陷；停止路径传播标准 `OperationCanceledException` 语义，应用协调层在取消后仍完成最终快照保存与状态恢复。
- 2026-04-13 Shell 全量回归：`CodingChatRoom.AvaloniaShell.Tests` 33/33 通过，阶段 1-5 的结构、固定启动、会话管理、消息投影和自动滚动行为均无回归。
- 完整解决方案构建成功，Avalonia XAML、compiled bindings、AgentLib、AgentLib.Coding 与全部解决方案项目编译通过。
- 2026-04-13 最终审查：本地改动审查无新增意见，`git diff --check` 通过；阶段 6 完成，下一步进入阶段 7：实现用户工作路径输入与事务化工作区切换。

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

### 实施记录

- 状态：实施中。
- 2026-04-13 阶段 7 审核结论：`CodingAgent` 已公开 `PrepareWorkspaceChangeAsync`，现有事务明确支持 Prepare、Apply、发布 UI 状态、`CommitAfterPublish` 和发布失败回滚；`CodingWorkspaceToolProvider` 会让旧工作区资源等待既有 Lease 释放，因此工作区切换不会热替换当前运行的工具集合。
- 当前 `CodingChatApplication.SendMessageAsync` 仍固定向运行器传入空工作区路径，`ChatViewModel` 与 `ChatView` 尚无工作路径绑定和命令；阶段 7 将新增 `CodingWorkspaceController`，并让每轮发送在启动时读取其 `CommittedWorkspacePath` 快照。
- Controller 将统一负责空白路径清除、`Path.GetFullPath` 规范化、目录存在性校验、平台相关路径比较、同路径短路和事务化状态发布；工作路径只保存在运行时，不进入 AgentApi 配置、会话摘要或历史快照。
- UI 状态发布复用 `IMainThreadDispatcher`。用户可在活动发送期间切换工作区，但新路径只影响下一轮发送；当前运行继续使用启动时取得的稳定 Lease。
- 阶段 7 测试将放入 `CodingChatRoom.AvaloniaShell.Tests`，覆盖有效与无效目录、候选失败保留旧路径、同一规范化路径不重复准备、Windows 大小写比较、清除、发布失败回滚、发送路径快照和 ViewModel 命令状态；Roslyn 初始化降级继续由 `AgentLib.Coding.Tests` 现有工具完整性测试回归。
- 2026-04-13 测试先行：已新增 `CodingWorkspaceControllerTests`，覆盖有效目录规范化并提交、不存在目录拒绝、候选准备失败保留旧路径、相同规范化路径短路、Windows 大小写忽略、空白路径清除和 UI 状态发布失败回滚。
- 新增测试当前按预期因 `CodingWorkspaceController`、`ICodingWorkspaceRuntime` 与 `WorkspaceChangeResult` 尚未实现而编译失败；下一步开始工作区控制器最小实现。
- 2026-04-13 已实现 `CodingWorkspaceController`、薄 `ICodingWorkspaceRuntime` 契约与 `CodingAgentWorkspaceRuntime` 生产适配器。Controller 使用串行事务门统一处理路径切换，并通过现有 `IMainThreadDispatcher` 发布绑定状态。
- 非空路径先去除首尾空白并调用 `Path.GetFullPath`，目录不存在时在候选准备前失败；路径比较默认在 Windows 使用 `OrdinalIgnoreCase`，其他平台使用 `Ordinal`，相同规范化路径不会重复创建工作区资源。
- Controller 按 Prepare、Apply、发布 `CommittedWorkspacePath`/`WorkspaceInput`/状态文本、`CommitAfterPublish` 顺序提交；发布失败时回滚已应用事务并恢复旧绑定状态。空白输入会事务化清除工作区。
- 当前验证：`CodingWorkspaceControllerTests` 7/7 通过。
- 2026-04-13 已将 `CodingWorkspaceController` 接入 `CodingChatApplication` 的发送协调。每轮发送在调用运行器时读取一次 `CommittedWorkspacePath`，并把该快照传给 `CodingAgent.RunAsync`；运行开始后的工作区切换不会改变本轮参数。
- 已新增发送路径测试，确认已提交工作区经过规范化后原样传入运行器；未注入 Controller 的既有测试构造仍保持空工作区兼容。
- 当前验证：`CodingChatSendingTests` 6/6 通过。
- 2026-04-13 已将工作区 Controller 接入 `CodingChatStartup`、`CodingChatRuntime` 与 `App.axaml.cs`；应用启动时工作路径保持空值，不从配置或历史恢复，运行时释放仍统一委托 `CodingAgent.DisposeAsync`。
- `ChatViewModel` 已公开 `WorkspaceInput`、`CommittedWorkspacePath`、`WorkspaceStatusText`、`IsChangingWorkspace`、`CanApplyWorkspace` 和 `ApplyWorkspaceCommand`，并订阅 Controller 属性变化刷新绑定与命令状态。命令异常由 Controller 状态文本呈现并写入诊断日志，不形成未观察异常。
- 已保留不注入工作区 Controller 的既有测试构造，阶段 1-6 的消息投影和发送测试无需伪造工作区依赖。
- 当前验证：`ChatViewModelTests` 9/9 通过，其中新增工作路径应用命令测试 1/1 通过。
- 2026-04-13 已在 `ChatView` 的会话/模型栏下增加独立工作区栏，包含“工作路径”标签、双向输入框、“应用”按钮、切换状态文本和当前已提交路径；空白输入提示明确可用于清除工作区。
- 工作区准备期间输入框与应用按钮禁用，完成或失败后由 Controller 属性通知恢复；消息列表和底部发送栏保持原有布局与行为。
- 当前验证：完整解决方案构建成功，Avalonia XAML 与 compiled bindings 编译通过。
- 2026-04-13 阶段 7 最终回归：`CodingChatRoom.AvaloniaShell.Tests` 42/42 通过，阶段 1-6 的结构、固定启动、会话管理、消息投影、自动滚动和发送生命周期均无回归。
- `CodingWorkspaceToolProviderTests` 21/21 通过，确认 Roslyn Language Server 启动失败时仍保留文件与 CLI 工具、无效候选保留旧工具、清除工作区、事务回滚/提交以及旧 Lease 延迟退休语义。
- 源码检查确认工作路径标识未进入 `CodingChatRoomPaths`、会话存储契约或 AgentLib 历史日志代码；工作路径只存在于运行时 Controller、发送参数和 UI 绑定，重启后为空。
- 完整解决方案构建成功，本地改动审查无新增意见，`git diff --check` 通过；阶段 7 完成，下一步进入阶段 8：增加手动发送审批工具绑定。

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
