# ChatRoom @Mention 与发言策略改造设计

## 背景

当前 `ChatRoomManager` + `ISpeakerSelector` 的发言选择逻辑过于简单，无法支持以下场景：

1. **角色参与模式**：有些角色应"默认参与所有对话"（如通用助手），有些角色应"仅在被 @ 时才发言"（如专业领域专家）
2. **@ 链式调用**：角色 A 发言中 @ 了角色 B → B 发言 → B 发言中 @ 了角色 C → C 发言 → ... 形成链式对话
3. **人类插话后的合理暂停**：人类 @ 某个角色后，该角色回复一次即暂停，不继续自动循环

当前 `ISpeakerSelector.SelectNextSpeakerAsync(roles, history, ct)` 接口仅接收角色列表和历史消息，缺少以下关键信息：

- 哪些角色被 @ 了（需要解析消息内容中的 @mention）
- 角色的参与模式（默认参与 vs 仅被 @ 时参与）
- 当前发言队列（@ 链式调用的上下文）

## 设计目标

1. 角色可配置参与模式：`AlwaysParticipate`（默认参与） vs `MentionOnly`（仅被 @ 时发言）
2. 支持 @mention 解析，从消息内容中提取被提及的角色
3. 发言队列支持 @ 链式调用：A @ B → B 发言 → B @ C → C 发言 → ...
4. 人类插话后：被 @ 的角色回复一次后暂停
5. 保持 `ISpeakerSelector` 可插拔，`RoundRobinSpeakerSelector` 作为默认实现
6. 向后兼容：未配置参与模式的角色默认 `AlwaysParticipate`

## 核心设计

### 1. 角色参与模式（`ChatRoomRoleDefinition` 新增属性）

在 `ChatRoomRoleDefinition` 中新增枚举和属性：

```csharp
/// <summary>
/// 角色在聊天室中的参与模式。
/// </summary>
public enum ChatRoomParticipationMode
{
    /// <summary>
    /// 默认参与所有对话。在自动循环中正常轮流发言。
    /// </summary>
    AlwaysParticipate,

    /// <summary>
    /// 仅在被 @ 时才发言。不会出现在自动循环中，
    /// 只有当其他角色或人类在消息中明确 @ 了该角色时才会被选中发言。
    /// </summary>
    MentionOnly,
}
```

```csharp
// ChatRoomRoleDefinition 新增属性
public ChatRoomParticipationMode ParticipationMode { get; set; } = ChatRoomParticipationMode.AlwaysParticipate;
```

### 2. @Mention 解析

在 `ChatRoomMessage` 中新增 `MentionedRoleIds` 属性，由 `ChatRoomManager` 在追加消息时解析填充：

```csharp
// ChatRoomMessage 新增属性
/// <summary>
/// 本条消息中 @ 提及的角色 RoleId 列表。
/// 由 ChatRoomManager 在追加消息时解析填充。
/// </summary>
public IReadOnlyList<string> MentionedRoleIds { get; init; } = Array.Empty<string>();
```

解析规则：
- 匹配 `@RoleName` 或 `@[RoleName]` 格式
- 在 `ChatRoomManager` 中根据 `Roles` 列表的 `RoleName` 反查 `RoleId`
- 人类消息和 LLM 消息都解析

解析器独立为一个工具类 `MentionParser`，便于测试和复用：

```csharp
internal static class MentionParser
{
    /// <summary>
    /// 从消息内容中解析被 @ 的角色 ID 列表。
    /// </summary>
    /// <param name="content">消息文本内容。</param>
    /// <param name="roles">当前聊天室中的角色列表，用于 RoleName → RoleId 映射。</param>
    /// <returns>被提及的角色 RoleId 列表（去重，保留首次出现顺序）。</returns>
    public static IReadOnlyList<string> ParseMentions(
        string content,
        IReadOnlyList<ChatRoomRole> roles);
}
```

### 3. 架构决策：@ 队列在 Selector 内部

**@ 队列（`_pendingMentionQueue`）是 `ISpeakerSelector` 实现的内部状态，不由 `ChatRoomManager` 管理。** 原因：

- Selector 是"谁下一个发言"的唯一权威。如果 @ 队列在 Manager 中，Manager 需要知道"什么时候队列为空、什么时候降级到正常轮流"，这导致 Manager 和 Selector 都在做选择决策，职责分裂。
- Selector 已经有内部可变状态（`_currentIndex`、`_currentRound`），`_pendingMentionQueue` 是同样性质的状态。
- `ISpeakerSelector` 接口签名无需修改：`SelectNextSpeakerAsync(roles, history, ct)` 已经包含了所有需要的信息——`history[^1].MentionedRoleIds` 有 @ 目标，`roles[i].Definition.ParticipationMode` 有参与模式。

**关键推论**：`StartAutoLoopAsync` 可以简化为纯粹的调用循环，不再包含 `isRespondingToHuman` 判断或任何发言选择逻辑。Selector 返回 `null` 就停，返回角色就发言。

### 4. ISpeakerSelector 接口（不变）

接口签名保持不变，保持可插拔性：

```csharp
public interface ISpeakerSelector
{
    /// <summary>
    /// 根据当前对话历史决定下一个发言的角色。
    /// 返回 null 表示对话自然结束（或应暂停等待下一次触发）。
    /// </summary>
    Task<ChatRoomRole?> SelectNextSpeakerAsync(
        IReadOnlyList<ChatRoomRole> roles,
        IReadOnlyList<ChatRoomMessage> history,
        CancellationToken cancellationToken = default);
}
```

### 5. ChatRoomManager.StartAutoLoopAsync 简化

移除所有发言选择逻辑，变为纯粹循环：

```text
StartAutoLoopAsync(ct):
  while (未停止):
    nextSpeaker = await SpeakerSelector.SelectNextSpeakerAsync(Roles, Session.Messages, ct)
    if nextSpeaker == null → break

    message = await StepAsync(nextSpeaker)
    解析 message 中的 @mention，填充 MentionedRoleIds
    AppendMessage(message)

    // 没有 isRespondingToHuman 判断！
    // 没有 pendingMentionQueue 管理！
    // 所有决策都在 Selector 内部完成。
```

### 6. RoundRobinSpeakerSelector 完整改造

#### 6.1 内部状态

```csharp
public sealed class RoundRobinSpeakerSelector : ISpeakerSelector
{
    private int _currentIndex = -1;
    private int _currentRound;
    private readonly Queue<string> _pendingMentionQueue = new();  // 新增

    public int? MaxRounds { get; init; }
    public int CurrentRound => _currentRound;
}
```

#### 6.2 过滤规则

自动循环只包含 `AlwaysParticipate` 的非人类角色：

```csharp
var autoRoles = roles.Where(r =>
    !r.Definition.IsHuman &&
    r.Definition.ParticipationMode == ChatRoomParticipationMode.AlwaysParticipate
).ToList();
```

注意：`MentionOnly` 角色不在 `autoRoles` 中，它们只能通过 @ 队列被选中。

#### 6.3 SelectNextSpeakerAsync 完整逻辑

**核心原则：队列优先。** 只要 `_pendingMentionQueue` 非空，就直接出队。只有当队列为空时，才检查 `history[^1]` 是否有新的 @ 需要入队。

这样确保：人类 `@A @B` → A 和 B 依次入队 → A 发言后 `history[^1]` 变成 A 的消息，但队列中还有 B → 直接从队列出队 B，完全不看 `history[^1]`。避免了 `history[^1]` 变化导致的信息丢失。

```text
SelectNextSpeakerAsync(roles, history, ct):
  1. 构建 autoRoles = AlwaysParticipate 的非人类角色
  2. 如果 autoRoles 为空 → return null

  3. // === 队列优先：只要队列非空，直接出队 ===
     如果 _pendingMentionQueue.Count > 0：
       roleId = _pendingMentionQueue.Dequeue()
       在所有 roles（含 MentionOnly）中查找匹配角色
       找到 → return 该角色
       未找到 → 继续（该角色可能已被移除）

  4. // === 队列为空，检查 history[^1] 触发源 ===
     如果 history.Count == 0：
       → 回到正常轮流（步骤 5）

     如果 history[^1].IsHumanMessage：
       // 人类消息 → 无条件标记"正在回应人类"
       _humanInterjectionPending = true
       入队 history[^1].MentionedRoleIds（去重）
       如果入队后队列非空 → return Dequeue()
       // 队列空（人类没 @ 任何人）→ 回到正常轮流

     // history[^1] 是 LLM 消息
     如果 _humanInterjectionPending：
       // 上一轮是回应人类，且队列已空 → 暂停
       _humanInterjectionPending = false
       return null

     // 非人类触发的自动循环中，LLM 可能 @ 了别人
     入队 history[^1].MentionedRoleIds（去重）
     如果入队后队列非空 → return Dequeue()
     // → 回到正常轮流

  5. // === 正常轮流 ===
     _currentIndex = (_currentIndex + 1) % autoRoles.Count
     如果 _currentIndex == 0 → _currentRound++
     如果 MaxRounds 超限 → return null
     return autoRoles[_currentIndex]
```

**关键点**：

- **步骤 3 是铁律**：队列非空 → 直接出队，不读 `history[^1]`。这保证了 `@A @B` 入队后，A 发言完毕，B 仍然在队列中等待。
- **`_humanInterjectionPending` 无条件设置**：只要 `history[^1]` 是人类消息就设为 true。后续循环中即使 `history[^1]` 变成了 LLM 消息，标记仍在。
- **暂停发生在队列空时**：`_humanInterjectionPending && 队列空` → `return null`。确保所有被 @ 的角色都发言完毕后才暂停。
- **LLM @ 链不暂停**：`_humanInterjectionPending` 只在人类消息时设置，LLM 间 @ 链式调用完成后回到正常轮流。

#### 6.4 Reset 方法

```csharp
public void Reset()
{
    _currentIndex = -1;
    _currentRound = 0;
    _pendingMentionQueue.Clear();
    _humanInterjectionPending = false;
}
```

### 7. 完整场景演练

#### 场景 1：人类 @ 单个 MentionOnly 角色

```
角色配置：
  - 助手 (AlwaysParticipate)
  - 代码专家 (MentionOnly)

对话流程：
  人类: "@代码专家 帮我看看这段代码"
    → MentionedRoleIds: [代码专家]

  SelectNextSpeakerAsync 第 1 次调用：
    → history[^1].IsHumanMessage == true
    → 入队 [代码专家]，队列: [代码专家]
    → Dequeue → 返回 代码专家

  代码专家 发言: "这段代码..."
    → MentionedRoleIds: []（没有 @ 别人）

  SelectNextSpeakerAsync 第 2 次调用：
    → history[^1] 是 LLM 消息，MentionedRoleIds 为空
    → 队列为空
    → _humanInterjectionPending == true → return null（暂停！）

  → 循环 break，等待人类下一次触发
```

#### 场景 2：人类不 @ 任何人

```
角色配置：
  - 助手 (AlwaysParticipate)
  - 代码专家 (MentionOnly)

对话流程：
  人类: "你好"
    → MentionedRoleIds: []

  SelectNextSpeakerAsync 第 1 次调用：
    → history[^1].IsHumanMessage == true
    → MentionedRoleIds 为空，不入队
    → _humanInterjectionPending = true
    → 进入正常轮流：_currentIndex 0 → 返回 助手

  助手 发言: "你好！有什么可以帮你的？"
    → MentionedRoleIds: []

  SelectNextSpeakerAsync 第 2 次调用：
    → history[^1] 是 LLM 消息，MentionedRoleIds 为空
    → 队列为空
    → _humanInterjectionPending == true → return null（暂停！）

  → 循环 break
```

#### 场景 3：LLM @ 链式调用（自动循环中）

```
角色配置：
  - 助手A (AlwaysParticipate)
  - 助手B (AlwaysParticipate)
  - 代码专家 (MentionOnly)

对话流程（自动循环已启动，非人类触发）：
  助手A: "我觉得需要 @代码专家 来看看"
    → MentionedRoleIds: [代码专家]

  SelectNextSpeakerAsync：
    → history[^1] 是 LLM 消息，MentionedRoleIds = [代码专家]
    → 入队 [代码专家]，队列: [代码专家]
    → Dequeue → 返回 代码专家

  代码专家: "这段代码有问题，@助手B 你怎么看？"
    → MentionedRoleIds: [助手B]

  SelectNextSpeakerAsync：
    → history[^1] 是 LLM 消息，MentionedRoleIds = [助手B]
    → 入队 [助手B]，队列: [助手B]
    → Dequeue → 返回 助手B

  助手B: "确实有问题，建议重构"
    → MentionedRoleIds: []

  SelectNextSpeakerAsync：
    → history[^1] 是 LLM 消息，MentionedRoleIds 为空
    → 队列为空
    → _humanInterjectionPending == false（不是回应人类）
    → 正常轮流：返回 助手A（下一轮）
    → ...
```

#### 场景 4：人类 @ 多个角色

```
角色配置：
  - 代码专家 (MentionOnly)
  - 安全专家 (MentionOnly)

对话流程：
  人类: "@代码专家 @安全专家 帮我审查这段代码"
    → MentionedRoleIds: [代码专家, 安全专家]

  SelectNextSpeakerAsync 第 1 次：
    → 队列为空，history[^1].IsHumanMessage == true
    → _humanInterjectionPending = true
    → 入队 [代码专家, 安全专家]，队列: [代码专家, 安全专家]
    → Dequeue → 返回 代码专家

  代码专家: "代码逻辑没问题，@安全专家 你看看安全性"
    → MentionedRoleIds: [安全专家]

  SelectNextSpeakerAsync 第 2 次：
    → 队列非空！直接出队，不看 history[^1]
    → Dequeue → 返回 安全专家
    （注意：history[^1] 是代码专家的消息，但队列优先保证 安全专家 不会被跳过）

  安全专家: "没有发现安全漏洞"
    → MentionedRoleIds: []

  SelectNextSpeakerAsync 第 3 次：
    → 队列为空
    → history[^1] 是 LLM 消息（安全专家），_humanInterjectionPending == true
    → return null（暂停！）
```

#### 场景 5：LLM 在自动循环中 @ 多个 MentionOnly 角色

```
角色配置：
  - 助手A (AlwaysParticipate)
  - 代码专家 (MentionOnly)
  - 安全专家 (MentionOnly)

对话流程（自动循环中）：
  助手A: "需要 @代码专家 和 @安全专家 一起审查"
    → MentionedRoleIds: [代码专家, 安全专家]

  SelectNextSpeakerAsync 第 1 次（助手A 发言后）：
    → 队列为空，history[^1] 是 LLM 消息（助手A），MentionedRoleIds = [代码专家, 安全专家]
    → _humanInterjectionPending == false
    → 入队 [代码专家, 安全专家]，队列: [代码专家, 安全专家]
    → Dequeue → 返回 代码专家

  代码专家: "代码没问题"
    → MentionedRoleIds: []

  SelectNextSpeakerAsync 第 2 次：
    → 队列非空！直接出队
    → Dequeue → 返回 安全专家

  安全专家: "安全也没问题"
    → MentionedRoleIds: []

  SelectNextSpeakerAsync 第 3 次：
    → 队列为空
    → history[^1] 是 LLM 消息，MentionedRoleIds 为空
    → _humanInterjectionPending == false
    → 正常轮流：返回 助手A（继续下一轮）
```

## 需要修改的文件

### AgentLib.ChatRoom 项目

| 文件 | 改动 |
|------|------|
| `Model/ChatRoomRoleDefinition.cs` | 新增 `ChatRoomParticipationMode` 枚举 + `ParticipationMode` 属性 |
| `Model/ChatRoomMessage.cs` | 新增 `MentionedRoleIds` 属性 |
| `ChatRoomManager.cs` | 简化 `StartAutoLoopAsync`：移除 `isRespondingToHuman` 和 @ 队列逻辑；追加消息前调用 `MentionParser` 填充 `MentionedRoleIds` |
| `SpeakerSelectors/RoundRobinSpeakerSelector.cs` | 完整改造：新增 `_pendingMentionQueue`、`_humanInterjectionPending`；过滤 `MentionOnly` 角色；@ 队列处理 + 暂停逻辑 |
| `ChatRoomPersistence.cs` | 序列化/反序列化 `ParticipationMode` 和 `MentionedRoleIds` |
| **新增** `MentionParser.cs` | @mention 解析工具类 |

### AgentLib.ChatRoom.Tests 项目

| 文件 | 改动 |
|------|------|
| `SpeakerSelectors/RoundRobinSpeakerSelectorTests.cs` | 全面更新：新增 MentionOnly 过滤、@ 队列、人类插话暂停等测试 |
| **新增** `MentionParserTests.cs` | @mention 解析测试 |

### ChatRoomAvaloniaDemo 项目

| 文件 | 改动 |
|------|------|
| `Models/ModelProviderConfig.cs` 或相关配置模型 | 新增 `ParticipationMode` 的 UI 绑定 |
| `Views/SettingsView.axaml` | 角色配置界面增加参与模式选择 |
| `ViewModels/SettingsViewModel.cs` | 对应 ViewModel 绑定 |

## 实施步骤

1. `ChatRoomRoleDefinition` 新增 `ChatRoomParticipationMode` 枚举和属性
2. `ChatRoomMessage` 新增 `MentionedRoleIds` 属性
3. 新增 `MentionParser` 工具类
4. 改造 `RoundRobinSpeakerSelector`：过滤 `MentionOnly` 角色
5. 改造 `ChatRoomManager.StartAutoLoopAsync`：@ 队列 + Mention 解析 + 暂停逻辑
6. 更新 `ChatRoomPersistence` 序列化
7. 编写/更新单元测试
8. 更新 Demo UI（角色配置界面）

## 开放问题

1. **@ 格式**：使用 `@RoleName` 还是 `@[RoleName]`？建议 `@RoleName`（空格分隔），简单直观。如果 RoleName 含空格则用 `@[Role Name]`。
2. **重复 @ 去重**：同一角色在同一条消息中被多次 @ 时，只入队一次（保留首次出现的位置）。
3. **@ 自己**：角色 @ 自己应忽略，不入队。
4. **MentionOnly 角色被 @ 后是否也加入自动循环**：不加入。MentionOnly 角色只在被 @ 时发言一次，之后回到"沉默"状态。
5. **MentionedRoleIds 是否需要持久化**：需要。恢复历史会话时，已解析的 @ 信息应保留，避免重新解析。
6. **多条消息的 @ 累积**：如果人类连续发送多条消息（每条都 @ 不同角色），`StartAutoLoopAsync` 在每次循环开始时只检查 `history[^1]`（最后一条），因此每次人类插话后只会处理最新一条消息中的 @ 列表。这符合"一次插话一次响应"的预期。
7. **@ 角色名匹配**：`MentionParser` 按 `RoleName` 精确匹配。如果两个角色同名，按 `RoleId` 去重时保留先出现的。建议 UI 层校验角色名唯一。
