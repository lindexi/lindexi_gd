# ChatRoom 代码审阅修复计划

## 背景

在 a91feb6ff15877fe1aa99430eb0fd003096227a5 提交的 ChatRoom 功能代码审阅中，发现了若干设计与实现问题。本文档记录修复方案与决策。

## 审阅发现与决策

### 问题 1：SystemPrompt 注入方式不正确（已修复）

**现状**：`ChatRoomRole.BuildFirstRoundInput()` 将角色人设（`SystemPrompt`）和记忆内容拼入 User 文本字符串，与对话内容混在一起发给 LLM。LLM 无法正确区分"指令"与"他人发言"。

**根因**：`ChatRoomRole.SpeakAsync` 调用 `CopilotChatManager.SendMessageAsync(string)` 重载，该重载不支持指定 `SystemPrompt`。

**修复方案**：改为调用 `CopilotChatManager.SendMessage(SendMessageRequest)`，通过 `SendMessageRequest.SystemPrompt` 属性注入。`CopilotChatManager.SendMessage` 内部会将 `SystemPrompt` 作为 `ChatRole.System` 消息放在 User 消息之前（见 `CopilotChatManager.cs` 第 473-476 行）。

```csharp
// 修复后：SystemPrompt 作为 ChatRole.System 正确注入
var request = new SendMessageRequest(incrementalUserText)
{
    WithHistory = true,
    SystemPrompt = systemPrompt,  // 首次发言时包含角色人设+记忆
    CancellationToken = cancellationToken,
};
SendMessageResult result = _chatManager.SendMessage(request);
await result.RunTask;
```

**影响文件**：`AgentLib.ChatRoom/ChatRoomRole.cs`
- 新增 `BuildSystemPrompt()` 方法（替代 `BuildFirstRoundInput()`）
- `SpeakAsync` 改用 `SendMessage` + `SendMessageRequest`

---

### 问题 2：模型提供商注册缺失（已修复）

**现状**：`ChatRoomRole.InitializeAsync` 中有如下代码块但实际未做任何注册：

```csharp
if (!string.IsNullOrWhiteSpace(Definition.ModelProviderId) && !string.IsNullOrWhiteSpace(Definition.ModelId))
{
    // 注释承认：API 需在外部完成，但 ChatRoomManager 也没有做
}
```

`ChatRoomRoleDefinition.ModelProviderId` 和 `ModelId` 字段无法生效，每个角色始终使用默认 provider。

**修复方案**：
1. `ChatRoomRole` 暴露 `EndpointManager` 公开只读属性，允许外部访问其 `AgentApiEndpointManager`。
2. `ChatRoomManager` 新增 `RegisterRoleModelProviders(IReadOnlyDictionary<string, ILanguageModelProvider>)` 方法，根据角色的 `ModelProviderId` 匹配并注册对应的 provider。

**影响文件**：
- `AgentLib.ChatRoom/ChatRoomRole.cs` — 新增 `EndpointManager` 属性
- `AgentLib.ChatRoom/ChatRoomManager.cs` — 新增 `RegisterRoleModelProviders` 方法

---

### 问题 3：SessionId 类型不匹配（已修复）

**现状**：`ChatRoomSession.SessionId` 为 `string` 类型（`Guid.NewGuid().ToString("N")`），但 `ChatRoomPersistence.SavePublicMessageAsync` 接收 `Guid sessionId`。`ChatRoomManager.AppendMessage` 中使用 `Guid.Parse(Session.SessionId)` 进行强制转换，类型不安全。

**修复方案**：仿照 `CopilotChatSession.SessionId` 的设计，将 `ChatRoomSession.SessionId` 和 `ChatRoomSessionData.SessionId` 统一改为 `Guid` 类型。

**影响文件**：
- `AgentLib.ChatRoom/ChatRoomSession.cs` — `SessionId` 类型、构造函数
- `AgentLib.ChatRoom/Model/ChatRoomSessionData.cs` — `SessionId` 类型
- `AgentLib.ChatRoom/ChatRoomManager.cs` — 移除 `Guid.Parse(Session.SessionId)`
- `AgentLib.ChatRoom/ChatRoomPersistence.cs` — `SaveConfigAsync` 中 `SessionId` 转字符串

---

### 问题 4：ChatRoomRole 异常被静默吞掉（已修复）

**现状**：`ChatRoomRole.SpeakAsync` 中有 `catch (Exception) { return null; }`，导致 LLM 调用失败时返回 `null`。`ChatRoomManager.StepAsync` 收到 `null` 后判断为"角色未产生有效回复"，不会触发 `OnRoleSpeakFailed` 事件——实际崩溃和主动空回复无法区分。

**修复方案**：删除 `catch (Exception)` 块，让异常透传到 `ChatRoomManager.StepAsync` 中统一处理（`ChatRoomManager.StepAsync` 已有完整的 `catch (Exception ex)` 处理逻辑，会触发 `OnRoleSpeakFailed` 事件并生成系统消息）。

**影响文件**：`AgentLib.ChatRoom/ChatRoomRole.cs` — 移除 `catch (Exception)` 块

---

### 问题 5：ChatRoomPersistence.GetRoleLogger 跨 session 键冲突（已修复）

**现状**：

```csharp
private FileCopilotChatLogger GetRoleLogger(string sessionFolder, string roleId)
{
    if (_roleLoggers.TryGetValue(roleId, out ...))  // 只用 roleId 做 key
        return logger;
    // ...
}
```

当存在多个 session 且有同名 roleId 时，第二个 session 会复用第一个 session 的 logger 实例，导致日志写入错误的文件夹。

**修复方案**：将字典 key 改为复合键 `$"{sessionFolder}:{roleId}"`。

**影响文件**：`AgentLib.ChatRoom/ChatRoomPersistence.cs`

---

### 问题 6：未使用的 GetPublicHistoryFilePath（已修复）

**现状**：`GetPublicHistoryFilePath` 方法已定义但从未被调用。公开消息实际上通过 `_publicLogger.LogMessageAsync` 写入纯文本日志，并非 XML。

**修复方案**：移除 `GetPublicHistoryFilePath` 方法，更新类注释以反映实际文件结构（`public_logs/` 纯文本，非 `room.history.xml`）。

**影响文件**：`AgentLib.ChatRoom/ChatRoomPersistence.cs`

---

### 问题 7：RoundRobinSpeakerSelector XML 文档错误（已修复）

**现状**：`SelectNextSpeakerAsync` 方法的 `<summary>` 文档内容是 `CurrentRound` 属性的内容（"当前轮次计数（从 1 开始）。"），属于拷贝粘贴错误。

**修复方案**：修正为正确的方法行为描述。

**影响文件**：`AgentLib.ChatRoom/SpeakerSelectors/RoundRobinSpeakerSelector.cs`

---

## 其他保留（不修改）的问题

| 问题 | 决策 | 理由 |
|------|------|------|
| `InitializeAsync` 伪装为异步 | 保留 | 未来可能改为真正异步（如远程拉取 skill 配置） |
| `RoundRobinSpeakerSelector` 线程安全 | 保留 | 当前 `StartAutoLoopAsync` 串行调用，无并发场景 |
| 空增量消息允许发送 | 保留 | 设计如此——即使无新消息也允许角色发言 |
| `DeriveGuidFromString` 非标准做法 | 保留 | 实践中可行，后续可统一优化 |
| `LlmSpeakerSelector` 未实现 | 保留 | 不在本次提交范围 |
| `net6.0;net9.0` 多目标 | 保留 | 设计如此 |

---

## 实施步骤

1. **修改 `ChatRoomSession.SessionId` 从 `string` 改为 `Guid`** — 对齐 `CopilotChatSession` 设计
2. **修改 `ChatRoomSessionData.SessionId` 从 `string` 改为 `Guid`** — 与 `ChatRoomSession` 保持一致
3. **修改 `ChatRoomRole.SpeakAsync`** — 通过 `SendMessageRequest.SystemPrompt` 注入角色人设和记忆，移除 `catch (Exception)` 吞异常
4. **修改 `ChatRoomManager`** — 移除 `Guid.Parse(Session.SessionId)`，添加 `RegisterRoleModelProviders` 方法，暴露 `ChatRoomRole.EndpointManager`
5. **修改 `ChatRoomPersistence.GetRoleLogger`** — 使用复合键 `$"{sessionFolder}:{roleId}"`
6. **修改 `ChatRoomPersistence`** — 移除未使用的 `GetPublicHistoryFilePath` 和过时注释
7. **修改 `RoundRobinSpeakerSelector`** — 修正 `SelectNextSpeakerAsync` 的 XML 文档
8. **编译验证** — 确保所有项目编译通过
