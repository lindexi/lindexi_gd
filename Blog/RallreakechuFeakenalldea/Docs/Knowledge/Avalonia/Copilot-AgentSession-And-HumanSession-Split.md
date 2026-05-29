# Copilot 人类会话与 AgentSession 分层

## 场景

当聊天管理器自己把 `CopilotChatSession.ChatMessages` 再拼回 `ChatMessage[]` 传给模型时，容易丢失工具调用、服务端会话标识和后续压缩能力依赖的机器侧状态。

## 当前约定

### 1. `CopilotChatSession` 只负责面向人的会话信息

当前 `CopilotChatSession` 继续保存：

- `SessionId`
- `StartedTime`
- `ChatMessages`
- `Title`

同时新增两项机器侧状态：

- `AgentSession`
- `SerializedAgentSessionState`

这两项只用于恢复与复用模型会话，不参与 UI 标题和消息展示逻辑。

### 2. 发送消息优先复用 `AgentSession`

`CopilotChatManager.SendMessageAsync(...)` 现在不再默认把整个历史重新拼成 `ChatMessage[]`。

当前策略是：

1. 先从 `CopilotChatSession.AgentSession` 取运行中的会话；
2. 如果内存里没有，再尝试从 `SerializedAgentSessionState` 恢复；
3. 如果仍没有，再调用 `chatClientAgent.CreateSessionAsync(...)` 创建新会话；
4. 后续 `RunStreamingAsync(...)` 始终传入该 `AgentSession`。

### 3. 当前持久化状态以 `ConversationId` 为主

由于当前解析到的 `Microsoft.Agents.AI` 版本里，`ChatClientAgentSession.Serialize/Deserialize` 不能直接从业务代码访问，当前落盘采取保守方案：

- 若 `ChatClientAgentSession.ConversationId` 可用，则把它保存到 `SerializedAgentSessionState`；
- 恢复时调用 `chatClientAgent.CreateSessionAsync(conversationId, ...)` 重建机器会话；
- 如果后续包版本允许直接访问完整序列化 API，再把该字段升级为完整 `AgentSession` JSON。

### 4. `withHistory` 的语义调整

当前如果已经有可恢复的机器会话状态，则 `withHistory` 主要影响“是否只发送当前输入消息”：

- `withHistory == false`：只发送当前用户输入；
- `withHistory == true` 且已有机器会话：同样只发送当前输入，历史由 `AgentSession` 负责；
- `withHistory == true` 且尚无机器会话：首次仍发送当前输入，由新建 `AgentSession` 接管后续上下文。

也就是说，历史上下文的主来源已经从本地 `ChatMessages` 切到了机器侧 `AgentSession`。

### 5. 聊天日志同时保存人类消息与机器状态

`FileCopilotChatLogger` 现在会继续写：

- 文本日志 `.log`
- 结构化历史 `.xml`

其中 XML 根节点下新增可选的 `AgentSessionState`，用于保存当前可恢复的机器侧会话状态。

## 适用场景

- 调整 Copilot 聊天上下文来源时；
- 后续接入会话压缩或服务端聊天历史时；
- 排查“为什么 UI 历史完整，但模型记忆没有延续”这类问题时。
