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

## 修复方案（ISpeakerSelector 接口扩展 + OnSpeakerResult 反馈）

### 方案 3（本次实施）：ISpeakerSelector 接口扩展

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

### RoundRobinSpeakerSelector 改造

移除间接推断机制（`_attemptedMentionRoleIds`），改为通过 `OnSpeakerResult` 直接管理状态：

**新增字段：**
- `_spokenRoleIdsInCurrentTurn: HashSet<string>` — 当前轮次中已成功发言的角色 ID 集合。`OnSpeakerResult(role, success: true)` 时加入。正常轮流时跳过此集合中的角色；所有可发言角色都在此集合中时返回 `null`（自然暂停）。
- `_failedMentionRoleIds: HashSet<string>` — 从 @mention 队列出队但发言失败的角色 ID 集合。`OnSpeakerResult(role, success: false)` 时加入。入队和出队时跳过此集合中的角色，防止死循环。

**清空时机：**
- `_spokenRoleIdsInCurrentTurn` 和 `_failedMentionRoleIds` 仅在 `history[^1]` 变为人类消息（新一轮开始）时清空。LLM 消息不会触发清空，确保同一轮内已发言角色不被重复选中。
- `Reset()` 中清空所有状态。

### ChatRoomManager 安全网

在 `StartAutoLoopAsync` 中：
1. `StepAsync` 完成后调用 `SpeakerSelector.OnSpeakerResult(role, success)` 反馈结果。
2. 人类角色跳过 `StepAsync`（不调 LLM，不计数空回复）。
3. 安全网阈值改为可发言的非人类角色数量：`Roles.Count(r => !r.Definition.IsHuman && r.Definition.ParticipationMode == AlwaysParticipate)`，至少为 1。

## 正确行为定义

1. **被 @ 的角色发言失败（`StepAsync` 返回 `null`）时**：
   - 通过 `OnSpeakerResult(role, false)` 通知 Selector
   - Selector 将该角色加入 `_failedMentionRoleIds`，后续不再重复选中
   - 不终止循环，继续选择下一个发言者

2. **人类插话后的行为**：
   - 人类消息清空 `_spokenRoleIdsInCurrentTurn` 和 `_failedMentionRoleIds`，开始新一轮
   - 各 AlwaysParticipate 角色各发言一次后，所有角色都在 `_spokenRoleIdsInCurrentTurn` 中，Selector 返回 `null`，循环自然暂停
   - **不再无尽发言**

3. **@ 链式调用**：
   - A @ B → B 从 @mention 队列出队发言 → B @ C → C 从队列出队发言 → ...
   - 被 @ 的角色通过队列优先机制被选中，不进入正常轮流
   - 队列空后回到正常轮流

## 测试覆盖

### Selector 单元测试（`RoundRobinSpeakerSelectorTests.cs`）

| 测试 | 验证点 |
|------|--------|
| `SelectNextSpeakerAsync_MentionedRoleHasNoReply_DoesNotReSelectSameRole` | `OnSpeakerResult(false)` 后不重复返回同一 @mention 角色 |
| `SelectNextSpeakerAsync_MentionedRoleReplies_RetryAllowed` | 发言成功后新消息中再次 @ 可以重新选中 |
| `SelectNextSpeakerAsync_LlmMentionsRole_RoleHasNoReply_FallsToNormalCycle` | LLM @ 的角色无回复时跳过并回到正常轮流 |
| `OnSpeakerResult_Success_AddsToSpokenSet` | 成功发言后角色加入已发言集合，不再被选中 |
| `OnSpeakerResult_Success_AllRolesSpoke_ReturnsNull` | 所有角色发言后返回 null |
| `OnSpeakerResult_Failure_PreventsReSelection` | 发言失败后不再被选中 |
| `OnSpeakerResult_HumanMessageClearsSpokenSet` | 人类插话后清空已发言集合，开始新一轮 |

### Manager 集成测试（`ChatRoomManagerIntegrationTests.cs`）

使用 `FakeChatClient` + `FakeLanguageModelProvider` 模拟完整 `StartAutoLoopAsync` 流程。

| 测试 | 验证点 |
|------|--------|
| `StartAutoLoopAsync_MentionedRoleReturnsEmpty_LoopTerminatesWithoutDeadLoop` | @mention 角色返回空内容时循环在有限步骤内终止 |
| `StartAutoLoopAsync_MentionedRoleReturnsEmpty_ContinuesToNextSpeaker` | @mention 角色返回空后跳过并继续选下一个发言者 |
| `StartAutoLoopAsync_NormalFlow_AllRolesSpeak` | 正常流程所有角色发言 |
| `StartAutoLoopAsync_MentionChain_AllMentionedRolesSpeak` | @ 链式调用 A→B→C 全部发言 |
| `StartAutoLoopAsync_HumanInterject_AllRolesSpeakOnceThenStop` | 人类插话后各角色各发言一次然后循环暂停 |
| `StartAutoLoopAsync_AllSpeakableRolesReturnEmpty_TerminatesQuickly` | 安全网阈值基于可发言角色数，不死循环 |

## 修改文件清单

| 文件 | 改动 |
|------|------|
| `AgentLib.ChatRoom/ISpeakerSelector.cs` | 新增 `OnSpeakerResult(ChatRoomRole, bool)` 方法（破坏性变更） |
| `AgentLib.ChatRoom/SpeakerSelectors/RoundRobinSpeakerSelector.cs` | 移除 `_attemptedMentionRoleIds`；新增 `_spokenRoleIdsInCurrentTurn`/`_failedMentionRoleIds`；实现 `OnSpeakerResult`；正常轮流跳过已发言角色；清空逻辑改为仅在人类消息时触发 |
| `AgentLib.ChatRoom/ChatRoomManager.cs` | `StartAutoLoopAsync` 调用 `OnSpeakerResult`；安全网阈值改为可发言非人类角色数；人类角色跳过 `StepAsync` |
| `AgentLib.ChatRoom.Tests/SpeakerSelectors/RoundRobinSpeakerSelectorTests.cs` | 死循环测试改用 `OnSpeakerResult` 反馈模式；新增轮次暂停、反馈测试 |
| `AgentLib.ChatRoom.Tests/ChatRoomManagerIntegrationTests.cs` | 新增人类插话暂停行为测试和安全网阈值测试 |
