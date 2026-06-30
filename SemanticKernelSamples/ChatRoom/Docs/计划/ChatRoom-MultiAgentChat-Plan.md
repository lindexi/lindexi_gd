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
| 对 AgentLib 的侵入 | 无 | 需要 AgentLib 补充少量 public API（`AgentApiEndpointManager` 属性加 `init`） |
| 独立测试 | 测试混在 AgentLib.Tests | 独立测试项目 |
| NuGet 包 | 一个包变"胖" | 两个独立包，单人聊天用户无需群聊代码 |
| 项目复杂度 | 低 | 中（需额外一个项目 + 测试项目） |

**选择方案 B 的理由**：
1. 聊天室功能是完全独立的使用场景，与单人聊天没有代码复用上的必然交集
2. 聊天室依赖 `AgentLib`，但 `AgentLib` 不需要知道聊天室的存在——依赖方向正确
3. 两个人可以分别维护两个项目，互不阻塞

**关键原则：不使用 `InternalsVisibleTo`**。`AgentLib` 本身就是一个公开库，如果有什么功能无法满足 `AgentLib.ChatRoom` 的需求，就应该改进 `AgentLib` 的公开 API，而不是通过 `internal` 走后门。`AgentLib.ChatRoom` 完全通过 `AgentLib` 的 `public` API 使用其能力。

### 项目结构

```
AgentLib.sln
├── AgentLib/                          ← 现有：单人聊天核心库
│   ├── CopilotChatManager.cs          ← 微小改造：AgentApiEndpointManager 属性增加 init
│   ├── Model/
│   │   ├── CopilotChatSession.cs
│   │   ├── CopilotChatMessage.cs
│   │   └── SendMessages_/
│   ├── Core/
│   │   └── AgentApiEndpointManager.cs
│   ├── Logging/
│   │   ├── ICopilotChatLogger.cs
│   │   └── FileCopilotChatLogger.cs
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
│   ├── ChatRoomPersistence.cs         ← 复用 FileCopilotChatLogger 的多文件持久化
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
5. 持久化：复用 `FileCopilotChatLogger` 的 XML 格式，分多文件存储（公开记录 + 每个角色私有记录 + 配置）
6. 流式输出：每个角色的发言实时流式推送到 UI

## 核心设计决策

### 决策 1：ChatRoomRole 内部包装 CopilotChatManager

`ChatRoomRole` 内部持有 `CopilotChatManager` 实例来复用现有能力：

| 复用的能力 | 对应 CopilotChatManager 成员 |
|---|---|
| 模型选择（独立模型） | `AgentApiEndpointManager` → 构造函数注入，每个角色可独立配置 |
| 工具注册 + 审批包装 | `CopilotToolManager` → `CreateDefaultTools` + `HumanApprovalTool` |
| 流式响应管道 | `SendMessage` → `RunStreamingAsync` 内部循环 |
| 历史压缩 | `SendMessageRequest.ChatReducer` → `CopilotChatManagerChatReducer` |
| 上下文提供者 / 技能 | `AIContextProviders` / `AddSkillFolder` |

为支持角色独立模型，`AgentLib` 中 `CopilotChatManager.AgentApiEndpointManager` 属性需从当前 `{ get; } = new()` 改为 `{ get; init; } = new()`，允许外部注入。

**不复用的部分**：
- 多会话管理（`ChatSessions`、`CreateNewSession`、`SelectedSession`）——ChatRoom 只有一个共享 Session
- UI 状态（`IsChatting`、`SendButtonText`、`CanEditInput`）——ChatRoom 不需要

### 决策 2：增量消息注入 + AgentSession 延续

`ChatRoomManager` 维护一份共享的 `ChatRoomSession`（公开消息），每个 `ChatRoomRole` 内部持有的 `CopilotChatManager` 维护角色自身的 `CopilotChatSession`（私有历史，含工具调用和思考）。

**核心原则：让 `CopilotChatManager` 正常走 "User → Assistant" 消息管理流程，不做改造。** 每个角色的 `CopilotChatManager` 持续记录该角色的完整会话内容（包括工具调用、思考细节）。当角色再次轮到发言时：

1. 从 `ChatRoomSession` 取出"该角色上次发言之后"的所有公开消息
2. 将这些消息拼接为一条 User 文本，调用该角色 `CopilotChatManager.SendMessageAsync(userText, withHistory: true)`
3. `withHistory: true` 确保 `AgentSession` 中的历史记录得以保留和延续，历史压缩等机制正常工作
4. 角色产生的 Assistant 回复（含工具调用、思考）记录在角色私有的 `CopilotChatSession` 中，同时将公开文本追加到 `ChatRoomSession`

```text
角色 A 首次发言（由 ChatRoomManager 发起）:
  初始话题文本 → CopilotChatManager.SendMessageAsync("初始话题", withHistory: false)
    → AgentSession 创建
    → CopilotChatSession 记录完整回复（含工具调用/思考）
    → 公开文本追加到 ChatRoomSession

角色 B 发言:
  取出 ChatRoomSession 中所有当前消息（全是角色 A 的）
  → CopilotChatManager.SendMessageAsync("角色A说：...", withHistory: false)
    → AgentSession 创建（首次发言）
    → 公开文本追加到 ChatRoomSession

角色 A 再次发言:
  取出 ChatRoomSession 中自角色 A 上次发言之后的所有消息（角色 B 的消息）
  → CopilotChatManager.SendMessageAsync("角色B说：...", withHistory: true)
    → AgentSession 延续（上一次发言的 AgentSession 保留，历史压缩正常工作）
    → 公开文本追加到 ChatRoomSession
```

**关键设计**：每个角色只看到"公开消息"（文本内容），看不到其他角色的内部思考（Reason）、工具调用细节。`ChatRoomMessage` 只暴露最终文本 `Content`，不暴露 `Reason` 和 `FunctionCallContent`。

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

### 决策 5：不使用 InternalsVisibleTo

**不使用 `InternalsVisibleTo`。** `AgentLib` 本身就是一个库，如果有什么功能无法满足的，应该改进公开 API。

`AgentLib` 需要做的唯一改动：`CopilotChatManager.AgentApiEndpointManager` 属性从 `{ get; } = new()` 改为 `{ get; init; } = new()`，使 `ChatRoomRole` 能在构造函数中注入独立的 `AgentApiEndpointManager`。

`ChatRoomRole` 完全通过现有 `public` API（`SendMessageAsync`、`SendMessage`、`SendMessageRequest` 等）使用 `CopilotChatManager`，走标准的 "User → Assistant" 消息管理流程。

### 决策 6：持久化 —— 复用 FileCopilotChatLogger 机制

持久化**不引入新的 JSON 格式**，而是复用 `FileCopilotChatLogger` 已有的 XML 格式和多文件结构。聊天室的持久化文件结构：

```
ChatRoomLogs/
└── {SessionId}/
    ├── room.config.json              ← 聊天室配置（主题、角色定义、SpeakerSelector 配置等）
    ├── room.history.xml              ← 整个聊天室的公开记录（仅文本内容，不含工具调用/思考）
    ├── {RoleId}_Alice/
    │   └── chat_history.xml          ← 角色 Alice 的完整记录（含工具调用、思考细节、用量）
    ├── {RoleId}_Bob/
    │   └── chat_history.xml          ← 角色 Bob 的完整记录
    └── ...
```

- `room.config.json`：使用 `System.Text.Json` 序列化 `ChatRoomRoleDefinition[]` + 聊天室元信息
- `room.history.xml`：复用 `FileCopilotChatLogger` 的 XML 写机制，只写公开文本内容
- `{RoleId}_*/chat_history.xml`：每个角色复用 `FileCopilotChatLogger` 写完整历史（含 `Message` → `Content` / `Reason` / `MessageItems` 的 `ToolItem`、`SubAgentItem` 等）

**理由**：
- `FileCopilotChatLogger` 的 XML 格式已经成熟，支持消息完整序列化
- 分文件结构使公开记录和私有记录天然分离
- 角色各自的日志也能独立分析/回放，便于调试
- `CopilotChatManager` 已有的 `ChatLogger` 机制可直接用于角色私有日志

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
├── ChatRoomSession              ← 共享对话历史（仅公开消息）
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
│           ├── AgentApiEndpointManager      ← 通过 init 注入（角色独立模型）
│           ├── CopilotToolManager           ← 角色专属工具
│           ├── CopilotChatSession           ← 角色私有会话（含工具调用/思考）
│           └── SendMessageAsync             ← 流式管道
│
├── ISpeakerSelector SpeakerSelector  ← 发言选择策略
│
└── ChatRoomPersistence? Persistence  ← 持久化（多文件，复用 FileCopilotChatLogger）
```

### ChatRoomManager 核心循环

```text
StartAutoLoopAsync(ct):
  while (未停止):
    1. speaker = await SpeakerSelector.SelectNextSpeakerAsync(roles, history, ct)
    2. if speaker == null → break（对话结束）
    3. message = await StepAsync(speaker, ct)
    4. 追加 message 到 Session.Messages（公开部分）
    5. 触发 UI 更新事件

StepAsync(role, ct):
  1. 从 Session.Messages 取出自 role 上次发言之后的所有公开消息
  2. 将这些消息拼成一条 User 文本: "角色B说：...\n角色C说：..."
  3. 调用 role.SpeakAsync(userText, ct) → 流式输出
     - 内部: _chatManager.SendMessageAsync(userText, withHistory: true/false)
     - 首次发言: withHistory = false（创建新 AgentSession）
     - 后续发言: withHistory = true（延续 AgentSession，历史压缩生效）
  4. 将流式输出组装为 ChatRoomMessage
  5. 返回 ChatRoomMessage

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
聊天室消息模型（公开可见的消息记录）。
- `MessageId`：消息唯一标识
- `SenderRoleId`：发言角色 Id
- `SenderRoleName`：发言角色显示名
- `Content`：纯文本内容（公开可见）
- `Timestamp`：时间戳
- `IsHumanMessage`：是否人类发送
- `IsSystemMessage`：是否系统消息（如错误提示）

> 注：不包含 `Reason` 和 `FunctionCallContent`——这些是角色私有的，记录在角色各自的 `CopilotChatSession` / `FileCopilotChatLogger` 中。

#### 3. `Model/ChatRoomRoleDefinition.cs`
角色定义（可编辑的配置，序列化到 `room.config.json`）。
- `RoleId`：角色唯一标识
- `RoleName`：角色显示名，如 "代码审查员"
- `SystemPrompt`：角色人设 / 系统提示词
- `IsHuman`：是否人类角色
- `ModelProviderId`：模型提供商 ID（null = 使用共享的 `AgentApiEndpointManager`）
- `ModelId`：具体模型 ID（null = 使用默认）
- `SkillFolders`：技能文件夹路径列表
- `Tools`：角色专属工具定义列表

#### 4. `ChatRoomRole.cs`
角色运行时。内部包装 `CopilotChatManager`。
- 构造函数接收 `ChatRoomRoleDefinition` + 可选的 `AgentApiEndpointManager`
- 内部创建 `CopilotChatManager`，通过 `AgentApiEndpointManager { get; init; }` 注入独立模型配置
- `SpeakAsync(string userText, bool withHistory, CancellationToken)`：调用 `_chatManager.SendMessageAsync(userText, withHistory, ...)`，返回流式输出
- `InitializeAsync(CancellationToken)`：初始化（注册模型 provider、加载技能等）

#### 5. `ChatRoomSession.cs`
共享聊天室会话（公开消息容器）。
- `string SessionId`
- `string Title`
- `DateTimeOffset CreatedAt`
- `ObservableCollection<ChatRoomMessage> Messages`
- `Dictionary<string, DateTimeOffset?> LastSpeakTimeByRole`：记录每个角色上次发言时间，用于增量消息提取
- `IReadOnlyList<ChatRoomMessage> GetMessagesSinceLastSpeak(string roleId)`：获取自指定角色上次发言后的所有公开消息

#### 6. `ChatRoomManager.cs`
核心编排器，继承 `NotifyBase`。
- `ChatRoomSession Session`
- `ObservableCollection<ChatRoomRole> Roles`
- `ISpeakerSelector SpeakerSelector`
- `ChatRoomPersistence? Persistence`
- `bool IsRunning`（UI 绑定）
- `ChatRoomRole? CurrentSpeaker`（UI 绑定）
- `StartAutoLoopAsync(CancellationToken)`
- `StepAsync(ChatRoomRole, CancellationToken)` → `ChatRoomMessage`
- `HumanInterjectAsync(string content, CancellationToken)`
- `Stop()`
- `SaveAsync(CancellationToken)`
- `LoadAsync(string sessionId, CancellationToken)`

#### 7. `ISpeakerSelector.cs`
发言选择策略接口。
```csharp
public interface ISpeakerSelector
{
    /// <summary>
    /// 根据当前对话历史决定下一个发言的角色。
    /// 返回 null 表示对话结束。
    /// </summary>
    Task<ChatRoomRole?> SelectNextSpeakerAsync(
        IReadOnlyList<ChatRoomRole> roles,
        IReadOnlyList<ChatRoomMessage> history,
        CancellationToken cancellationToken);
}
```

#### 8. `SpeakerSelectors/RoundRobinSpeakerSelector.cs`
固定顺序轮流选择器。
- `MaxRounds`：最大轮次数（null = 无限）
- `CurrentRound`：当前轮次计数
- 人类插话后恢复队列位置

#### 9. `SpeakerSelectors/LlmSpeakerSelector.cs`
LLM 驱动的发言选择器。
- 内部使用 `AgentApiEndpointManager` 获取 Flash 模型
- SystemPrompt 描述选择逻辑（根据对话内容判断下一个发言者或对话结束）

#### 10. `ChatRoomPersistence.cs`
持久化管理器。内部复用 `FileCopilotChatLogger` 的 XML 格式 + `System.Text.Json` 的配置序列化。

文件结构：
- `{logFolder}/{SessionId}/room.config.json`：JSON，聊天室配置 + 角色定义
- `{logFolder}/{SessionId}/room.history.xml`：XML，公开聊天记录（复用 `FileCopilotChatLogger`）
- `{logFolder}/{SessionId}/{RoleId}/chat_history.xml`：XML，每个角色的私有完整记录

公开方法：
- `SaveConfigAsync(ChatRoomSession, IReadOnlyList<ChatRoomRoleDefinition>)`
- `SavePublicMessageAsync(Guid sessionId, ChatRoomMessage)`
- `SaveRoleMessageAsync(Guid sessionId, string roleId, CopilotChatMessage)`
- `LoadConfigAsync(string sessionId)` → `(ChatRoomSessionData, ChatRoomRoleDefinition[])`
- `ListSessionsAsync()` → `IReadOnlyList<string>`

### AgentLib.ChatRoom.Tests 项目

#### 11. `AgentLib.ChatRoom.Tests.csproj`
测试项目文件。

#### 12. 测试文件
- `ChatRoomRoleTests.cs`：角色发言、增量消息注入、AgentSession 延续
- `ChatRoomManagerTests.cs`：自动循环、人类插话、停止
- `RoundRobinSpeakerSelectorTests.cs`：轮流逻辑、最大轮次、人类插话后恢复
- `ChatRoomPersistenceTests.cs`：多文件持久化、恢复

## 需要修改的现有文件（AgentLib）

### 1. `AgentLib/CopilotChatManager.cs`

将 `AgentApiEndpointManager` 属性从：

```csharp
public AgentApiEndpointManager AgentApiEndpointManager { get; } = new();
```

改为：

```csharp
public AgentApiEndpointManager AgentApiEndpointManager { get; init; } = new();
```

**理由**：`ChatRoomRole` 需要通过构造函数注入独立的 `AgentApiEndpointManager`（注册角色专属的模型 provider），而不是使用 `CopilotChatManager` 默认创建的实例。改为 `init` 保持不可变性（构造后不变），同时允许外部注入。

### 2. 不变更的文件

- `CopilotChatSession.cs` — `ChatRoomRole` 内部的 `CopilotChatManager` 正常使用
- `CopilotChatMessage.cs` — 聊天室公开消息使用新的 `ChatRoomMessage`，角色私有消息仍使用 `CopilotChatMessage`
- `SendMessageRequest.cs` — `ChatRoomRole` 通过 `SendMessageAsync` 直接使用，无需修改
- `SessionTitleGenerator.cs` — 不相关
- `AgentApiEndpointManager.cs` — 已有 API 满足需求
- `FileCopilotChatLogger.cs` — 聊天室持久化复用其 XML 格式，通过 `ChatRoomPersistence` 封装
- `ICopilotChatLogger.cs` — 不修改接口，聊天室持久化在 `ChatRoomPersistence` 中使用 `FileCopilotChatLogger` 实例

## 开放问题（待确认）

1. **工具调用审批**：聊天室中的角色执行工具时是否需要审批？如果 Role A 调用文件删除工具，谁来审批？可能的方案：(a) 角色自己的工具不需审批（信任）；(b) 统一由人类角色审批。
2. **历史压缩**：每个角色内部的 `CopilotChatManager` 已自带压缩（`SendMessageRequest.ChatReducer`）。共享 `ChatRoomSession` 的公开消息不需要压缩（仅存文本，量小）。各角色独立压缩互不干扰。
3. **角色间直接通信**：是否允许 Role A 通过工具调用直接请求 Role B 的发言？（类似 SubAgent 模式）还是只能通过公开消息间接沟通？
4. **并发发言**：是否允许多个角色同时发言（并行 LLM 调用）？还是严格串行？
5. **模型共享**：如果两个角色使用同一个提供商和模型，是否共享 `IChatClient` 实例以减少连接开销？

---

## 实施步骤

1. **改造 `AgentLib/CopilotChatManager.cs`** — `AgentApiEndpointManager` 属性增加 `init` setter。
2. **创建 `AgentLib.ChatRoom` 项目** — 新建 `.csproj`，引用 `AgentLib`，配置 `Nullable`、`ImplicitUsings` 等。
3. **创建数据模型** — `ChatRoomMessage`、`ChatRoomRoleDefinition`、`ChatRoomSessionData`。
4. **实现 `ChatRoomSession`** — 共享对话历史容器，支持 `ObservableCollection<ChatRoomMessage>` 和增量消息提取 `GetMessagesSinceLastSpeak`。
5. **实现 `ChatRoomRole`** — 内部包装 `CopilotChatManager`，实现增量注入式 `SpeakAsync`。
6. **实现 `ISpeakerSelector` 和 `RoundRobinSpeakerSelector`** — 默认固定顺序轮流。
7. **实现 `ChatRoomManager`** — 核心编排：`StartAutoLoopAsync`、`StepAsync`、`HumanInterjectAsync`、`Stop`。
8. **实现 `ChatRoomPersistence`** — 多文件持久化（复用 `FileCopilotChatLogger` + JSON 配置）。
9. **实现 `LlmSpeakerSelector`** — LLM 驱动的发言选择器。
10. **创建 `AgentLib.ChatRoom.Tests` 项目并编写单元测试**。
11. **编译验证 + 集成测试**。