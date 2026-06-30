# 状态提升与 Session 复用探索

## 概述

本文档记录对"将 SlideML 状态提升到 `SlideGenerationPipeline`，并利用 `CopilotChatManager` 的 Session 机制实现状态跟随会话"这一思路的探索过程和结论。

探索目标：
1. 能否定义一个状态对象，表示当前 SlideML 内容（XML DOM 树、资源等），存放于 `SlideGenerationPipeline`
2. 将该状态传递到 `StreamingSlideGenerator`，使重试和跨轮对话都能延续
3. 能否复用 `CopilotChatManager` 的 Session 状态，让 SlideML 状态跟随会话走

---

## 探索过程

### 1. SlideML 状态包含哪些内容

通过代码审查，确认一次完整的 SlideML 流式生成过程涉及以下可变状态：

| 状态 | 持有者 | 生命周期 | 跨轮需求 |
|------|--------|----------|----------|
| XML DOM 树（XDocument） | `SlideMlStreamingMerger._document` | 单次生成 | 是——重试和跨轮都需要保留已合并的片段 |
| Id 索引（Dictionary<string, XElement>） | `SlideMlStreamingMerger._idIndex` | 单次生成 | 是——后续片段通过 Id 匹配已有元素 |
| StyleId 索引（Dictionary<string, XElement>） | `SlideMlStreamingMerger._styleIdIndex` | 单次生成 | 是——StyleFrom 引用需要查找已有 StyleId |
| 悬空元素列表（List<XElement>） | `SlideMlStreamingMerger._danglingElements` | 单次生成 | 是——样式模板元素跨轮有效 |
| 诊断信息（Warnings / Errors） | `SlideMlPipelineContext._warnings / _errors` | 每轮独立 | 否——每轮重试应清空 |
| 素材资源管理器 | `SlideMlPipelineContext.MaterialResourceManager` | 单次生成 | 是——已注册的素材（图片等）跨轮有效 |
| 文档级配置（画布尺寸等） | `SlideMlPipelineContext.SlideDocumentContext` | 不可变 | 是——画布尺寸不变 |
| 片段提取器缓冲区 | `SlideMlFragmentExtractor._buffer` | 每轮独立 | 否——每轮重试新建，残留缓冲区无意义 |
| 渲染服务状态（预览图、回填 XML） | `SlideStreamRenderService` | 单次生成 | 是——LLM 工具和 UI 需要读取最新渲染结果 |
| 渲染回填后的 XML | `SlideMlRenderTool.LatestRenderedXml` | 跨整个应用 | 是——跨轮对话的初始 XML 来源 |

**结论**：SlideML 状态的核心是 `SlideMlStreamingMerger`（DOM 树 + 索引）和 `SlideMlPipelineContext`（资源管理器 + 文档配置），外加 `SlideStreamRenderService` 的渲染状态。

### 2. 当前 SlideGenerationPipeline 的状态现状

`SlideGenerationPipeline` 当前持有的状态：

```
SlideGenerationPipeline
  ├─ _copilotChatManager: CopilotChatManager（引用，非自身状态）
  ├─ _promptProvider: ISlideMlPromptProvider（引用，无状态）
  ├─ SlideMlRenderTool: SlideMlRenderTool（有状态——Latest* 属性）
  │    ├─ LatestPreviewImage
  │    ├─ LatestRenderedXml
  │    ├─ LatestSlideXml
  │    └─ LatestWarnings
  ├─ _lastSlideEvaluation: SlideEvaluationResult?（评估结果快照）
  └─ _lastPromptEvaluation: PromptEvaluationResult?（评估结果快照）
```

当前 `SlideGenerationPipeline` **没有**持有 `SlideStreamingPipeline` 或 `SlideMlStreamingMerger` 实例。每次 `SendMessageAsync(useStreaming: true)` 都创建新的 `StreamingSlideGenerator`，后者在 `RunStreamingLoopAsync` 中创建新的 `SlideStreamingPipeline`。

`SlideMlRenderTool` 的 `Latest*` 属性是当前唯一跨轮保留的状态——它保存了最近一次渲染的回填 XML。但这是"渲染结果"而非"合并器状态"：它不包含 Id 索引、StyleId 索引、悬空元素等合并器内部状态。

### 3. CopilotChatManager 的 Session 机制

通过代码审查确认 `CopilotChatManager` 的会话管理机制：

```
CopilotChatManager
  ├─ ChatSessions: ObservableCollection<CopilotChatSession>（所有会话）
  ├─ SelectedSession: CopilotChatSession（当前选中会话）
  │    ├─ SessionId: Guid
  │    ├─ ChatMessages: ObservableCollection<CopilotChatMessage>（可见消息列表）
  │    └─ AgentSession: AgentSession?（代理会话）
  │         └─ InMemoryChatHistoryProvider（内部对话历史，跨 RunStreamingAsync 调用保留）
  └─ CreateNewSession()（创建新会话并切换）
```

`CreateManualSendMessageContextAsync` 的关键实现（第 546-568 行）：

```csharp
public async Task<IManualSendMessageContext> CreateManualSendMessageContextAsync(...)
{
    CopilotChatSession currentSession = SelectedSession;
    // ...创建空壳消息...
    return new ManualSendMessageContext
    {
        ChatManager = this,
        Session = currentSession,  // ← 使用当前选中会话
        // ...
    };
}
```

`ManualSendMessageContext.GetAgentSessionAsync`（第 97-114 行）：

```csharp
public async Task<AgentSession> GetAgentSessionAsync(CancellationToken ct)
{
    if (_agentSession is not null) return _agentSession;
    if (Session.AgentSession is { } existingSession)
    {
        _agentSession = existingSession;  // ← 复用已有 AgentSession
        return _agentSession;
    }
    // 首次创建
    _agentSession = await chatClientAgent.CreateSessionAsync(ct);
    Session.SetAgentSession(_agentSession);
    return _agentSession;
}
```

**关键发现**：

1. `AgentSession` 通过 `CopilotChatSession.AgentSession` 属性持久化，跨多次 `RunStreamingAsync` 调用复用。这意味着 LLM 的对话历史（`InMemoryChatHistoryProvider`）是自动跨轮保留的——重试和跨轮对话时 LLM 能看到之前所有对话。

2. `CopilotChatSession` 本身只保存聊天消息列表和 `AgentSession` 引用，不保存任何 SlideML 特有的状态（DOM 树、索引等）。

3. `CopilotChatManager` 的 `SelectedSession` 在不调用 `CreateNewSession()` 的情况下保持不变，因此跨轮对话默认复用同一会话。

### 4. 方案 A：在 SlideGenerationPipeline 中提升状态

**思路**：在 `SlideGenerationPipeline` 中新增一个状态对象（如 `SlideStreamingState`），持有 `SlideStreamingPipeline` 和 `SlideMlPipelineContext` 实例。每次流式生成时复用这些实例。

```
SlideGenerationPipeline
  └─ StreamingState: SlideStreamingState?（新增）
       ├─ Pipeline: SlideStreamingPipeline（跨轮复用）
       │    └─ _merger: SlideMlStreamingMerger（DOM 树 + 索引跨轮保留）
       └─ Context: SlideMlPipelineContext（跨轮复用，每轮 Reset 诊断信息）
```

**可行性分析**：

| 方面 | 评估 |
|------|------|
| 重试场景 | 完全可行——`GenerateAsync` 的重试循环直接使用 `StreamingState.Pipeline`，不再每轮新建 |
| 跨轮对话 | 可行——`SendMessageAsync` 每次调用复用同一 `StreamingState`，merger 保留上一轮 DOM 树 |
| 新会话 | 需要处理——当 `createNewSession` 为 `true` 或 `isFirstMessage` 为 `true` 时，应重置 `StreamingState` |
| 设计影响 | **`SlideGenerationPipeline` 从无状态变为有状态**——只能用于单页生成。如果要支持多页（未来），需要每页一个状态实例 |
| 与一次性模式的共存 | 无冲突——一次性模式不走 `useStreaming` 分支，不触碰 `StreamingState` |

**关键决策点**：是否接受 `SlideGenerationPipeline` 变为有状态？

当前 `SlideGenerationPipeline` 已经通过 `SlideMlRenderTool.Latest*` 属性隐式持有渲染状态。新增 `StreamingState` 只是将流式合并状态也显式持有，设计上是连贯的。限制为"单页生成"在当前阶段是可接受的——项目仍处于单页实验阶段（系统提示词中明确写"当前只需要生成单页"）。

### 5. 方案 B：复用 CopilotChatSession 携带状态

**思路**：将 SlideML 状态挂载到 `CopilotChatSession` 上，让状态跟随会话走。切换会话时自动切换状态，新建会话时自动创建新状态。

**可行性分析**：

`CopilotChatSession` 位于 `AgentLib` 项目中，是通用的聊天会话模型，不包含任何 SlideML 特有概念。将 SlideML 状态直接加到 `CopilotChatSession` 上有以下问题：

| 问题 | 说明 |
|------|------|
| 职责侵入 | `CopilotChatSession` 是 AgentLib 的通用组件，不应耦合 PptxGenerator 的 SlideML 概念 |
| 依赖方向 | AgentLib 不依赖 PptxGenerator.Core，无法引用 `SlideStreamingPipeline` 等类型 |
| 通用性丧失 | 其他使用 AgentLib 的项目（如 DeepSeekWpf）不需要 SlideML 状态 |

**替代方案 B'**：通过 `CopilotChatSession` 的扩展机制（如 `Tag` 属性或外部 `Dictionary<SessionId, SlideStreamingState>` 映射）关联状态，不修改 `CopilotChatSession` 本身。

| 方面 | 评估 |
|------|------|
| 可行性 | 可行——在 `SlideGenerationPipeline` 或 `SlideChatManager` 中维护 `Dictionary<Guid, SlideStreamingState>`，以 `SessionId` 为键 |
| 优势 | 状态真正跟随会话——切换会话时状态自动隔离，新建会话时自动缺失（按需创建） |
| 劣势 | 增加一层间接映射，管理复杂度上升；需要处理会话删除时的状态清理 |
| 当前需求 | 当前只有单会话场景，多会话切换尚未实现 |

**关键发现**：`AgentSession` 的对话历史已经通过 `CopilotChatSession.AgentSession` 实现了跨轮保留。这是 LLM 上下文层面的状态延续。但 SlideML 合并器状态（DOM 树、索引）是**引擎层面**的状态，与 LLM 对话历史是两个不同维度：

- LLM 上下文（AgentSession）：LLM 看到的对话历史，包含之前的用户消息、助手回复、工具调用结果
- 引擎状态（SlideMlStreamingMerger）：合并器维护的 XML DOM 树和 Id 索引

两者需要协同：LLM 上下文告诉模型"之前做了什么"，引擎状态告诉合并器"当前页面长什么样"。如果 LLM 上下文被 ChatReducer 压缩，LLM 可能丢失之前的输出细节，但引擎状态仍然保留完整 DOM 树——这正是引擎状态独立存在的价值。

### 6. 方案 C：混合方案——SlideGenerationPipeline 持有状态 + Session 关联

**思路**：将状态对象提升到 `SlideGenerationPipeline` 中作为单实例字段（方案 A），同时在 `SlideGenerationPipeline` 中监听 `CopilotChatManager` 的会话切换事件，在会话切换时重置状态。未来如需多会话独立状态，再扩展为 SessionId 映射（方案 B'）。

```
SlideGenerationPipeline
  ├─ _streamingState: SlideStreamingState?（单实例，当前会话的状态）
  └─ SendMessageAsync(...)
       ├─ if isFirstMessage 或 createNewSession → _streamingState = null（重置）
       ├─ _streamingState ??= new SlideStreamingState(...)
       └─ 传递 _streamingState 给 StreamingSlideGenerator
```

**优势**：

1. 最小改动量——不需要修改 `CopilotChatSession` 或 `CopilotChatManager`
2. 状态生命周期清晰——`isFirstMessage` 为 `true` 时重置，为 `false` 时复用
3. 与现有 `SlideMlRenderTool.Latest*` 状态管理模式一致
4. 未来可扩展——需要多会话独立状态时，将 `_streamingState` 改为 `Dictionary<Guid, SlideStreamingState>`

### 7. 状态对象的定义

综合以上分析，状态对象应封装流式生成所需的可变状态：

```csharp
/// <summary>
/// 流式生成的可变状态，跨重试轮次和跨轮对话复用。
/// 包含合并器 DOM 树、Id 索引、资源管理器和渲染服务状态。
/// </summary>
internal sealed class SlideStreamingState
{
    /// <summary>
    /// 流式渲染管道（包含 SlideMlStreamingMerger）。
    /// 跨轮复用，保留 DOM 树和 Id/StyleId 索引。
    /// </summary>
    public SlideStreamingPipeline Pipeline { get; }

    /// <summary>
    /// 渲染上下文（包含诊断信息和资源管理器）。
    /// 跨轮复用，每轮重试前调用 Reset() 清空诊断信息。
    /// </summary>
    public SlideMlPipelineContext Context { get; }

    public SlideStreamingState(
        ISlideMlPromptProvider promptProvider,
        ISlideMlRenderPipeline renderPipeline,
        IMainThreadDispatcher dispatcher)
    {
        Pipeline = new SlideStreamingPipeline(promptProvider, renderPipeline, dispatcher);
        Context = new SlideMlPipelineContext();
    }
}
```

### 8. 悬空元素的序列化问题

探索中发现一个关键问题：`SlideMlStreamingMerger.GetMergedXml()` 只返回 `_document.ToString()`，不包含 `_danglingElements` 中的悬空元素。

这意味着：
- 如果使用方案 A/C（直接复用 merger 实例），悬空元素自然保留——不需要序列化
- 如果使用"从 XML 字符串恢复"的方案（第一版修复计划），悬空元素会丢失——因为 `GetMergedXml()` 不包含它们

**这进一步验证了方案 A/C 的优势**：直接复用 merger 实例比从 XML 字符串恢复更完整，不会丢失悬空元素状态。

### 9. AgentSession 对话历史的角色

`AgentSession` 的 `InMemoryChatHistoryProvider` 自动保留所有对话历史（用户消息、助手回复、工具调用结果）。跨轮对话时：

1. 第一轮：用户发送"生成标题页" → LLM 输出 XML 片段 → 助手消息追加到历史
2. 第二轮：用户发送"把标题改成红色" → LLM 在历史中看到之前的对话 → 理解当前页面状态

但存在一个风险：`ChatReducer`（`CopilotChatManagerToolCallChatReducer`）可能在上下文过长时压缩历史。压缩后 LLM 可能丢失之前输出的 XML 细节（如元素 Id），导致无法精确做增量修改。

**缓解措施**：在非首轮消息的提示词中附加当前合并 XML 的摘要，或提供 `get_slide_state` 工具供 LLM 查询当前状态。但这是提示词优化层面的问题，不影响状态管理架构的设计。

---

## 结论

### 推荐方案：方案 C（混合方案）

在 `SlideGenerationPipeline` 中持有 `SlideStreamingState` 单实例，通过 `isFirstMessage` / `createNewSession` 控制状态重置。不修改 `CopilotChatSession` 或 `CopilotChatManager`。

**理由**：

1. **AgentSession 已自动处理 LLM 上下文延续**——对话历史通过 `CopilotChatSession.AgentSession.InMemoryChatHistoryProvider` 自动跨轮保留，无需额外处理。`CreateManualSendMessageContextAsync` 使用 `SelectedSession`，`GetAgentSessionAsync` 复用 `Session.AgentSession`，整个链路天然支持跨轮对话。

2. **引擎状态需要独立管理**——`SlideMlStreamingMerger` 的 DOM 树和索引不属于 LLM 上下文范畴，是引擎内部的增量合并状态。直接复用 merger 实例比从 XML 字符串恢复更完整（特别是悬空元素不会丢失）。

3. **最小改动量**——不需要修改 AgentLib，不需要引入 SessionId 映射，只需要在 `SlideGenerationPipeline` 中新增一个字段，在 `StreamingSlideGenerator` 中改为接收外部传入的状态。

4. **与现有设计一致**——`SlideMlRenderTool` 已经是 `SlideGenerationPipeline` 持有的有状态对象，新增 `SlideStreamingState` 是同一模式的延伸。

### 与第一版修复计划的差异

| 对比项 | 第一版修复计划 | 本方案（方案 C） |
|--------|---------------|-----------------|
| 状态传递方式 | 通过 `initialXml` 参数传递 XML 字符串，每次新建 merger 并 `LoadFromXml` | 直接复用 merger 实例，不涉及序列化/反序列化 |
| 悬空元素 | 丢失（`GetMergedXml` 不含悬空元素） | 保留（直接复用实例） |
| Id/StyleId 索引 | 需要 `LoadFromXml` 重建 | 自然保留（同一实例） |
| 资源管理器 | 每次新建，已注册素材丢失 | 跨轮保留 |
| 改动范围 | 需要 `LoadFromXml` 新方法 + 多处传参 | 状态对象提升到 Pipeline，StreamingSlideGenerator 接收外部状态 |
| 跨轮对话 | 传入 `LatestRenderedXml` 作为 `initialXml` | 直接复用 `StreamingState`，无需传 XML |

### 实施方向（概要）

1. 新增 `SlideStreamingState` 内部类，封装 `SlideStreamingPipeline` + `SlideMlPipelineContext`
2. `SlideGenerationPipeline` 新增 `_streamingState` 字段，在 `isFirstMessage`/`createNewSession` 时重置
3. `StreamingSlideGenerator.GenerateAsync` 改为接收 `SlideStreamingState` 参数
4. `RunStreamingLoopAsync` 使用 `StreamingState.Pipeline` 和 `StreamingState.Context`，不再自行创建
5. 重试循环中 `Context.Reset()` 清空诊断信息但保留 merger 状态
6. `BuildErrorFeedback` 措辞改为"仅输出修正和后续片段"

详细实施步骤将在确认方案后更新到修复计划文档中。
