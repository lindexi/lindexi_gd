# ChatRoom 管理者角色发言设计

## 背景

当前聊天室的自动循环在所有可发言角色都发言完毕且 @ 队列为空时，`RoundRobinSpeakerSelector.SelectNextSpeakerAsync` 返回 `null`，`ChatRoomManager.StartAutoLoopAsync` 随即 `break` 退出循环，进入等待状态。只有用户再次通过 `HumanInterjectAsync` 插话才能重新触发对话。

这导致一个问题：**当用户发言 → 助手承接 → 被 @ 的角色发言 → 链式 @ 结束 → 循环终止，缺少一个"收尾"角色。** 理想行为是：所有角色发言结束后，由"管理者"角色进行最后一次发言，总结或决定下一步。如果管理者在发言中 @ 了其他角色，则被 @ 的角色继续发言；只有管理者发言后没有 @ 任何角色，才真正结束循环、等待用户。

## 设计目标

1. 引入"管理者"角色——当所有角色发言结束且 @ 队列为空时，由管理者进行发言
2. 管理者发言后如果 @ 了其他角色，继续由被 @ 的角色发言（链式继续）
3. 管理者发言后如果没有 @ 任何角色，才真正结束循环，等待用户下一次插话
4. 管理者不参与正常轮流（否则会每个角色发言后都插入），只在"所有角色发言结束"时触发
5. 管理者可以多次发言（任务没做完时继续循环），但**不能连续发言**——管理者发言后必须有其他角色发言，才能再次触发管理者

## 当前发言规则分析

### 角色参与模式

| 模式 | 行为 |
|------|------|
| `AlwaysParticipate` | 在自动循环中正常轮流发言 |
| `MentionOnly` | 仅在被 @ 时才发言，不参与正常轮流 |
| `IsHuman = true` | 人类角色，不通过 `StepAsync` 发言，仅通过 `HumanInterjectAsync` 插话 |

### 选择器决策流程（RoundRobinSpeakerSelector）

```
SelectNextSpeakerAsync(roles, history, ct):
  1. 检测 history[^1] 是否变化
     → 如果是人类消息，清空 _spokenRoleIdsInCurrentTurn 和 _failedMentionRoleIds

  2. 队列优先：TryDequeueMention()
     → _pendingMentionQueue 非空时直接出队

  3. 检查 history[^1] 触发源
     → 人类消息：重置 _currentIndex = -1
     → 非系统消息：EnqueueMentions(history[^1].MentionedRoleIds)
     → 再次 TryDequeueMention()

  4. 正常轮流
     → autoRoles = AlwaysParticipate 且非人类角色
     → 跳过 _spokenRoleIdsInCurrentTurn 中的角色
     → 所有可发言角色都已发言 → return null（自然暂停）
```

### 关键缺陷

当 `SelectNextSpeakerAsync` 返回 `null` 时，`StartAutoLoopAsync` 直接 `break`。没有任何机制让"管理者"在此时进行发言。

## 核心设计

### 1. 管理者角色标识

在 `ChatRoomRoleDefinition` 中新增属性，标识该角色为"管理者"：

```csharp
/// <summary>
/// 是否为管理者角色。当所有可发言角色都发言完毕且 @ 队列为空时，
/// 由管理者进行发言。管理者发言后如果 @ 了其他角色则继续链式对话，
/// 否则真正结束循环。
/// 支持两种配置方式：
/// - MentionOnly + IsManagerRole：管理者不参与正常轮流，仅在被 @ 或所有人发言完毕后兜底
/// - AlwaysParticipate + IsManagerRole：管理者正常参与轮流，同时在所有非管理者角色发言完毕后兜底
///   当 autoRoles 中仅含管理者角色时（如单助手场景），不会触发兜底，避免重复发言
/// </summary>
public bool IsManagerRole { get; set; }
```

**设计要点：**

- `IsManagerRole = true` 的角色支持两种配置方式：
  - **`MentionOnly` + `IsManagerRole`**：管理者不参与正常轮流，仅在被 @ 或所有人发言完毕后兜底
  - **`AlwaysParticipate` + `IsManagerRole`**：管理者正常参与轮流发言，同时在所有非管理者角色发言完毕后兜底。当 `autoRoles` 中仅含管理者角色时（如单助手场景），不会触发兜底，避免重复发言
- 管理者角色建议配置为 `ParticipationMode = MentionOnly` + `IsManagerRole = true`
  - `MentionOnly` 确保不在 `autoRoles` 中、不被正常轮流选中
  - `IsManagerRole` 确保在管理者检查时被触发
  - 管理者仍然可以通过被 @ 来发言（`MentionOnly` 的正常行为）
- 一个聊天室中应只有一个 `IsManagerRole = true` 的角色（多选时取第一个）
- 管理者角色仍需注册模型提供商、配置系统提示词等，与普通角色一致

### 2. 选择器改造

#### 2.1 新增内部状态

```csharp
/// <summary>
/// 管理者是否刚发言过且中间没有其他角色发言。
/// 管理者通过管理者机制发言后设为 true，防止管理者连续发言（无其他角色介入时重复触发）。
/// 任何非管理者角色发言成功后设为 false，使管理者可以在下一轮"所有角色发言完毕"时再次触发。
/// 当 history[^1] 变为人类消息（新一轮开始）时清空。
/// </summary>
private bool _managerJustSpoke;

/// <summary>
/// 标记上一次选择是否为管理者触发。
/// 用于 OnSpeakerResult 中区分"管理者触发"和"@ 触发"的管理者发言。
/// </summary>
private bool _isManagerTrigger;
```

#### 2.2 决策流程变更

在正常轮流的"所有可发言角色都已发言 → return null"之前，插入管理者检查：

```
SelectNextSpeakerAsync(roles, history, ct):
  步骤 1~3：（不变——队列优先 + history[^1] 检查 + @ 入队出队）

  步骤 4：正常轮流
    autoRoles = AlwaysParticipate 且非人类角色
    availableRoles = autoRoles - _spokenRoleIdsInCurrentTurn

    如果 availableRoles.Count > 0：
      _isManagerTrigger = false
      → 正常轮流返回下一个角色

    // 所有 AlwaysParticipate 角色都已发言
    // === 新增：管理者检查 ===
    // 仅当 autoRoles 为空（无 AlwaysParticipate 角色）或 autoRoles 中存在非 IsManagerRole 角色时才触发管理者兜底。
    // 如果 autoRoles 全是 IsManagerRole 角色（如单助手场景），管理者已通过正常轮流发言，不再兜底。
    如果 _managerJustSpoke == false 且 (autoRoles 为空 或 autoRoles 中存在非 IsManagerRole 角色)：
      查找 roles 中 IsManagerRole == true 的角色
      如果找到：
        _isManagerTrigger = true
        → 返回该角色
      如果没找到：
        _isManagerTrigger = false
        → return null（无管理者配置，保持原有行为）

    // 管理者刚发言且中间无其他角色发言，不再触发
    _isManagerTrigger = false
    return null
```

#### 2.3 OnSpeakerResult 改造

```csharp
public void OnSpeakerResult(ChatRoomRole role, bool success)
{
    ArgumentNullException.ThrowIfNull(role);

    if (success)
    {
        _spokenRoleIdsInCurrentTurn.Add(role.Definition.RoleId);

        if (_isManagerTrigger && role.Definition.IsManagerRole)
        {
            // 管理者通过管理者机制发言，标记防止连续触发
            _managerJustSpoke = true;
        }
        else
        {
            // 非管理者角色发言（或管理者被 @ 触发发言），清除标记
            _managerJustSpoke = false;
        }
    }
    else
    {
        _failedMentionRoleIds.Add(role.Definition.RoleId);

        if (_isManagerTrigger && role.Definition.IsManagerRole)
        {
            // 管理者发言失败也标记，防止死循环
            _managerJustSpoke = true;
        }
    }

    _isManagerTrigger = false;
}
```

**为什么需要区分管理者触发和 @ 触发？**

如果用户直接 @ 管理者让其发言，这是用户主动调用的，不应标记 `_managerJustSpoke`。这样后续其他角色发言完毕后，管理者仍然可以被管理者机制触发。通过 `_isManagerTrigger` 区分两种触发路径。

#### 2.4 人类消息清空逻辑

在检测到 `history[^1]` 是人类消息时，同时清空 `_managerJustSpoke`：

```csharp
if (history[^1].IsHumanMessage)
{
    _spokenRoleIdsInCurrentTurn.Clear();
    _failedMentionRoleIds.Clear();
    _managerJustSpoke = false;  // 新增
}
```

#### 2.5 Reset 方法

```csharp
public void Reset()
{
    _currentIndex = -1;
    _currentRound = 0;
    _pendingMentionQueue.Clear();
    _spokenRoleIdsInCurrentTurn.Clear();
    _failedMentionRoleIds.Clear();
    _managerJustSpoke = false;   // 新增
    _isManagerTrigger = false;   // 新增
    _lastSeenHistoryLastMessageId = null;
}
```

### 3. 管理者发言后不清空已发言集合

**决策：管理者发言后不清空 `_spokenRoleIdsInCurrentTurn`。**

理由：

- 管理者发言后如果 @ 了角色，被 @ 的角色通过 `EnqueueMentions` → `TryDequeueMention` 被选中发言
- `TryDequeueMention` 不检查 `_spokenRoleIdsInCurrentTurn`（只检查 `_failedMentionRoleIds`），因此已发言过的角色仍可被 @ 队列选中
- 等所有被 @ 的角色发言完毕且没有新的 @，循环自然结束（因为 `_managerJustSpoke == true`，不会再次触发管理者）

### 4. 防连续发言机制

**核心规则：管理者不能连续发言，中间必须有其他角色发言过。**

`_managerJustSpoke` 的状态转换：

| 事件 | `_managerJustSpoke` 变化 |
|------|--------------------------|
| 管理者通过管理者机制发言（成功或失败） | → `true` |
| 任何非管理者角色发言成功 | → `false` |
| 管理者通过 @ 被触发发言成功 | → `false`（因为是 @ 触发，不是管理者机制触发） |
| 人类消息（新一轮开始） | → `false` |

**防循环验证：**

| 场景 | 结果 |
|------|------|
| 管理者发言（无 @） | `_managerJustSpoke = true` → 下次 → `return null` ✓ |
| 管理者发言（@ expert）→ expert 发言（无 @） | expert 发言时 `_managerJustSpoke = false` → 下次 → 管理者可再次发言 ✓ |
| 管理者发言（@ expert）→ expert 发言失败 | expert 失败不改变 `_managerJustSpoke`（仍为 `true`）→ 下次 → `return null` ✓ |
| 管理者发言（@ A）→ A 发言（@ B）→ B 发言（无 @） | B 发言时 `_managerJustSpoke = false` → 管理者可再次发言 ✓ |
| 用户新消息 | `_managerJustSpoke = false` → 管理者可用 ✓ |

### 5. 防死循环安全网

`ChatRoomManager.StartAutoLoopAsync` 中的 `consecutiveEmptyReplies` 安全网需要将管理者也计入 `speakableRoleCount`：

```csharp
int speakableRoleCount = Math.Max(1, Roles.Count(r =>
    !r.Definition.IsHuman &&
    (r.Definition.ParticipationMode == ChatRoomParticipationMode.AlwaysParticipate ||
     r.Definition.IsManagerRole)));
```

### 6. 管理者角色的系统提示词建议

管理者的系统提示词应引导其做"总结+协调"的工作：

```
你是聊天室的管理者。你的职责是：
1. 当所有其他角色发言结束后，由你进行总结和收尾
2. 总结各角色的观点，给出综合结论
3. 如果需要某个角色进一步补充，使用 @【角色名】 指定该角色发言
4. 如果对话已经完整，不需要进一步讨论，直接给出最终总结即可
```

### 7. BuildChatRoomContext 配合

`BuildChatRoomContext` 中应体现管理者的存在和职责。当 `IsManagerRole == true` 时，角色列表中标注"（管理者）"：

```csharp
foreach (ChatRoomRole r in Roles)
{
    string roleInfo = $"- {r.Definition.RoleName}";
    if (r.Definition.IsHuman)
    {
        roleInfo += "（人类）";
    }
    else if (r.Definition.IsManagerRole)
    {
        roleInfo += "（管理者）";
    }
    else if (r.Definition.ParticipationMode == ChatRoomParticipationMode.MentionOnly)
    {
        roleInfo += "（仅被@时参与）";
    }
    sb.AppendLine(roleInfo);
}
```

## 完整场景演练

### 场景 1：基础流程——管理者总结

```
角色配置：
  - 管理者 (MentionOnly, IsManagerRole=true)
  - 代码审查员 (AlwaysParticipate)

对话流程：
  用户: "帮我审查这段代码"
    → history[^1] 是人类消息
    → 清空 _spokenRoleIdsInCurrentTurn、_failedMentionRoleIds、_managerJustSpoke

  SelectNextSpeakerAsync 第 1 次：
    → 人类消息，重置 _currentIndex = -1
    → 无 @，队列空
    → 正常轮流：autoRoles = [代码审查员]
    → 返回 代码审查员

  代码审查员 发言: "这段代码有以下问题..."
    → MentionedRoleIds: []
    → OnSpeakerResult(代码审查员, success=true)
    → _spokenRoleIdsInCurrentTurn = {代码审查员}
    → _managerJustSpoke = false（非管理者角色发言）

  SelectNextSpeakerAsync 第 2 次：
    → 队列空，检查 history[^1]：代码审查员消息，无 @
    → 正常轮流：availableRoles 为空
    → 管理者检查：_managerJustSpoke == false → 返回 管理者
    → _isManagerTrigger = true

  管理者 发言: "总结：代码审查员指出了以下问题。建议修复后再提交。"
    → MentionedRoleIds: []
    → OnSpeakerResult(管理者, success=true)
    → _isManagerTrigger == true → _managerJustSpoke = true

  SelectNextSpeakerAsync 第 3 次：
    → 队列空，无 @
    → 正常轮流：availableRoles 为空
    → 管理者检查：_managerJustSpoke == true → return null

  → 循环 break，等待用户下一次发言 ✓
```

### 场景 2：管理者 @ 其他角色，链式继续

```
角色配置：
  - 管理者 (MentionOnly, IsManagerRole=true)
  - 代码审查员 (AlwaysParticipate)
  - 安全专家 (MentionOnly)

对话流程：
  用户: "帮我审查这段代码"
    → 清空状态

  SelectNextSpeakerAsync 第 1 次：
    → 人类消息，无 @
    → 正常轮流 → 返回 代码审查员

  代码审查员 发言: "代码逻辑没问题，但需要安全审查。"
    → _spokenRoleIdsInCurrentTurn = {代码审查员}
    → _managerJustSpoke = false

  SelectNextSpeakerAsync 第 2 次：
    → 队列空，无 @
    → availableRoles 为空
    → 管理者检查：_managerJustSpoke == false → 返回 管理者

  管理者 发言: "代码审查员认为需要安全审查。@安全专家 请帮忙看看"
    → MentionedRoleIds: [安全专家]
    → _managerJustSpoke = true

  SelectNextSpeakerAsync 第 3 次：
    → 队列空，检查 history[^1]：管理者消息，MentionedRoleIds = [安全专家]
    → EnqueueMentions → 队列: [安全专家]
    → TryDequeueMention → 返回 安全专家

  安全专家 发言: "没有发现安全漏洞，代码是安全的。"
    → MentionedRoleIds: []
    → OnSpeakerResult(安全专家, success=true)
    → _managerJustSpoke = false（非管理者角色发言）

  SelectNextSpeakerAsync 第 4 次：
    → 队列空，无 @
    → availableRoles 为空
    → 管理者检查：_managerJustSpoke == false → 返回 管理者

  管理者 发言: "总结：代码审查和安全审查都通过了，可以提交。"
    → _managerJustSpoke = true

  SelectNextSpeakerAsync 第 5 次：
    → 管理者检查：_managerJustSpoke == true → return null

  → 循环 break ✓
```

### 场景 3：只有管理者存在（无 AlwaysParticipate 角色）

```
角色配置：
  - 管理者 (MentionOnly, IsManagerRole=true)

对话流程：
  用户: "你好"
    → 清空状态

  SelectNextSpeakerAsync 第 1 次：
    → 人类消息，无 @
    → autoRoles 为空 → availableRoles 为空
    → 管理者检查：_managerJustSpoke == false → 返回 管理者

  管理者 发言: "你好！有什么可以帮你的？"
    → _managerJustSpoke = true

  SelectNextSpeakerAsync 第 2 次：
    → 队列空，无 @
    → autoRoles 为空
    → 管理者检查：_managerJustSpoke == true → return null

  → 循环 break ✓
```

### 场景 4：用户 @ 管理者 + 其他角色

```
角色配置：
  - 管理者 (MentionOnly, IsManagerRole=true)
  - 代码审查员 (AlwaysParticipate)

对话流程：
  用户: "@管理者 安排代码审查"
    → MentionedRoleIds: [管理者]

  SelectNextSpeakerAsync 第 1 次：
    → 人类消息，入队 [管理者]
    → TryDequeueMention → 返回 管理者
    → _isManagerTrigger = false（队列出队，非管理者机制触发）

  管理者 发言: "好的，我来安排。"
    → OnSpeakerResult(管理者, success=true)
    → _isManagerTrigger == false → _managerJustSpoke = false
    → （@ 触发的管理者发言不标记 _managerJustSpoke）

  SelectNextSpeakerAsync 第 2 次：
    → 队列空，检查 history[^1]：管理者消息，无 @
    → 正常轮流 → 返回 代码审查员

  代码审查员 发言: "代码有以下问题..."
    → _managerJustSpoke = false

  SelectNextSpeakerAsync 第 3 次：
    → 队列空，无 @
    → availableRoles 为空
    → 管理者检查：_managerJustSpoke == false → 返回 管理者
    → _isManagerTrigger = true

  管理者 发言: "总结：代码审查员指出了问题，建议修复。"
    → _managerJustSpoke = true

  SelectNextSpeakerAsync 第 4 次：
    → 管理者检查：_managerJustSpoke == true → return null

  → 循环 break ✓
```

### 场景 5：管理者 @ 已发言过的角色

```
角色配置：
  - 管理者 (MentionOnly, IsManagerRole=true)
  - 代码审查员 (AlwaysParticipate)

对话流程：
  用户: "审查代码"
    → 清空状态

  SelectNextSpeakerAsync 第 1 次：
    → 人类消息，无 @
    → 正常轮流 → 返回 代码审查员

  代码审查员 发言: "代码有性能问题。"
    → _spokenRoleIdsInCurrentTurn = {代码审查员}
    → _managerJustSpoke = false

  SelectNextSpeakerAsync 第 2 次：
    → 队列空，无 @
    → availableRoles 为空
    → 管理者检查 → 返回 管理者

  管理者 发言: "@代码审查员 请具体说明性能问题的位置"
    → MentionedRoleIds: [代码审查员]
    → _managerJustSpoke = true

  SelectNextSpeakerAsync 第 3 次：
    → 队列空，检查 history[^1]：管理者消息，MentionedRoleIds = [代码审查员]
    → EnqueueMentions([代码审查员])
    → TryDequeueMention → 返回 代码审查员

  代码审查员 发言: "在第 42 行的循环中..."
    → OnSpeakerResult(代码审查员, success=true)
    → _managerJustSpoke = false（非管理者角色发言）

  SelectNextSpeakerAsync 第 4 次：
    → 队列空，无 @
    → availableRoles 为空
    → 管理者检查：_managerJustSpoke == false → 返回 管理者

  管理者 发言: "明确了，建议修复第 42 行的性能问题。"
    → _managerJustSpoke = true

  SelectNextSpeakerAsync 第 5 次：
    → 管理者检查：_managerJustSpoke == true → return null

  → 循环 break ✓
```

### 场景 6：管理者发言后被 @ 的角色发言失败

```
角色配置：
  - 管理者 (MentionOnly, IsManagerRole=true)
  - 代码审查员 (AlwaysParticipate)
  - 安全专家 (MentionOnly)

对话流程：
  用户: "审查代码"
    → 清空状态

  SelectNextSpeakerAsync 第 1 次：
    → 正常轮流 → 返回 代码审查员

  代码审查员 发言: "代码逻辑没问题。"
    → _managerJustSpoke = false

  SelectNextSpeakerAsync 第 2 次：
    → availableRoles 为空
    → 管理者检查 → 返回 管理者

  管理者 发言: "@安全专家 请检查安全性"
    → MentionedRoleIds: [安全专家]
    → _managerJustSpoke = true

  SelectNextSpeakerAsync 第 3 次：
    → EnqueueMentions → 队列: [安全专家]
    → TryDequeueMention → 返回 安全专家

  安全专家 发言失败（空回复）
    → OnSpeakerResult(安全专家, success=false)
    → _failedMentionRoleIds = {安全专家}
    → _managerJustSpoke 仍为 true（失败不改变标记）

  SelectNextSpeakerAsync 第 4 次：
    → 队列空，检查 history[^1]：安全专家的消息（但无新 @）
    → availableRoles 为空
    → 管理者检查：_managerJustSpoke == true → return null

  → 循环 break ✓（安全专家发言失败，管理者不会重复触发）
```

## 需要修改的文件

### AgentLib.ChatRoom 项目

| 文件 | 改动 |
|------|------|
| `Model/ChatRoomRoleDefinition.cs` | 新增 `IsManagerRole` 属性 |
| `SpeakerSelectors/RoundRobinSpeakerSelector.cs` | 新增 `_managerJustSpoke`、`_isManagerTrigger` 状态；管理者检查逻辑；`OnSpeakerResult` 区分管理者触发；`Reset` 清空新状态 |
| `ChatRoomManager.cs` | `StartAutoLoopAsync` 的 `speakableRoleCount` 计算包含 `IsManagerRole` 角色；`BuildChatRoomContext` 标注管理者角色 |
| `ChatRoomPersistence.cs` | 序列化/反序列化 `IsManagerRole` |

### AgentLib.ChatRoom.Tests 项目

| 文件 | 改动 |
|------|------|
| `SpeakerSelectors/RoundRobinSpeakerSelectorTests.cs` | 新增管理者角色相关测试 |
| `ChatRoomManagerIntegrationTests.cs` | 新增端到端集成测试 |

## 实施步骤

1. `ChatRoomRoleDefinition` 新增 `IsManagerRole` 属性
2. `RoundRobinSpeakerSelector` 新增 `_managerJustSpoke`、`_isManagerTrigger` 状态和管理者检查逻辑
3. `RoundRobinSpeakerSelector.OnSpeakerResult` 区分管理者触发
4. `RoundRobinSpeakerSelector.Reset` 清空新状态
5. `ChatRoomManager.StartAutoLoopAsync` 的 `speakableRoleCount` 计算包含管理者
6. `ChatRoomManager.BuildChatRoomContext` 标注管理者角色
7. `ChatRoomPersistence` 序列化 `IsManagerRole`
8. 编写单元测试
9. 编写集成测试
10. 更新 README 文档

## 开放问题

1. **多个 `IsManagerRole` 角色**：建议取第一个，或在使用时给出警告。UI 层应限制只能配置一个。
2. **管理者发言失败**：标记 `_managerJustSpoke = true` 防止死循环，同时触发 `OnRoleSpeakFailed` 事件通知 UI。
3. **管理者与 `MaxRounds` 的关系**：管理者发言不消耗 `MaxRounds`（因为它不在 `autoRoles` 中，不走正常轮流逻辑）。如果需要限制管理者发言次数，可以单独添加 `MaxManagerRounds` 属性。
4. **持久化兼容**：旧配置文件中没有 `IsManagerRole` 字段，反序列化时默认为 `false`，向后兼容。
5. **管理者角色建议配置**：`ParticipationMode = MentionOnly` + `IsManagerRole = true`。也支持 `AlwaysParticipate` + `IsManagerRole = true`：管理者正常参与轮流发言，同时在所有非管理者角色发言完毕后兜底。当 `autoRoles` 中仅含管理者角色时（如单助手场景），不会触发兜底，避免重复发言。
