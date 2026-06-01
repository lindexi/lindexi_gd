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
- 运行期 `AgentSession`

其中 `AgentSession` 仅作为运行期机器侧会话引用，用于连续对话时把服务端上下文继续传给模型；它不参与 UI 标题和消息展示逻辑，也不再在 `CopilotChatSession` 内缓存额外的字符串化状态。

### 2. 发送消息优先复用 `AgentSession`

`CopilotChatManager.SendMessageAsync(...)` 现在不再默认把整个历史重新拼成 `ChatMessage[]`。

当前策略是：

1. 先从 `CopilotChatSession.AgentSession` 取运行中的会话；
2. 仅在 `withHistory == true` 且内存里没有时，调用 `chatClientAgent.CreateSessionAsync(...)` 创建新会话；
3. 首次创建后通过 `currentSession.SetAgentSession(agentSession)` 保存一次；
4. 后续 `RunStreamingAsync(...)` 直接传入当前 `AgentSession`，不再在发送前后重复同步状态。

### 3. `withHistory` 只控制是否传入 `AgentSession`

`withHistory` 现在只表达一个语义：

- `withHistory == true`：本次运行传入当前 `AgentSession`
- `withHistory == false`：本次运行不传 `AgentSession`

它不再负责：

- 决定是否改发消息数组；
- 决定是否只传最后一条消息；
- 从本地 `ChatMessages` 回退拼历史。

### 4. 聊天日志按需序列化机器状态

`FileCopilotChatLogger` 现在会继续写：

- 文本日志 `.log`
- 结构化历史 `.xml`

其中 XML 根节点下继续保留可选的 `AgentSessionState`，但新的边界是：

- `ICopilotChatLogger` 不再接收字符串化状态；
- 聊天管理器只传入一个 `ICopilotChatSessionStateProvider`；
- 提供器在真正落盘时才调用 `ChatClientAgent.SerializeSessionAsync(...)` 生成 `JsonElement`；
- XML 中的 `AgentSessionState` 使用 JSON 原文 `CDATA` 保存。

### 5. 会话恢复改为独立功能点

当前 `SendMessageAsync(...)` 不再隐式从历史日志恢复 `AgentSession`。

也就是说，本次改造后：

- 运行期仍支持连续聊天记忆；
- 日志里仍会保存结构化 `AgentSessionState`；
- 但从历史文件恢复机器会话需要单独的显式加载流程，而不是藏在发送链路里。

## 适用场景

- 调整 Copilot 聊天上下文来源时；
- 后续单独实现会话恢复或服务端聊天历史接续时；
- 排查“为什么 UI 历史完整，但模型记忆没有延续”这类问题时。
