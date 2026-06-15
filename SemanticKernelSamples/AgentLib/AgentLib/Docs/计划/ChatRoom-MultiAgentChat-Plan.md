# ChatRoom 多角色群聊功能设计计划

## 背景

当前 `AgentLib` 是 "单人 ↔ 单 AI" 的聊天管理库：`CopilotChatManager` 管理多个 `CopilotChatSession`，每个 Session 中用户发消息 → 一个 AI 回复。现需要支持**多个 LLM 角色在一个聊天室中对话**的场景：

- 聊天室中有多个角色，每个角色有独立的系统提示词（人设）、记忆内容、技能和工具
- 角色可以选用不同的模型（多模态、Flash、思考模型等）
- 角色之间能看到彼此的发言（作为 User 消息），但看不到其他角色的内部思考/工具调用
- 人类可以作为其中一个角色随时插话
- 对话需要持久化，关闭后下次可以继续

参考对象：AutoGen / CrewAI 等框架的 GroupChat 模式。

## 项目组织决策：方案 B（独立项目 `AgentLib.ChatRoom`）

### 决策 0：独立项目 vs 放在 AgentLib 内

选择 **方案 B：独立项目 `AgentLib.ChatRoom`**。

| 维度 | 方案 A（AgentLib 内） | 方案 B（独立项目，采用） |
|------|----------------------|------------------------|
| 关注点分离 | 单人聊天 + 群聊混在一起 | 清晰隔离 |
| InternalsVisibleTo | 不需要 | 需要 `AgentLib` 暴露少量 `internal` API 给 `AgentLib.ChatRoom` |
| 独立测试 | 测试混在 AgentLib.Tests | 独立测试项目 |
| NuGet 包 | 一个包变"胖" | 两个独立包，单人聊天用户无需群聊代码 |
| 项目复杂度 | 低 | 中（需额外一个项目 + 测试项目） |

**选择方案 B 的理由**：
1. 聊天室功能是完全独立的使用场景，与单人聊天没有代码复用上的必然交集
2. 聊天室依赖 `AgentLib`，但 `AgentLib` 不需要知道聊天室的存在——依赖方向正确
3. 两个人可以分别维护两个项目，互不阻塞
4. 即使需要 `InternalsVisibleTo`，暴露面很小（仅 `CopilotChatManager` 的流式管道入口），可控

### 项目结构

```
AgentLib.sln
├── AgentLib/                          ← 现有：单人聊天核心库
│   ├── CopilotChatManager.cs          ← 需要新增 internal 方法供 ChatRoom 使用
│   ├── Model/
│   │   ├── CopilotChatSession.cs
│   │   ├── CopilotChatMessage.cs
│   │   └── SendMessages_/
│   ├── Core/
│   └── ...
│
├── AgentLib.ChatRoom/                 ← 新增：聊天室群聊库
│   ├── AgentLib.ChatRoom.csproj       ← 项目引用 AgentLib
│   ├── Model/
│   │   ├── ChatRoomMessage.cs
│   │   ├── ChatRoomRoleDefinition.cs
│   │   └── ChatRoomSessionData.cs
│   ├── ChatRoomRole.cs
│   ├── ChatRoomSession.cs
│   ├── ChatRoomManager.cs
│   ├── ISpeakerSelector.cs
│   ├── IChatRoomPersistence.cs
│   ├── FileChatRoomPersistence.cs
│   └── SpeakerSelectors/
│       ├── RoundRobinSpeakerSelector.cs
│       └── LlmSpeakerSelector.cs
│
├── AgentLib.ChatRoom.Tests/           ← 新增：聊天室单元测试
│   └── ...
│
└── AgentLib.Tests/                    ← 现有
```

## 设计目标

1. 每个角色独立配置：SystemPrompt、记忆、技能、工具、模型
2. 共享对话历史，角色视角自动转换（其他角色发言 → User 消息）
3. 可插拔的发言选择策略：RoundRobin（默认）、LLM 主持人、手动触发
4. 人类作为角色之一，可随时插话
5. 持久化：JSON 文件存储，支持恢复
6. 流式输出：每个角色的发言实时流式推送到 UI

## 核心设计决策

### 决策 1：ChatRoomRole 内部包装 CopilotChatManager

`ChatRoomRole` 内部持有 `CopilotChatManager` 实例来复用现有能力：

| 复用的能力 | 对应 CopilotChatManager 成员 |
|---|---|
| 模型选择（独立模型） | `AgentApiEndpointManager` → 每个角色可独立注册 provider |
| 工具注册 + 审批包装 | `CopilotToolManager` → `CreateDefaultTools` + `HumanApprovalTool` |
| 流式响应管道 | `SendMessage` → `RunStreamingAsync` 内部循环 |
| 历史压缩 | `SendMessageRequest.ChatReducer` → `CopilotChatManagerChatReducer` |
| 上下文提供者 / 技能 | `AIContextProviders` / `AddSkillFolder` |

**不复用的部分**：
- 多会话管理（`ChatSessions`、`CreateNewSession`、`SelectedSession`）——ChatRoom 只有一个共享 Session
- UI 状态（`IsChatting`、`SendButtonText`、`CanEditInput`）——ChatRoom 不需要

### 决策 2：共享对话历史 ChatRoomSession

所有角色读写同一份 `ChatRoomSession`。当角色 A 要发言时：

```
ChatRoomSession.Messages
    → 过滤：去掉 IsPresetInfo 的消息
    → 角色转换：
        - 角色 A 的历史发言 → ChatRole.Assistant
        - 其他角色（含人类）的发言 → ChatRole.User
    → 传入角色 A 内部的 CopilotChatManager
    → 流式输出追加到 ChatRoomSession
```

**关键设计**：每个角色只看到"公开消息"（文本内容），看不到其他角色的内部思考（Reason）、工具调用细节。这通过 `ChatRoomMessage` 的 `Content` 字段控制——只暴露最终文本，不暴露 `Reason` 和 `FunctionCallContent`。


### 决策 3：SpeakerSelector 可插拔

发言选择策略接口 `ISpeakerSelector`，三种内置实现：

| 策略 | 类 | 行为 |
|---|---|---|
| 固定轮流 | `RoundRobinSpeakerSelector` | 按注册顺序轮流，支持最大轮次、人类插话后恢复 |
| LLM 主持人 | `LlmSpeakerSelector` | 用轻量模型根据上下文决定下一轮谁发言 |
| 手动触发 | `ManualSpeakerSelector` | 外部代码调用 `StepAsync(role)` 显式触发 |

默认使用 `RoundRobinSpeakerSelector`。人类插话通过 `ChatRoomManager.HumanInterjectAsync` 触发，插话后 SpeakerSelector 决定下一个回复者。

### 决策 4：人类角色

人类是 `ChatRoomRoleDefinition` 中 `IsHuman = true` 的特殊角色。人类不调用 LLM，其"发言"直接通过 `HumanInterjectAsync(string content)` 追加到 `ChatRoomSession`。

### 决策 5：InternalsVisibleTo 暴露面

`AgentLib` 需要暴露给 `AgentLib.ChatRoom` 的 `internal` API：

1. `CopilotChatManager` 新增构造函数 `CopilotChatManager(AgentApiEndpointManager, ICopilotChatLogger)` —— 允许注入外部 endpoint manager
2. `CopilotChatManager` 新增 `internal` 方法 `SendMessageForChatRoom(SendMessageRequest)` —— 让 `ChatRoomRole` 复用流式管道但不走完整的 "User → Assistant" 消息管理流程
3. `CopilotChatMessage` 的 `ToChatMessage()` 和 `CreateUser`/`CreateAssistant` 静态方法 —— 已为 `public`，无需额外暴露

### 决策 6：持久化格式

使用 `System.Text.Json` 序列化 `ChatRoomSessionData`，存储为 JSON 文件：

```json
{
  "sessionId": "guid",
  "title": "聊天室标题",
  "createdAt": "ISO8601",
  "lastActivityAt": "ISO8601",
  "roles": [ /* ChatRoomRoleDefinition[] */ ],
  "messages": [ /* ChatRoomMessage[] */ ]
}
```

注意：不持久化工具调用细节和思考内容，只持久化最终文本。角色配置（SystemPrompt、技能、工具）完整持久化。

### 决策 7：错误处理

- 某个角色 LLM 调用失败 → 生成一条系统消息（"角色 X 发言失败：{error}"），不中断整个聊天室循环
- 取消令牌触发 → 优雅停止当前循环，保留已产生的消息
- 空回复 → 生成一条系统消息（"角色 X 未产生回复"）
- 持久化失败 → 记录日志，不中断聊天

### 决策 8：ChatRoomManager 生命周期

```
创建 ChatRoomManager
    → 注册角色 (ChatRoomRole)
    → 设置 SpeakerSelector
    → 设置 Persistence
    → StartAutoLoopAsync(ct)     ← 自动循环
    → HumanInterjectAsync(text)   ← 人类随时插话
    → Stop()                     ← 停止自动循环
    → SaveAsync()                ← 持久化
```

自动循环的停止条件：
1. `Stop()` 被调用
2. `CancellationToken` 被取消
3. `RoundRobinSpeakerSelector` 达到最大轮次（如配置 `MaxRounds = 10`）
4. `LlmSpeakerSelector` 返回 `null`（LLM 判断对话自然结束）

## 核心架构

```
ChatRoomManager（导演，继承 NotifyBase）
├── ChatRoomSession              ← 共享对话历史
│   ├── string SessionId
│   ├── string Title
│   ├── DateTimeOffset CreatedAt
│   └── ObservableCollection<ChatRoomMessage> Messages
│
├── ObservableCollection<ChatRoomRole> Roles
│   └── ChatRoomRole
│       ├── ChatRoomRoleDefinition Definition
│       │   ├── string RoleId
│       │   ├── string RoleName        ← 显示名
│       │   ├── string SystemPrompt    ← 角色人设
│       │   ├── bool IsHuman           ← 是否人类
│       │   ├── string? ModelProviderId
│       │   ├── string? ModelId
│       │   ├── List<string> SkillFolders
│       │   └── List<ToolDefinition> Tools
│       │
│       └── CopilotChatManager _chatManager  ← 内部复用
│           ├── AgentApiEndpointManager      ← 角色独立模型
│           ├── CopilotToolManager           ← 角色专属工具
│           └── SendMessage → RunStreamingAsync ← 流式管道
│
├── ISpeakerSelector SpeakerSelector  ← 发言选择策略
│
└── IChatRoomPersistence? Persistence ← 持久化接口
```

### ChatRoomManager 核心循环

```text
StartAutoLoopAsync(ct):
  while (未停止):
    1. speaker = await SpeakerSelector.SelectNextSpeakerAsync(roles, history, ct)
    2. if speaker == null → break（对话结束）
    3. message = await StepAsync(speaker, ct)
    4. 追加 message 到 Session.Messages
    5. 触发 UI 更新事件

StepAsync(role, ct):
  1. 从 Session.Messages 构建角色视角的 List<ChatMessage>
     - role 的历史消息 → ChatRole.Assistant
     - 其他角色的消息 → ChatRole.User
     - 注入 role.Definition.SystemPrompt
  2. 调用 role.SpeakAsync(视角消息列表, ct) → 流式输出
  3. 将流式输出组装为 ChatRoomMessage
  4. 返回 ChatRoomMessage

HumanInterjectAsync(text, ct):
  1. 创建 ChatRoomMessage（SenderRoleId = 人类角色 Id, IsHumanMessage = true）
  2. 追加到 Session.Messages
  3. 触发 UI 更新
  4. 如果 AutoLoop 正在运行：暂停当前发言，让 SpeakerSelector 重新选择
```

## 需要新增的文件

### AgentLib.ChatRoom 项目

#### 1. `AgentLib.ChatRoom.csproj`
项目文件，引用 `AgentLib`。

#### 2. `Model/ChatRoomMessage.cs`
聊天室消息模型。
- `MessageId`：消息唯一标识
- `SenderRoleId`：发言角色 Id
- `SenderRoleName`：发言角色显示名
- `Role`：`ChatRole.User` 或 `ChatRole.Assistant`
- `Content`：纯文本内容（公开可见）
- `Timestamp`：时间戳
- `IsHumanMessage`：是否人类发送
- `IsSystemMessage`：是否系统消息（如错误提示）
- `MessageItems`：富内容项列表（可选，用于 UI 渲染思考/工具调用）

#### 3. `Model/ChatRoomRoleDefinition.cs`
角色定义（可编辑的配置）。
- `RoleId`、`RoleName`、`SystemPrompt`、`IsHuman`
- `ModelProviderId`、`ModelId`：模型选择
- `SkillFolders`、`Tools`：技能和工具

#### 4. `Model/ChatRoomSessionData.cs`
持久化数据模型（纯数据 DTO）。
- `SessionId`、`Title`、`CreatedAt`、`LastActivityAt`
- `Roles`：`List<ChatRoomRoleDefinition>`
- `Messages`：`List<ChatRoomMessage>`

#### 5. `ChatRoomRole.cs`
角色运行时。内部包装 `CopilotChatManager`。
- 构造函数接收 `ChatRoomRoleDefinition` + `AgentApiEndpointManager`
- `SpeakAsync(IReadOnlyList<ChatMessage> roleViewHistory, CancellationToken)` → `IAsyncEnumerable<ChatRoomStreamingChunk>`

#### 6. `ChatRoomSession.cs`
共享聊天室会话。
- `ObservableCollection<ChatRoomMessage> Messages`
- `FromPersistence(ChatRoomSessionData)` / `ToPersistence()`

#### 7. `ChatRoomManager.cs`
核心编排器，继承 `NotifyBase`。
- `StartAutoLoopAsync`、`StepAsync`、`HumanInterjectAsync`、`Stop`
- `IsRunning`、`CurrentSpeaker` 等 UI 绑定属性
- `SaveAsync` / `LoadAsync` 持久化方法

#### 8. `ISpeakerSelector.cs`
发言选择策略接口。
- `SelectNextSpeakerAsync(roles, history, ct)` → `ChatRoomRole?`

#### 9. `SpeakerSelectors/RoundRobinSpeakerSelector.cs`
固定顺序轮流选择器。
- 属性：`MaxRounds`（最大轮次数，null = 无限）, `CurrentRound`

#### 10. `SpeakerSelectors/LlmSpeakerSelector.cs`
LLM 驱动的发言选择器。
- 内部使用 `AgentApiEndpointManager` 获取 Flash 模型
- SystemPrompt 描述选择逻辑

#### 11. `IChatRoomPersistence.cs`
持久化接口。
- `SaveAsync`、`LoadAsync`、`DeleteAsync`、`ListSessionIdsAsync`

#### 12. `FileChatRoomPersistence.cs`
JSON 文件持久化实现。

### AgentLib.ChatRoom.Tests 项目

#### 13. `AgentLib.ChatRoom.Tests.csproj`
测试项目文件。

#### 14. 测试文件
- `ChatRoomRoleTests.cs`：角色视角构建、流式输出
- `ChatRoomManagerTests.cs`：自动循环、人类插话、停止
- `RoundRobinSpeakerSelectorTests.cs`：轮流逻辑、最大轮次
- `FileChatRoomPersistenceTests.cs`：序列化/反序列化

## 需要修改的现有文件（AgentLib）

### 1. `AgentLib.csproj`
新增 `[InternalsVisibleTo("AgentLib.ChatRoom")]`。

### 2. `AgentLib/CopilotChatManager.cs`
- 新增构造函数 `CopilotChatManager(AgentApiEndpointManager endpointManager, ICopilotChatLogger chatLogger)`，允许注入外部 endpoint manager。
- 新增 `internal SendMessageResult SendMessageForChatRoom(SendMessageRequest request)` 方法：与现有 `SendMessage` 逻辑几乎相同，但不自动管理 `CopilotChatSession` 的消息追加（`AppendMessageAsync` 调用由 `ChatRoomRole` 控制）。
- 或者更简单的方案：新增 `internal Task<ChatClientAgentCreatedResult> CreateChatClientAgentForChatRoomAsync(SendMessageRequest request)` 方法，返回 `ChatClientAgent` + `AgentSession`，由 `ChatRoomRole` 自行调用 `RunStreamingAsync`。

> 方案选择取决于 `ChatRoomRole` 需要多深地介入流式管道。推荐后者（只暴露 Agent 创建），保持最小暴露面。

### 3. 不变更的文件

- `CopilotChatSession.cs` — 聊天室不使用
- `CopilotChatMessage.cs` — 聊天室使用新的 `ChatRoomMessage`
- `SendMessageRequest.cs` — 聊天室直接使用，无需修改
- `SessionTitleGenerator.cs` — 不相关
- `AgentApiEndpointManager.cs` — 已有 API 满足需求

## 开放问题（待确认）

1. **工具调用审批**：聊天室中的角色执行工具时是否需要审批？如果 Role A 调用文件删除工具，谁来审批？可能的方案：(a) 角色自己的工具不需审批（信任）；(b) 统一由人类角色审批。
2. **历史压缩**：共享 `ChatRoomSession` 的消息量可能快速增长。是否需要引入压缩机制？如果需要，压缩后的摘要如何与各角色的独立 `AgentSession` 协调？
3. **角色间直接通信**：是否允许 Role A 通过工具调用直接请求 Role B 的发言？（类似 SubAgent 模式）还是只能通过公开消息间接沟通？
4. **并发发言**：是否允许多个角色同时发言（并行 LLM 调用）？还是严格串行？
5. **模型共享**：如果两个角色使用同一个提供商和模型，是否共享 `IChatClient` 实例以减少连接开销？

---

## 实施步骤

1. **创建 `AgentLib.ChatRoom` 项目** — 新建 `.csproj`，引用 `AgentLib`，配置 `Nullable`、`ImplicitUsings` 等。
2. **创建数据模型** — `ChatRoomMessage`、`ChatRoomRoleDefinition`、`ChatRoomSessionData`。
3. **改造 `AgentLib/CopilotChatManager.cs`** — 新增 `internal` 构造函数和 `internal CreateChatClientAgentForChatRoomAsync` 方法。
4. **实现 `ChatRoomRole`** — 内部包装 `CopilotChatManager`，实现 `SpeakAsync`。
5. **实现 `ChatRoomSession`** — 共享对话历史容器，支持 `ObservableCollection<ChatRoomMessage>`。
6. **实现 `ISpeakerSelector` 和 `RoundRobinSpeakerSelector`** — 默认固定顺序轮流。
7. **实现 `ChatRoomManager`** — 核心编排：`StartAutoLoopAsync`、`StepAsync`、`HumanInterjectAsync`、`Stop`。
8. **实现 `IChatRoomPersistence` 和 `FileChatRoomPersistence`** — JSON 文件持久化。
9. **实现 `LlmSpeakerSelector`** — LLM 驱动的发言选择器。
10. **创建 `AgentLib.ChatRoom.Tests` 项目并编写单元测试**。
11. **编译验证 + 集成测试**。