# Copilot SendMessage 分阶段返回结果

## 场景

当业务侧既想“立刻拿到本次发送关联的消息对象与工具列表”，又想“按需等待聊天客户端创建或整次运行结束”时，单一的 `Task SendMessageAsync(...)` 不够灵活。

尤其是在需要先把用户消息和占位助手消息挂到 UI，再由业务决定是否等待执行完成的场景里，更适合把发送流程拆成多个阶段返回。

## 当前约定

### 1. `SendMessageRequest` 统一承载发送参数

当前发送参数不再只散落在 `CopilotChatManager.SendMessageAsync(...)` 的参数列表里。

`SendMessageRequest` 负责统一表达：

- `InputText`
- `WithHistory`
- `CreateNewSession`
- `Tools`
- `ToolMode`
- `CancellationToken`

这样同步入口和异步入口就可以共用同一份请求模型，避免参数持续漂移。

### 2. `SendMessage(...)` 负责立即返回阶段性结果

`CopilotChatManager.SendMessage(...)` 现在会先完成这些同步准备工作：

1. 创建并注册当前聊天的 `CancellationTokenSource`；
2. 按需创建新会话；
3. 创建用户消息与助手占位消息；
4. 构造 `CopilotChatContext` 并解析工具列表；
5. 立即返回 `SendMessageResult`。

因此调用方可以在不等待流式输出结束的情况下，先拿到：

- `UserChatMessage`
- `AssistantChatMessage`
- `ToolList`
- `CreateChatClientAgentTask`
- `RunTask`

### 3. 聊天客户端创建与实际运行分成两个异步阶段

当前实现把后续流程拆成两个内部任务：

- `CreateChatClientAgentAsync(...)`：负责获取 `IChatClient`、创建 `ChatClientAgent`，并在需要历史时准备 `AgentSession`；
- `RunSendMessageAsync(...)`：负责写入用户消息、追加助手消息、消费 `RunStreamingAsync(...)`、记录日志、处理取消与异常。

这样做的好处是：

- 调用方可以单独等待客户端创建完成；
- UI 可更早持有消息对象并开始绑定；
- `SendMessageAsync(...)` 也能直接复用同一条内部链路。

### 4. `SendMessageAsync(...)` 只是对 `RunTask` 的封装等待

当前 `SendMessageAsync(...)` 不再自己维护一整套发送实现，而是：

1. 构造 `SendMessageRequest`；
2. 调用 `SendMessage(...)`；
3. 仅等待返回结果里的 `RunTask`。

这样可以保证：

- 旧调用方式继续可用；
- 新旧入口共享同一份取消、异常、日志和会话处理逻辑；
- 后续如果要继续扩展阶段结果，只需要改一处核心流程。

## 适用场景

- 需要在 UI 中先展示本次发送对应消息对象，再决定是否等待完成时；
- 需要把聊天客户端创建阶段与运行阶段分别接入状态显示时；
- 需要在保留旧 `SendMessageAsync(...)` API 的同时，增加更细粒度控制能力时。
