# 异步 SubAgent + 后验工具 设计方案

## 背景

当前 `SubAgentToolProvider.InvokeSubAgentAsync` 是严格同步阻塞的：调用后 `await foreach` 等待子 Agent 的 `RunStreamingAsync` 完全结束才返回结果。父 Agent 在工具调用期间无法做任何其他事情。

此外，当前没有"后验门禁"机制：模型无法在一开始设置质量门禁条件，等本轮输出完成后再自动检查是否满足要求。

## 需求

1. **异步 SubAgent**：模型调用 InvokeSubAgent 后立即得到 JobId 确认，后台执行子 Agent。模型可继续其他工具调用，也可通过 QuerySubAgentStatus 查询进度。本轮完成后，已完成的结果自动注入。
2. **后验工具**：模型在一开始调用 RegisterPostValidationGate 设置门禁条件，本轮输出完成后自动执行门禁检查，不通过的结果作为新消息注入到下一轮对话。

## 核心架构分析

### 当前聊天循环 (`CopilotChatManager.RunAsync`, 第 453-507 行)

```csharp
await foreach (AgentResponseUpdate ... in chatClientAgent.RunStreamingAsync(...))
{
    AppendAssistantResponseUpdate(assistantChatMessage, agentRunResponseUpdate);
}
```

这是一个**完全由框架管理的黑盒循环**。`Microsoft.Agents.AI` 的 `RunStreamingAsync` 在内部自动执行 tool-call → tool-result → continue 循环。`CopilotChatManager` 只做流式输出的消费，无法直接干预框架内部的 tool-call 循环。

### 框架拦截点

1. **`SessionTitleGenerator`** 示范了 `UseFunctionInvocation` + `context.Terminate` 模式：可以在单个工具调用完成后直接终止 Agent 运行。
2. **`HumanApprovalTool`** 示范了 `AIFunction` 的两阶段包装模式：配置阶段 (`ConfiguredHumanApprovalFunction`) → 运行时阶段 (`RuntimeHumanApprovalFunction`)，运行时可用 `TaskCompletionSource` 等待外部信号。
3. **`ChatClientAgentOptions`** 提供了 `AIContextProviders`、`ChatHistoryProvider` 等扩展点，但不暴露 tool-call 循环的钩子。

### 现有数据模型能力

- **`CopilotChatSubAgentItem`**：已有完整的 UI 数据模型，支持嵌套消息、输入文本、输出文本、CallId 跟踪、递归嵌套。
- **`CopilotChatContext`**：已在 `SubAgentToolProvider` 中使用 `_chatContext?.CreateSubAgentContext(subAgentItem)` 创建子上下文。
- **`CopilotChatMessage`**：支持 `IsPresetInfo`（预设消息不参与对话上下文），可用于 System 消息注入。

## 设计理念

在 `CopilotChatManager` 层面引入**管道化的 Pre/Post 钩子**机制，不改动框架内部、不改动现有工具接口签名。所有新特性通过"中间存储 + 钩子触发"实现。

---

## 一、异步 SubAgent 工具设计

### 1.1 工具 API

```
InvokeSubAgentAsync(prompt, systemPrompt?, subAgentType?)
  → 返回 '{"JobId":"<guid>", "Status":"Submitted"}'

QuerySubAgentStatusAsync(jobId)
  → 返回 '{"JobId":"<guid>", "Status":"Running"|"Completed"|"Failed", "Result":"..."}'
```

### 1.2 SubAgentJob 状态模型

新增 `AgentLib/Model/SubAgentJob.cs`：

```csharp
namespace AgentLib.Model;

/// <summary>
/// 表示一个异步执行的子 Agent 任务。
/// </summary>
public sealed class SubAgentJob
{
    /// <summary>任务唯一标识符。</summary>
    public string JobId { get; }

    /// <summary>当前任务状态。</summary>
    public SubAgentJobStatus Status { get; internal set; }

    /// <summary>任务完成后（或运行中）的输出文本。</summary>
    public string? Result { get; internal set; }

    /// <summary>任务出错时的错误信息。</summary>
    public string? Error { get; internal set; }

    /// <summary>关联的 UI 模型，用于流式更新子 Agent 输出。</summary>
    public CopilotChatSubAgentItem? SubAgentItem { get; }

    /// <summary>用于等待任务完成的 TaskCompletionSource。</summary>
    internal TaskCompletionSource? CompletionSource { get; }
}

/// <summary>
/// 子 Agent 任务的执行状态。
/// </summary>
public enum SubAgentJobStatus
{
    Submitted,
    Running,
    Completed,
    Failed
}
```

### 1.3 AsyncSubAgentJobManager

新增 `AgentLib/Tools/AsyncSubAgentJobManager.cs`：

- **职责**：管理所有异步 SubAgent 任务的生命周期
- **核心 API**：
  - `SubmitJobAsync(SubAgentJob job, Func<CancellationToken, Task<string>> executor)`：启动后台 Task
  - `GetJobStatus(string jobId)`：查询单个任务状态
  - `CollectCompletedJobs()`：收集本轮所有已完成的任务结果
  - `CancelAll()`：取消所有进行中的任务
- **并发安全**：使用 `ConcurrentDictionary<string, SubAgentJob>`
- **CancellationToken**：后台 Task 使用 `LinkedTokenSource` 连接 chat 和 job 级别的 token

### 1.4 SubAgentToolProvider 修改

保持 `InvokeSubAgentAsync` **方法签名不变**（API 兼容），内部行为改变：

**Before**（同步阻塞）：
```csharp
// 创建 ChatClientAgent → await foreach RunStreamingAsync → return output
```

**After**（异步非阻塞）：
```csharp
public async Task<string> InvokeSubAgentAsync(string prompt, string? systemPrompt, string? subAgentType)
{
    // 1. 验证参数
    // 2. 创建 CopilotChatSubAgentItem（UI 模型）
    // 3. 创建 SubAgentJob，提交到 AsyncSubAgentJobManager
    // 4. 启动后台 Task（内部 await foreach RunStreamingAsync）
    // 5. 立即返回 JobId
    return JsonSerializer.Serialize(new { JobId = job.JobId, Status = "Submitted" });
}
```

新增 `QuerySubAgentStatusAsync`：
```csharp
[Description("查询异步子智能体任务的执行状态。")]
public Task<string> QuerySubAgentStatusAsync(
    [Description("之前调用 InvokeSubAgent 返回的 JobId。")]
    string jobId)
{
    var job = _asyncJobManager.GetJobStatus(jobId);
    if (job is null)
        return Task.FromResult("{ \"Error\": \"Job not found\" }");

    return Task.FromResult(JsonSerializer.Serialize(new
    {
        job.JobId,
        Status = job.Status.ToString(),
        Result = job.Status == SubAgentJobStatus.Completed ? job.Result : null
    }));
}
```

### 1.5 UI 流式更新

后台 Task 执行时，通过 `CopilotChatSubAgentItem.AppendText()` / `AppendReasoning()` / `AppendFunctionCall()` 实时更新 MessageItems。由于这些方法不是线程安全的，需要：
- 方案 A：通过 `SynchronizationContext.Post` 调度到 UI 线程
- 方案 B：在 `CopilotChatManager` 上提供 `Action<SubAgentJob>` 委托，调用方实现 UI 线程调度

建议采用方案 B，保持库本身对 UI 框架无关。

### 1.6 CopilotChatManager.RunAsync 修改

在 `RunStreamingAsync` **完成之后**、`return` 之前：

```csharp
// == Post-turn: Collect async SubAgent results ==
var completedJobs = _asyncSubAgentJobManager.CollectCompletedJobs();
foreach (var job in completedJobs)
{
    // 更新关联的 CopilotChatSubAgentItem.OutputText
    if (job.SubAgentItem is not null)
    {
        job.SubAgentItem.OutputText = job.Result ?? job.Error ?? string.Empty;
    }
}
```

### 1.7 数据流总览

```
模型调用 InvokeSubAgentAsync("分析代码")
  → 工具立即返回 '{"JobId":"abc-123", "Status":"Submitted"}'
  → 后台 Task 开始执行子 Agent（流式更新 CopilotChatSubAgentItem.MessageItems）
  ↓
  → 模型继续其他工作（可以调用其他工具）
  ↓
  → 模型可选调用 QuerySubAgentStatus("abc-123") 查询进度
  ↓
  ↓ RunStreamingAsync 结束（模型本轮输出完成）
  ↓
  → RunAsync Post 钩子收集已完成结果 → 更新 SubAgentItem.OutputText
```

---

## 二、后验工具（PostValidation）设计

### 2.1 工具 API

```
RegisterPostValidationGate(name, criteria, description)
  → 返回 "Gate '<name>' has been registered."
```

### 2.2 数据模型

新增 `AgentLib/Model/PostValidationGate.cs`：

```csharp
namespace AgentLib.Model;

/// <summary>
/// 表示一个后验门禁定义。
/// </summary>
/// <param name="Name">门禁名称。</param>
/// <param name="Criteria">自然语言描述的检查条件。</param>
/// <param name="Description">人类可读的说明。</param>
public sealed record PostValidationGate(
    string Name,
    string Criteria,
    string Description
);
```

新增 `AgentLib/Model/GateEvaluationResult.cs`：

```csharp
namespace AgentLib.Model;

/// <summary>
/// 表示一个门禁的评估结果。
/// </summary>
/// <param name="GateName">门禁名称。</param>
/// <param name="Passed">是否通过。</param>
/// <param name="Reason">判定理由。</param>
/// <param name="SuggestedFix">如果不通过，建议的修正方向。</param>
public sealed record GateEvaluationResult(
    string GateName,
    bool Passed,
    string Reason,
    string? SuggestedFix
);
```

### 2.3 PostValidationGateRegistry

新增 `AgentLib/Tools/PostValidationGateRegistry.cs`：

- **职责**：管理当前轮次的门禁注册
- **核心 API**：
  - `Register(PostValidationGate gate)`：注册一个门禁
  - `Clear()`：清空门禁（每轮开始前调用）
  - `IReadOnlyList<PostValidationGate> PendingGates { get; }`：待执行的门禁列表

### 2.4 PostValidationGateExecutor

新增 `AgentLib/Tools/PostValidationGateExecutor.cs`：

- **职责**：调用 LLM（Flash 模型）评估门禁是否通过
- **核心方法**：

```csharp
/// <summary>
/// 执行门禁评估。
/// </summary>
/// <param name="assistantOutput">本轮模型最终输出的全部内容。</param>
/// <param name="gates">需要评估的门禁列表。</param>
/// <param name="flashChatClient">用于评估的 Flash 模型 ChatClient（低成本）。</param>
/// <param name="cancellationToken">取消令牌。</param>
/// <returns>每个门禁的评估结果。</returns>
public async Task<IReadOnlyList<GateEvaluationResult>> ExecuteGatesAsync(
    string assistantOutput,
    IReadOnlyList<PostValidationGate> gates,
    IChatClient flashChatClient,
    CancellationToken cancellationToken)
```

**实现策略**：使用 `SessionTitleGenerator` 的 `FunctionInvoker + context.Terminate` 模式：

1. 构造 System Prompt：包含 assistant 的完整输出 + 所有 gates 的 criteria
2. 为每个 gate 创建一个 AIFunction（SubmitGateResult）
3. 使用 `UseFunctionInvocation` 在提交结果后立即 `context.Terminate`
4. 用 Flash 模型执行，收集所有 gate 评估结果

### 2.5 PostValidationToolProvider

新增 `AgentLib/Tools/PostValidationToolProvider.cs`：

```csharp
namespace AgentLib.Tools;

/// <summary>
/// 提供后验门禁注册工具。
/// </summary>
public sealed class PostValidationToolProvider
{
    private readonly PostValidationGateRegistry _registry;

    public PostValidationToolProvider(PostValidationGateRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public IReadOnlyList<AITool> CreateDefaultTools()
    {
        return
        [
            AIFunctionFactory.Create(RegisterPostValidationGateAsync,
                name: "RegisterPostValidationGate",
                description: "注册一个后验门禁条件。本轮对话完成后会自动检查这些条件是否满足。")
        ];
    }

    [Description("注册一个后验门禁条件，本轮对话完成后会自动检查是否满足。应在对话早期调用，避免上下文污染。")]
    public Task<string> RegisterPostValidationGateAsync(
        [Description("门禁名称，简短标识。")]
        string name,
        [Description("具体的检查条件，用自然语言描述。例如：'输出中必须包含单元测试代码'。")]
        string criteria,
        [Description("门禁的人类可读说明。可为空。")]
        string? description = null)
    {
        _registry.Register(new PostValidationGate(name, criteria, description ?? criteria));
        return Task.FromResult($"Gate '{name}' has been registered and will be evaluated after this turn.");
    }
}
```

### 2.6 CopilotChatManager.RunAsync 修改

```csharp
async Task<SendMessageRunState> RunAsync()
{
    try
    {
        // ======== Pre-turn hooks ========
        _postValidationGateRegistry?.Clear();
        _asyncSubAgentJobManager?.CancelAll();

        // ======== Main run ========
        ChatClientAgentCreatedResult chatClientAgentCreatedResult = await createChatClientAgentTask;
        currentSession.AddMessage(assistantChatMessage);

        bool isFirst = true;
        // ... (existing RunStreamingAsync loop) ...

        // ======== Post-turn hooks ========

        // 1. Collect async SubAgent results
        if (_asyncSubAgentJobManager is not null)
        {
            var completedJobs = _asyncSubAgentJobManager.CollectCompletedJobs();
            foreach (var job in completedJobs)
            {
                if (job.SubAgentItem is not null)
                {
                    job.SubAgentItem.OutputText = job.Result ?? job.Error ?? string.Empty;
                }
            }
        }

        // 2. Execute post-validation gates
        if (_postValidationGateRegistry is not null)
        {
            var pendingGates = _postValidationGateRegistry.PendingGates;
            if (pendingGates.Count > 0)
            {
                var flashChatClient = await ResolveFlashChatClientAsync();
                if (flashChatClient is not null)
                {
                    var results = await _postValidationGateExecutor.ExecuteGatesAsync(
                        assistantChatMessage.FullContent,
                        pendingGates,
                        flashChatClient,
                        currentChatCancellationToken);

                    var failedGates = results.Where(r => !r.Passed).ToList();
                    if (failedGates.Count > 0)
                    {
                        var gateMessage = BuildGateFailureMessage(failedGates);
                        var systemMessage = CopilotChatMessage.CreateAssistant(gateMessage, isPresetInfo: true);
                        await AppendMessageAsync(currentSession, systemMessage, currentChatCancellationToken);
                    }
                }
            }
        }

        return new SendMessageRunState(IsSuccess: true, WasCanceled: false);
    }
    // ... (existing catch blocks) ...
}
```

### 2.7 门禁失败消息格式

```csharp
private static string BuildGateFailureMessage(IReadOnlyList<GateEvaluationResult> failedGates)
{
    var sb = new StringBuilder();
    sb.AppendLine("【后验门禁检查结果】以下门禁未通过：");
    sb.AppendLine();
    foreach (var gate in failedGates)
    {
        sb.AppendLine($"❌ {gate.GateName}");
        sb.AppendLine($"   原因：{gate.Reason}");
        if (!string.IsNullOrWhiteSpace(gate.SuggestedFix))
        {
            sb.AppendLine($"   建议：{gate.SuggestedFix}");
        }
        sb.AppendLine();
    }
    return sb.ToString();
}
```

### 2.8 数据流总览

```
用户: "写一个排序算法，要求 O(n log n)，要有单元测试"
  ↓
模型首轮输出前调用 RegisterPostValidationGate("perf_gate",
    "检查排序算法是否实现了 O(n log n) 时间复杂度", "性能门禁")
模型首轮输出前调用 RegisterPostValidationGate("test_gate",
    "检查输出中是否包含单元测试代码", "测试门禁")
  ↓
模型输出排序代码（但忘了写单元测试）
  ↓
RunStreamingAsync 结束 → Post hook 触发
  ↓
PostValidationGateExecutor 用 Flash 模型评估:
  - perf_gate: Passed ✓
  - test_gate: Failed ✗ （缺少单元测试）
  ↓
注入 System Message: "【后验门禁检查结果】以下门禁未通过: test_gate..."
  ↓
用户下轮发送 → 模型看到系统消息 → 补充单元测试
```

---

## 三、对 CopilotChatManager 的完整修改方案

### 3.1 构造函数变更

```csharp
public CopilotChatManager(ICopilotChatLogger chatLogger)
{
    ChatLogger = chatLogger;
    _toolManager = new CopilotToolManager(this.AgentApiEndpointManager);

    // 新增：异步任务管理器
    _asyncSubAgentJobManager = new AsyncSubAgentJobManager();

    // 新增：后验门禁注册表
    _postValidationGateRegistry = new PostValidationGateRegistry();

    // 新增：后验门禁执行器
    _postValidationGateExecutor = new PostValidationGateExecutor();

    _titleGenerator = new SessionTitleGenerator(AgentApiEndpointManager);
    CreateNewSession();
}
```

### 3.2 新增字段

```csharp
private readonly AsyncSubAgentJobManager _asyncSubAgentJobManager;
private readonly PostValidationGateRegistry _postValidationGateRegistry;
private readonly PostValidationGateExecutor _postValidationGateExecutor;
```

### 3.3 RunAsync 完整流程

```
Pre hooks:
  ┌─────────────────────────────────────────┐
  │ 1. Clear PostValidationGateRegistry     │
  │ 2. Cancel all pending async SubAgent    │
  │    jobs (optional: only cancel if new   │
  │    turn starts)                         │
  └─────────────────────────────────────────┘
                    ↓
Main loop:
  ┌─────────────────────────────────────────┐
  │ await foreach RunStreamingAsync(...)    │
  │   - 框架管理 tool-call 循环              │
  │   - CopilotChatManager 流式消费输出       │
  └─────────────────────────────────────────┘
                    ↓
Post hooks:
  ┌─────────────────────────────────────────┐
  │ 1. Collect async SubAgent results       │
  │    → 更新 CopilotChatSubAgentItem       │
  │                                         │
  │ 2. Execute post-validation gates        │
  │    → 收集未通过的 gate                  │
  │    → 注入 System Message 到会话         │
  └─────────────────────────────────────────┘
```

---

## 四、影响范围

### 4.1 新增文件

| 文件 | 职责 |
|------|------|
| `AgentLib/Tools/AsyncSubAgentJobManager.cs` | 管理异步 SubAgent 任务生命周期 |
| `AgentLib/Tools/PostValidationGateRegistry.cs` | 门禁注册与查询 |
| `AgentLib/Tools/PostValidationGateExecutor.cs` | 调用 LLM（Flash）评估门禁 |
| `AgentLib/Tools/PostValidationToolProvider.cs` | 提供 RegisterPostValidationGate 工具函数 |
| `AgentLib/Model/SubAgentJob.cs` | SubAgent 任务状态模型（含 `SubAgentJobStatus` 枚举） |
| `AgentLib/Model/PostValidationGate.cs` | 门禁定义模型 |
| `AgentLib/Model/GateEvaluationResult.cs` | 门禁评估结果模型 |

### 4.2 修改文件

| 文件 | 变更 |
|------|------|
| `AgentLib/CopilotChatManager.cs` | ① 构造函数集成 `AsyncSubAgentJobManager`、`PostValidationGateRegistry`、`PostValidationGateExecutor`；② `RunAsync` 添加 Pre/Post 钩子；③ 新增 `ResolveFlashChatClientAsync` 辅助方法 |
| `AgentLib/Tools/SubAgentToolProvider.cs` | ① `InvokeSubAgentAsync` 改为非阻塞（返回 JobId）；② 新增 `QuerySubAgentStatusAsync` 工具方法；③ 构造函数注入 `AsyncSubAgentJobManager` |
| `AgentLib/Tools/CopilotToolManager.cs` | ① 集成 `AsyncSubAgentJobManager`、`PostValidationToolProvider`；② `CreateDefaultTools` 添加 QuerySubAgentStatus 和 RegisterPostValidationGate 工具 |

### 4.3 不变更的文件

| 文件 | 原因 |
|------|------|
| `AgentLib/Model/CopilotChatSubAgentItem.cs` | 现有 UI 模型已经足够（InputText, OutputText, MessageItems, CallId） |
| `AgentLib/Model/CopilotChatMessage.cs` | 现有消息模型已经足够（System Role, IsPresetInfo） |
| `AgentLib/Model/CopilotChatApprovalToolItem.cs` | 与后验工具无关 |
| `AgentLib/Tools/HumanApprovalTool.cs` | 后验工具是独立机制，不影响审批流程 |
| `AgentLib/Tools/WorkspaceToolProvider.cs` | 不相关 |
| `AgentLib/AgentExtensions/ChatClientAgentExtension.cs` | 不相关 |
| `AgentLib/Core/AgentApiEndpointManager.cs` | 不相关 |

---

## 五、风险与注意点

### 5.1 CancellationToken 传播

| 场景 | 处理方式 |
|------|----------|
| 用户取消聊天 | `CancelAll()` 取消所有进行中的异步 SubAgent |
| 单个 SubAgent 超时 | 后台 Task 内部用 `CancellationTokenSource` + `CancelAfter` |
| SubAgent 内部取消 | 子 ChatClientAgent 使用 `LinkedTokenSource` |

### 5.2 并发安全

- `AsyncSubAgentJobManager` 使用 `ConcurrentDictionary<string, SubAgentJob>`
- `PostValidationGateRegistry` 使用 `ConcurrentBag<PostValidationGate>`
- `SubAgentJob.Status` 的读写使用 `Interlocked` 或 `lock`

### 5.3 UI 线程安全

- 后台 SubAgent Task 更新 `CopilotChatSubAgentItem.MessageItems` 时需要确保线程安全
- 建议在 `AsyncSubAgentJobManager` 上提供 `SynchronizationContext` 属性，或提供事件回调让 UI 层自行调度
- 运行门禁评估的 Flash 模型调用不在 UI 线程上，结果注入在聊天线程

### 5.4 门禁评估的 LLM 成本

- 使用 Flash 模型降低评估成本
- 门禁评估是一次性的 LLM 调用，不进入无限循环
- 如果 Flash 模型不可用，回退到 PrimaryModel

### 5.5 无限循环防护

- 后验工具结果作为 **新的 System Message** 注入到下一轮对话，而不是本轮 `function_result`
- 这避免了"模型看到 gate 不通过 → 立即修正 → 再次触发 gate → 不通过 → ..."的死循环
- 如果用户希望模型自动修正，可以另外实现一个"AutoRetry"模式（未来扩展）

### 5.6 向后兼容性

- `InvokeSubAgentAsync` 方法签名不变
- `CopilotChatManager` 公开 API 不变
- `SubAgentToolProvider` 构造函数新增可选参数，现有调用方无需修改
- `SendMessageRequest` 不变

### 5.7 异步 SubAgent 的结果丢失风险

如果用户在两轮对话之间（SubAgent 还在运行中）就发送了新消息：
- 方案：`RunAsync` 的 Pre hook 检查是否有上一轮未完成的 SubAgent，将已完成的结果先注入
- 对于仍在运行的 SubAgent，可选择取消或继续等待

---

## 六、与现有 HumanApprovalTool 的对比

| 维度 | HumanApprovalTool | 异步 SubAgent | 后验工具 |
|------|-------------------|---------------|----------|
| 触发时机 | 工具调用时 | 工具调用时 | 本轮完成后 |
| 阻塞模式 | 同步等待人工确认 | 非阻塞，立即返回 JobId | 工具调用本身不阻塞 |
| 结果注入 | 在 tool-call 循环内 | Post hook 阶段 | Post hook → 下轮对话 |
| UI 模型 | `CopilotChatApprovalToolItem` | `CopilotChatSubAgentItem`（已有） | 仅文本消息 |
| 外部信号 | `ApproveToolExecution` / `RejectToolExecution` | `QuerySubAgentStatus` | 无（自动评估） |

---

## 七、实施步骤（待编码）

1. 创建 `SubAgentJob` + `SubAgentJobStatus` 模型（`AgentLib/Model/SubAgentJob.cs`）
2. 创建 `PostValidationGate` 模型（`AgentLib/Model/PostValidationGate.cs`）
3. 创建 `GateEvaluationResult` 模型（`AgentLib/Model/GateEvaluationResult.cs`）
4. 创建 `AsyncSubAgentJobManager`（`AgentLib/Tools/AsyncSubAgentJobManager.cs`）
5. 创建 `PostValidationGateRegistry`（`AgentLib/Tools/PostValidationGateRegistry.cs`）
6. 创建 `PostValidationGateExecutor`（`AgentLib/Tools/PostValidationGateExecutor.cs`）
7. 创建 `PostValidationToolProvider`（`AgentLib/Tools/PostValidationToolProvider.cs`）
8. 修改 `SubAgentToolProvider`：支持异步模式、新增 QuerySubAgentStatus
9. 修改 `CopilotToolManager`：集成新组件
10. 修改 `CopilotChatManager`：Pre/Post 钩子
11. 编译验证
12. 编写单元测试
    - 异步 SubAgent：提交任务 → 立即返回 JobId → 查询状态 → 完成后结果注入
    - 后验工具：注册 gate → 模型完成 → 评估 → 不通过时 System Message 注入
    - 取消测试：聊天取消时异步 SubAgent 也被取消
    - 并发测试：多个异步 SubAgent 同时运行

---

## 八、讨论结论：异步 SubAgent 结果的双层注入策略

### 8.1 核心约束回顾

`Microsoft.Agents.AI` 的 `RunStreamingAsync` 是一个**黑盒循环**，内部自动执行 tool-call → tool-result → continue 循环。`CopilotChatManager` 仅流式消费输出，无法向框架内部的 tool-call 循环注入新消息（如函数结果）。

因此，异步 SubAgent 的结果注入存在两个层次的需求：

| 层次 | 目标受众 | 时机要求 |
|------|----------|----------|
| **UI 展示层** | 用户 | 实时/近实时，SubAgent 完成即更新界面 |
| **模型感知层** | LLM 模型 | 需在 tool-call 循环中作为函数结果出现，模型才能继续推理 |

### 8.2 第一层：流内 UI 刷新（✅ 完全可行）

在 `await foreach` 循环中，每次收到 `FunctionResultContent` 后 piggyback 检查 `AsyncSubAgentJobManager`，将已完成 job 的结果更新到对应 `CopilotChatSubAgentItem.OutputText`，UI 立即刷新。

**实现要点**：

```csharp
// 在 RunAsync 的 await foreach 循环中
await foreach (AgentResponseUpdate agentRunResponseUpdate in chatClientAgent.RunStreamingAsync(...))
{
    AppendAssistantResponseUpdate(assistantChatMessage, agentRunResponseUpdate);

    // 每次收到内容更新后，刷新已完成的异步 SubAgent 结果到 UI
    FlushCompletedSubAgentJobs(assistantChatMessage);
}

void FlushCompletedSubAgentJobs(CopilotChatMessage assistantMessage)
{
    var completedJobs = _asyncSubAgentJobManager.CollectCompletedJobs();
    foreach (var job in completedJobs)
    {
        if (job.SubAgentItem is not null)
        {
            job.SubAgentItem.OutputText = job.Result ?? job.Error ?? string.Empty;
        }
    }
}
```

**优点**：
- 用户实时看到 SubAgent 输出完成
- 不侵入框架，只在现有循环中加一行调用
- `OutputText` 更新触发 `INotifyPropertyChanged`，UI 自动刷新

**注意**：
- 如果模型连续输出多段文本而不调用工具，已完成 job 不会被立即检测到（需等到下一轮 `FunctionResultContent` 或 `RunStreamingAsync` 结束后的 Post hook）
- `CopilotChatSubAgentItem.MessageItems`（`ObservableCollection`）不是线程安全的，后台 SubAgent Task 更新它时需要调度到 UI 线程

### 8.3 第二层：模型同一轮感知（⚠️ 需绕行）

让模型在同一轮对话中看到 SubAgent 结果，有两条路：

#### 方案 A：AIFunction 包装器（模型可见，但 hacky）

类似 `HumanApprovalTool` 的两阶段包装模式，给**所有工具**包一层装饰器：内部工具执行完毕后，检查 `AsyncSubAgentJobManager`，若有已完成 job，将结果**追加到本工具返回值末尾**。

```csharp
// 伪代码：工具包装器
protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken ct)
{
    object? result = await _innerFunction.InvokeAsync(arguments, ct);

    // 检查异步 SubAgent 是否完成，有则追加到返回值
    var completedJobs = _jobManager.CollectCompletedJobs();
    if (completedJobs.Count > 0)
    {
        string appendText = string.Join("\n", completedJobs.Select(j => $"[异步子智能体已完成] {j.Result}"));
        result = $"{result}\n\n{appendText}";
    }

    return result;
}
```

| 优点 | 缺点 |
|------|------|
| 模型在同一次 tool-result 中看到 SubAgent 结果 | 污染了工具返回值，模型可能困惑 |
| 实现简单，只需包装现有工具 | 只在有其他工具调用时才会触发检查 |
| 与 HumanApprovalTool 模式一致 | 模型可能无法区分"工具输出"和"注入信息" |

#### 方案 B：Post-hook + 下轮 System Message 注入（推荐）

`RunStreamingAsync` 完全结束后，在 Post hook 中收集已完成结果，作为 **System Message（`IsPresetInfo = true`）** 注入到会话，模型在下一轮看到。

```
本轮:  模型 → tool-call → tool-result → ... → 输出完成
                                                ↓
                                       Post hook: 收集已完成 job
                                       → 注入 system message
下轮:  模型看到 system message → 基于 SubAgent 结果继续推理
```

| 优点 | 缺点 |
|------|------|
| 干净，不污染工具结果 | 模型不能在同一轮使用结果 |
| 不侵入框架 | 需要等用户发下一条消息（或自动发送空消息触发下一轮） |
| 符合现有 `IsPresetInfo` 设计模式 | — |

### 8.4 推荐组合策略：两层互补

```
                    ┌─────────────────────────────────────────┐
                    │            RunStreamingAsync             │
                    │  ┌─────────────────────────────────┐    │
                    │  │ 模型 → tool-call → tool-result   │    │
                    │  │   ↓ piggyback: Flush UI          │    │
                    │  │ 模型 → tool-call → tool-result   │    │
                    │  │   ↓ piggyback: Flush UI          │    │
                    │  │ 模型 → 最终文本输出               │    │
                    │  └─────────────────────────────────┘    │
                    │              ↓                           │
                    │     Post hook:                           │
                    │     ① Flush 剩余已完成 SubAgent → UI     │
                    │     ② BuildGateFailureMessage →          │
                    │       注入 System Message → 下轮模型感知  │
                    └─────────────────────────────────────────┘
```

1. **流内 Piggyback**：每次 `FunctionResultContent` 出现时检查已完成 SubAgent → 更新 UI（用户实时看到）
2. **Post-hook 兜底**：`RunStreamingAsync` 结束后再次收集 + 注入 System Message（模型下轮感知，补充丢失的检测窗口）
3. **两者各司其职**：UI 更新在流内做，模型感知在轮间做，互不干扰

### 8.5 流内 Piggyback 的检测时机与覆盖盲区

| 事件 | 检测行为 | 盲区 |
|------|----------|------|
| `FunctionResultContent` 到达 | 立即触发 `FlushCompletedSubAgentJobs` | — |
| 连续文本输出（无工具调用） | **不触发** | SubAgent 在这期间完成则延迟到下一次 tool-result 或 Post-hook |
| `RunStreamingAsync` 结束 | Post-hook 兜底 | — |

由于 LLM 通常在输出文本后调用工具（或直接结束），盲区很小。Post-hook 可完全覆盖。

### 8.6 线程安全策略

| 组件 | 线程模型 | 措施 |
|------|----------|------|
| `AsyncSubAgentJobManager` 状态字典 | 多线程并发 | `ConcurrentDictionary<string, SubAgentJob>` |
| `SubAgentJob.Status` 读写 | 多线程并发 | `Interlocked.Exchange` 或 `lock` |
| `CopilotChatSubAgentItem.MessageItems`（`ObservableCollection`） | 后台 Task 写入，UI 线程读取 | 通过 `SynchronizationContext.Post` 调度更新，或提供 `Action<SubAgentJob>` 委托给 UI 层自行线程调度 |
| `CopilotChatSubAgentItem.OutputText` 字段 | 聊天线程写入，UI 线程读取 | 通过属性 setter 触发 `INotifyPropertyChanged`，WPF/UWP 自动 marshal |

建议采用**委托回调**方式，保持 `AgentLib` 对 UI 框架无关：

```csharp
/// <summary>
/// 当 SubAgent 流式输出到达时的回调。调用方（UI 层）负责将更新调度到 UI 线程。
/// </summary>
public Action<SubAgentJob, AgentResponseUpdate>? OnSubAgentStreamingUpdate { get; set; }
```

### 8.7 Job ↔ SubAgentItem 关联

当前 `CopilotChatMessage.AppendFunctionCall`（`CopilotChatMessage.cs` 第 525 行）对 `InvokeSubAgent` 特殊处理：CallId 加入 `_invokeSubAgentCallIds` 但不创建 UI item。而 `SubAgentToolProvider` 内部通过 `CopilotChatContext.CurrentContent.CreateSubAgentItem` 创建 `CopilotChatSubAgentItem`。

异步模式下，`InvokeSubAgentAsync` 需要：
1. 调用 `chatContext.CurrentContent.CreateSubAgentItem(...)` 创建 UI item，获得 `CopilotChatSubAgentItem`（含 `CallId`）
2. 创建 `SubAgentJob`，设置 `SubAgentItem` 属性指向此 UI item
3. `JobId` 可以使用 `CallId`（简化关联），或新建 Guid 并维护 `ConcurrentDictionary<string, string>` 映射表

建议 **`JobId` 直接复用 `CallId`**，减少映射层。

### 8.8 流内 Piggyback 的修改点汇总

| 文件 | 变更 |
|------|------|
| `CopilotChatManager.cs` | ① `await foreach` 循环中加 `FlushCompletedSubAgentJobs` 调用；② Post-hook 中兜底收集；③ 新增 `FlushCompletedSubAgentJobs` 方法 |
| `SubAgentToolProvider.cs` | ① `InvokeSubAgentAsync` 改为非阻塞（方案文档已有）；② 返回 JSON 中 `JobId` 使用 `subAgentItem.CallId` |
| `CopilotChatMessage.cs` | `AppendFunctionCall` 对 `InvokeSubAgent` 的处理可能需要调整：异步模式下仍需在 `_invokeSubAgentCallIds` 中记录，但 UI item 由 `SubAgentToolProvider` 通过 `chatContext` 创建 |
| `AsyncSubAgentJobManager.cs` | 新增 `FlushCompletedJobsToUI` 或 `CollectCompletedJobs` 方法 |