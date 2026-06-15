# ChatRoom 多角色群聊功能设计与实施计划

## 背景

当前 `AgentLib` 是"单人 ↔ 单 AI"模式。`CopilotChatManager` 管理多会话、`SendMessageRequest` 支持按请求定制 SystemPrompt/Tools/ChatClient，这些是多角色群聊的良好基础。

本计划在现有基础设施上构建**多角色群聊**能力。

## 架构总览

```
ChatRoomManager（导演）
├── ChatRoomSession              ← 共享对话历史
│   └── List<ChatRoomMessage>    ← 每条消息标记说话角色
├── List<ChatRoomRole>           ← 参与角色
│   └── ChatRoomRole
│       ├── SystemPrompt         ← 角色人设
│       ├── CopilotChatManager   ← 复用现有能力
│       ├── Tools / Skills       ← 角色专属技能
│       └── Memory               ← 角色长期记忆（可选）
├── ISpeakerSelector             ← 发言选择策略（可插拔）
└── Persistence                  ← 持久化存储
```

## 核心设计决策

### 决策 1：ChatRoomRole 内部使用 CopilotChatManager

`ChatRoomRole` 内部持有 `CopilotChatManager` 实例，原因：
- `CopilotChatManager` 的 `SendMessage` → `RunStreamingAsync` 是经过充分测试的流式响应管道
- 复用 `AgentApiEndpointManager` 的模型选择（Flash、多模态、思考模型）
- 复用 `CopilotToolManager` 的工具注册和 `HumanApprovalTool` 包装
- 复用 `SendMessageRequest` 的 `SystemPrompt`、`Tools`、`ChatReducer`、`AIContextProviders` 注入

不使用 `CopilotChatManager` 的部分：多会话管理（`ChatSessions`、`CreateNewSession`、`SelectedSession`）——ChatRoom 只有一个共享 Session。

### 决策 2：共享对话历史 ChatRoomSession

所有角色读写同一份 `ChatRoomSession`。当角色 A 要发言时：
1. 从 `ChatRoomSession` 构建角色 A 视角的消息列表（其他角色的发言 → `ChatRole.User`）
2. 调用角色 A 内部的 `CopilotChatManager` 流式输出
3. 输出追加到 `ChatRoomSession`

### 决策 3：SpeakerSelector 可插拔

发言选择策略接口 `ISpeakerSelector`：
- `RoundRobinSpeakerSelector`：固定顺序轮流（默认）
- `LlmSpeakerSelector`：由 LLM "主持人"根据上下文决定下一轮
- `ManualSpeakerSelector`：外部手动触发
- 人类随时可插话（`HumanInterjectAsync`）

### 决策 4：持久化

`ChatRoomSession` 支持序列化/反序列化（JSON），通过 `IChatRoomPersistence` 接口实现文件/数据库存储。

### 决策 5：放在 AgentLib 项目内

`ChatRoomRole.SpeakAsync` 需要复用 `CopilotChatManager` 的流式管道逻辑，同程序集可直接调用 `internal` 方法，避免 `InternalsVisibleTo` 的隐性耦合。文件夹结构提供逻辑隔离：

```
AgentLib/
├── CopilotChatManager.cs          ← 现有：单人聊天
├── SessionTitleGenerator.cs       ← 现有
├── Model/
│   ├── CopilotChatSession.cs      ← 现有
│   ├── CopilotChatMessage.cs      ← 现有
│   └── ChatRoom/                  ← 新增：聊天室模型
│       ├── ChatRoomMessage.cs
│       ├── ChatRoomRoleDefinition.cs
│       └── ChatRoomSessionData.cs
├── ChatRoom/                      ← 新增：聊天室核心
│   ├── ChatRoomRole.cs
│   ├── ChatRoomSession.cs
│   ├── ChatRoomManager.cs
│   ├── ISpeakerSelector.cs
│   ├── IChatRoomPersistence.cs
│   ├── FileChatRoomPersistence.cs
│   └── SpeakerSelectors/
│       ├── RoundRobinSpeakerSelector.cs
│       └── LlmSpeakerSelector.cs
└── Docs/
    └── 计划/
        └── ChatRoom-Plan.md       ← 本计划文档
```

## 需要新增的文件

### 1. `AgentLib/Model/ChatRoom/ChatRoomMessage.cs`
聊天室消息模型。与 `CopilotChatMessage` 不同的关键点：每条消息携带 `SenderRoleId` 标记是哪个角色说的。

### 2. `AgentLib/Model/ChatRoom/ChatRoomRoleDefinition.cs`
角色定义（可编辑的配置）。包含 RoleId、RoleName、SystemPrompt、MemoryContent、ModelProviderId、ModelId、SkillFolders、Tools。

### 3. `AgentLib/ChatRoom/ChatRoomRole.cs`
角色运行时。内部包装 `CopilotChatManager`。核心方法 `SpeakAsync`：给定共享上下文，流式输出回复。

### 4. `AgentLib/ChatRoom/ChatRoomSession.cs`
共享聊天室会话，持有所有消息。支持序列化/反序列化。

### 5. `AgentLib/ChatRoom/ISpeakerSelector.cs`
发言选择策略接口。`SelectNextSpeakerAsync` 根据当前对话历史决定下一个发言的角色。

### 6. `AgentLib/ChatRoom/SpeakerSelectors/RoundRobinSpeakerSelector.cs`
固定顺序轮流发言选择器。支持最大轮次限制和人类插话后自动回到队列。

### 7. `AgentLib/ChatRoom/SpeakerSelectors/LlmSpeakerSelector.cs`
基于 LLM "主持人"的发言选择器。用 Prompt 让 LLM 根据对话上下文决定下一个发言者。

### 8. `AgentLib/ChatRoom/ChatRoomManager.cs`
聊天室核心管理器。`StartAutoLoopAsync`、`StepAsync`、`HumanInterjectAsync`、`Stop`。

### 9. `AgentLib/ChatRoom/IChatRoomPersistence.cs`
持久化接口。SaveAsync、LoadAsync、DeleteAsync、ListSessionIdsAsync。

### 10. `AgentLib/ChatRoom/FileChatRoomPersistence.cs`
文件系统持久化实现。`ChatRoomSessionData` 序列化为 JSON 文件。

## 需要修改的现有文件

### 11. `AgentLib/CopilotChatManager.cs`
- 新增构造函数重载 `CopilotChatManager(AgentApiEndpointManager, ICopilotChatLogger)`，允许注入外部 endpoint manager
- 新增 `internal` 方法供 `ChatRoomRole` 调用流式输出管道

## 实施步骤

1. **创建 `ChatRoom` 命名空间的基础数据模型** — 新建 `ChatRoomMessage`、`ChatRoomRoleDefinition`、`ChatRoomSessionData` 等 Model 类。

2. **实现 `ChatRoomRole`** — 内部包装 `CopilotChatManager`，实现 `SpeakAsync` 方法：构建角色视角的消息列表（其他角色→User），调用流式输出。

3. **实现 `ChatRoomSession`** — 共享对话历史容器，支持 `ObservableCollection<ChatRoomMessage>` 用于 UI 绑定。

4. **实现 `ISpeakerSelector` 和 `RoundRobinSpeakerSelector`** — 默认固定顺序轮流，含最大轮次和人类插话打断。

5. **实现 `ChatRoomManager`** — 核心编排器：`StartAutoLoopAsync`、`StepAsync`、`HumanInterjectAsync`、`Stop`。

6. **改造 `CopilotChatManager` 支持共享 `AgentApiEndpointManager`** — 新增构造函数重载，允许注入外部 endpoint manager。

7. **实现 `IChatRoomPersistence` 和 `FileChatRoomPersistence`** — JSON 文件持久化。

8. **实现 `LlmSpeakerSelector`** — LLM 驱动的发言选择器（可选扩展）。

9. **编写单元测试** — 覆盖：RoundRobin 发言顺序、人类插话、持久化恢复、角色视角上下文构建。

10. **编译验证 + 集成测试** — 确保与现有 `AgentLib` 功能不冲突。
