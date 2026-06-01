# Copilot AgentSession 状态提供器与日志边界

## 场景

当 `AgentSession` 已经从人类消息历史中分离出来之后，日志层仍然需要把机器侧会话状态保存到 XML。此时如果继续在 `CopilotChatSession` 或 `CopilotChatManager` 里缓存字符串化状态，会把序列化细节泄漏到运行期模型和业务 API。

## 当前约定

### 1. 日志接口只依赖状态提供器

`ICopilotChatLogger.LogMessageAsync(...)` 现在不再接收字符串状态，而是接收可选的 `ICopilotChatSessionStateProvider`。

它的职责只有一个：

- 在需要时异步返回当前会话的序列化 `AgentSession` 状态；
- 返回值类型是 `JsonElement?`；
- 当前没有可用机器会话时返回 `null`。

### 2. `AgentSessionStateProvider` 负责调用官方 API

`AgentLib.Logging.AgentSessionStateProvider` 持有：

- `ChatClientAgent`
- `AgentSession`

真正写日志时，它会调用：

- `chatClientAgent.SerializeSessionAsync(agentSession, ...)`

这样序列化动作只在真正需要写历史文件时发生一次，聊天管理器不需要知道 JSON 细节。

### 3. `CopilotChatManager` 只在助手消息落盘时传提供器

当前链路里：

- 用户消息落盘时不传状态提供器；
- 助手消息流式完成后，如果本次运行传入了 `AgentSession`，才创建 `AgentSessionStateProvider` 交给日志层；
- `withHistory == false` 时不会创建或保存运行期 `AgentSession`。

### 4. `FileCopilotChatLogger` 写 JSON CDATA

`FileCopilotChatLogger` 在写 XML 时：

1. 先判断是否提供了状态提供器；
2. 提供了就异步获取 `JsonElement`；
3. 将 `GetRawText()` 结果写入 `AgentSessionState` 节点的 `CDATA`。

这样可以保留结构化 JSON，又不要求 XML 侧理解内部字段。

## 适用场景

- 调整日志接口而不想把会话序列化细节暴露到业务层时。
- 后续替换 `JsonSerializerOptions` 或持久化策略时。
- 需要排查为什么某条用户消息没有刷新 `AgentSessionState` 时。