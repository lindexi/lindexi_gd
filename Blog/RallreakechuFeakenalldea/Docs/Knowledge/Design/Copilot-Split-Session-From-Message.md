# 右键消息创建新会话

## 需求

在 CopilotSlideBar 聊天消息的右键菜单中增加"从当前消息创建新会话"功能。新会话保留从最早消息到被右击消息（含）之间的所有消息，形成独立会话。

## 已确认行为

| 问题 | 结论 |
|---|---|
| "最前面"的语义 | 最早的消息（即 `ChatMessages[0]`） |
| 原会话处理 | 仅复制消息，原会话保持不变 |
| 自动切换 | 新会话创建后自动切换到该新会话 |
| AgentSession 携带 | 新会话不携带 AgentSession，仅复制 UI 可见消息 |
| 消息复制深度 | 需要深拷贝，包括 CopilotChatMessage 及其 MessageItems |
| 适用范围 | 用户消息和助手消息的右键菜单都显示 |
| 边界情况 | 仅一条消息也正常创建新会话，无需特殊处理 |
| 预设消息 | 不过滤 IsPresetInfo，全部复制（预设消息本身已存在于会话中） |

## 消息范围示意

```
原会话: [M0] [M1] [M2] [M3] [M4] ← 右击 M2
新会话: [M0] [M1] [M2]          ← 从最早到 M2（含）
原会话: [M0] [M1] [M2] [M3] [M4] ← 保持不变
```

## 涉及文件

| 文件 | 作用 |
|---|---|
| `AvaloniaAgentLib/View/CopilotSlideBar.axaml` | 在 `UserMessageTemplate` 和 `AssistantMessageTemplate` 右键 ContextMenu 中加入新菜单项 |
| `AvaloniaAgentLib/View/CopilotSlideBar.axaml.cs` | 添加 `CreateSessionFromMessageMenuItem_OnClick` 事件处理方法 |
| `AvaloniaAgentLib/ViewModel/CopilotViewModel.cs` | 新增 `CreateSessionFromMessage` 方法 |
| `AgentLib/Model/CopilotChatMessage.cs` | 可能需要新增深拷贝方法 `Clone()` |
| `AgentLib/` CopilotChatManager | 复用 `CreateNewSession()` 和 `AddMessage()` |

## 实现方案

选型：**方案 A** — 在 ViewModel 层新增方法，View 只转发事件。符合 MVVM 分层。

### 核心逻辑：CopilotViewModel.CreateSessionFromMessage

```
输入: CopilotChatMessage fromMessage
流程:
  1. 在 SelectedSession.ChatMessages 中找到 fromMessage 的索引
  2. 取 [0..index] 范围内的消息（含 fromMessage）
  3. 调用 CreateNewSession() 创建空会话
  4. 对每条消息深拷贝后 AddMessage 进新会话
  5. 将新会话加入 ChatSessions
  6. 设置 SelectedSession = 新会话
```

### 深拷贝实现

`CopilotChatMessage` 新增 `Clone()` 方法：
- 复制 Role、CreatedTime、IsPresetInfo 等基础属性
- 遍历 MessageItems，每个 item 各自实现 Clone
- `CopilotChatTextItem`、`CopilotChatReasoningItem`、`CopilotChatToolItem`、`CopilotChatApprovalToolItem`、`CopilotChatSubAgentItem` 等都需要 Clone

需要先检查各 MessageItem 类型是否已有 Clone 方法。

### XAML 改动

在 `UserMessageTemplate` 和 `AssistantMessageTemplate` 的 ContextMenu 中各新增一个 MenuItem：

```xml
<MenuItem Header="从当前消息创建新会话" Click="CreateSessionFromMessageMenuItem_OnClick"
          CommandParameter="{Binding}" />
```

### Code-behind 改动

```csharp
private void CreateSessionFromMessageMenuItem_OnClick(object? sender, RoutedEventArgs e)
{
    if (sender is not MenuItem { CommandParameter: CopilotChatMessage message })
    {
        return;
    }
    ViewModel.CreateSessionFromMessage(message);
}
```

## 风险与设计考虑

- **深拷贝复杂度**：MessageItems 中 `CopilotChatSubAgentItem` 可能嵌套子 MessageItems，需要递归深拷贝。
- **会话标题**：新会话标题由 `CopilotChatSession.AddMessage` 自动触发 `TryUpdateTitle`，从第一条用户消息生成。
- **日志暂不处理**：右键拆分出的新会话不主动写日志（`CreateNewSession` 本身不触发日志写入，日志是在 `SendMessage` 时写入的）。拆分出的新会话没有 AgentSession，后续用户若在该会话中发送消息，日志链路会正常启动。
- **空会话复用检查**：此处不适用空会话复用逻辑。拆分操作始终创建新会话。

## 已确认的讨论要点

1. 用户消息和助手消息都显示此菜单项 ✓
2. 新会话保留消息之间的相对时间顺序（按原顺序 Copy） ✓
3. 仅一条消息也正常创建新会话，无需特殊处理 ✓
