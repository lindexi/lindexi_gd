# Copilot 流式消息片段与工作区工具异步化

## 场景

当模型已经支持交错思考、工具调用以及工具执行后继续输出时，继续把 `CopilotChatMessage` 固定拆成 `Reason` 和 `Content` 两段就不够用了。

同时，`WorkspaceToolProvider` 里如果仍使用同步文件 I/O，在 Avalonia UI 线程触发工具执行时容易造成界面卡顿。

## 当前约定

### 1. `CopilotChatMessage` 改为维护可观测片段集合

助手消息内部现在使用 `ObservableCollection<ICopilotChatMessageItem>` 表达流式片段，当前片段类型包括：

- `CopilotChatTextItem`：正文输出；
- `CopilotChatReasoningItem`：思考输出；
- `CopilotChatToolItem`：工具调用与结果。
- `CopilotChatSubAgentItem`：子智能体调用、逐步进度与向上级返回的最终输出。

`Content`、`Reason`、`FullContent` 仍然保留，但都由片段集合聚合计算得到，主要用于：

- 兼容历史调用链；
- 支持复制菜单；
- 支持日志和 XML 落盘。

### 2. 发送流程直接走 `RunStreamingAsync`

`CopilotViewModel.SendMessageAsync(...)` 不再额外调用 `RunReasoningStreamingAsync(...)`。

现在直接遍历 `RunStreamingAsync(...)` 的 `AgentResponseUpdate.Contents`，按内容类型写入片段集合：

- `TextReasoningContent` -> 思考片段；
- `TextContent` -> 正文片段；
- `FunctionCallContent` -> 工具片段输入；
- `FunctionResultContent` -> 工具片段输出；
- `UsageContent` -> 用量统计。

这样既能支持思考与正文交错，也能支持工具调用后回到正文继续输出。

### 3. UI 依靠片段类型做模板选择

`CopilotChatMessageTemplateSelector` 现在同时支持：

- 整条 `CopilotChatMessage`；
- 单个 `ICopilotChatMessageItem`。

助手消息模板内部不再手工写死“思考区 + 正文区”，而是对 `MessageItems` 做 `ItemsControl` 绑定，再交给模板选择器分发。

其中工具片段使用折叠区域显示：

- Header 显示工具名；
- 展开后显示输入参数；
- 再显示工具输出。

子智能体片段同样使用独立折叠区域，但内部继续绑定自己的 `MessageItems`，因此可以直接显示多级嵌套的子智能体进度，不再只能看到一次性返回结果。

### 3.1 `SubAgent` 约定改成显式返回输出

当前子智能体默认工具名改为带动词语义的 `InvokeSubAgent`。

子智能体执行时允许继续拿到：

- 默认工作区工具；
- `InvokeSubAgent` 本身，用于继续向下委托；
- `ReturnOutputToParent`，仅在确实需要给上一级代理返回结果时才调用。

也就是说，子智能体流里产生的普通文本和思考只用于进度展示，不会自动作为工具返回值交给上一级智能体。只有显式调用 `ReturnOutputToParent`，传入的文本才会成为上一级智能体拿到的结果。

### 3.2 默认工具改为共享聊天上下文

当前默认工具不再直接拿 `CopilotChatMessage` 或 `ISubAgentProgressContainer`，而是统一拿一个 `CopilotChatContext`。

这个上下文只保留两类状态：

- 当前聊天历史；
- 当前聊天内容承载器。

其中当前聊天内容由 `ICopilotChatCurrentContent` 抽象，`CopilotChatMessage` 与 `CopilotChatSubAgentItem` 都实现该接口。

当前发送流程会先创建顶层助手消息，再基于当前会话历史和该助手消息构造 `CopilotChatContext`，再传给 `CopilotToolManager.CreateDefaultTools(...)`。这样默认 `SubAgent` 工具拿到的就不再是某个具体消息类型，而是一份可继续派生的聊天状态。

当子智能体再次调用子智能体时，会先在当前聊天内容里创建新的 `CopilotChatSubAgentItem`，再基于该项派生新的 `CopilotChatContext` 继续向下传递。于是嵌套层级不再依赖外部额外查找进度容器，只需要沿着当前上下文继续向下写入即可。

这次也一并移除了 `ISubAgentProgressContainer`。子智能体的进度、工具调用和最终输出都直接落到当前 `CopilotChatSubAgentItem` 上，不再额外暴露一组带 `callId` 的进度写入接口。

### 4. `WorkspaceToolProvider` 的文件读取接口改为异步

当前默认文件工具里涉及文件读取和逐行扫描的 API 改为 `Task<string>`：

- `ReadFile`；
- `FindFilesContainingText`；
- `ReadFileLines`。

其余主要做内存内目录枚举和字符串拼装的方法也统一改为 `Task<string>`，保持工具调用风格一致。

实现时要求使用真正异步 I/O，不能再包一层同步实现返回 `Task`。

### 5. 聊天历史 XML 保留旧字段并追加新结构

为了兼容旧日志查看方式，XML 中原有 `Content`、`Reason` 仍继续写入。

同时新增 `MessageItems` 节点，把每个片段按类型分别记录：

- `TextItem`；
- `ReasoningItem`；
- `ToolItem`；
- `SubAgentItem`（内部继续递归包含 `MessageItems`）。

这样后续如果要做更精细的聊天历史回放，就可以直接按结构恢复，而不是再从聚合文本里反推。

## 适用场景

- 扩展 Copilot 聊天 UI 展示更丰富的流式内容时；
- 排查工具调用过程中为什么会出现多段输出时；
- 继续增加消息片段类型或历史记录字段时；
- 优化默认工作区工具执行卡顿问题时。
