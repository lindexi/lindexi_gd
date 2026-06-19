# AgentLib 流式增量通知改造计划

## 背景

`SlideML 流式输出实现方案` 要求 `SlideStreamingPipeline` 在 LLM 逐 token 推送过程中实时提取 XML 片段并合并渲染。方案中设计了 `ProcessIncrementalContent` 方法，通过记录 `_lastProcessedLength` 对 `CopilotChatMessage.Content` 全量文本做差集来获取增量。

但现有 `CopilotChatMessage` 存在以下问题，导致差集方案不可靠：

1. **`PropertyChanged` 不携带增量内容**：`AppendText` / `AppendReasoning` 追加文本后，只触发 `OnPropertyChanged("Content")`，通知中不包含本次追加的文本
2. **`ClearMessageItems` 干扰差集**：`CopilotChatManager.RunAsync` 在第一个 token 到达时调用 `assistantChatMessage.ClearMessageItems()`（`CopilotChatManager.cs` 第 491 行），导致 `Content` 从占位文本 `"..."` 突变为空串，`_lastProcessedLength` 与实际内容不匹配
3. **`MessageItems` 中间插入非文本项**：`AppendFunctionCall` / `AppendFunctionResult` 会在文本项之间插入工具项，`Content` 拼接结果不连续增长

因此需要在 `CopilotChatMessage` 上新增**增量追加事件**，让订阅者直接获取增量文本，无需差集计算。

---

## 决策

采用**方案 B**：在 `CopilotChatMessage` 的 `AppendText` / `AppendReasoning` 方法中新增增量事件，直接传递追加的文本。

- 不修改 `CopilotChatTextItem` / `CopilotChatReasoningItem` / `NotifyBase`
- 不改变现有 `PropertyChanged("Content")` 通知机制，UI 绑定不受影响
- 新增的事件是附加能力，不订阅的调用方完全无感知

---

## 涉及的关键文件

| 文件 | 角色 |
|------|------|
| `AgentLib/Model/CopilotChatMessage.cs` | 新增 `TextAppended` / `ReasoningAppended` 事件，在 `AppendText` / `AppendReasoning` 中触发 |

---

## 详细步骤

### 步骤 1：新增增量追加事件

- **文件**：`AgentLib/Model/CopilotChatMessage.cs`
- **内容**：

#### 1.1 新增事件声明

在 `CopilotChatMessage` 类中新增两个事件：

```csharp
/// <summary>
/// 文本内容追加时触发，携带本次追加的增量文本。
/// 仅在 <see cref="AppendText"/> 调用时触发，不包含全量替换场景。
/// </summary>
public event Action<string>? TextAppended;

/// <summary>
/// 推理内容追加时触发，携带本次追加的增量文本。
/// 仅在 <see cref="AppendReasoning"/> 调用时触发，不包含全量替换场景。
/// </summary>
public event Action<string>? ReasoningAppended;
```

#### 1.2 修改 AppendText 方法

在 `AppendText` 方法中，追加文本后触发 `TextAppended` 事件：

```csharp
public void AppendText(string text)
{
    if (string.IsNullOrEmpty(text))
    {
        return;
    }

    if (MessageItems.LastOrDefault() is CopilotChatTextItem lastTextItem)
    {
        lastTextItem.Text += text;
    }
    else
    {
        MessageItems.Add(new CopilotChatTextItem(text));
    }

    TextAppended?.Invoke(text);
}
```

#### 1.3 修改 AppendReasoning 方法

在 `AppendReasoning` 方法中，追加文本后触发 `ReasoningAppended` 事件：

```csharp
public void AppendReasoning(string text)
{
    if (string.IsNullOrEmpty(text))
    {
        return;
    }

    if (MessageItems.LastOrDefault() is CopilotChatReasoningItem lastReasoningItem)
    {
        lastReasoningItem.Text += text;
    }
    else
    {
        MessageItems.Add(new CopilotChatReasoningItem(text));
    }

    ReasoningAppended?.Invoke(text);
}
```

#### 1.4 清理事件订阅

在 `ClearMessageItems` 方法中不需要清理 `TextAppended` / `ReasoningAppended` 的事件订阅——这两个事件挂在 `CopilotChatMessage` 自身上，而非 `MessageItems` 中的子项上。`ClearMessageItems` 清理的是子项的 `PropertyChanged` 订阅，与新增事件无关。

---

## 改动影响分析

### 不受影响的部分

| 部分 | 说明 |
|------|------|
| `PropertyChanged("Content")` 通知 | 保持不变，UI 绑定继续工作 |
| `PropertyChanged("Text")` on `CopilotChatTextItem` | 保持不变 |
| `MessageItems.CollectionChanged` | 保持不变 |
| `Content` 属性 getter | 保持不变，仍是 `string.Concat(...)` |
| 不订阅新事件的调用方 | 完全无感知，行为不变 |

### 受益的场景

| 场景 | 改善 |
|------|------|
| `SlideStreamingPipeline` 流式 XML 提取 | 直接订阅 `TextAppended` 获取增量，无需 `_lastProcessedLength` 差集 |
| `ClearMessageItems` 导致的 Content 跳变 | 新事件不受影响，`AppendText` 传入的 `text` 就是增量 |
| `MessageItems` 中间插入工具项 | 新事件不受影响，只传递 `AppendText` 的参数 |

---

## 风险与注意事项

1. **线程安全**：`TextAppended` / `ReasoningAppended` 的触发与 `AppendText` / `AppendReasoning` 在同一线程。`CopilotChatManager.RunAsync` 中的 `RunStreamingAsync` 循环在后台线程执行，因此事件回调也在后台线程。订阅者需自行处理线程调度（如 `SlideStreamingPipeline` 中的 `_dispatcher.InvokeAsync`）。

2. **事件不会被 ClearMessageItems 清除**：`ClearMessageItems` 只清理 `MessageItems` 子项的 `PropertyChanged` 订阅，不清理 `CopilotChatMessage` 自身的事件。订阅者需在消息生命周期结束时自行取消订阅（`-= `），或使用弱引用模式。这与现有 `PropertyChanged` 的订阅生命周期一致。

3. **通用性**：新增的事件是通用能力，不引入 SlideML 或任何业务特定代码。任何需要感知流式增量文本的调用方都可以订阅。

4. **不修改 `Text` setter**：`CopilotChatTextItem.Text` 的 `internal set` 可能被同一程序集内的其他代码直接赋值（非追加式修改）。这种场景不会触发 `TextAppended`，因为事件只在 `CopilotChatMessage.AppendText` 中触发。这是预期行为——事件语义是"追加"，不是"变更"。

---

## 实施顺序

1. 在 `CopilotChatMessage` 中新增 `TextAppended` / `ReasoningAppended` 事件声明
2. 修改 `AppendText` 方法，末尾触发 `TextAppended`
3. 修改 `AppendReasoning` 方法，末尾触发 `ReasoningAppended`
4. 编译 AgentLib，确认无编译错误
