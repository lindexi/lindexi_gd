# AgentLib.ChatRoom

基于 [AgentLib](../AgentLib/) 的多角色群聊库。允许在一个聊天室中让多个 LLM 角色自动对话，每个角色拥有独立的系统提示词、记忆、模型和工具。

## 快速开始

```csharp
using AgentLib.ChatRoom;
using AgentLib.ChatRoom.Model;

// 1. 创建聊天室
var chatRoom = new ChatRoomManager();

// 2. 定义角色
var aliceDef = new ChatRoomRoleDefinition
{
    RoleId = "alice",
    RoleName = "Alice",
    SystemPrompt = "你叫 Alice，是一位资深 C# 开发者。你擅长代码审查和性能优化。回答简洁专业。",
};

var bobDef = new ChatRoomRoleDefinition
{
    RoleId = "bob",
    RoleName = "Bob",
    SystemPrompt = "你叫 Bob，是一位项目经理。你关注代码的可维护性和团队协作。",
};

// 3. 创建角色并加入聊天室
var alice = new ChatRoomRole(aliceDef);
var bob = new ChatRoomRole(bobDef);
chatRoom.Roles.Add(alice);
chatRoom.Roles.Add(bob);

// 4. 注册模型提供商（以 OpenAI 为例）
alice.EndpointManager.RegisterLanguageModelProvider(/* ... */);
bob.EndpointManager.RegisterLanguageModelProvider(/* ... */);

// 5. 启动自动循环
await chatRoom.StartAutoLoopAsync();
```

## 核心概念

### 聊天室（ChatRoomManager）

聊天室是"导演"，负责编排对话流程：

- **`ChatRoomSession`** — 共享对话历史，所有角色的公开消息集中存储
- **`Roles`** — 参与聊天的角色集合
- **自动调度循环** — 根据人类消息、@mention、普通角色和管理者角色推进对话
- **`Persistence`** — 可选的持久化支持

### 角色（ChatRoomRole）

每个角色是独立的 LLM Agent，拥有：

| 配置项 | 说明 |
|--------|------|
| `SystemPrompt` | 角色人设/系统提示词 |
| `MemoryContent` | 角色的长期记忆（注入到首次发言的 SystemPrompt 中） |
| `ModelProviderId` / `ModelId` | 独立选择模型（可为不同角色分配不同模型） |
| `SkillFolders` | 技能文件夹路径 |
| `Tools` | 角色专属工具定义 |

角色内部包装了一个 `CopilotChatManager` 实例，因此天然支持流式输出、工具调用、历史压缩等能力。

### 自动发言调度

自动循环围绕当前消息创建一轮调度：

- 消息中显式 @ 的非人类角色优先发言
- 人类消息未 @ 任何角色时，所有 `AlwaysParticipate` 普通角色按注册顺序各尝试一次
- 普通角色无法继续推进时，所有 `IsManagerRole` 管理者按注册顺序依次介入
- 同一轮调度内同一角色最多自动尝试一次，避免空回复或重复 @ 导致死循环

### 增量消息注入

角色发言时，只看到"上次发言之后"其他角色产生的增量消息。这意味着：

- 角色的 `AgentSession` 得以延续，历史压缩正常工作
- 角色看不到其他角色的内部思考和工具调用
- Token 消耗可控

### 人类插话

人类作为特殊角色参与对话：

```csharp
await chatRoom.HumanInterjectAsync("我觉得应该先重构再优化", "human", "我");
```

人类插话后，当前正在发言的角色会先完成；随后自动循环从最新人类消息重新调度，让助手优先回应新的上下文。

## 使用场景

### 场景 1：代码审查三人组

```csharp
var architect = new ChatRoomRole(new ChatRoomRoleDefinition
{
    RoleId = "architect",
    RoleName = "架构师",
    SystemPrompt = "你是软件架构师。关注系统设计、扩展性和技术选型。",
});

var developer = new ChatRoomRole(new ChatRoomRoleDefinition
{
    RoleId = "dev",
    RoleName = "开发者",
    SystemPrompt = "你是开发者。关注代码实现细节和性能。",
});

var tester = new ChatRoomRole(new ChatRoomRoleDefinition
{
    RoleId = "qa",
    RoleName = "测试工程师",
    SystemPrompt = "你是测试工程师。关注边界情况、异常场景和测试覆盖。",
});

chatRoom.Roles.Add(architect);
chatRoom.Roles.Add(developer);
chatRoom.Roles.Add(tester);

// 启动自动循环
await chatRoom.StartAutoLoopAsync();
```

### 场景 2：角色使用不同模型

```csharp
// 思考型角色使用推理模型
var analyst = new ChatRoomRole(new ChatRoomRoleDefinition
{
    RoleId = "analyst",
    RoleName = "分析师",
    SystemPrompt = "你是数据分析师，需要深入思考后再回答。",
    ModelId = "gemini-2.5-pro",  // 慢但强
});

// 快速回复型角色使用 Flash 模型
var assistant = new ChatRoomRole(new ChatRoomRoleDefinition
{
    RoleId = "assistant",
    RoleName = "助手",
    SystemPrompt = "你是聊天助手，回答简洁快速。",
    ModelId = "gemini-2.0-flash",  // 快但轻
});
```

### 场景 3：持久化与恢复

```csharp
// 设置持久化
chatRoom.Persistence = new ChatRoomPersistence("C:\\ChatRoomLogs");

// 保存
await chatRoom.SaveAsync();

// 恢复
var restored = new ChatRoomManager();
restored.Persistence = new ChatRoomPersistence("C:\\ChatRoomLogs");
await restored.LoadAsync("session-guid-here");
```

### 场景 4：处理发言事件

```csharp
chatRoom.OnMessageAdded += (sender, message) =>
{
    Console.WriteLine($"[{message.SenderRoleName}]: {message.Content}");
};

chatRoom.OnRoleSpeakFailed += (sender, args) =>
{
    Console.WriteLine($"角色 {args.Role.Definition.RoleName} 发言失败: {args.Exception.Message}");
};

chatRoom.OnSpeakingChanged += (sender, args) =>
{
    Console.WriteLine($"发言者切换: {args.PreviousSpeaker?.Definition.RoleName ?? "无"} → {args.CurrentSpeaker?.Definition.RoleName ?? "无"}");
};
```

### 场景 5：停止自动循环

```csharp
// 启动后台循环
_ = Task.Run(() => chatRoom.StartAutoLoopAsync());

// 随时停止
chatRoom.Stop();

// 检查状态
if (!chatRoom.IsRunning)
{
    Console.WriteLine("聊天室已停止");
}
```

## API 参考

### ChatRoomManager

| 成员 | 说明 |
|------|------|
| `Session` | 共享会话（`ChatRoomSession`） |
| `Roles` | 角色集合（`ObservableCollection<ChatRoomRole>`） |
| `Persistence` | 持久化管理器，null 时不持久化 |
| `IsRunning` | 是否正在自动循环 |
| `CurrentSpeaker` | 当前正在发言的角色 |
| `StartAutoLoopAsync(ct)` | 启动自动循环 |
| `Stop()` | 停止自动循环 |
| `StepAsync(role, ct)` | 手动让指定角色发言一次 |
| `HumanInterjectAsync(content, roleId, roleName, ct)` | 人类插话 |
| `SaveAsync(ct)` | 持久化当前会话 |
| `LoadAsync(sessionId, ct)` | 从持久化恢复 |
| `OnMessageAdded` | 新消息事件 |
| `OnRoleSpeakFailed` | 角色发言失败事件 |
| `OnSpeakingChanged` | 发言角色变更事件 |

### ChatRoomRole

| 成员 | 说明 |
|------|------|
| `Definition` | 角色配置（`ChatRoomRoleDefinition`） |
| `EndpointManager` | 角色的端点管理器，可注册模型提供商 |
| `MainThreadDispatcher` | UI 线程调度器（init-only） |
| `InitializeAsync(ct)` | 初始化角色（加载技能等） |
| `SpeakAsync(userText, ct)` | 发言一次，返回公开回复文本 |

### ChatRoomRoleDefinition

| 属性 | 说明 |
|------|------|
| `RoleId` | 角色唯一标识 |
| `RoleName` | 角色显示名 |
| `SystemPrompt` | 系统提示词（角色人设） |
| `IsHuman` | 是否人类角色 |
| `ModelProviderId` | 模型提供商 ID |
| `ModelId` | 模型 ID |
| `SkillFolders` | 技能文件夹路径列表 |
| `Tools` | 工具定义列表 |
| `MemoryContent` | 角色记忆内容 |

### ChatRoomPersistence

持久化文件结构：

```
{baseFolder}/
├── public_logs/
│   └── {sessionId}.log
└── {SessionId}/
    ├── room.config.json       ← JSON 配置（角色定义 + 消息）
    └── {RoleId}/
        └── chat_history.xml   ← 角色私有完整历史（含工具调用和思考）
```

| 方法 | 说明 |
|------|------|
| `SaveConfigAsync(data, ct)` | 保存会话配置 |
| `LoadConfigAsync(sessionId, ct)` | 加载会话配置 |
| `SavePublicMessageAsync(sessionId, message)` | 保存公开消息 |
| `SaveRoleMessageAsync(sessionId, roleId, message)` | 保存角色私有消息 |
| `ListSessionIds()` | 列出所有会话 ID |
| `Delete(sessionId)` | 删除指定会话 |

## 设计原则

1. **不使用 InternalsVisibleTo** — 完全通过 `AgentLib` 的 public API 使用其能力
2. **增量注入** — 角色的 `CopilotChatManager` 持续记录完整历史，每次发言只注入增量消息
3. **公开/私有分离** — 公开消息仅文本，角色的工具调用和思考细节记录在私有日志中
4. **调度内聚** — 自动发言调度是聊天室核心业务流程，由 `ChatRoomManager` 统一维护
