# CodingChatRoom 实现细节

## 文档定位

本文把 `CodingChatRoom-架构设计.md` 细化为可执行的目录结构、类型职责、方法流程、数据格式和测试接缝。

本文不修改代码。类型名为推荐命名；实施时可按现有代码风格微调，但职责和边界不应改变。

## 一、项目与依赖

### CodingChatRoom.AvaloniaShell.csproj

保留：

- `net10.0`
- Avalonia Desktop、Fluent、Inter 字体和调试工具
- 对 `AgentLib.Coding` 的项目引用

增加：

- `AvaloniaUseCompiledBindingsByDefault=true`
- `InternalsVisibleTo` 指向新的 Shell 测试项目（若创建）
- 三个样式资源的 `AvaloniaResource`

不增加：

- `AgentLib.ChatRoom` 引用
- 配置框架或依赖注入容器包
- 数据库或 ORM
- MVVM 第三方框架

`AgentLib.Coding` 已传递引用 `AgentLib`。若编译器或项目规范要求直接引用 `AgentLib`，可以显式增加项目引用，但不能通过 `AgentLib.ChatRoom` 间接获得。

## 二、建议目录

```text
CodingChatRoom.AvaloniaShell
├── App.axaml
├── App.axaml.cs
├── MainWindow.axaml
├── MainWindow.axaml.cs
├── Program.cs
├── Infrastructure
│   ├── AvaloniaMainThreadDispatcher.cs
│   ├── CodingChatRoomPaths.cs
│   ├── StartupFailureViewModel.cs
│   └── AppLifetimeCoordinator.cs
├── Services
│   ├── CodingChatApplication.cs
│   ├── CodingChatSessionRepository.cs
│   ├── CodingWorkspaceController.cs
│   └── WorkspacePathToolFactory.cs
├── ViewModels
│   ├── ViewModelBase.cs
│   ├── Commands.cs
│   ├── MainViewModel.cs
│   ├── SessionListViewModel.cs
│   ├── ChatViewModel.cs
│   └── MessageItemViewModel.cs
├── Views
│   ├── MainView.axaml
│   ├── MainView.axaml.cs
│   ├── SessionListView.axaml
│   ├── SessionListView.axaml.cs
│   ├── ChatView.axaml
│   ├── ChatView.axaml.cs
│   └── ChatMessageItemTemplateSelector.cs
├── Converters
│   └── BoolConverters.cs
└── Styles
    ├── Colors.axaml
    ├── Controls.axaml
    └── MessageBubble.axaml
```

若历史存储能力被提升到 `AgentLib`，Shell 中的 `CodingChatSessionRepository` 应是很薄的适配器或可删除，不能再复制 XML 序列化逻辑。

## 三、路径对象

### CodingChatRoomPaths

建议定义不可变对象：

```text
CodingChatRoomPaths
- RootDirectory
- ConfigurationFile
- LogDirectory
- SessionDirectory
```

生产工厂：

```text
CreateForCurrentUser()
  root = Path.Join(
      Environment.GetFolderPath(LocalApplicationData),
      "CodingChatRoom")
```

测试工厂：

```text
Create(rootDirectory)
```

测试可注入临时目录不等于生产回退。生产入口始终调用 `CreateForCurrentUser()`。

初始化方法只创建目录：

- RootDirectory
- LogDirectory
- SessionDirectory

不创建有效配置文件，不扫描其他目录。

## 四、App 组合根

### InitializeAppCoreAsync

推荐顺序：

1. `CodingChatRoomPaths.CreateForCurrentUser()`。
2. 创建固定目录。
3. 检查 `ConfigurationFile.Exists`；不存在则抛出包含完整路径的 `FileNotFoundException`。
4. 调用 `AgentApiManagerConfiguration.FromJsonFileAsync`。
5. 创建 `AgentApiEndpointManager` 并调用 `LoadConfiguration`。
6. 读取一次 `PrimaryModel`，提前验证可用模型。
7. 创建 `AvaloniaMainThreadDispatcher`。
8. 创建显式日志器：`new FileCopilotChatLogger(paths.LogDirectory, paths.SessionDirectory)` 或后续统一存储实现。
9. 创建 `CopilotChatManager`，设置 `MainThreadDispatcher` 和 `AgentApiEndpointManager`。
10. 创建 `CodingAgent`。
11. 创建 `CodingWorkspaceController`。
12. 创建会话仓储并加载摘要/会话。
13. 创建 `CodingChatApplication` 协调服务。
14. 创建 `MainViewModel`、`MainView`、`MainWindow`。
15. 注册退出协调器。
16. 显示窗口。

注意：`CopilotChatManager.AgentApiEndpointManager` 是 `init` 属性，应在对象初始化器中注入，不要先使用默认 Manager 再复制 provider。

建议构造形式：

```text
new CopilotChatManager(logger)
{
    AgentApiEndpointManager = endpointManager,
    MainThreadDispatcher = dispatcher,
}
```

### 启动失败

`InitializeApp` 捕获异常后不要只写 Trace 并静默退出。应创建一个最小失败窗口，至少显示：

- “CodingChatRoom 启动失败”
- 配置文件完整路径
- 异常消息
- 退出按钮

仍然可以 `Trace.TraceError` 记录完整异常。

失败窗口不提供浏览其他配置文件或打开设置页的入口。

## 五、应用协调服务

### CodingChatApplication

该类型协调 Shell 用例，不承担 UI 属性通知。

依赖：

- `CopilotChatManager`
- `CodingAgent`
- `CodingWorkspaceController`
- 会话仓储
- `IReadOnlyList<AITool> hostControlTools`

职责：

- 初始化历史会话
- 新建、打开、删除会话
- 直接发送编程任务
- 取消当前发送
- 等待发送完成
- 刷新会话摘要
- 关闭与释放

建议状态：

```text
Task? ActiveRunTask
CancellationTokenSource? ActiveRunCancellationTokenSource
bool IsClosing
SemaphoreSlim SessionOperationGate
```

首版通过 `SessionOperationGate` 串行化：

- 新建会话
- 打开会话
- 删除会话
- 启动发送
- 关闭

不要让该门覆盖整个模型流。启动发送时只需完成上下文创建、`CodingAgent.RunAsync` 和活动任务发布，然后释放门；发送期间通过 `ActiveRunTask != null` 拒绝会话切换和重复发送。

### SendAsync

推荐伪流程：

```text
校验 text 非空
校验未关闭、无活动发送
取得当前 SelectedSession
创建 linked CTS
创建 IManualSendMessageContext
调用 CodingAgent.RunAsync(
    context,
    text,
    workspaceController.CommittedWorkspacePath,
    hostControlTools,
    token)
保存 CompletionTask 为 ActiveRunTask
返回 AssistantChatMessage/运行句柄给 ViewModel
门外等待 CompletionTask
完成后刷新标题和摘要
finally 清理 ActiveRunTask、CTS 和状态
```

`CodingAgent.RunAsync` 会把用户消息和助手占位消息追加到当前会话。Shell 不应重复追加。

### 取消

`CancelCurrentRun()` 只调用本轮 CTS 的 `Cancel()`。

不建议调用 `CopilotChatManager.CancelCurrentChat()`，因为 CodingAgent 使用自己的 linked CTS 与 `StartChatting`，当前取消所有权在应用协调服务。

### 并发约束

- 一个 `CopilotChatSession.AgentSession` 不应被两个 CodingAgent 运行并发修改。
- 因此 Shell 首版只允许一个全局活动发送。
- 发送期间左侧列表仍可滚动，但选择、删除和新建命令禁用。
- 工作区切换可以在审批工具内部发生；Controller 与 CodingAgent Provider 的事务保证当前 Lease 稳定。

## 六、工作区控制器

### CodingWorkspaceController

建议属性：

```text
string WorkspaceInput
string? CommittedWorkspacePath
bool IsChangingWorkspace
string? ErrorMessage
```

核心方法：

```text
Task<WorkspaceChangeResult> ChangeWorkspaceAsync(
    string? requestedPath,
    WorkspaceChangeOrigin origin,
    CancellationToken cancellationToken)
```

`WorkspaceChangeOrigin` 可区分：

- `User`
- `AgentTool`

它只用于结果文案和日志，不改变审批逻辑。审批由工具包装器负责，Controller 接到 AgentTool 调用时已经获得用户同意。

### 规范化

- 空白字符串可定义为清除工作区；若产品首版不需要清除按钮，则 UI 不暴露，但核心可以支持 `null`。
- 非空路径先 `Path.GetFullPath`。
- 必须是存在的目录。
- 路径比较在 Windows 使用 `OrdinalIgnoreCase`，其他平台使用 `Ordinal`。
- 与已提交路径相同时返回 NoChange，不重新创建 Roslyn 会话。

### 事务发布

```text
await using transaction = await codingAgent.PrepareWorkspaceChangeAsync(path, token)
transaction.Apply()
try
    publish CommittedWorkspacePath
    publish WorkspaceInput
catch
    rollback published UI state
    await transaction.RollbackAsync()
    throw
transaction.CommitAfterPublish()
```

Controller 发布状态应在 Avalonia UI 线程完成。可注入 `IMainThreadDispatcher`，不要直接从工具执行线程修改绑定属性。

### 结果

建议 `WorkspaceChangeResult` 包含：

- `PreviousPath`
- `CurrentPath`
- `Changed`
- `EffectiveFromNextRun`
- 用户可读消息

AgentTool 来源成功消息：

```text
工作区路径已设置为：{path}。当前编程任务仍使用启动时工作区，新路径从下一条消息开始生效。
```

用户直接设置成功消息可以省略“下一条消息”提示，但若发送正在进行中，仍应显示相同提示。

## 七、设置工作路径工具

### WorkspacePathToolFactory

Shell 创建工具，不把 Shell 类型传入 `AgentLib.Coding`：

```text
Create(CodingWorkspaceController controller)
  → AIFunctionFactory.Create(SetWorkspacePathAsync, "set_workspace_path", description)
  → HumanApprovalTool.Wrap(tool, ApprovalOptions)
```

审批展示：

- `DisplayName = "设置工作区路径"`
- `ApprovalDescription = "编程助手请求访问新的本地工作区。审批后，它将在后续消息中获得该目录的代码读取、修改、构建和测试能力。"`
- `InputTemplate = "目标路径：{path}"`

工具函数：

```text
Task<string> SetWorkspacePathAsync(string path, CancellationToken token)
  → controller.ChangeWorkspaceAsync(path, AgentTool, token)
  → return result.Message
```

路径无效时可以返回友好错误字符串；审批基础设施或取消异常应保留取消语义，不要全部吞成普通文本。

## 八、AgentLib 手动审批绑定改造

### 新接口

建议在 `AgentLib/Model/SendMessages_` 新增：

```text
public interface IManualSendRuntimeToolBinder
{
    IReadOnlyList<AITool> BindRuntimeTools(
        IReadOnlyList<AITool> tools,
        CancellationToken cancellationToken = default);
}
```

接口保持最小：

- 不暴露 `CopilotChatContext`
- 不追加默认工具
- 不修改 Session
- 不负责工具去重

### ManualSendMessageContext

实现该接口时：

1. 用当前 `Session.ChatMessages` 与 `AssistantChatMessage` 创建 `CopilotChatContext`。
2. 对传入工具逐个调用共享的运行时绑定逻辑。
3. 对审批工具在 `chatContext.CurrentContent` 注册审批描述。
4. 返回新的只读工具快照。

### CopilotChatManager

把当前 `ResolveTools` 拆分为：

```text
BindRuntimeTools(tools, chatContext, token)
CreateDefaultTools(chatContext, token)
ResolveTools = BindRuntimeTools(explicitTools) + BindRuntimeTools(defaultTools)
```

标准 `SendMessage` 行为不变；手动上下文复用 `BindRuntimeTools`。

必须测试配置态审批工具不会未经绑定直接执行。

## 九、AgentLib.Coding 宿主工具改造

### 新重载

保留现有重载兼容，并新增：

```text
RunAsync(
    IManualSendMessageContext context,
    string prompt,
    string? workspacePath,
    IReadOnlyList<AITool> hostControlTools,
    CancellationToken cancellationToken = default)

RunAsync(
    IManualSendMessageContext context,
    IReadOnlyList<AIContent> contents,
    string? workspacePath,
    IReadOnlyList<AITool> hostControlTools,
    CancellationToken cancellationToken = default)
```

旧重载委托到新重载并传 `Array.Empty<AITool>()`。

### 工具准备

在获得 Lease 后、创建运行任务前：

1. 快照 `hostControlTools`。
2. 有宿主工具时要求 `context is IManualSendRuntimeToolBinder`。
3. 绑定运行时工具。
4. 校验所有工具名非空且唯一。
5. 校验 Lease 工具与宿主工具无同名。
6. 形成只读最终数组。
7. 把最终工具数组传入 `RunCoreAsync`。

失败必须发生在启动模型流之前，并正确释放 Lease/CTS。

### RunCoreAsync

从：

```text
ChatOptions.Tools = lease.Tools
```

改为使用已经准备好的最终工具快照。

仍保持：

```text
AIContextProviders = []
```

防止 `IManualSendMessageContext` 的默认上下文提供者意外进入固定 CodingAgent 流程。

## 十、历史存储与恢复

### 当前格式问题

`FileCopilotChatLogger` 的 XML 当前保存：

- SessionId
- CreatedTime
- 最新 AgentSessionState
- 消息角色、作者、时间、预设标志
- 文本、思考、图片、音频、工具、审批和子代理片段
- Token 用量

但它没有：

- 公共列表 API
- 加载为 `CopilotChatSession` 的 API
- 删除 API
- 标题字段
- 明确的损坏文件结果模型

### 推荐的 AgentLib 能力

建议增加 `FileCopilotChatSessionStore`，或把 `FileCopilotChatLogger` 扩展为组合式存储。推荐契约：

```text
IReadOnlyList<CopilotChatSessionSummary> ListSessions()
Task<CopilotChatSessionSnapshot> LoadSessionAsync(filePath, token)
Task DeleteSessionAsync(sessionId, token)
```

`CopilotChatSessionSnapshot` 至少包含：

- `SessionId`
- `StartedTime`
- `Title`
- `IReadOnlyList<CopilotChatMessage>`
- `JsonElement? AgentSessionState`
- `HistoryFilePath`
- `MessageCount`
- `LastActivityTime`

### 格式升级

根元素建议增加：

```text
Version="2"
Title="..."
LastUpdatedTime="..."
```

兼容现有 Version 1：

- 缺少 Title 时从首条非预设用户消息重建。
- 缺少 Version 时按现有格式读取。
- 未知消息片段应报告具体错误，不静默丢失上下文。

### 恢复到 CopilotChatManager

当前 `ChatSessions` 是可写集合，可以在 UI 线程装入恢复会话；但应优先为 `CopilotChatManager` 增加明确方法，而不是 Shell 直接操作内部顺序：

```text
AddOrReplaceSession(CopilotChatSession session)
RemoveSession(CopilotChatSession session)
SelectSession(CopilotChatSession session)
```

若不增加 API，Shell 直接修改集合必须集中在一个仓储适配器中。

恢复步骤：

1. 创建指定 ID 和 StartedTime 的 `CopilotChatSession`。
2. 设置 Dispatcher。
3. 设置标题。
4. 按顺序添加消息。
5. 创建手动上下文和 `ChatClientAgent`。
6. 调用 `DeserializeSessionAsync` 恢复 `AgentSessionState`。
7. `session.SetAgentSession(agentSession)`。
8. 加入 Manager 并选中。

恢复消息时不能触发新的日志追加，也不能重复生成欢迎消息。

### 日志删除

历史 XML 文件名已含 SessionId；文本日志首部也含 SessionId，但日志位于日期子目录。

建议存储层维护 SessionId 到文件的索引并负责删除：

- 会话 XML
- 对应 `.log`

删除失败应返回部分失败详情，UI 不应先从列表永久移除再忽略磁盘错误。

## 十一、ViewModel 细节

### MainViewModel

只持有：

- `SessionListViewModel`
- `ChatViewModel`

不需要页面枚举、导航状态或可空页面 ViewModel。

### SessionItemViewModel

属性：

- `SessionId`
- `Title`
- `StartedTime`
- `LastActivityTime`
- `MessageCount`
- `DisplayTime`
- `IsSelected`
- `HasLoadError`
- `LoadErrorText`

副标题示例：

```text
12 条消息 · 03-15 14:32
```

不包含角色数。

### SessionListViewModel

命令：

- `CreateNewSessionCommand`
- `OpenSessionCommand`
- `DeleteSessionCommand`

属性：

- `Sessions`
- `SelectedSession`
- `IsBusy`
- `CanChangeSession`

选中项 setter 不建议直接 fire-and-forget 打开会话。可由 SelectionChanged 调用显式异步命令，或 setter 内捕获异常并恢复旧选择，避免未观察异常。

### MessageItemViewModel

直接包装 `CopilotChatMessage`：

- `Role`
- `Author`
- `Content`
- `FullContent`
- `CreatedTime`
- `TimeText`
- `MessageItems`
- `IsUserMessage`
- `IsAssistantMessage`
- `IsSystemMessage`
- `HasUsageDetails`
- `UsageSummaryText`
- `IsStreaming`

`CopilotChatMessage` 当前没有显式 `IsStreaming`。Shell 可根据：

- 当前活动运行的 `AssistantChatMessage` 引用
- `CopilotChatManager.IsChatting`

组合计算，不要把 ChatRoom 的 `ChatRoomMessage.IsStreaming` 模型复制进来。

### ChatViewModel

依赖 `CodingChatApplication` 与 `CodingWorkspaceController`。

属性：

- `Messages`
- `InputText`
- `WorkspaceInput`
- `CommittedWorkspacePath`
- `WorkspaceStatusText`
- `IsRunning`
- `CanSend`
- `CanStop`
- `CanApplyWorkspace`
- `CurrentSessionTitle`
- `CurrentModelDisplayName`
- `ActiveAssistantMessage`

命令：

- `SendCommand`
- `StopCommand`
- `ApplyWorkspaceCommand`

方法：

- `ApproveTool(CopilotChatApprovalToolItem item)`
- `RejectTool(CopilotChatApprovalToolItem item, string? reason = null)`

审批可继续调用 `CopilotChatManager.ApproveToolExecution/RejectToolExecution`。

### 消息集合投影

切换会话时：

1. 退订旧 `ChatMessages.CollectionChanged`。
2. 清空 Shell `Messages`。
3. 包装新会话已有消息。
4. 订阅新集合。
5. 新增消息时回到 Avalonia UI 线程添加。

每个 `MessageItemViewModel` 订阅底层 `CopilotChatMessage.PropertyChanged` 和 `MessageItems.CollectionChanged`，用于刷新内容、用量和完整复制文本。

需要实现解除订阅，避免删除/切换大量会话后保留引用。

## 十二、View 细节

### MainView.axaml

```text
Grid ColumnDefinitions="260,4,*"
- Column 0: SessionListView
- Column 1: GridSplitter
- Column 2: ChatView
```

没有其他 Panel 或页面导航。

### SessionListView.axaml

顶部：标题 + 新建。

列表项：标题、消息数、时间、删除按钮。

底部无需任何按钮，可保留版本信息或空白。

### ChatView.axaml

建议行：

```text
Auto  顶部会话/模型栏
Auto  工作区栏
*     消息列表
Auto  输入栏
```

工作区栏：

- Label“工作路径”
- TextBox 双向绑定 `WorkspaceInput`
- Button“应用”
- 状态/错误文本

消息模板直接使用 `CopilotChatMessage` 投影，不再使用角色颜色转换器。

审批模板保留同意/拒绝按钮。首版拒绝原因可为空；若增加输入对话框，属于后续增强。

### 自动滚动

新增消息或当前助手文本追加时滚动到底部，但用户已主动向上滚动时不要强制抢回。可使用距离底部阈值判断自动跟随状态。

## 十三、关闭生命周期

### AppLifetimeCoordinator

负责：

- 监听桌面退出或主窗口 Closing
- 首次关闭时取消默认关闭
- 异步执行 `CodingChatApplication.CloseAsync`
- 完成后再次关闭窗口
- 防止重复关闭进入两次释放

`CloseAsync`：

1. 设置 `IsClosing`。
2. 取消活动运行。
3. 等待 `ActiveRunTask`。
4. 等待历史写入。
5. `await codingAgent.DisposeAsync()`。
6. Dispose ViewModels/订阅。

取消导致的 `OperationCanceledException` 视为正常关闭；资源释放错误要记录并允许用户看到或至少写 Trace。

## 十四、测试项目建议

建议创建：

```text
SemanticKernelSamples/ChatRoom/Code/CodingChatRoom.Shell.Tests
```

使用 MSTest，风格与现有项目一致。

### 测试替身

- 临时根目录的 `CodingChatRoomPaths`
- FakeLanguageModel/FakeChatClient
- 测试主线程调度器
- 可控 `CodingWorkspaceToolProvider` 或通过 AgentLib.Coding internal 测试覆盖核心
- 内存会话仓储接口替身

### 重点测试

#### 启动

1. 只读取固定配置路径。
2. 配置不存在时失败。
3. 配置损坏时失败。
4. 无有效 provider 时失败。
5. 日志器使用 CodingChatRoom 子目录。

#### 会话

1. 无历史时创建空会话。
2. 有历史时默认打开最近会话。
3. 新建时复用真正空会话。
4. 发送期间禁止打开/删除/新建。
5. 删除失败时列表保持。
6. 损坏单个历史文件不阻止其他历史加载。

#### 发送

1. `Ctrl+Enter` 与发送命令只启动一次。
2. 用户和助手消息立即进入同一会话。
3. 流式增量更新同一助手对象。
4. 停止取消当前任务。
5. 异常后命令恢复可用。
6. 空回复不保留 `...`。

#### 工作区

1. 用户应用路径成功后提交规范化路径。
2. 无效目录保持旧路径。
3. 候选初始化失败保持旧路径。
4. 相同路径不重复切换。
5. 模型工具审批前不改变路径。
6. 同意后改变路径。
7. 拒绝、取消后路径不变。
8. 当前运行旧 Lease、下一轮新 Lease。

#### UI 投影

1. 切换会话正确退订旧集合。
2. 工具和审批片段可显示。
3. 复制正文与完整消息分别使用 `Content` 和 `FullContent`。
4. 不存在角色管理和设置命令。

## 十五、实施时需要重点审查的风险

1. **历史恢复不是只读 XML 问题**：必须同时恢复 AgentSession，否则连续编程对话语义不成立。
2. **配置态审批工具不能直接执行**：没有运行时绑定能力时必须失败，不能降级绕过审批。
3. **工作区切换与当前 Lease**：工具成功后不能让当前运行立即使用新目录。
4. **日志与历史重复写入**：手动发送上下文会在追加消息时写日志，最终 AgentSessionState 的持久化时机需要确认，避免只有用户消息或没有最新状态。
5. **CopilotChatManager 当前构造自动创建欢迎会话**：加载历史时要避免重复空会话和重复欢迎消息。
6. **历史标题**：现有 XML 没有 Title，需要格式升级或稳定重建。
7. **线程**：消息集合、会话集合、工作区绑定属性都必须在 Avalonia UI 线程更新。
8. **关闭**：不能让窗口退出快于 CodingAgent Lease 和 Roslyn 资源清理。
9. **项目版本**：ChatRoom 使用 Avalonia 12.0.3，新项目使用 12.1.0；复制 XAML 时按新项目版本验证 API，不回退包版本。
10. **设计器构造函数**：如保留无参 ViewModel，只填充纯 UI 示例数据，不创建真实模型、日志或 CodingAgent。

## 完成定义

实现完成必须同时满足：

- Shell 只有两列界面。
- 直接发送链路完全脱离 ChatRoom。
- 固定配置和本地目录规则可测试且无回退。
- 历史可恢复公开消息和 AgentSession。
- 用户与模型共用同一工作区变更核心。
- 模型工作区工具必须经过审批。
- 当前运行 Lease 稳定，下一轮应用新路径。
- 关闭能等待运行和外部资源释放。
- AgentLib、AgentLib.Coding、Shell 测试及完整构建通过。
