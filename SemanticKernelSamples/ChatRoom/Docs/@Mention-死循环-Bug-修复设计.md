# @Mention 死循环 Bug 修复设计

## 背景

在 ChatRoom 多角色聊天室中，当助手在末次发言中 @ 了某个角色，该角色被选中发言后发现自己没有什么需要说的（LLM 返回空内容或 `BuildIncrementalUserText` 返回空），`StepAsync` 返回 `null`。此时对话历史未被更新，但 `SelectNextSpeakerAsync` 再次从 `history[^1]` 读取相同的 @mention，重新入队同一角色，形成无限循环。

### 复现路径

```
1. 助手发言："需要 @expert 来看看"  → history[^1] = 助手消息，MentionedRoleIds = [expert]
2. SelectNextSpeakerAsync → expert 从 @mention 队列出队
3. StepAsync(expert) → LLM 返回空内容 → StepAsync 返回 null
4. 对话历史未更新（history[^1] 仍是助手消息）
5. SelectNextSpeakerAsync → 再次从 history[^1] 读取 @expert → 重新入队 expert
6. 回到步骤 2 → 无限循环
```

## 深层设计问题

`ISpeakerSelector` 接口设计为"无状态选择"——传入 `roles` 和 `history`，返回下一个发言者。但 `RoundRobinSpeakerSelector` 内部维护有状态信息（`_currentIndex`、`_currentRound`、`_pendingMentionQueue`）。

这个"有状态的 Selector + 无状态接口"的矛盾导致关键问题：

- **`StartAutoLoopAsync` 和 `Selector` 之间的状态脱节**：`StepAsync` 返回 `null` 时，`Session.Messages` 没有更新（历史不变），但 Selector 内部已经"消费"了那个 @mention（从队列出队了）。下次 `SelectNextSpeakerAsync` 被调用时，Selector 会再次看到相同的 `history[^1]`，重新入队同一角色。

- **没有"发言失败/空回复"的反馈通道**：`ISpeakerSelector` 接口没有让 Manager 告诉 Selector"上一个选中的角色没有说话"。Selector 无法知道它选的角色是否真的发了言。

## 修复方案（双重防护）

### 方案 1：Selector 层 — 记录已尝试的 @mention 避免重复入队

在 `RoundRobinSpeakerSelector` 中新增 `_attemptedMentionRoleIds: HashSet<string>` 集合，记录本轮已经从 @mention 队列出队并返回过的角色 ID。

**核心逻辑：**

1. **入队时跳过已尝试角色**：`EnqueueMentions` 方法在入队前检查 `_attemptedMentionRoleIds`，跳过本轮已尝试过的角色。
2. **出队时记录已尝试角色**：`TryDequeueMention` 方法在成功出队并返回角色时，将角色 ID 加入 `_attemptedMentionRoleIds`。
3. **history[^1] 变化时清空记录**：通过 `_lastSeenHistoryLastMessageId` 检测 `history[^1]` 是否变化（有新消息追加），变化时清空 `_attemptedMentionRoleIds`，表示新的一轮 @ 可以重新尝试。
4. **`Reset()` 中清空**：重置时一并清空所有状态。

**关键文件**：`AgentLib.ChatRoom/SpeakerSelectors/RoundRobinSpeakerSelector.cs`

### 方案 2：ChatRoomManager 层 — 连续空回复检测（安全网）

在 `StartAutoLoopAsync` 循环中跟踪连续 `StepAsync` 返回 `null` 的次数。当连续空回复达到 `Roles.Count` 时 `break`，避免任何情况下的死循环。

这是安全网，防止 Selector 修复不完全或其他边界情况导致的死循环。

**关键文件**：`AgentLib.ChatRoom/ChatRoomManager.cs`

### 方案 3（深层，后续迭代）：ISpeakerSelector 接口扩展

为 `ISpeakerSelector` 增加反馈方法，让 Manager 告诉 Selector 上一个角色是否成功发言：

```csharp
public interface ISpeakerSelector
{
    Task<ChatRoomRole?> SelectNextSpeakerAsync(
        IReadOnlyList<ChatRoomRole> roles,
        IReadOnlyList<ChatRoomMessage> history,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 通知 Selector 上一个选中的角色的发言结果。
    /// </summary>
    /// <param name="role">上一个选中的角色。</param>
    /// <param name="success">是否成功发言（产生了有效内容）。</param>
    void OnSpeakerResult(ChatRoomRole role, bool success);
}
```

这样 Selector 可以精确知道角色是否成功发言，不需要通过 `history[^1]` 变化来推断。

**本次不实施此方案**，因为修改接口会影响所有 `ISpeakerSelector` 实现和 Mock。当前双重防护方案已经足够。记录于此供后续迭代参考。

## 正确行为定义

当被 @ 的角色被选中发言但 `StepAsync` 返回 `null`（无话可说）时：

- **不终止循环**：继续选择下一个发言者
- **不重复选同一角色**：避免死循环
- **跳过该角色，继续选择队列中下一个角色或回到正常轮流**

当 `history[^1]` 变化（有新消息追加）时，已尝试记录清空，新的 @ 可以重新尝试。

## 测试覆盖

### Selector 单元测试（`RoundRobinSpeakerSelectorTests.cs`）

| 测试 | 验证点 |
|------|--------|
| `SelectNextSpeakerAsync_MentionedRoleHasNoReply_DoesNotReSelectSameRole` | history[^1] 不变时不重复返回同一 @mention 角色 |
| `SelectNextSpeakerAsync_MentionedRoleReplies_RetryAllowed` | history[^1] 变化后可以重新尝试同一角色 |
| `SelectNextSpeakerAsync_LlmMentionsRole_RoleHasNoReply_FallsToNormalCycle` | LLM @ 的角色无回复时跳过并回到正常轮流 |

### Manager 集成测试（`ChatRoomManagerIntegrationTests.cs`）

使用 `FakeChatClient` + `FakeLanguageModelProvider` 模拟完整 `StartAutoLoopAsync` 流程。

| 测试 | 验证点 |
|------|--------|
| `StartAutoLoopAsync_MentionedRoleReturnsEmpty_LoopTerminatesWithoutDeadLoop` | @mention 角色返回空内容时循环在有限步骤内终止 |
| `StartAutoLoopAsync_MentionedRoleReturnsEmpty_ContinuesToNextSpeaker` | @mention 角色返回空后跳过并继续选下一个发言者 |
| `StartAutoLoopAsync_NormalFlow_AllRolesSpeak` | 正常流程所有角色发言 |
| `StartAutoLoopAsync_MentionChain_AllMentionedRolesSpeak` | @ 链式调用 A→B→C 全部发言 |

## 已知预存问题（非本次修复范围）

以下 2 个测试在本次修改前已失败，根因是 `StepAsync` 中 `BuildIncrementalUserText` 在 `Session.Messages` 为空时返回空字符串，导致 `StepAsync` 在调用 LLM 之前就返回 `null`：

- `StartAutoLoopAsync_StepAsyncReturnsMessage_AppendsMessageAndFiresEvent`
- `StepAsync_NonHumanRole_WhenSpeakThrows_FiresOnRoleSpeakFailedAndReturnsSystemMessage`

这些测试需要在 `Session` 中有消息才能走到 LLM 调用路径。后续应修复这些测试或调整 `StepAsync` 逻辑。

## 修改文件清单

| 文件 | 改动 |
|------|------|
| `AgentLib.ChatRoom/SpeakerSelectors/RoundRobinSpeakerSelector.cs` | 新增 `_attemptedMentionRoleIds`、`_lastSeenHistoryLastMessageId`；`EnqueueMentions` 跳过已尝试角色；`TryDequeueMention` 记录已尝试；`Reset()` 清空新字段 |
| `AgentLib.ChatRoom/ChatRoomManager.cs` | `StartAutoLoopAsync` 新增 `consecutiveEmptyReplies` 计数器，达到 `Roles.Count` 时 `break` |
| `AgentLib.ChatRoom.Tests/SpeakerSelectors/RoundRobinSpeakerSelectorTests.cs` | 新增 3 个死循环复现测试 |
| `AgentLib.ChatRoom.Tests/ChatRoomManagerIntegrationTests.cs` | 新增 4 个集成测试，使用 `FakeChatClient` 模拟完整流程 |
