# CopilotChatManager 会话标题自动生成计划（最终版）

## 背景

当前 `CopilotChatSession` 的标题生成逻辑（`TryUpdateTitle`）仅取首条用户消息的前 20 个字符作为标题，存在以下问题：

1. 标题质量差：首条消息往往是"你好"、"帮我看看这个"等不具描述性的内容
2. 截断生硬：`MaxTitleLength = 20` 字符可能在中文字符中间截断，丢失语义
3. 无法反映对话全貌：仅看首条消息，无法理解后续对话的主题

理想方案：利用 LLM 对对话内容进行总结，生成简洁、有意义的会话标题。

## 设计目标

1. 为 `CopilotChatManager` 增加 LLM 驱动的会话标题生成能力
2. 优先使用 **Flash 模型**（轻量、快速、低成本），取不到时回退到 `PrimaryModel`
3. 保持向后兼容：现有 `TryUpdateTitle` 行为作为 LLM 不可用时的回退
4. 标题生成异步执行，不阻塞 UI 和主流程
5. 支持两种触发方式：显式调用（调用方控制时机）+ 自动触发（首次助手回复后）

## 核心设计决策

### 决策 1：触发方式 —— 显式调用 + 自动触发

提供两种触发方式：

1. **显式调用**：提供 `public async Task GenerateSessionTitleAsync(...)` 方法，由调用方决定何时触发。
2. **自动触发**：在 `SendMessage` 内部 `RunAsync` 的 `finally` 中，当会话尚无 LLM 生成标题时（`HasCustomTitle == false`）自动触发，使用 fire-and-forget 方式不阻塞主流程。

**自动触发设计要点**：
- 仅在首次成功聊天后触发（`HasCustomTitle == false` 表示还没有 LLM 标题覆盖）
- 使用 `_ = GenerateSessionTitleAsync(session)` fire-and-forget，忽略异常
- 通过 `HasCustomTitle` 防止重复生成

### 决策 2：模型选择策略 —— Flash 优先，PrimaryModel 回退

```text
1. 从 AgentApiEndpointManager.GetSupportedModels() 中筛选 Flash 模型
   - 条件: m.ModelDefinition.Capabilities?.IsFlash == true
2. 如果找到 Flash 模型，使用 AgentApiEndpointManager.GetBestModel(predicate) 取能力最强的
3. 如果找不到 Flash 模型，回退到 AgentApiEndpointManager.PrimaryModel
```

**理由**：
- Flash 模型（如 Gemini Flash、GPT-4o-mini）速度快、成本低，非常适合标题生成这类轻量任务
- `GetBestModel` 在多个 Flash 模型中选能力最强的，保证质量
- 回退到 `PrimaryModel` 确保在没有 Flash 模型的环境下也能工作

### 决策 3：Title setter 可见性

`CopilotChatSession.Title` 的 setter 当前为 `private`。需要新增一个 `internal` 方法（如 `SetTitle`）供 `CopilotChatManager` 调用。

**理由**：
- 不直接暴露 `Title` setter 为 `public`，保持封装性
- `internal` 方法允许同程序集的 `CopilotChatManager` 写入
- 现有 `TryUpdateTitle` 保持不变，`SetTitle` 作为补充入口

### 决策 4：现有 TryUpdateTitle 的保留策略

保留 `TryUpdateTitle` 作为 LLM 生成失败或不可用时的回退。LLM 生成标题后通过 `SetTitle` 写入，同时设置 `_hasCustomTitle = true`，阻止后续 `TryUpdateTitle` 覆盖。

**行为流程**：
```text
首次 AddMessage（用户消息） → TryUpdateTitle 设置截断标题（_hasCustomTitle = true）
自动/显式触发 GenerateSessionTitleAsync → LLM 生成标题 → SetTitle 覆盖（_hasCustomTitle = true）
后续 AddMessage → _hasCustomTitle 已为 true，TryUpdateTitle 不再触发
```

> 注：由于 `TryUpdateTitle` 在首次用户消息时已将 `_hasCustomTitle` 设为 `true`，自动触发判断需通过新的 `internal bool HasCustomTitle` 属性区分"截断标题"和"LLM 标题"。引入 `_llmTitleGenerated` 字段标记 LLM 是否已生成过标题。

### 决策 5：方法签名

放在 `CopilotChatManager` 上，支持两种重载：

```csharp
// 对当前 SelectedSession 生成标题
public async Task GenerateSessionTitleAsync(CancellationToken cancellationToken = default);

// 对任意 CopilotChatSession 生成标题
public async Task GenerateSessionTitleAsync(CopilotChatSession session, CancellationToken cancellationToken = default);
```

同时提供静态辅助方法用于构建标题生成的 System Prompt 和 ChatMessage 列表，方便外部复用。

### 决策 6：System Prompt 设计

沿用 `CopilotChatManagerChatReducer` 的风格，设计简洁的标题生成 Prompt：

```text
基于对话内容，生成一个简洁的会话标题（不超过 20 个字）。
直接输出标题文本，不要包含引号、前缀或额外解释。
只使用对话中实际讨论的主题，不要编造内容。
```

### 决策 7：对话内容提取策略

从 `CopilotChatSession.ChatMessages` 中提取：
- 非 `IsPresetInfo` 的消息
- 优先取用户消息的 `Content` 和助手消息的 `Content`
- 注意：不包含 `Reason`（推理内容），只包含最终文本
- 如果消息过多，只取最近 N 条（如最近 10 条）或限制总字符数

### 决策 8：错误处理

- LLM 调用失败 → 静默失败，不抛异常，保留现有标题（已经由 TryUpdateTitle 设置）
- 返回空标题 → 不更新
- 取消令牌触发 → 静默退出，不更新标题
- 会话无有效消息 → 不调用 LLM，直接返回

## 需要变更的文件

### 1. `AgentLib\Model\CopilotChatSession.cs`

- 新增 `internal void SetTitle(string title)` 方法，允许同程序集写入标题
- `_hasCustomTitle` 字段考虑调整为允许 `SetTitle` 后续覆盖

### 2. `AgentLib\CopilotChatManager.cs`

- 新增 `public async Task GenerateSessionTitleAsync(CancellationToken cancellationToken = default)` 方法
- 新增 `public async Task GenerateSessionTitleAsync(CopilotChatSession session, CancellationToken cancellationToken = default)` 方法
- 新增 `private async Task<string?> GenerateTitleCoreAsync(CopilotChatSession session, CancellationToken cancellationToken)` 核心逻辑
- 新增 `private static IEnumerable<ChatMessage> BuildTitleGenerationMessages(CopilotChatSession session)` 辅助方法
- 新增 `private ILanguageModel? ResolveTitleGenerationModel()` 辅助方法（Flash 优先 → PrimaryModel 回退）

### 3. 不变更的文件

- `AgentApiEndpointManager.cs` — 现有 API（`GetSupportedModels`、`GetBestModel`、`PrimaryModel`）已满足需求
- `SendMessageRequest.cs` — 标题生成独立于消息发送，不需要修改
- `CopilotChatMessage.cs` — 现有 `Content`、`ToChatMessage()` 已满足需求

## 开放问题（已确认）

1. **触发时机**：✅ 两种都要。显式调用 + 自动触发（在 `RunAsync` 的 `finally` 中，首次成功聊天后）。
2. **标题最大长度**：✅ 保持 `MaxTitleLength = 20` 不变，UI 限制不能放宽。LLM Prompt 中要求不超过 20 个字，超出时代码侧截断。
3. **`_hasCustomTitle` 字段语义**：✅ LLM 生成标题后设 `_hasCustomTitle = true`，阻止后续 `TryUpdateTitle` 覆盖。引入 `_llmTitleGenerated` 字段区分"截断标题"和"LLM 标题"，自动触发仅当 `_llmTitleGenerated == false` 时执行。
4. **状态指示**：✅ 不需要，不加 `IsGeneratingTitle` 属性。
5. **日志记录**：✅ 不需要记录标题生成事件。

## 实施步骤

1. 在 `CopilotChatSession` 上新增 `internal void SetTitle(string title)` 方法
2. 在 `CopilotChatManager` 上新增 `ResolveTitleGenerationModel` 辅助方法（Flash 优先 → PrimaryModel 回退）
3. 在 `CopilotChatManager` 上新增 `BuildTitleGenerationMessages` 静态辅助方法
4. 在 `CopilotChatManager` 上新增 `GenerateTitleCoreAsync` 核心逻辑
5. 在 `CopilotChatManager` 上新增两个公开重载 `GenerateSessionTitleAsync`
6. 编写单元测试验证：Flash 模型路径、回退路径、空消息、错误处理
7. 编译验证通过