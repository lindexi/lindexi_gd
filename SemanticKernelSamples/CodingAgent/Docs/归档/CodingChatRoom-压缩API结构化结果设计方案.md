# CodingChatRoom 压缩 API 结构化结果设计方案

## 文档定位

本文是 `CodingChatRoom-压缩对话后展示与新会话探索.md` 中“方案 B：让压缩 API 返回结果，再由 Shell 决定如何展示”的独立深化方案。

本文回答以下问题：

1. `AgentLib` 应返回什么结构化结果；
2. 如何准确区分“已压缩”“没有历史”“因工具调用跳过”和“压缩器未改变历史”；
3. 如何取得可展示摘要，而不让 Shell 解析 `AgentSession`；
4. 如何兼容现有两个 `ReduceSessionAsync` 重载；
5. `CodingChatApplication` 和 `ChatViewModel` 应分别承担什么职责；
6. 如何保证取消、多模态内容、持久化和后续扩展的语义清晰。

本文只给出设计与实施建议，不实现代码。

## 一、结论摘要

推荐采用“富压缩器结果 + 会话压缩结果 + Shell 应用结果”三层结构：

1. `ICopilotChatReducer` 返回压缩器原始输出，明确给出：
   - 压缩后的模型消息；
   - 本次新生成的摘要内容；
   - 压缩器是否实际改变历史。
2. `CopilotChatManager.ReduceSessionWithResultAsync` 负责：
   - 检查 `AgentSession` 和内存历史；
   - 检查未完成工具调用；
   - 调用压缩器；
   - 在取消检查通过后替换模型历史；
   - 返回稳定的会话级结果。
3. `CodingChatApplication.CompressConversationAsync` 返回带 `SessionId` 的应用级结果，并负责互斥与保存。
4. `ChatViewModel` 根据结果决定：
   - 只更新状态；
   - 展示一次性摘要预览；
   - 请求应用层把摘要追加到公开消息。
5. 不让 `ChatViewModel` 读取或分析 `AgentSession`，也不让 `AgentLib` 直接创建带 UI 语义的 `CopilotChatMessage`。
6. 现有两个 `ReduceSessionAsync` 重载继续保留：默认压缩器和富压缩器路径复用新入口，只实现旧 `IChatReducer` 的调用继续走隔离的兼容路径。

第一版不需要修改会话持久化格式。压缩后的 `AgentSessionState` 仍按现有方式保存；只有当用户明确选择把摘要写入公开历史时，才把公开摘要作为普通 `ChatMessages` 保存。

## 二、现有实现的真实边界

### 2.1 当前手动压缩入口

`CopilotChatManager` 目前有两个公开入口：

- `ReduceSessionAsync(IChatReducer?)`：
  - 压缩当前选中会话的内部历史；
  - 从压缩结果中取所有 Assistant 内容；
  - 向当前公开历史追加“总结对话”和摘要消息。
- `ReduceSessionAsync(AgentSession?, IChatReducer?, CancellationToken)`：
  - 只压缩指定 `AgentSession`；
  - 不修改公开消息；
  - 不返回压缩结果。

两者最终都调用私有 `ReduceAgentSessionAsync`。该方法会把 reducer 返回的消息列表直接写回：

```text
agentSession.SetInMemoryChatHistory(resultList)
```

因此，当前私有方法实际上已经取得了结构化结果的核心数据，只是把结果限制为内部 `List<ChatMessage>`，没有对外表达状态和摘要语义。

### 2.2 当前默认手动压缩器

`CopilotChatManagerChatReducer` 的流程是：

1. 复制输入消息列表；
2. 从开头取出连续 System 消息；
3. 给剩余历史添加总结提示词；
4. 调用主模型；
5. 返回“原开头 System 消息 + 模型响应消息”。

对于该内置压缩器，可以准确知道：

- 哪些消息是保留的 System 历史；
- 哪些消息是本次模型新生成的摘要；
- 摘要消息中的 Assistant `AIContent` 是什么。

这些信息不应在 `CopilotChatManager` 中通过“扫描所有 Assistant 消息”反推，而应由压缩器在生成结果时直接返回。

### 2.3 当前工具调用保护只覆盖发送期间

发送消息时，历史提供器中的 reducer 会被 `ToolCallAwareChatReducer` 包装。存在未配对的 `FunctionCallContent` 时，该包装器原样返回历史，避免工具执行前过早压缩。

但是，手动 `ReduceSessionAsync` 的默认路径直接创建 `CopilotChatManagerChatReducer`，没有经过 `ToolCallAwareChatReducer`。

这意味着当前手动压缩 API 无法表达：

- 是否发现了未完成工具调用；
- 是否因此跳过压缩；
- reducer 是未执行，还是执行后恰好返回原历史。

新结构化 API 应把该安全检查提升到会话压缩编排层，而不是依赖某一个 reducer 装饰器的隐式行为。

### 2.4 Shell 已有合适的业务协调层

`CodingChatApplication.CompressConversationAsync` 当前已经负责：

- 与发送、新建、打开、删除会话互斥；
- 获取当前会话和 `AgentSession`；
- 调用压缩；
- 保存压缩后的会话；
- 更新左侧会话摘要；
- 通知状态变化。

因此，结构化结果应先返回到 `CodingChatApplication`，再由 `ChatViewModel` 消费。

`ChatViewModel` 当前只负责：

- 命令状态；
- 运行状态文案；
- 把 `ChatMessages` 投影为界面消息；
- 展示成功、取消和失败反馈。

这一分层应继续保持。

## 三、设计目标与非目标

### 3.1 设计目标

新 API 需要满足：

- Shell 不解析 `AgentSession`；
- 返回模型实际采用的压缩后历史；
- 返回本次压缩器明确生成的摘要内容；
- 区分未压缩的不同原因；
- 支持文本、图片、音频等现有 `AIContent`；
- 取消时不提交压缩结果；
- 与现有 `IChatReducer` 保持兼容；
- 现有调用方可以继续忽略返回值；
- 不强制公开摘要写入 `ChatMessages`；
- 不修改现有会话文件格式。

### 3.2 非目标

本方案第一版不负责：

- 在用户确认前预演压缩、确认后才提交；
- 为两个会话提供跨文件事务；
- 持久化独立摘要面板状态；
- 自动创建摘要新会话；
- 给所有第三方 `IChatReducer` 猜测摘要语义；
- 统计精确 Token 节省量。

特别需要区分：

> 本方案中的新入口是“执行压缩、提交内部历史、返回结果”，不是“准备一个尚未提交的压缩事务”。

如果未来要求用户先预览摘要、点击确认后才替换模型历史，应另行设计 `Prepare/Commit` 两阶段 API，不能让本结果对象同时承担事务句柄职责。

## 四、为什么需要两种结果对象

### 4.1 `IChatReducer` 的能力不足

框架 `IChatReducer` 只返回：

```text
IEnumerable<ChatMessage>
```

它不能表达：

- 哪些消息是新摘要；
- 是否因阈值不足而未压缩；
- 是否因安全条件而跳过；
- 返回原列表是否代表失败、跳过或无需压缩。

如果 `CopilotChatManager` 只比较消息数量，会出现误判：

- 摘要替换后消息数可能不变；
- reducer 可能改变内容但不改变数量；
- reducer 可能返回更长但更有效的结构；
- 工具尾块 reducer 未达到阈值时会原样返回。

如果扫描所有 Assistant 消息作为摘要，也会出现误判：

- 部分压缩器可能保留旧 Assistant 消息；
- 第三方 reducer 可能克隆完整历史；
- 结果中的 Assistant 消息不一定全部是本次摘要。

同样不能用对象引用比较可靠判断旧 reducer 是否压缩：

- reducer 可能返回内容相同的克隆消息；
- reducer 可能原位修改输入消息后返回同一对象；
- 任意 `AIContent` 不存在统一、稳定且低成本的结构相等规则。

因此，新结构化入口只接受能明确报告结果的富压缩器。旧 `IChatReducer` 继续由旧 API 兼容，但不会被强行适配成一个看似精确的结构化结果。

### 4.2 推荐的两级结果

建议区分：

1. 压缩器级输出：描述 reducer 做了什么；
2. 会话级结果：描述针对 `AgentSession` 的整个操作结果。

这样可以让：

- 压缩器准确声明自己新生成的摘要；
- Manager 统一处理会话缺失、工具调用、取消和历史提交；
- Shell 只消费稳定的会话级结果。

## 五、推荐的 AgentLib 类型设计

### 5.1 会话压缩状态

建议新增 `CopilotChatReductionStatus`：

```csharp
public enum CopilotChatReductionStatus
{
    Reduced,
    Unchanged,
    NoAgentSession,
    HistoryUnavailable,
    EmptyHistory,
    SkippedPendingToolCall,
}
```

各状态定义如下：

| 状态 | 含义 | 是否修改 AgentSession | Shell 建议 |
|---|---|---:|---|
| `Reduced` | reducer 已产生并提交不同的历史 | 是 | 可展示摘要或仅提示成功 |
| `Unchanged` | reducer 正常返回，但声明或判定历史未改变 | 否 | 提示当前无需压缩 |
| `NoAgentSession` | 未提供可压缩的 AgentSession | 否 | 提示没有模型历史 |
| `HistoryUnavailable` | AgentSession 不是可读取的内存历史 | 否 | 提示当前会话类型不支持压缩 |
| `EmptyHistory` | 内存历史存在但为空 | 否 | 提示没有可压缩内容 |
| `SkippedPendingToolCall` | 存在未配对工具调用，为保证历史完整而跳过 | 否 | 提示等待工具调用完成 |

异常和取消不放进该枚举：

- `OperationCanceledException` 继续向上传播；
- 模型调用、配置和 reducer 异常继续向上传播；
- Shell 继续通过异常路径显示取消或失败。

这样可以避免把失败伪装成一个普通成功结果。

### 5.2 压缩器级输出

建议新增 `CopilotChatReductionOutput`：

```csharp
public sealed record CopilotChatReductionOutput
{
    public required IReadOnlyList<ChatMessage> Messages { get; init; }

    public required IReadOnlyList<AIContent> SummaryContents { get; init; }

    public required bool WasReduced { get; init; }
}
```

属性语义：

- `Messages`：压缩器建议写回模型历史的完整消息；
- `SummaryContents`：本次压缩器明确生成、可供上层展示的 Assistant 内容；
- `WasReduced`：压缩器是否实际进行了压缩。

`SummaryContents` 不应由 Manager 扫描整个结果历史生成。它应来自压缩器本次模型响应中的 Assistant 内容。

容器应使用数组或其他独立只读快照，不应把内部可修改的 `List<T>` 直接暴露给调用方。

富压缩器输出还应遵守以下不变量：

- `WasReduced == false` 时，`Messages` 必须与输入保持相同的有序消息对象序列，且 `SummaryContents` 为空；
- `WasReduced == true` 时，`Messages` 必须是可提交的非空结果，并且不能与输入保持相同的有序消息对象序列；
- `Messages`、`SummaryContents` 或其中必需的数据为 `null` 时，Manager 在提交前抛出 `InvalidOperationException`；
- reducer 不得原位修改输入 `ChatMessage` 或其 `AIContent`。

Manager 应验证能够验证的容器和序列不变量。对于“不得原位修改”的约束，应通过接口文档和契约测试固定；现有消息模型没有通用深拷贝能力，不能依赖 Manager 自动修复恶意或错误实现。

### 5.3 AgentLib 自有的富压缩器接口

建议新增：

```csharp
public interface ICopilotChatReducer : IChatReducer
{
    Task<CopilotChatReductionOutput> ReduceWithResultAsync(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken cancellationToken);
}
```

设计理由：

- 继续实现框架 `IChatReducer`，不影响 `InMemoryChatHistoryProvider`；
- AgentLib 自己的编排可以取得结构化结果；
- 第三方只实现 `IChatReducer` 仍可继续工作；
- 不需要给框架类型伪造兼容层。

`CopilotChatManagerChatReducer` 应实现该接口：

- `ReduceWithResultAsync` 执行真实压缩并返回完整结果；
- 原有 `ReduceAsync` 调用 `ReduceWithResultAsync`，只返回 `Messages`。

对于内置全量摘要压缩器：

- `Messages` 为“前导 System 消息 + 模型响应消息”；
- `SummaryContents` 为本次 `ChatResponse.Messages` 中 Assistant 消息的内容；
- 空输入返回 `WasReduced = false`；
- 模型没有返回包含内容的 Assistant 消息时，应抛出明确的 `InvalidOperationException`，不得把原历史替换为只有 System Prompt 的不完整结果。

### 5.4 会话级结果

建议新增 `CopilotChatReductionResult`：

```csharp
public sealed record CopilotChatReductionResult
{
    public required CopilotChatReductionStatus Status { get; init; }

    public required IReadOnlyList<ChatMessage> ReducedMessages { get; init; }

    public required IReadOnlyList<AIContent> SummaryContents { get; init; }

    public required int OriginalMessageCount { get; init; }

    public int ReducedMessageCount => ReducedMessages.Count;

    public bool WasReduced => Status == CopilotChatReductionStatus.Reduced;

    public bool HasSummary => SummaryContents.Count > 0;
}
```

语义要求：

- `ReducedMessages` 表示操作结束后模型内部实际使用的消息快照；
- `Reduced` 时它是已写回 `AgentSession` 的新历史；
- `Unchanged`、`EmptyHistory` 和 `SkippedPendingToolCall` 时它是当前历史快照；
- `NoAgentSession`、`HistoryUnavailable` 时返回空列表；
- `SummaryContents` 只包含压缩器明确报告的本次摘要；
- 自定义 `ICopilotChatReducer` 可以在确实无法提供用户可见摘要时返回空的 `SummaryContents`，但仍必须准确报告 `WasReduced` 和完整结果历史。

不建议第一版加入完整原历史：

- Shell 不需要原历史；
- 会增加一次完整会话引用保留；
- 容易让上层开始自行回滚或修改 Agent 历史；
- 真正的事务回滚应由专门 API 负责。

### 5.5 新的 Manager 入口

建议增加：

```csharp
public Task<CopilotChatReductionResult> ReduceSessionWithResultAsync(
    AgentSession? agentSession,
    ICopilotChatReducer? chatReducer = null,
    CancellationToken cancellationToken = default)
```

该方法中 `agentSession == null` 应明确表示 `NoAgentSession`，不要隐式回退到 `SelectedSession.AgentSession`。

隐式回退只保留在旧兼容重载中。新 API 的参数语义应保持直接、可预测。

## 六、推荐的压缩执行算法

`ReduceSessionWithResultAsync` 建议按以下顺序执行。

### 6.1 前置检查

1. 调用 `cancellationToken.ThrowIfCancellationRequested()`；
2. `agentSession == null`：返回 `NoAgentSession`；
3. `TryGetInMemoryChatHistory` 失败：返回 `HistoryUnavailable`；
4. 复制当前消息容器为输入快照；
5. 消息数为零：返回 `EmptyHistory`；
6. 存在未配对 `FunctionCallContent`：返回 `SkippedPendingToolCall`。

未完成工具调用检查应从 `ToolCallAwareChatReducer` 的私有方法中提取为共享的内部辅助逻辑，使：

- 发送期间自动压缩；
- 手动结构化压缩；

使用同一套配对规则。

### 6.2 选择压缩器

- 未传 `chatReducer`：使用 `CopilotChatManagerChatReducer`；
- 传入 `ICopilotChatReducer`：调用 `ReduceWithResultAsync`；
- 只实现旧 `IChatReducer` 的压缩器不能传给该新入口，应继续使用现有兼容重载。

这个限制是有意的。结构化结果 API 的首要目标是返回可信语义，而不是让所有旧实现都能进入新签名。

### 6.3 取消与提交顺序

执行 reducer 后，应再次调用：

```text
cancellationToken.ThrowIfCancellationRequested()
```

只有该检查通过后才执行：

```text
agentSession.SetInMemoryChatHistory(...)
```

推荐顺序是：

1. 读取原历史；
2. 在独立列表上执行 reducer；
3. 物化并验证 reducer 输出；
4. 再次检查取消；
5. 一次性替换 AgentSession 历史；
6. 构造并返回结果。

这样可保证：

- reducer 调用期间取消不会写入半成品；
- reducer 抛异常时原历史不变；
- 输出枚举延迟执行时的异常发生在提交之前；
- AgentSession 只有一个明确的提交点。

需要注意，`ChatMessage` 自身仍是引用对象。应通过契约和测试禁止 reducer 原位修改输入消息，否则即使没有调用 `SetInMemoryChatHistory`，原历史对象也可能被改写。

### 6.4 结果判定

推荐判定如下：

- 富压缩器 `WasReduced == false`：返回 `Unchanged`，不调用 `SetInMemoryChatHistory`；
- 富压缩器 `WasReduced == true`：提交后返回 `Reduced`；

“已压缩”不应定义为“消息数量减少”。

## 七、摘要内容的准确来源

### 7.1 不扫描整个压缩后历史

现有无参 `ReduceSessionAsync` 会从整个压缩结果中取所有 Assistant 内容。该行为对当前全量摘要 reducer 通常有效，但不能成为新 API 的稳定定义。

例如，部分压缩器可能返回：

```text
原 System
原 User
原 Assistant
新摘要 Assistant
```

如果扫描全部 Assistant，就会把旧回复和新摘要一起展示。

因此，新 API 的 `SummaryContents` 必须由富压缩器在生成响应时明确提供。

### 7.2 多模态内容

`SummaryContents` 直接使用 `Microsoft.Extensions.AI.AIContent`，理由是：

- `AgentLib` 已公开依赖 `Microsoft.Extensions.AI`；
- 可以保留 `TextContent`、`DataContent` 及未来内容类型；
- 不需要复制一套容易落后的多模态 DTO；
- 该结果是运行时结果，不是持久化格式。

Shell 当前可通过：

```text
new CopilotChatMessage(ChatRole.Assistant, summaryContents)
```

复用已有文本、图片和音频投影。

但需要明确：当前 `CopilotChatMessage` 构造过程只把支持的内容类型转换为公开消息项。工具调用、用量等非展示内容不应被假定会自动显示。

`ReducedMessages` 可能包含内部 System Prompt。Shell 可以将它用于诊断或后续会话分支，但不得直接把完整列表展示给用户；用户可见内容只使用 `SummaryContents`，避免泄露内部指令。

### 7.3 多条摘要消息

第一版可继续把多个 Assistant 响应的 `AIContent` 按原顺序展平为一条公开 Assistant 消息，以保持现有行为。

如果未来需要保留多条消息边界，可在结果中增加 `SummaryMessages`，但第一版不必同时维护两套摘要表示。

## 八、现有 API 的兼容方式

### 8.1 仅压缩 AgentSession 的旧重载

现有：

```text
ReduceSessionAsync(AgentSession?, IChatReducer?, CancellationToken)
```

继续返回 `Task`，内部改为：

1. 保留现有 `null` 回退到 `SelectedSession.AgentSession` 的行为；
2. 未传 reducer 或传入 `ICopilotChatReducer` 时，调用 `ReduceSessionWithResultAsync` 并丢弃结果；
3. 传入只实现 `IChatReducer` 的旧压缩器时，保留现有私有兼容执行路径。

这样现有 `CodingChatApplication`、`ChatRoomRole` 和其他调用方不需要立即修改。

### 8.2 会向公开历史追加摘要的旧重载

现有：

```text
ReduceSessionAsync(IChatReducer?)
```

内部改为：

1. 未传 reducer 或传入富压缩器时，对当前选中会话调用新入口；
2. 优先使用 `result.SummaryContents`；
3. 若传入的是只实现旧 `IChatReducer` 的自定义压缩器，走现有兼容执行路径，并保持“扫描结果中所有 Assistant 内容”的旧行为；
4. 只有存在可展示内容时才追加“总结对话”和摘要；
5. 继续保留该 API 的产品行为，不把它作为新 Shell 的推荐入口。

兼容扫描只应存在于旧重载中，不能污染新结果 API 的语义。

### 8.3 自动历史压缩不需要立即改造

`SendMessage` 中的自动工具尾块压缩仍由 `InMemoryChatHistoryProvider` 调用 `IChatReducer`。

该路径不需要把每次自动压缩结果传给 Shell，因此可以继续使用框架接口。第一版只需：

- 复用共享的未完成工具调用检查；
- 保持现有自动压缩行为；
- 避免把自动工具尾块压缩与用户主动执行的全量对话压缩混为同一个产品动作。

## 九、CodingChatApplication 的推荐调整

### 9.1 应用级结果

建议在 AvaloniaShell 增加内部类型：

```csharp
internal sealed record CodingChatCompressionResult(
    Guid SessionId,
    CopilotChatReductionResult ReductionResult);
```

加入 `SessionId` 的原因是：

- 压缩结果属于某个明确会话；
- ViewModel 不应仅依赖“操作完成时当前仍选中哪个会话”；
- 后续增加预览、追加摘要或创建摘要会话时需要校验来源；
- 可以避免把旧结果误应用到另一个会话。

### 9.2 修改应用入口返回值

建议把：

```text
Task CompressConversationAsync(...)
```

改为：

```text
Task<CodingChatCompressionResult> CompressConversationAsync(...)
```

推荐流程：

1. 单独检查是否已有活动操作，存在时抛出 `InvalidOperationException`；不要继续使用当前把“活动操作”和“没有 AgentSession”合并在一起的 `if (!CanCompressConversation)` 作为入口校验；
2. 捕获当前 `CopilotChatSession`；
3. 设置 `_isCompressionActive = true`；
4. 调用 `ReduceSessionWithResultAsync(session.AgentSession, ...)`；
5. 只有 `WasReduced` 时才保存会话；
6. 保存成功后更新摘要列表；
7. 返回带原会话 ID 的结果；
8. 在 `finally` 中清除压缩状态。

“没有 AgentSession”应由结构化结果表达，而不是作为普通失败抛出。`CanCompressConversation` 仍可用于禁用按钮，但应用入口应能处理命令状态变化和测试直接调用。

### 9.3 保存时机

应用层应在返回 `Reduced` 结果之前完成：

- `AgentSessionState` 保存；
- 左侧会话摘要更新。

这样 ViewModel 收到 `Reduced` 时，可以认为压缩后的模型状态已经按现有存储能力提交。

建议继续用 `CancellationToken.None` 保存已经提交的模型状态：

- 取消发生在 Manager 提交前：抛出取消，不保存；
- Manager 已完成提交：保存不应因 UI 取消而中断。

现有文件存储不具备回滚 AgentSession 内存修改的事务能力。如果保存失败：

- 应用方法抛出异常；
- ViewModel 不显示摘要成功预览；
- 内存中的 AgentSession 可能已压缩，但磁盘仍是旧状态。

这是现有提交模型的限制，不应通过结果对象伪装为原子事务。若未来要求严格回滚，需要专门的会话状态事务或序列化快照方案。

## 十、ChatViewModel 如何消费结果

### 10.1 不再把所有非异常结果显示为“压缩完成”

当前 ViewModel 在应用调用未抛异常时统一显示“对话压缩完成”。结构化结果引入后，应按状态映射：

| 状态 | 建议文案 |
|---|---|
| `Reduced` 且有摘要 | 对话压缩完成，可查看摘要 |
| `Reduced` 但无摘要 | 对话已压缩，但压缩器未提供可展示摘要 |
| `Unchanged` | 当前对话无需压缩 |
| `NoAgentSession` | 当前会话没有模型历史 |
| `HistoryUnavailable` | 当前会话历史不支持压缩 |
| `EmptyHistory` | 当前会话没有可压缩内容 |
| `SkippedPendingToolCall` | 存在未完成的工具调用，本次未压缩 |

取消和异常继续走现有 `catch` 分支。

这些用户可见文案应放入现有 `Styles/Strings.axaml` 或后续统一的本地化资源，不在 ViewModel 中新增硬编码字符串。

### 10.2 推荐先做一次性预览

方案 B 的第一步建议是：

- 压缩成功后不修改 `ChatMessages`；
- ViewModel 保存一个只属于当前会话的临时预览对象；
- 弹窗、侧栏或聊天区外的摘要卡片展示该预览；
- 切换会话时清理，或按 `SessionId` 隔离。

预览可以复用 `CopilotChatMessage` 和 `MessageItemViewModel` 的现有多模态投影，但不要把预览对象直接加入会话公开消息集合。

这种方式可以先验证：

- API 是否返回了正确摘要；
- 多模态映射是否完整；
- 用户是否需要后续“追加到会话”或“以摘要新建会话”。

### 10.3 追加公开摘要必须回到应用层

如果后续提供“添加到当前会话”动作，不建议由 `ChatViewModel` 直接调用 `session.AddMessageAsync`。

原因是公开消息一旦修改，还需要：

- 校验结果所属 `SessionId`；
- 检查摘要是否存在；
- 添加预设消息；
- 保存会话；
- 更新会话摘要列表；
- 处理保存失败。

建议增加应用层方法，例如：

```text
PublishCompressionSummaryAsync(CodingChatCompressionResult result, CancellationToken)
```

该方法负责把摘要转换为：

1. 可选的预设用户消息“总结对话”；
2. 一条预设 Assistant 摘要消息；
3. 保存公开历史。

ViewModel 只发出动作，不直接负责持久化。

### 10.4 当前成功系统消息的持久化问题

当前流程是：

1. `CodingChatApplication` 先保存压缩后的会话；
2. `ChatViewModel` 再追加“对话压缩完成。”系统消息。

因此，这条成功消息不会被本次保存包含。

结构化结果方案不建议继续把操作状态写入公开会话。成功、跳过和失败更适合状态栏或通知；只有用户明确要求保留摘要时，才把摘要作为公开消息持久化。

## 十一、持久化设计

### 11.1 第一版不增加持久化字段

`CopilotChatSessionPersistenceData` 当前已经分开保存：

- `Messages`；
- `AgentSessionState`。

结构化压缩结果本身是操作结果，不是会话长期状态，因此第一版不需要增加：

- `LastReductionResult`；
- `SummaryContents`；
- `ReductionStatus`；
- `ReducedAt`。

### 11.2 不同展示方式的保存规则

| 展示方式 | 是否修改 ChatMessages | 是否需要新格式 |
|---|---:|---:|
| 只更新状态文案 | 否 | 否 |
| 一次性摘要预览 | 否 | 否 |
| 把摘要追加到当前会话 | 是 | 否，复用现有消息格式 |
| 独立摘要面板并跨重启恢复 | 否 | 是，未来另加字段 |
| 以摘要创建新会话 | 创建新会话消息和 AgentSession | 否，复用现有会话格式 |

### 11.3 结果对象不是持久化 DTO

不要把 `CopilotChatReductionResult` 直接序列化到会话文件：

- 它包含运行时 `ChatMessage` 和 `AIContent`；
- 状态中的“跳过”“空历史”是一次操作事实；
- 同一个会话可能反复压缩；
- 持久化真正需要的是最终公开消息和最终 AgentSession 状态。

## 十二、取消、失败与并发语义

### 12.1 取消

推荐保证：

- 调用前已取消：不调用 reducer；
- reducer 运行中取消：不替换历史；
- reducer 返回后发现取消：不替换历史；
- 历史已替换后：应用层完成不可取消保存；
- ViewModel 不把取消显示成 `Unchanged` 或 `Reduced`。

需要额外注意：`CopilotChatManagerToolCallChatReducer` 当前捕获所有 `Exception`，也会捕获取消异常。若继续保留自动压缩的 fail-open 策略，应让匹配当前令牌的 `OperationCanceledException` 重新抛出，只捕获明确允许降级的异常并记录日志，不能静默吞掉所有异常并把取消伪装成“原样返回”。

### 12.2 reducer 异常

新 Manager 入口不应吞掉异常：

- 模型配置失败；
- 网络失败；
- reducer 返回非法结果；
- 输出枚举物化失败；

都应在提交前抛出，保持原历史不变。

自动工具尾块 reducer 的“失败时原样返回”属于自动压缩的 fail-open 策略，不应默认套用到用户主动点击的全量压缩操作。

### 12.3 并发

`CodingChatApplication` 现有 `HasActiveOperation` 已能禁止：

- 压缩期间发送；
- 压缩期间切换会话；
- 压缩期间重复压缩；
- 压缩期间新建和删除会话。

结构化结果返回后，应继续使用结果中的 `SessionId` 校验预览或发布动作，避免结果跨会话误用。

## 十三、文件级修改建议

### 13.1 AgentLib 新增文件

建议新增：

- `AgentLib/Model/CopilotChatReductionStatus.cs`
  - 会话压缩状态枚举。
- `AgentLib/Model/CopilotChatReductionResult.cs`
  - 会话级结构化结果。
- `AgentLib/Model/CopilotChatReductionOutput.cs`
  - 富压缩器输出。
- `AgentLib/ICopilotChatReducer.cs`
  - AgentLib 自有富压缩器接口。
- `AgentLib/ChatHistoryReductionGuard.cs`
  - 未完成工具调用检查等共享安全规则。

所有公共类型和成员需要完整 XML 文档注释。

### 13.2 AgentLib 修改文件

- `AgentLib/CopilotChatManagerChatReducer.cs`
  - 实现 `ICopilotChatReducer`；
  - 直接返回本次模型响应中的摘要内容；
  - 保留 `IChatReducer.ReduceAsync` 兼容入口。
- `AgentLib/ToolCallAwareChatReducer.cs`
  - 复用共享未完成工具调用检查。
- `AgentLib/CopilotChatManager.cs`
  - 增加 `ReduceSessionWithResultAsync`；
  - 让两个旧重载改为调用新入口；
  - 旧公开消息重载保留兼容摘要提取。

第一版不要求修改 `CopilotChatManagerToolCallChatReducer` 的公开能力，但建议同步修正其取消异常处理。

### 13.3 AvaloniaShell 新增或修改文件

- `Services/CodingChatCompressionResult.cs`
  - 保存 `SessionId` 与 `CopilotChatReductionResult`。
- `Services/CodingChatApplication.cs`
  - 让压缩方法返回应用级结果；
  - 按结果决定是否保存；
  - 后续可增加发布摘要方法。
- `ViewModels/ChatViewModel.cs`
  - 按状态展示不同反馈；
  - 保存或清理一次性摘要预览；
  - 不直接解析 AgentSession。
- 对应 Avalonia 视图和资源文件
  - 只有实际实现摘要卡片、弹窗或发布按钮时才需要修改。

### 13.4 测试文件

- `AgentLib.Tests/CopilotChatManagerChatReducerTests.cs`
  - 增加结构化结果和状态测试。
- `AgentLib.Tests/CopilotChatManagerToolCallChatReducerTests.cs`
  - 保持自动 reducer 行为测试，并补取消语义。
- `CodingChatRoom.AvaloniaShell.Tests/CodingChatApplicationTests.cs`
  - 验证结果返回、保存和互斥。
- `CodingChatRoom.AvaloniaShell.Tests/ChatViewModelTests.cs`
  - 验证状态映射和摘要预览。

测试继续遵循现有 MSTest 约定，每个测试使用 `DisplayName` 和硬超时 `Timeout`。

## 十四、推荐实施顺序

### 阶段一：只改 AgentLib 结果能力

1. 增加状态、输出和结果类型；
2. 增加 `ICopilotChatReducer`；
3. 改造 `CopilotChatManagerChatReducer`；
4. 提取工具调用安全检查；
5. 实现 `ReduceSessionWithResultAsync`；
6. 用新入口重写旧重载；
7. 完成 AgentLib 单元测试。

该阶段完成后，现有 Shell 行为可以完全不变，但新 API 已可被独立验证。

### 阶段二：让 CodingChatApplication 返回结果

1. 增加 `CodingChatCompressionResult`；
2. 修改 `CompressConversationAsync` 返回结果；
3. 只在 `Reduced` 时保存；
4. 增加不同状态和保存失败测试。

### 阶段三：Shell 展示摘要预览

1. ViewModel 按状态显示准确文案；
2. `Reduced && HasSummary` 时创建一次性预览；
3. 切换会话时按 `SessionId` 清理或隔离预览；
4. 不修改 `ChatMessages`；
5. 增加 ViewModel 和 UI 测试。

### 阶段四：可选的公开摘要动作

1. 增加“添加到当前会话”动作；
2. 由 `CodingChatApplication` 追加并保存预设摘要消息；
3. 后续再复用同一结果实现“以摘要新建会话”。

分阶段实施可以避免一次改动同时涉及核心 API、持久化、UI 布局和新会话事务。

## 十五、测试清单

### 15.1 AgentLib 会话压缩结果

- `agentSession == null` 返回 `NoAgentSession`；
- 非内存历史返回 `HistoryUnavailable`；
- 空历史返回 `EmptyHistory`；
- 未完成工具调用返回 `SkippedPendingToolCall`；
- 跳过时 reducer 不被调用；
- 跳过时历史对象序列不变；
- 默认压缩器返回 `Reduced`；
- `OriginalMessageCount` 和 `ReducedMessageCount` 正确；
- `ReducedMessages` 与 AgentSession 实际历史一致；
- 开头连续 System 消息保持不变；
- `SummaryContents` 只来自本次模型响应；
- 多条 Assistant 响应的内容按顺序保留；
- `DataContent` 摘要不丢失；
- 富压缩器声明未改变时返回 `Unchanged`；
- 富压缩器声明未改变但返回了不同消息序列时拒绝提交；
- 富压缩器声明已压缩但返回相同消息序列时拒绝提交；
- 富压缩器声明已压缩但返回空消息时拒绝提交；
- 默认压缩器未返回有效 Assistant 摘要时原历史不变并抛出明确异常；
- reducer 抛异常时原历史不变；
- reducer 运行中取消时原历史不变；
- reducer 返回后令牌被取消时原历史不变；
- 返回列表容器不能直接修改 AgentSession 内部列表。

### 15.2 兼容 API

- 旧 AgentSession 重载仍能完成压缩；
- 旧重载传 `null` 时仍回退到当前选中会话；
- 旧公开消息重载仍追加“总结对话”和摘要；
- 旧公开消息重载使用富压缩器的精确摘要；
- 旧公开消息重载使用普通 `IChatReducer` 时保持兼容提取行为；
- `ChatRoomRole.ReduceSessionAsync` 和 `ChatRoomService.ReduceRoleSessionAsync` 无需改签名即可继续工作；
- 无摘要时不追加空消息。

### 15.3 CodingChatApplication

- 返回结果包含操作开始时的 `SessionId`；
- `Reduced` 时保存一次；
- `Unchanged` 和各跳过状态不保存；
- 压缩期间发送和会话命令禁用；
- 保存成功后才向 ViewModel 返回 `Reduced`；
- 保存失败时抛异常且不更新摘要列表；
- 取消不报告成功；
- 操作结束后总能清除 `_isCompressionActive`。

### 15.4 ChatViewModel

- 不同状态显示不同文案；
- `Reduced && HasSummary` 创建预览；
- `Reduced && !HasSummary` 不创建空预览；
- `SkippedPendingToolCall` 不显示压缩成功；
- 预览不加入 `ChatMessages`；
- 切换会话后不显示旧会话预览；
- 发布摘要动作通过应用层完成；
- 发布并保存失败时不留下未持久化的成功状态。

## 十六、风险与取舍

### 16.1 富压缩器接口增加了一层类型

代价是新增 `ICopilotChatReducer` 和输出类型，但它解决了 `IChatReducer` 无法表达摘要与是否压缩的根本限制。

只在 Manager 中继续做启发式推断，代码表面更少，但会把不可靠语义固化进公共 API，不建议采用。

### 16.2 第三方旧 reducer 不进入结构化入口

这是有意的兼容边界。

只实现 `IChatReducer` 的第三方压缩器仍可通过两个旧 `ReduceSessionAsync` 重载使用，但不能传给 `ReduceSessionWithResultAsync`。这样可以避免把无法证明的“是否压缩”和“哪些内容是摘要”伪装成精确结果。

如果第三方需要让 Shell 展示结构化摘要，应实现 `ICopilotChatReducer`。

### 16.3 返回的是浅层消息快照

数组可以保护列表容器，但 `ChatMessage` 和 `AIContent` 仍是引用对象。

第一版应明确结果只读契约，不允许 Shell 修改这些对象。若未来需要跨线程长期保存或编辑，应增加专用不可变 DTO，而不是直接扩展当前运行时结果。

### 16.4 保存失败不是跨层事务

Manager 提交模型历史和 Shell 保存会话之间没有事务。结构化结果能让状态更清晰，但不能自动解决跨层原子性。

不建议为了第一版摘要展示引入完整事务系统。应先记录并测试现有失败语义，再根据真实需求决定是否增加回滚能力。

## 十七、最终推荐

推荐按以下最小闭环落地：

1. 在 `AgentLib` 增加富压缩器接口和结构化结果；
2. 让 `CopilotChatManagerChatReducer` 直接报告本次摘要；
3. 增加 `ReduceSessionWithResultAsync`，统一处理安全检查、取消和历史提交；
4. 让旧重载的默认与富压缩器路径基于新入口实现，并为纯旧 `IChatReducer` 保留隔离兼容路径；
5. 让 `CodingChatApplication` 返回带 `SessionId` 的结果并完成保存；
6. Shell 第一版只展示一次性摘要预览，不修改公开历史；
7. 后续再增加由应用层持久化的“添加到当前会话”和“以摘要新建会话”动作。

该设计的核心原则是：

- reducer 负责说明自己生成了什么；
- Manager 负责说明会话压缩发生了什么；
- Application 负责说明该结果属于哪个会话并完成持久化；
- ViewModel 只决定如何展示，不解释或重建模型历史。

这样既能完成方案 B 的目标，也为后续摘要预览、新会话分支和更完整的压缩统计保留稳定扩展点。
