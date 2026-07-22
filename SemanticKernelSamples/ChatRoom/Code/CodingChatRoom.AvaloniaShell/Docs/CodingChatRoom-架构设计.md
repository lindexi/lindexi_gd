# CodingChatRoom 架构设计

## 文档定位

本文定义 `CodingChatRoom.AvaloniaShell` 的目标架构、职责边界和关键行为。

本项目不是 `ChatRoom.AvaloniaShell` 的精简皮肤，也不是只保留一个角色的聊天室。它是一个面向单个编程助手的桌面聊天宿主：直接承载 `AgentLib.Coding.CodingAgent`，围绕代码工作区、流式消息、工具调用、人工审批和会话历史提供最小但完整的交互。

本文只输出设计，不实现代码。

## 已确认的现状

### 新项目当前状态

`CodingChatRoom.AvaloniaShell` 当前是 Avalonia 模板项目：

- 目标框架为 `.NET 10`
- 使用 Avalonia 12.1
- 已引用 `AgentLib.Coding`
- `MainWindow` 仍显示模板欢迎文本
- 尚无 ViewModel、聊天视图、会话服务、配置加载和日志装配

由于 `AgentLib.Coding` 已引用 `AgentLib`，Shell 可以直接使用：

- `CodingAgent`
- `CopilotChatManager`
- `CopilotChatSession`
- `CopilotChatMessage`
- `AgentApiEndpointManager`
- `AgentApiManagerConfiguration`
- `FileCopilotChatLogger`
- 人工审批消息模型

不需要引用 `AgentLib.ChatRoom`。

### ChatRoom 中可复用的内容

可以复用或移植：

- `Styles/Colors.axaml`
- `Styles/Controls.axaml`
- `Styles/MessageBubble.axaml`
- `AvaloniaMainThreadDispatcher`
- `ViewModelBase` 与命令基础设施
- `ChatMessageItemTemplateSelector`
- `ChatView` 中的文本、思考、工具、审批、子代理消息模板
- 复制正文、复制整条消息、审批同意/拒绝、`Ctrl+Enter` 发送等交互
- 左侧会话列表的视觉结构

不能复制为 CodingChatRoom 领域逻辑：

- `ChatRoomService`
- `ChatRoomManager`
- `ChatRoomSession`
- `ChatRoomRole` 与角色定义
- 自动发言循环
- 当前发言角色和轮次状态
- `@mention`
- 角色大厅、角色列表、角色编辑
- 设置页和 `AppSettings`
- 聊天室的多角色持久化格式

### AgentLib 与 CodingAgent 的现有能力

`CopilotChatManager` 已提供：

- 多个 `CopilotChatSession`
- 当前会话选择
- 流式可观察消息
- 会话标题基础能力
- 手动发送上下文 `IManualSendMessageContext`
- 聊天状态与取消基础能力
- 人工审批消息的同意/拒绝入口

`CodingAgent` 已提供：

- 固定编程助手提示词
- Roslyn、工作区文件和 .NET CLI 工具
- 工作区候选准备、应用、回滚和提交事务
- 单次运行稳定的 `CodingWorkspaceToolLease`
- 使用既有 `CopilotChatSession.AgentSession` 连续对话
- 流式返回同一个 `CopilotChatMessage`
- 取消、异常和异步释放

当前缺口：

1. `FileCopilotChatLogger` 能写日志和历史 XML，但没有公开的历史枚举、加载、恢复和删除能力。
2. `CodingAgent` 当前只向模型提供工作区 Lease 工具，不接收 Shell 提供的“设置工作路径”工具。
3. `HumanApprovalTool` 的运行时绑定发生在 `CopilotChatManager` 标准发送路径中；`CodingAgent` 使用手动发送上下文，不能直接把配置态审批工具加入工具列表，否则可能绕过审批。

## 产品目标

1. 启动后直接进入编程助手聊天界面。
2. 主界面固定为两列：左侧历史会话，右侧当前聊天。
3. 不存在角色概念、角色导航和角色管理。
4. 用户发送后直接运行 `CodingAgent`，不经过聊天室自动循环。
5. 聊天区支持流式文本、思考过程、工具调用、审批工具和 Token 用量展示。
6. 用户可在聊天区直接输入和应用工作路径。
7. 编程助手可调用 `set_workspace_path` 请求改变工作路径，但必须等待用户审批。
8. 当前运行持有稳定旧工作区 Lease；工作路径切换只影响下一次发送。
9. 配置只使用 `AgentApiManagerConfiguration`。
10. 配置、日志和历史数据只使用 `LocalApplicationData/CodingChatRoom`。
11. 启动不搜索仓库文件、不读取工作目录配置、不使用环境变量路径覆盖，也不回退到 `AgentLib` 默认目录。
12. 历史会话可列出、打开、新建和删除，并能继续已有 AgentSession 上下文。

## 非目标

1. 不提供设置界面。
2. 不允许运行时编辑模型提供商或模型列表。
3. 不提供角色大厅、角色管理、角色模板或角色持久化。
4. 不实现自动轮询、多 Agent 调度、@mention 或人类插话语义。
5. 不引用 `AgentLib.ChatRoom` 来复用会话或工作区管理。
6. 不把工作区路径写入 `AgentApiManagerConfiguration`。
7. 不持久化当前工作区授权；应用重启后工作区为空，需由用户重新设置或再次审批。
8. 不在一次 CodingAgent 运行中热替换 Roslyn、文件和 CLI 工具。
9. 不增加任意 Shell/脚本执行能力。
10. 首版不支持附件、多模态输入和会话分叉。

## 总体架构

```text
CodingChatRoom.AvaloniaShell
├── App 组合根
│   ├── CodingChatRoomPaths
│   ├── AgentApiManagerConfiguration
│   ├── AgentApiEndpointManager
│   ├── CopilotChatManager
│   ├── CodingAgent
│   ├── CodingWorkspaceController
│   ├── CodingChatSessionRepository
│   └── MainViewModel
├── Views
│   ├── MainView
│   ├── SessionListView
│   └── ChatView
└── ViewModels
    ├── MainViewModel
    ├── SessionListViewModel
    ├── ChatViewModel
    └── MessageItemViewModel

AgentLib
├── CopilotChatManager / CopilotChatSession / CopilotChatMessage
├── 文件日志与会话历史编解码
└── 手动发送运行时审批工具绑定能力

AgentLib.Coding
├── CodingAgent
├── CodingWorkspaceToolProvider
├── CodingWorkspaceToolLease
└── 宿主控制工具运行入口
```

依赖方向必须保持：

```text
CodingChatRoom.AvaloniaShell → AgentLib.Coding → AgentLib
```

不得出现：

```text
CodingChatRoom.AvaloniaShell → AgentLib.ChatRoom
AgentLib.Coding → CodingChatRoom.AvaloniaShell
AgentLib.Coding → AgentLib.ChatRoom
```

## 本地目录设计

唯一根目录：

```text
Environment.SpecialFolder.LocalApplicationData
└── CodingChatRoom
    ├── AgentApiManagerConfiguration.json
    ├── Logs
    │   └── yyyyMMdd
    │       └── yyyyMMdd_HHmmss_{SessionId}.log
    └── Sessions
        └── yyyyMMdd_HHmmss_{SessionId}.xml
```

路径规则：

- `CodingChatRoomPaths` 只根据 `LocalApplicationData` 计算一次绝对路径。
- Shell 必须把显式的 `Logs` 和 `Sessions` 路径传给日志/历史组件。
- 不调用 `new FileCopilotChatLogger()` 的默认构造函数，避免写入 `LocalApplicationData/AgentLib/CopilotChatLogs`。
- 不探测解决方案目录、可执行文件目录或当前工作目录。
- 不读取 `CHATROOM_PERSISTENCE_PATH` 等 ChatRoom 环境变量。
- 可以创建 `CodingChatRoom`、`Logs`、`Sessions` 目录；但缺少或损坏配置文件时必须启动失败，不能生成另一套隐式有效配置后继续运行。

## 配置设计

配置文件固定为：

```text
%LOCALAPPDATA%/CodingChatRoom/AgentApiManagerConfiguration.json
```

启动流程只允许：

```text
AgentApiManagerConfiguration.FromJsonFileAsync
  → AgentApiEndpointManager.LoadConfiguration
  → 校验至少存在一个模型
  → 读取 PrimaryModel
```

不增加 `AppSettings`、`SettingsService`、`ModelProviderService` 或 Shell 自定义配置模型。

失败策略：

- 文件不存在：显示明确错误并退出。
- JSON 无效：显示解析错误并退出。
- 没有有效 Key，导致没有模型注册：显示配置无可用模型并退出。
- `PrimaryModel` 指向未知模型：保留 `AgentApiEndpointManager` 的明确异常并退出。
- 不回退到自动生成模板、仓库示例配置、环境变量或其他目录。

`AgentApiManagerConfiguration.DefaultTemplateFileContent` 可在错误信息或文档中提示，但启动流程不自动把它当作有效配置加载。

## 会话模型

### 权威运行时

`CopilotChatManager` 是当前进程内会话和发送状态的权威对象：

- `ChatSessions`：所有已加载会话
- `SelectedSession`：右侧当前会话
- `SelectedSession.ChatMessages`：当前聊天历史
- `SelectedSession.AgentSession`：模型连续上下文
- `IsChatting`：当前发送状态

Shell 不再创建平行的聊天消息领域模型。`MessageItemViewModel` 直接投影 `CopilotChatMessage`。

### 历史仓储

历史仓储负责：

- 列出 `Sessions` 目录中的会话摘要
- 加载消息片段、会话 ID、创建时间和 AgentSession 状态
- 删除会话历史文件和对应文本日志
- 把恢复结果装入 `CopilotChatManager`
- 保存时保持日志和可恢复历史的一致格式

不应只恢复公开文本而丢弃 `AgentSessionState`，否则界面显示“历史已恢复”，模型却无法延续工具调用和上下文。

建议把现有 `FileCopilotChatLogger` 的 XML 编解码提取为 AgentLib 中可复用的文件会话存储能力，避免 Shell 复制 `CopilotChatMessageItem` 序列化细节。日志写入和历史恢复必须共享同一格式实现。

### 新建与空会话

- 启动后先加载历史摘要。
- 如果存在历史，默认打开最近会话。
- 如果没有历史，创建一个新会话。
- 点击“新建”时，如果当前会话仍只有欢迎消息，则复用该空会话；否则创建新会话。
- 初始欢迎消息沿用 `CopilotChatManager` 的“你好，我是 Copilot。请开始输入你的问题。”。

### 标题

首版使用 `CopilotChatSession` 当前标题机制：首条用户消息生成截断标题。后续可显式调用 `GenerateSessionTitleAsync` 提升标题质量，但标题生成失败不得阻塞发送和持久化。

历史格式需要保存最终标题或提供稳定的标题重建规则，避免每次启动后列表标题退化。

## 直接发送模型

用户发送链路：

```text
ChatViewModel.SendAsync
  → 校验输入、会话、配置和非运行状态
  → 创建本轮 CancellationTokenSource
  → CopilotChatManager.CreateManualSendMessageContextAsync
  → CodingAgent.RunAsync(context, prompt, committedWorkspacePath, hostControlTools, token)
  → 立即获得 CodingAgentRunResult.AssistantChatMessage
  → UI 通过 SelectedSession.ChatMessages 自动显示用户消息和流式助手消息
  → 等待 CompletionTask
  → 生成/更新标题
  → 刷新左侧摘要
  → 确认历史已持久化
```

发送规则：

- 首版同一窗口只允许一个活动发送。
- 发送期间禁用再次发送和会话切换。
- 输入框是否继续可编辑可按现有交互决定，但不能启动第二个发送。
- “停止”只取消当前发送，不关闭会话、不清空输入历史。
- 取消、异常和空回复必须可见且不能留下永久的 `...` 占位符。
- 不调用 `ChatRoomService.HumanInterjectAsync`、`StartAutoLoopAsync` 或任何角色发言 API。

## 工作区设计

### 状态所有权

新增 Shell 级 `CodingWorkspaceController`，它是 UI 当前工作区文本和 CodingAgent 已提交工作区之间的协调边界。

它不拥有 Roslyn 或工具实例；实际资源仍由 `CodingAgent`/`CodingWorkspaceToolProvider` 拥有。

状态至少区分：

- `WorkspaceInput`：输入框尚未应用的文本
- `CommittedWorkspacePath`：已成功提交给 CodingAgent 的规范化路径
- `IsChangingWorkspace`：是否正在准备或发布候选工作区
- `WorkspaceError`：最近一次设置失败信息

### 用户直接设置

用户点击应用工作区时不需要额外审批，因为这是显式用户操作：

```text
输入路径
  → CodingAgent.PrepareWorkspaceChangeAsync
  → 候选工作区完成目录校验和工具初始化
  → 进入短发布临界区
  → transaction.Apply
  → 发布 CommittedWorkspacePath 到 UI
  → transaction.CommitAfterPublish
```

候选创建失败或取消时：

- 保留旧工作区和旧工具
- UI 显示错误
- 不清空用户输入

### 模型请求设置

Shell 创建唯一宿主控制工具：

```text
set_workspace_path(path)
```

该工具：

- 接收绝对工作区路径
- 使用 `HumanApprovalTool.Wrap`
- 显示名为“设置工作区路径”
- 审批说明明确提示本地文件访问范围
- 审批通过后调用同一个 `CodingWorkspaceController.ChangeWorkspaceAsync`
- 审批拒绝或取消时不改变工作区

用户输入框和模型工具必须复用同一个工作区变更核心，不能形成两套校验、事务和错误处理。

### 当前运行的生效语义

`CodingAgent` 在本轮开始时取得工作区 Lease。因此模型在本轮调用 `set_workspace_path` 后：

- 当前运行继续使用旧 Lease
- 新路径成功提交到 Controller 和 CodingAgent Provider
- 当前运行不获得新路径的 Roslyn/文件/CLI 工具
- 下一次用户发送使用新工作区
- 工具结果必须明确提示“新路径从下一次消息开始生效”

尚未设置工作区时，本轮工具集合至少包含 `set_workspace_path`。审批成功后本轮结束或继续普通文本回复，但不能在同一轮使用新工作区代码工具。

## 审批工具接入设计

### 问题

`HumanApprovalTool.Wrap` 只产生配置态包装器。真正等待用户审批的运行时工具需要当前助手消息、聊天上下文和本轮取消令牌。

`CodingAgent` 当前直接设置：

```text
ChatOptions.Tools = lease.Tools
```

因此不能把配置态 `set_workspace_path` 直接追加进去。

### 推荐方案

在 AgentLib 手动发送能力中增加可选的运行时工具绑定接口，例如：

```text
IManualSendRuntimeToolBinder
  → BindRuntimeTools(tools, cancellationToken)
```

内置 `ManualSendMessageContext` 实现该接口，并复用 `CopilotChatManager` 的审批绑定与审批项注册逻辑。

`CodingAgent` 增加宿主控制工具重载：

```text
RunAsync(context, contents, workspacePath, hostControlTools, cancellationToken)
```

装配顺序：

1. 获取当前工作区 Lease。
2. 快照宿主控制工具。
3. 通过可选绑定接口转换审批工具。
4. 校验宿主工具与 Coding 工具名称冲突。
5. 合并 `lease.Tools + boundHostControlTools`。
6. 一次性写入 `ChatOptions.Tools`。

安全规则：

- 有宿主工具但上下文不支持运行时绑定时立即失败。
- 绝不能退化为直接执行配置态审批工具。
- 宿主工具只包含 `set_workspace_path`，不自动带入 `IManualSendMessageContext.DefaultTools`。
- 工具同名时失败，不静默覆盖 Coding 工具。

## UI 设计

### 主布局

```text
┌──────────────────┬─────────────────────────────────────────┐
│ 历史会话          │ 工作路径 [________________] [应用]      │
│ [+ 新建]          ├─────────────────────────────────────────┤
│                  │                                         │
│ 会话 A            │ 消息历史                                │
│ 会话 B            │ 用户 / Copilot / 工具 / 审批 / 思考     │
│ 会话 C            │                                         │
│                  ├─────────────────────────────────────────┤
│                  │ 输入框                    [发送] [停止]  │
└──────────────────┴─────────────────────────────────────────┘
```

- 左栏建议初始宽度 260，可通过 `GridSplitter` 调整。
- 右栏占据剩余空间。
- 不保留 ChatRoom 的右侧角色栏和页面切换 Panel。

### 左侧历史

保留：

- 新建会话
- 会话标题
- 创建或最后活动时间
- 消息数量
- 当前选中态
- 删除历史会话

删除：

- 角色数量
- 角色大厅按钮
- 设置按钮

### 右侧聊天

顶部：

- 当前会话标题
- 当前模型显示名
- 工作路径输入框、应用按钮和状态文本
- 可选的“工作区从下一轮生效”提示

消息区：

- 用户消息靠右
- Copilot 消息靠左
- 不显示角色彩色头像和角色名菜单
- 作者固定显示“我”和“Copilot”
- 保留思考、工具、审批、子代理、用量和复制功能
- 系统错误可使用居中消息或顶部错误条

输入区：

- 多行输入
- `Ctrl+Enter` 发送
- 发送按钮
- 运行时显示停止按钮
- Placeholder 不包含 `@角色名`

## 启动与关闭

### 启动

```text
计算 LocalAppData/CodingChatRoom 路径
  → 创建根目录、Logs、Sessions
  → 从固定配置文件加载 AgentApiManagerConfiguration
  → 创建 AgentApiEndpointManager 并加载配置
  → 校验 PrimaryModel
  → 创建 AvaloniaMainThreadDispatcher
  → 创建显式 FileCopilotChatLogger(Logs, Sessions)
  → 创建 CopilotChatManager
  → 创建 CodingAgent
  → 创建 WorkspaceController 和 SessionRepository
  → 加载会话历史
  → 选择最近会话或创建空会话
  → 创建 MainViewModel/MainView/MainWindow
```

任何配置错误均进入统一启动失败路径并退出，不创建无模型的主界面。

### 关闭

关闭顺序：

1. 阻止新发送、会话切换和工作区切换。
2. 取消当前发送。
3. 等待当前 `CompletionTask` 结束清理。
4. 确认会话历史已写入。
5. 异步释放 `CodingAgent`，等待 Roslyn 等资源退出。
6. 解除 ViewModel 事件订阅。
7. 允许桌面生命周期退出。

不得只依赖进程结束回收 Roslyn Language Server。

## 错误处理

### 启动错误

使用简单错误窗口或主窗口内启动错误页显示：

- 固定配置文件路径
- 错误摘要
- 必要的异常详情
- 退出按钮

不提供设置页或替代配置路径选择。

### 运行错误

- 模型失败：在消息区追加可见错误状态，输入恢复可用。
- 工作区无效：保留旧工作区，工作区区域显示错误。
- 审批拒绝：审批项保持拒绝状态，路径不变。
- 审批等待时取消：取消等待，路径不变。
- 历史文件损坏：单个文件标记为不可加载，不阻止其他会话启动；不从其他目录回退。
- 历史保存失败：明确显示未保存状态，不能假装成功。

## 测试边界

### Shell 测试

- 固定 LocalAppData 路径计算可通过注入根路径测试，不在生产代码增加回退。
- 配置缺失、损坏、无模型时启动失败。
- 启动不访问仓库或当前工作目录配置。
- 双列主界面只暴露历史和聊天。
- 新建、选择、删除会话行为。
- 发送只调用 CodingAgent 一次，不启动自动循环。
- 工作区输入成功、失败、取消和旧状态保留。
- 发送期间禁止会话切换和重复发送。
- 审批同意/拒绝正确转发。

### AgentLib 测试

- 历史 XML 可完整往返消息片段和 AgentSession 状态。
- 列表、加载、删除和损坏文件隔离。
- 手动发送运行时工具绑定创建审批项。
- 审批前内部函数不执行；同意后执行一次；拒绝/取消时不执行。

### AgentLib.Coding 测试

- Lease 工具与宿主控制工具同时可见。
- 不自动加入 `context.DefaultTools`。
- 宿主审批工具正确绑定。
- 工具名冲突立即失败。
- 当前运行保持旧 Lease，下一轮使用新工作区。
- 空工作区时仍能调用 `set_workspace_path`。
- 取消、异常和释放不泄漏 Lease 或外部资源。

## 验收标准

1. 应用启动后只有历史会话和编程助手聊天两列。
2. 代码和 UI 中不存在角色大厅、角色管理、角色编辑和设置页入口。
3. Shell 不引用 `AgentLib.ChatRoom`。
4. 配置只从 `%LOCALAPPDATA%/CodingChatRoom/AgentApiManagerConfiguration.json` 加载。
5. 配置缺失或无效时明确失败，不使用任何回退路径。
6. 日志和历史只写入 `%LOCALAPPDATA%/CodingChatRoom` 下的固定子目录。
7. 用户发送后直接进入 `CodingAgent`，消息可流式展示。
8. 历史会话可重新打开，并恢复可继续使用的 AgentSession。
9. 用户可直接设置工作路径，失败时旧路径仍可用。
10. CodingAgent 可请求 `set_workspace_path`，且审批前路径绝不改变。
11. 审批同意后新路径只对下一次发送生效。
12. 当前运行始终只使用一个稳定工作区 Lease。
13. 停止、关闭或异常不会遗留永久占位符、未完成审批或 Roslyn 进程。
