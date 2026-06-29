# SlideML 流式输出实现计划

## 概述

本文档是 [SlideML 流式输出规范](./SlideML%20流式输出规范.md) 和 [SlideML V3 规范文档](./SlideML%20V3%20规范文档.md) 第二部分（§§9-16）的工程实现方案。

流式输出面向 LLM 以 Flash 模式无思考连续生成的场景：模型持续输出 XML 片段序列，系统需要实时接收 token 流、增量提取 XML 片段、逐片段合并到元素树，并在每次合并成功后立即触发实时渲染，让用户看到页面变化，同时提供 AI Tool 供 LLM 随时读取当前页面状态（回填 XML + 预览图）。

---

## 现有架构分析

### 现有调用链路（一次性模式）

```
SlideGenerationPipeline.SendMessageAsync
  → CopilotChatManager.SendMessage(SendMessageRequest)
    → AgentFramework 内部 RunStreamingAsync（按 chunk 接收 AgentResponseUpdate）
    → LLM 生成完整 XML 后调用 render_slide tool
    → SlideMlRenderTool.RenderSlideAsync
      → SlideMlRenderPipeline.RenderAsync
        → SlideMlParser.Parse（一次性完整解析）
        → 布局引擎 PreLayout / FinalLayout
        → 渲染引擎 PreMeasure / Render
```

### 流式相关基础设施

| 组件 | 位置 | 说明 |
|------|------|------|
| `CopilotChatMessage.TextAppended` | `AgentLib\Model\CopilotChatMessage.cs` | 每次 `AppendText` 调用时触发，参数为增量文本。这是 LLM 输出的 token 级增量 |
| `CreateManualSendMessageContextAsync` | `AgentLib\CopilotChatManager.cs` | 提供裸 `IChatClient` + 延迟创建的 `ChatClientAgent`（支持 `RunStreamingAsync` 返回 `IAsyncEnumerable<AgentResponseUpdate>`）+ 空壳 `CopilotChatMessage` + `StartChatting()` 状态管理。**当前 PptxGenerator 代码中未使用此方法**，正是流式模式的切入点 |
| `IManualSendMessageContext.AppendResponseUpdate` | `AgentLib\Model\SendMessages_\IManualSendMessageContext.cs` | 将 `AgentResponseUpdate` 追加到 `AssistantChatMessage`，内部调用 `AppendText` → 触发 `TextAppended` |

### 模型不可变性

所有模型类使用 `{ get; init; }` 不可变属性：

- `SlideMlElement`（基类）：`Id`、`X`、`Y`、`Width`、`Height` 等
- `SlideMlPanelElement`：`Padding`、`Background`、`Layout`、`Gap`、`Children`
- `SlideMlRectElement`：`Fill`、`Stroke`、`StrokeThickness`、`CornerRadius` 等
- `SlideMlTextElement`：`Text`、`FontName`、`FontSize`、`Foreground` 等
- `SlideMlImageElement`：`Source`、`Stretch`

**处理策略：不修改现有模型类。** 流式解析器工作在 `XElement` 层面，维护一棵可变 XML DOM 树，所有 Merge 操作在 XML DOM 上进行。流结束时将合并完成的 `XElement` 树序列化为完整 XML 字符串，交给现有 `SlideMlParser.Parse` 完成最终解析。

---

## 四层架构

```
┌─────────────────────────────────────────────────────────────────┐
│  SlideStreamingPipeline（编排层）                                │
│  - 使用 CreateManualSendMessageContextAsync 获取流式上下文       │
│  - 调用 ChatClientAgent.RunStreamingAsync                        │
│  - 在 TextAppended 回调中实时驱动提取→合并→渲染                  │
│  - 每次片段合并后立即触发实时渲染，更新状态供 LLM 工具读取        │
│  - 注册 SlideStreamToolProvider 提供的 AI Tool 供 LLM 调用       │
└──────────────┬──────────────────────────────────────────────────┘
               │ 增量文本
┌──────────────▼──────────────────────────────────────────────────┐
│  SlideMlFragmentExtractor（片段提取层）                           │
│  - 维护一个 StringBuilder 缓冲区                                 │
│  - Append(token) 追加增量文本                                     │
│  - TryExtractFragments() 尝试从缓冲区中切分出完整的顶层元素       │
│    使用深度计数器，识别 </Tag> 闭合的完整片段                      │
│  - 返回 List<string>（0~N 个完整片段），残留文本保留在缓冲区      │
└──────────────┬──────────────────────────────────────────────────┘
               │ 完整 XML 片段
┌──────────────▼──────────────────────────────────────────────────┐
│  SlideMlStreamingMerger（合并层）                                 │
│  - 维护 XDocument（可变 XML DOM 树）                              │
│  - 维护 Dictionary<string, XElement> 全局 Id 索引                 │
│  - AcceptFragment(string xml) 解析片段并合并                      │
│    ├─ <Page>          → 更新页面属性，合并子元素                  │
│    ├─ <Panel/Rect/...> → 按 Id 查找并 Merge 或新增               │
│    ├─ <Remove>        → 从树中删除目标元素                        │
│    └─ 悬空元素         → 仅登记到 Id 索引                         │
│  - GetMergedXml() 输出当前合并状态的 XML（随时可调用）            │
└──────────────┬──────────────────────────────────────────────────┘
               │ 合并后的 XML
┌──────────────▼──────────────────────────────────────────────────┐
│  SlideStreamRenderService（实时渲染层）                           │
│  - 接收合并后的 XML → 调用 SlideMlRenderPipeline.RenderAsync      │
│  - 渲染节流：避免每个片段都触发渲染（可配置最小间隔）              │
│  - 维护 LatestRenderedXml / LatestPreviewImage 供 LLM 工具读取    │
│  - 触发 SlideRendered 事件通知 UI 更新                            │
│  - 提供 AI Tool（get_slide_state / get_slide_preview）供 LLM 调用 │
└─────────────────────────────────────────────────────────────────┘
```

### 数据流（实时处理）

```
LLM token 流
  │
  ├─ AgentResponseUpdate → AppendResponseUpdate → AppendText → TextAppended 事件
  │                                                                     │
  │           ┌─────────────────────────────────────────────────────────┘
  │           ▼
  │  SlideMlFragmentExtractor.Append(token)
  │           │
  │           ▼ TryExtractFragments()
  │  List<string> 完整片段（0~N 个）
  │           │
  │           ▼ 逐个片段
  │  SlideMlStreamingMerger.AcceptFragment(fragmentXml)
  │           │
  │           ▼ （XElement Merge → 属性覆盖 + 子元素排序算法）
  │  更新 XDocument + Id 索引
  │           │
  │           ▼ 节流判断（距上次渲染 > MinRenderInterval？）
  │  SlideStreamRenderService.TryRender(merger.GetMergedXml())
  │           │
  │           ▼ SlideMlRenderPipeline.RenderAsync(currentXml)
  │  更新 LatestRenderedXml + LatestPreviewImage
  │  触发 SlideRendered 事件 → UI 实时刷新
  │           │
  │           ▼ LLM 可随时调用工具读取
  │  get_slide_state → 返回当前回填 XML + 警告
  │  get_slide_preview → 返回当前预览图 PNG
  │
  └─ EOF（流结束）
       │
       ▼
  最终渲染（确保最后一次合并一定渲染）
  → 返回最终 SlideMlRenderResult
```

**核心变化**：渲染不再只在流结束时做一次，而是**在每个片段合并后实时触发**（带节流）。LLM 在流式输出过程中可以通过 `get_slide_state` 和 `get_slide_preview` 工具随时获取当前页面状态。

---

## 关键设计决策

1. **不修改** 任何现有模型类、`SlideMlParser`、`SlideMlRenderPipeline`、`SlideGenerationPipeline`
2. Merge 操作在 `XElement` DOM 上进行，最终通过 `SlideMlParser.Parse` 完成解析
3. **实时渲染**：每合并一个片段后立即触发渲染（带节流），用户可实时看到页面变化，LLM 可通过工具随时读取当前状态
4. **提供 AI Tool**：流式模式下注册 `get_slide_state`（获取当前回填 XML + 警告）和 `get_slide_preview`（获取当前预览图）工具供 LLM 调用，使 LLM 能在输出过程中自主检查排版效果
5. `SlideMlFragmentExtractor` 使用深度计数器算法处理 token 在标签中间断开的情况
6. 现有一次性模式与流式模式并存，Demo 可选择使用哪种

---

## 实施步骤

### 步骤 1：SlideMlFragmentExtractor（片段提取层）

**文件**：`Code/PptxGenerator.Core/Streaming/SlideMlFragmentExtractor.cs`（新建）

**职责**：从 LLM token 流中增量切分出完整 XML 片段。

**核心 API**：

```csharp
public sealed class SlideMlFragmentExtractor
{
    // 追加增量文本到内部缓冲区
    public void Append(string text);

    // 尝试从缓冲区中提取完整的顶层 XML 元素
    // 返回 0~N 个完整片段，残留文本保留在缓冲区
    public List<string> TryExtractFragments();

    // 获取未完成的缓冲区内容（用于 EOF 时的容错处理）
    public string GetRemaining();
}
```

**算法**：深度计数器

- 维护 `StringBuilder` 缓冲区
- 扫描文本，遇到 `<TagName` 深度 +1（排除 `<?xml` 声明和注释 `<!-- -->`）
- 遇到 `</TagName>` 深度 -1
- 遇到自闭合 `<Tag/>` 深度归零
- 深度归零时切分出一个完整片段
- 处理 token 在标签中间断开的情况（如 `<Panel Id="hea` + `der">`）

### 步骤 2：SlideMlStreamingMerger（合并层）

**文件**：`Code/PptxGenerator.Core/Streaming/SlideMlStreamingMerger.cs`（新建）

**职责**：维护 `XDocument` DOM 树 + Id 索引，逐片段合并。

**核心 API**：

```csharp
public sealed class SlideMlStreamingMerger
{
    // 接收一个完整 XML 片段并合并到 DOM 树
    public void AcceptFragment(string fragmentXml, SlideMlPipelineContext context);

    // 输出合并完成的完整 XML 字符串
    public string GetMergedXml();

    // 清空状态以复用
    public void Reset();
}
```

**内部状态**：

- `XDocument` — 文档 DOM 树，根节点为 `<Page>`
- `Dictionary<string, XElement>` — 全局 Id 索引，O(1) 查找
- `List<XElement>` — 悬空元素列表（不在 Page 子树内，仅供 StyleFrom 引用）

**片段分发逻辑**：

```
AcceptFragment(fragmentXml, context)
  │
  ├─ XDocument.Parse(fragmentXml) 解析片段
  │
  ├─ 根元素为 Page
  │    ├─ 首次出现 → 创建 XDocument 根节点
  │    └─ 后续出现 → 合并属性到根节点 + 合并子元素
  │
  ├─ 根元素为 Panel/Rect/TextElement/Image
  │    ├─ Id 在索引中存在 → 属性 Merge + 子元素 Merge
  │    └─ Id 不存在 → 新增元素，注册到索引
  │         ├─ 在 Page 子树内 → 插入到对应父容器
  │         └─ 不在 Page 子树内 → 悬空元素
  │
  └─ 根元素为 Remove
       └─ 从索引和父节点中移除目标元素及其子树
```

### 步骤 3：子元素 Merge 算法（§14）

**位置**：`SlideMlStreamingMerger` 内部方法 `MergeChildren`

**规范引用**：[SlideML V3 规范文档 §14](./SlideML%20V3%20规范文档.md) / [流式输出规范](./SlideML%20流式输出规范.md)

**算法**：

```
给定：
  当前子元素列表 L = [e₁, e₂, e₃, ...]
  片段子元素列表 F = [f₁, f₂, f₃, ...]

步骤：

① 定位锚点
   从 f₁ 开始依次检查 F 中每个元素：
     如果该元素的 Id 在 L 中也存在 → 记这个 Id 在 L 中的位置为 P，停止检查。
   如果 F 中所有元素的 Id 在 L 中都不存在 → P = |L|（L 的末尾位置）。

② 移除冲突
   遍历 L，对于 L 中的每个元素：
     如果它的 Id 在 F 中也出现了 → 从 L 中删掉它。
     否则保留。

③ 插入
   把 F 整个列表插入到 L 的位置 P 处。
   若 P > |L| → 追加到末尾。
```

**实现要点**：

- 操作 `XElement` 子节点（`XContainer.Nodes()` / `XContainer.Add()` / `XContainer.RemoveNodes()`）
- 新增子元素注册到 Id 索引
- 递归处理子元素的子元素（深层 Merge）
- 同片段内重复 Id 检测 → `[Error]`

**容器无子元素规则（§15）**：

片段中出现的容器元素不含子元素时，仅执行属性 Merge，现有子元素保持不动。`<Panel Id="x"/>` 和 `<Panel Id="x"></Panel>` 在 XML 语义上等价，解析器不区分两者。

### 步骤 4：错误处理（§16 容错续行）

**位置**：`SlideMlStreamingMerger` 内部

**规范引用**：[SlideML V3 规范文档 §16](./SlideML%20V3%20规范文档.md)

| 错误类型 | 处理方式 |
|----------|----------|
| XML 格式错误（标签未闭合、属性语法错误等） | `try-catch` 包裹 `XDocument.Parse`，报 `[Error]` 并附位置信息，跳过该片段，继续解析后续内容 |
| 元素缺少 `Id` | 报 `[Error]`，跳过该元素（Page 除外） |
| 同一个 `Id` 出现在文档树的两个不同父容器下 | 报 `[Error]`（检测：新元素的 Id 已在索引中，但其 Parent != 当前目标容器） |
| 同一个片段内出现两个相同 `Id` | 报 `[Error]` |
| `<Remove>` 的 `TargetId` 不存在 | 报 `[Warning]`，忽略该操作 |

### 步骤 5：StyleFrom 支持（§12）

**位置**：`SlideMlStreamingMerger` 内部

**规范引用**：[SlideML V3 规范文档 §12](./SlideML%20V3%20规范文档.md)

**逻辑**：

1. 解析片段元素时，检查 `StyleFrom` 属性
2. 如果存在，从 Id 索引中查找源元素
3. 复制源元素的全部属性到当前元素（作为默认值）
4. 再用当前元素显式声明的属性覆盖
5. 不复制子元素
6. 源元素不存在 → `[Error]` + 移除 StyleFrom 属性继续处理

**优先级链**：`StyleFrom` 源属性 < 元素显式声明的属性 < 后续片段 Merge 的属性

### 步骤 6：SlideStreamingPipeline（编排层）

**文件**：`Code/PptxGenerator.Core/Streaming/SlideStreamingPipeline.cs`（新建）

**职责**：对接 `CopilotChatManager.CreateManualSendMessageContextAsync`，驱动整个流式生命周期，集成中断-纠错逻辑（详见 [流式中断与纠错反馈](#流式中断与纠错反馈) 章节）。

**依赖**：

- `CopilotChatManager` — 获取流式上下文
- `ISlideMlRenderPipeline` — 实时渲染（每个片段合并后触发）
- `ISlideMlPromptProvider` — 构建流式模式提示词
- `SlideStreamInterruptionController` — 中断控制（步骤 6a）
- `SlideStreamRenderService` — 实时渲染服务 + AI Tool 提供者（步骤 6c）

**核心 API**：

```csharp
public sealed class SlideStreamingPipeline
{
    public SlideStreamingPipeline(
        CopilotChatManager copilotChatManager,
        ISlideMlRenderPipeline renderPipeline,
        ISlideMlPromptProvider promptProvider);

    // 流式生成幻灯片（含实时渲染 + 自动中断-纠错重试）
    public Task<SlideMlRenderResult> StreamSlideAsync(
        string userPrompt,
        CancellationToken cancellationToken = default);

    // 渲染完成事件（每次实时渲染后触发，UI 可订阅以刷新预览）
    public event Action? SlideRendered;
}
```

**`StreamSlideAsync` 流程**（简化版，完整中断-纠错流程见 [流式中断与纠错反馈](#流式中断与纠错反馈) 章节）：

```
StreamSlideAsync(userPrompt, cancellationToken)
  │
  ├─ 1. 创建 SlideStreamInterruptionController + SlideMlStreamingMerger
  ├─ 2. 创建 SlideStreamRenderService（封装实时渲染 + AI Tool）
  │
  └─ while (true)  ← 重试循环
       │
       ├─ 3. controller.StartRound()
       │
       ├─ 4. 调用 _copilotChatManager.CreateManualSendMessageContextAsync()
       │     → 获取 IManualSendMessageContext
       │
       ├─ 5. 填充 UserChatMessage
       │     → 首轮：使用 prompt provider 构建流式模式提示词
       │     → 重试轮：为空（纠错消息已在上轮追加到会话历史）
       │
       ├─ 6. 获取 ChatClientAgent + AgentSession（复用同一 session）
       │
       ├─ 7. 创建 SlideMlFragmentExtractor（每轮新建，缓冲区不跨轮）
       │
       ├─ 8. 订阅 context.AssistantChatMessage.TextAppended
       │     → extractor.Append(text)
       │     → fragments = extractor.TryExtractFragments()
       │     → 逐个 merger.AcceptFragment(fragment, context)
       │     → 检查错误 → controller.ReportTolerableError / ReportFatalError
       │     → 触发中断时 controller.Cancel() 打断 RunStreamingAsync
       │     → ★ 每次成功合并后：
       │        renderService.TryRender(merger.GetMergedXml(), context)
       │        → 节流通过则调用 RenderAsync → 更新 LatestRenderedXml / LatestPreviewImage
       │        → 触发 SlideRendered 事件 → UI 实时刷新
       │        → LLM 可随时通过 get_slide_state / get_slide_preview 工具读取状态
       │
       ├─ 9. 构建 ChatOptions，将 renderService 的 AI Tool 加入 Tools
       │     → get_slide_state：返回当前回填 XML + 警告列表
       │     → get_slide_preview：返回当前预览图 PNG
       │
       ├─ 10. using context.StartChatting()
       │      → 管理 IsChatting 状态
       │
       ├─ 11. await context.AppendMessagesToSessionAsync()
       │      → 将消息加入会话（保留上下文）
       │
       ├─ 12. try
       │      await foreach (var update in agent.RunStreamingAsync(
       │          messages, session, options, controller.Token))
       │      {
       │        context.AppendResponseUpdate(update);
       │        // ↑ AppendResponseUpdate 内部触发 TextAppended
       │        //   → 步骤 8 的回调执行实时提取/合并/渲染
       │      }
       │      → 流正常结束 → 确保最后一次渲染 → break
       │
       │  catch (OperationCanceledException) when (系统中断)
       │      → 进入纠错流程 ↓
       │  catch (OperationCanceledException) when (用户取消)
       │      → break（不重试）
       │
       ├─ 纠错流程:
       │     ├─ 收集错误信息（context.Errors）
       │     ├─ 收集当前合并 XML（merger.GetMergedXml()）
       │     ├─ 构建纠错反馈消息（含当前回填 XML + 预览状态）
       │     ├─ AppendMessageAsync(纠错消息)  ← 追加到会话
       │     ├─ controller.CanRetry() ?
       │     │     ├─ 是 → continue（回到 while 循环顶部，新一轮）
       │     │     └─ 否 → break（达到最大重试次数）
       │
       └─ 循环结束
            │
            ▼
       13. 返回 renderService 的最终 SlideMlRenderResult
       14. 触发 SlideRendered 事件通知 UI
```

**关键变化**：实时渲染和工具提供在步骤 8 中完成，不再等待循环结束。`await foreach` 循环体内通过 `AppendResponseUpdate` → `TextAppended` 事件回调链触发整个提取→合并→渲染流程。LLM 在流式输出过程中可以随时调用 `get_slide_state` 和 `get_slide_preview` 工具获取当前页面状态。

### 步骤 7：流式模式提示词

**文件**：

- `Code/PptxGenerator.Core/Prompt/ISlideMlPromptProvider.cs`（修改：新增方法）
- `Code/PptxGenerator.Core/Prompt/SlideMlPromptProvider.cs`（修改：实现新方法）

**新增接口方法**：

```csharp
public interface ISlideMlPromptProvider
{
    // 现有方法保持不变
    string BuildSystemPrompt();
    string BuildInitialUserPrompt(string userPrompt);

    // 新增：流式模式提示词
    string BuildStreamingSystemPrompt();
    string BuildStreamingUserPrompt(string userPrompt);
}
```

**`BuildStreamingSystemPrompt()` 完整内容**：

```csharp
public string BuildDefaultStreamingSystemPrompt()
{
    return $"""
你是 SlideML 流式幻灯片生成器。你的任务是根据用户需求，连续输出符合 SlideML 规范的 XML 片段序列，供解析器逐片段接收并合并成一页幻灯片。

输出约束：
1. 生成幻灯片时，直接输出 XML 片段序列；不要输出 XML 声明；不要使用 Markdown、代码块、反引号、HTML、CSS、XAML、JSON。
2. 输出完成后直接停止，不要输出任何额外结束标记。
3. 除非用户明确要求解释，否则不要输出自然语言说明。若用户要求说明，只能输出普通纯文本；不要使用 Markdown 标题、列表、表格或代码块；不要把说明文字混入 XML 片段流。
4. 只能使用本文列出的标签和属性，标签名与属性名大小写必须完全一致。
5. XML 必须格式正确：每个片段都是一个完整顶层 XML 元素；标签必须闭合；属性值必须加引号。
6. 文本属性中的特殊字符必须转义：& 转为 &amp;，< 转为 &lt;，> 转为 &gt;，" 转为 &quot;，' 转为 &apos;。
7. 不要输出 ActualWidth、ActualHeight、ActualLineCount，这些由渲染引擎回填。

画布与基础约定：
1. 画布宽高分别使用 $(SlideWidth) 和 $(SlideHeight)。表示整页宽度或高度时使用这两个占位符，不要写死整页宽高数字。
2. 坐标原点在左上角，X 向右，Y 向下。
3. 所有数值默认单位为 px，不写单位。
4. 颜色格式为 #RRGGBB 或 #AARRGGBB。
5. 子元素坐标相对于直接父容器左上角。
6. 同一父容器内，文档顺序决定层级，后出现的元素在上层。
7. 子元素超出父容器边界会被裁剪。
8. 渐变子元素优先于同名纯色属性。
9. 文本溢出、图片缺失、元素超出画布、未知属性或标签会产生 Warning，应尽量避免。

流式输出模型：
1. 输出是连续 XML 片段序列。每个片段是一个完整的顶层 XML 元素。
2. 通常先输出 <Page> 定义页面背景和初始布局，再继续输出 <Panel>、<Rect>、<TextElement>、<Image>、<Page> 或 <Remove> 片段来补充、修改、重排或删除内容。
3. Page 是根容器，最终只有一个 Page。Page 可作为后续片段再次出现，用于更新页面属性或调整顶层结构。
4. Panel、Rect、TextElement、Image 必须有 Id。复用已有 Id 表示更新该元素。不要把同一个 Id 用作两个不同元素；不要让同一个 Id 出现在两个不同父容器下；同一片段内不要出现重复 Id。
5. Span、Fill、Stroke、Shadow、LinearGradient、Stop 不使用 Id。Remove 使用 TargetId。
6. 不在 Page 子树内、作为顶层片段输出的 Panel、Rect、TextElement、Image 是悬空元素。悬空元素不参与渲染，只供 StyleFrom 引用。悬空元素创建后，不要再把同一个 Id 放入 Page 或 Panel 子树。

流式合并规则：
1. 解析器用 Id 匹配已有元素。匹配到已有元素时，片段中显式声明的属性覆盖旧值；片段中未声明的属性保留旧值；片段中未声明的子元素保留旧子元素。
2. 片段中的容器元素不含子元素时，只合并属性，已有子元素保持不动。<Panel Id="Area"/> 与 <Panel Id="Area"></Panel> 等价。
3. 流片段只影响显式声明的元素及其子树；未提及的元素保持原样。
4. 当父元素片段包含子元素列表 F，要与当前子元素列表 L 合并时：从 F 开头寻找第一个已存在于 L 的 Id，取其在 L 中的位置 P；若没有找到，则 P 为 L 末尾。然后从 L 中移除所有 Id 出现在 F 中的元素。最后把整个 F 插入位置 P；若 P 超出当前 L 长度则追加到末尾。
5. 删除已有元素及其子树时，输出 <Remove TargetId="目标元素Id"/>。若目标不存在则忽略。

StyleFrom：
1. StyleFrom 是 Panel、Rect、TextElement、Image 的通用属性，值为源元素 Id。
2. 解析器先复制源元素的全部属性作为默认值，再用当前元素显式声明的属性覆盖；不复制源元素的子元素。
3. 优先级：StyleFrom 源属性 < 当前元素显式属性 < 后续片段显式合并属性。
4. 只引用已经存在的源元素 Id。可先输出悬空模板元素，再由后续元素通过 StyleFrom 复用样式。

通用属性：Panel、Rect、TextElement、Image 支持：Id（必填）；StyleFrom（可选，引用源元素 Id）；X、Y（可选，默认 0）；Width、Height（可选）；HorizontalAlignment（可选，Left/Center/Right，仅不写 X 时生效）；VerticalAlignment（可选，Top/Center/Bottom，仅不写 Y 时生效）；Opacity（可选，0.0~1.0，默认 1.0）；Margin（可选，逗号分隔 1~4 个值，如 "0,0,0,8"）。

Page：根容器。属性：Background（可选，默认 #FFFFFF）。Page 可包含 Panel、Rect、TextElement、Image。Page 不需要 Id。

Panel：容器，支持嵌套、绝对定位和单向流式布局。专有属性：Padding（可选，默认 0）；Background（可选，默认透明）；Layout（可选，Absolute/Horizontal/Vertical，默认 Absolute）；Gap（可选，默认 0）。Width、Height 不写时自动撑开到包裹所有子元素和 Padding。Layout="Absolute" 时子元素按各自 X、Y 定位。Layout="Horizontal" 时子元素沿水平方向排列，子元素 X 被忽略，跨轴仍使用 Y 或 VerticalAlignment。Layout="Vertical" 时子元素沿垂直方向排列，子元素 Y 被忽略，跨轴仍使用 X 或 HorizontalAlignment。流式布局不支持换行，子元素超出 Panel 尺寸时只产生警告。流式布局实际间距为 max(Gap, 相邻元素在排列方向上的 Margin 之和)。Panel 可包含 Fill 子元素定义渐变背景，Fill 优先于 Background。

Rect：矩形。专有属性：Fill（可选，默认透明）；Stroke（可选，默认无描边）；StrokeThickness（可选，默认 0）；CornerRadius（可选，默认 0，支持 1~4 值逗号分隔，如 "8" 四角统一、"8,0,8,0" 左上右下圆角）；StrokeDashArray（可选，逗号分隔数值，如 "4,2"）；Shadow（可选，字符串格式 "OffsetX OffsetY Blur Color"，Color 可含 alpha）。Rect 可包含 Fill、Stroke、Shadow 子元素；子元素优先于同名 XML 属性。

TextElement：文本。专有属性：Text（无 Span 时必填，有 Span 时可省略）；FontName（可选，默认 Microsoft YaHei）；FontSize（可选，可为绝对 px 数字或字号等级 L1~L5，默认 16）；IsBold（可选，True/False）；IsItalic（可选，True/False）；Foreground（可选，默认 #000000）；TextAlignment（可选，Left/Center/Right/Justify，默认 Left）；LineHeight（可选，行高倍数，默认 1.2）。Width 不写则单行无限宽；写 Width 则在约束宽度内自动换行。可包含 Span 子元素。

Span：TextElement 内的富文本片段。属性：Text（必填）；FontSize、FontName、Foreground、IsBold、IsItalic 可选并继承 TextElement；TextDecoration（可选，None/Underline，默认 None）。

Image：图片。专有属性：Source（必填，图片资源 ID，不是 URL）；Stretch（可选，None/Fill/Uniform/UniformToFill，默认 Uniform）。

Fill、Stroke、Shadow、LinearGradient、Stop：Fill 用于 Panel、Rect 的渐变填充，包含 LinearGradient。Stroke 用于 Rect 的渐变描边，包含 LinearGradient，需配合 StrokeThickness。LinearGradient 属性：X1、Y1 默认 0、0；X2、Y2 默认 1、0；数值 0~1 表示相对元素尺寸比例。Stop 是 LinearGradient 子元素，属性 Offset（必填，0~1）、Color（必填）。Shadow 子元素用于 Rect，属性 OffsetX（默认 0）、OffsetY（默认 4）、Blur（默认 12）、Color（默认 #00000033）、Opacity（默认 1）。

字号等级：FontSize 可使用 L1~L5。基准为 1280×720 画布，L3 基准为 48px。L1=1.67（封面主标题）、L2=1.17（页面标题）、L3=1.00（正文）、L4=0.83（辅助文字）、L5=0.67（微文字）。实际字号按 min(当前画布宽/1280, 当前画布高/720) 缩放。数字 FontSize 表示绝对 px，不参与缩放。推荐优先使用字号等级统一文字层级。

可用工具：系统在每个片段合并后自动渲染，你不需要主动触发渲染。可使用 get_slide_state（无参数）查看当前回填后的完整 XML 和警告列表；使用 get_slide_preview（无参数）获取当前页面 PNG 预览图。建议每输出 3~5 个片段后调用一次 get_slide_state 检查排版效果，发现问题用后续片段修正。流式模式下不存在 render_slide 和 get_render_preview 工具，不要尝试调用它们。

推荐生成策略：
1. 先输出 Page，建立背景和主要区域占位。
2. 使用 Panel 划分 Header、Content、Footer、Card、Sidebar 等逻辑区域。
3. 复杂卡片用 Panel 包住 Rect 和 TextElement。
4. 同样式元素可用 StyleFrom 或悬空模板减少重复。
5. 后续片段只输出变化部分，依靠 Id 合并保留未变化内容。
6. 需要重排同一父容器内子元素时，在同一个父容器片段中按目标顺序输出相关子元素。
7. 需要删除元素时使用 Remove。

示例片段序列：
<Page Background="#F5F5F5">
  <Panel Id="Header" X="0" Y="0" Width="$(SlideWidth)" Height="100"/>
  <Panel Id="Content" X="80" Y="140" Width="1120" Height="500"/>
</Page>
<Panel Id="Header" Background="#1A1A2E">
  <TextElement Id="HeaderTitle" X="80" Y="28" Width="1120" Text="标题" FontSize="L2" IsBold="True" Foreground="#FFFFFF" TextAlignment="Center"/>
</Panel>
<Panel Id="Content">
  <Panel Id="CardOne" X="0" Y="0" Width="340" Height="180">
    <Rect Id="CardOneBackground" X="0" Y="0" Width="340" Height="180" Fill="#FFFFFF" CornerRadius="12" Shadow="0 4 12 #00000033"/>
    <TextElement Id="CardOneTitle" X="24" Y="24" Width="292" Text="要点" FontSize="L4" IsBold="True" Foreground="#1A1A2E"/>
    <TextElement Id="CardOneBody" X="24" Y="72" Width="292" Text="这里是卡片正文内容。" FontSize="L5" Foreground="#666666" LineHeight="1.4"/>
  </Panel>
</Panel>
<Remove TargetId="CardOne"/>
""";
}
```

**`BuildStreamingUserPrompt()` 完整内容**：

```csharp
public string BuildDefaultStreamingUserPrompt(string userPrompt)
{
    ArgumentNullException.ThrowIfNull(userPrompt);

    return $"""
请根据以下需求以流式片段方式生成单页 SlideML：

{userPrompt}

要求：
1. 尽量使用浅色主题，视觉清爽。
2. 标题、副标题、正文层级明显，优先使用字号等级（标题 L2、正文 L3、辅助 L4）。
3. 页面内容要适合画布 $(SlideWidth) × $(SlideHeight)。
4. 若需图片可用占位资源 ID，如 image_001。
5. 先输出 Page 骨架，再逐步填充和细化。
6. 每个元素必须带 Id 且全局唯一；同类元素优先用悬空模板 + StyleFrom 减少重复。
7. 合理使用 Panel 的 Layout 属性减少手动坐标计算。
8. 输出过程中可随时调用 get_slide_state 和 get_slide_preview 查看排版效果。
9. 发现问题用后续片段修正（调整坐标/尺寸，或用 Remove 删除后重来）。
""";
}
```

**接口变更**：

`ISlideMlPromptProvider` 新增两个方法：

```csharp
/// <summary>
/// 构建流式模式系统提示词。
/// </summary>
string BuildStreamingSystemPrompt();

/// <summary>
/// 构建流式模式用户提示词，包裹用户的自然语言需求。
/// </summary>
/// <param name="userPrompt">用户自然语言需求描述。</param>
string BuildStreamingUserPrompt(string userPrompt);
```

**实现要点**：

- `SlideMlPromptProvider` 新增 `_streamingSystemPromptOverride` / `_streamingUserPromptTemplateOverride` 字段及对应的 `UpdateStreamingPrompts` / `ResetStreamingToDefault` 方法，与现有 override 机制一致
- `BuildStreamingSystemPrompt()` / `BuildStreamingUserPrompt()` 优先返回 override 值，未设置时返回上面定义的默认内容
- 画布尺寸使用 `$(SlideWidth)` / `$(SlideHeight)` 占位符，由 `SlideGenerationPipeline` 在注入提示词时替换为实际像素值（如 `1280` / `720`）
- 流式提示词中**不提及** `render_slide` / `get_render_preview` 工具，LLM 只使用 `get_slide_state` / `get_slide_preview`
- 提示词首句强调输出约束：默认只输出 XML 片段，禁止 Markdown、代码块、反引号等污染内容；必须说明时仅用纯文本短句
- 强调"每个元素必须带 Id 且全局唯一"、"不要输出 `<?xml` 声明"、"不要用 markdown 代码块包裹"
- 提示词自身不使用 Markdown 结构（标题、列表、表格、代码块），全部使用纯文本段落，确保高信息密度
- 不提及 EOF 流结束信号，防止 LLM 输出 `<EOF>` 等内容
- 不提及版本号（如"SlideML V3"），统称 SlideML
- 不包含动画相关内容（Storyboard、Appear、OnClick 等）
- 流式输出示例见 [SlideML 流式输出规范 §完整示例](./SlideML%20流式输出规范.md#完整示例)

> **注意**：一次性模式的提示词也需要同步升级以支持 SlideML 规范新增特性（字号等级、LineHeight、StyleFrom、Shadow 子元素、CornerRadius 多值等），但这不在流式输出计划的范围内，应在另外的迁移工作中完成。

### 步骤 8：SlideChatManager 暴露流式入口

**文件**：`Code/PptxGenerator.Core/SlideChatManager.cs`（修改）

**变更**：

- 新增 `SlideStreamingPipeline` 属性或 `CreateStreamingPipeline()` 方法
- 让 Demo 项目（WPF / Avalonia）可以选择使用流式模式或一次性模式
- 现有 `Pipeline`（`SlideGenerationPipeline`）保持不变

### 步骤 9：单元测试

**文件**：`Code/PptxGenerator.Tests/Streaming/`（新建目录）

#### SlideMlFragmentExtractorTests

| 测试 | 场景 |
|------|------|
| `Append_SingleFragment_ReturnsAfterClose` | 完整片段一次性到达 |
| `Append_FragmentSplitAcrossTokens_ReturnsWhenComplete` | 片段在标签中间断开 |
| `Append_SelfClosingTag_ReturnsImmediately` | 自闭合标签 `<Tag/>` |
| `Append_MultipleFragments_ReturnsAll` | 多个片段连续到达 |
| `Append_PartialFragment_StaysInBuffer` | 不完整片段保留在缓冲区 |
| `Append_XmlDeclaration_NotTreatedAsFragment` | `<?xml` 声明不被误识别 |
| `Append_Comment_NotTreatedAsFragment` | `<!-- -->` 注释不被误识别 |

#### SlideMlStreamingMergerTests

| 测试 | 场景 | 规范引用 |
|------|------|----------|
| `AttributeMerge_ExplicitOverridesExisting` | 片段显式属性覆盖已有值 | §11 |
| `AttributeMerge_UnspecifiedPreserved` | 片段未声明属性保留原有值 | §11 |
| `ChildrenMerge_NewElementAppended` | F = [card3]，L = [card1, card2] → [card1, card2, card3] | §14 例① |
| `ChildrenMerge_AnchorReorders` | F = [card4, card2]，L = [card1, card2, card3] → [card1, card4, card2, card3] | §14 例② |
| `ChildrenMerge_PExceedsLength_AppendsToEnd` | F = [card3, card2]，L = [card1, card4, card2, card3] → [card1, card4, card3, card2] | §14 例③ |
| `ContainerNoChildren_PreservesExistingChildren` | 片段容器不含子元素时现有子元素不动 | §15 |
| `Remove_ExistingElement_RemovesFromTree` | 删除存在的元素 | §9 |
| `Remove_NonExistingTarget_WarningAndIgnore` | 删除不存在的元素 → [Warning] | §9 |
| `DanglingElement_RegisteredButNotRendered` | 悬空元素登记到索引但不渲染 | §13 |
| `StyleFrom_CopiesSourceAttributes` | 复制源元素属性作为默认值 | §12 |
| `StyleFrom_SourceNotFound_ErrorAndContinue` | 源元素不存在 → [Error] | §12 |
| `Error_XmlFormatError_SkipsFragment` | XML 格式错误 → [Error] + 跳过 | §16 |
| `Error_MissingId_SkipsElement` | 缺少 Id → [Error] + 跳过 | §16 |
| `Error_DuplicateIdAcrossParents_Error` | Id 出现在两个不同父容器下 → [Error] | §16 |
| `Error_DuplicateIdInSameFragment_Error` | 同片段内重复 Id → [Error] | §16 |

#### SlideStreamingPipelineTests

| 测试 | 场景 |
|------|------|
| `StreamSlideAsync_MockTokenStream_ProducesMergedXml` | mock IChatClient 返回预定义 token 序列 → 验证最终合并 XML |
| `StreamSlideAsync_MockTokenStream_TriggersRender` | 验证流结束后调用渲染管线 |
| `StreamSlideAsync_TextAppended_FeedsExtractor` | 验证 TextAppended 事件被正确订阅和处理 |
| `StreamSlideAsync_RealTimeRender_OnEachFragment` | 验证每个片段合并成功后触发实时渲染（非仅流结束后） |
| `StreamSlideAsync_RenderThrottle_SkipsFrequentRender` | 验证节流逻辑：短时间多次合并只渲染一次 |
| `StreamSlideAsync_LlmCallsGetSlideState_ReturnsCurrentXml` | 验证 LLM 调用 get_slide_state 工具时返回当前回填 XML |
| `StreamSlideAsync_LlmCallsGetSlidePreview_ReturnsCurrentImage` | 验证 LLM 调用 get_slide_preview 工具时返回当前预览图 |

---

## 流式中断与纠错反馈

### 场景

LLM 在流式输出过程中可能产出错误内容（XML 格式错误、Id 冲突、非法属性值等）。需要在不等 LLM 自然结束的情况下：

1. **立刻打断** LLM 输出（通过 CancellationToken）
2. **保留已有上下文**（已追加到会话的部分消息不丢弃）
3. **拼接纠错指令**（将错误信息作为新的用户消息追加到会话）
4. **重新发起流式请求**（LLM 看到历史 + 错误反馈，修正输出）

### 架构发现

通过对 AgentLib 的分析，确认以下关键机制：

| 机制 | 说明 |
|------|------|
| `CancellationTokenSource.Cancel()` | `RunStreamingAsync` 内部 `await foreach` 收到取消信号后抛出 `OperationCanceledException`，终止流式枚举 |
| `IManualSendMessageContext.StartChatting()` | 返回 `IDisposable`，dispose 时恢复 `IsChatting = false`。取消后需要 dispose 以释放聊天状态 |
| `AssistantChatMessage` 部分内容保留 | 取消时 `AssistantChatMessage` 已通过 `AppendResponseUpdate` 积累了部分文本，这些内容已经追加到会话的 `ChatMessages` 中 |
| `AgentSession` 会话历史 | `ManualSendMessageContext` 内部创建的 `ChatClientAgent` 使用 `InMemoryChatHistoryProvider`，`AgentSession` 跨多次 `RunStreamingAsync` 调用保持历史。因此取消后可以复用同一个 `AgentSession` 继续对话 |
| `CopilotChatManager.AppendMessageAsync` | 可向会话追加任意消息（用户/助手/预设信息），用于追加纠错指令 |

### 核心设计

引入 `SlideStreamInterruptionController`（中断控制器），在 `SlideMlStreamingMerger.AcceptFragment` 检测到错误时，通过回调通知 `SlideStreamingPipeline` 触发中断。

```
┌─────────────────────────────────────────────────────────────────┐
│  SlideStreamingPipeline                                         │
│                                                                 │
│  ┌─ CancellationTokenSource _interruptCts                       │
│  ┌─ SlideStreamInterruptionController _interruptController     │
│                                                                 │
│  TextAppended 事件回调:                                         │
│    extractor.Append(text)                                       │
│    fragments = extractor.TryExtractFragments()                 │
│    foreach fragment:                                            │
│      merger.AcceptFragment(fragment, context)                   │
│      ↓                                                          │
│      如果 merger 检测到不可容错错误:                              │
│        _interruptController.RequestInterruption(errors)        │
│        ↓                                                        │
│        _interruptCts.Cancel()  ← 打断 RunStreamingAsync         │
│                                                                 │
│  catch (OperationCanceledException):                            │
│    → 进入纠错流程                                                │
└─────────────────────────────────────────────────────────────────┘
```

### 错误分级

并非所有错误都需要中断。根据 §16 的容错续行策略，错误分为两级：

| 级别 | 说明 | 处理方式 | 示例 |
|------|------|----------|------|
| **可容错** | 跳过坏片段，继续接收后续内容 | 报 `[Error]`/`[Warning]`，跳过片段，不中断 | XML 格式错误（单个片段）、`Remove` 目标不存在、`StyleFrom` 源不存在 |
| **不可容错** | 继续接收无意义或会导致合并树损坏 | 报 `[Error]`，触发中断 | 同片段内重复 Id、Id 跨父容器冲突、连续 N 个片段格式错误（错误率过高） |

**可配置阈值**：`MaxConsecutiveErrors`（默认 3），连续可容错错误达到此值时升级为不可容错，触发中断。避免 LLM 持续产出垃圾内容。

### 中断-纠错流程

```
检测到不可容错错误
  │
  ▼
① 取消当前流
  _interruptCts.Cancel()
  → RunStreamingAsync 抛出 OperationCanceledException
  → await foreach 循环退出
  │
  ▼
② 保留上下文
  - AssistantChatMessage 已包含部分输出（通过 AppendResponseUpdate 积累）
  - UserChatMessage 已追加到会话
  - AssistantChatMessage 已追加到会话（AppendMessagesToSessionAsync 已调用）
  - AgentSession 内部历史已包含本次对话
  - SlideMlStreamingMerger 的 DOM 树保留已成功合并的片段（不重置）
  - SlideStreamRenderService 的 LatestRenderedXml / LatestPreviewImage 保留最后一次实时渲染结果
  - 不删除任何已有消息
  │
  ▼
③ 拼接纠错指令
  - 收集错误信息（来自 SlideMlPipelineContext.Errors）
  - 收集当前合并 XML（来自 merger.GetMergedXml()，包含已成功合并的部分）
  - 可选：附上最后一次实时渲染的回填 XML 和警告（来自 renderService）
  - 构建纠错用户消息：

    "[系统纠错反馈]
    你在流式输出过程中产生了以下错误，已中断：

    {错误列表}

    已成功接收的部分内容：
    {当前合并 XML}

    上次实时渲染的警告（如有）：
    {警告列表}

    请基于已有内容修正错误并继续输出。
    注意：不要重复已成功的片段，只输出修正和后续内容。
    可以调用 get_slide_state 查看当前状态，调用 get_slide_preview 查看预览图。"

  - 通过 CopilotChatManager.AppendMessageAsync 追加到会话
  │
  ▼
④ 重新发起流式请求
  - 创建新的 CancellationTokenSource
  - 复用同一个 AgentSession（历史已包含之前的对话 + 纠错反馈）
  - 复用同一个 SlideMlStreamingMerger（保留已合并的 DOM 树状态）
  - 复用同一个 SlideStreamRenderService（保留 LatestState 供 LLM 工具读取）
  - 重新订阅 TextAppended
  - 再次调用 RunStreamingAsync
  │
  ▼
⑤ 循环直到成功或达到最大重试次数
  - MaxRetries（默认 2），超过后返回当前渲染结果 + 错误列表
```

### 实现步骤（新增）

在原有步骤 6（SlideStreamingPipeline）中集成中断-纠错逻辑和实时渲染逻辑，并新增步骤 6a、6b 和 6c。

#### 步骤 6a：SlideStreamInterruptionController（中断控制器）

**文件**：`Code/PptxGenerator.Core/Streaming/SlideStreamInterruptionController.cs`（新建）

**职责**：管理中断状态、错误收集、重试计数。

```csharp
/// <summary>
/// 流式中断控制器，管理错误分级、中断信号和重试逻辑。
/// </summary>
public sealed class SlideStreamInterruptionController
{
    private readonly int _maxConsecutiveErrors;
    private readonly int _maxRetries;
    private int _consecutiveErrorCount;
    private int _retryCount;
    private CancellationTokenSource? _cts;

    /// <summary>
    /// 当前重试轮次（从 0 开始）。
    /// </summary>
    public int RetryRound => _retryCount;

    /// <summary>
    /// 是否已达到最大重试次数。
    /// </summary>
    public bool MaxRetriesReached => _retryCount >= _maxRetries;

    /// <summary>
    /// 是否已请求中断。
    /// </summary>
    public bool IsInterruptionRequested => _cts?.IsCancellationRequested ?? false;

    /// <summary>
    /// 关联的 CancellationToken，供 RunStreamingAsync 使用。
    /// </summary>
    public CancellationToken Token => _cts?.Token ?? CancellationToken.None;

    /// <summary>
    /// 开始新一轮流式请求。
    /// </summary>
    public void StartRound();

    /// <summary>
    /// 重置连续错误计数（成功接收一个有效片段后调用）。
    /// </summary>
    public void ResetErrorCount();

    /// <summary>
    /// 报告一个可容错错误。连续达到阈值时升级为不可容错。
    /// </summary>
    /// <returns>是否触发了中断。</returns>
    public bool ReportTolerableError(string error);

    /// <summary>
    /// 报告一个不可容错错误，立即触发中断。
    /// </summary>
    public void ReportFatalError(string error);

    /// <summary>
    /// 结束当前轮次，返回是否可以重试。
    /// </summary>
    public bool CanRetry();

    /// <summary>
    /// 取消当前轮次。
    /// </summary>
    public void Cancel();
}
```

#### 步骤 6b：SlideStreamingPipeline 集成中断-纠错

**文件**：`Code/PptxGenerator.Core/Streaming/SlideStreamingPipeline.cs`（修改步骤 6 的设计）

**`StreamSlideAsync` 更新流程**：

```
StreamSlideAsync(userPrompt, cancellationToken)
  │
  ├─ 1. 创建 SlideStreamInterruptionController（maxRetries: 2, maxConsecutiveErrors: 3）
  ├─ 2. 创建 SlideMlStreamingMerger（跨重试保留状态）
  ├─ 3. 创建 SlideStreamRenderService（封装实时渲染 + AI Tool，跨重试保留状态）
  │
  └─ while (true)
       │
       ├─ controller.StartRound()
       │
       ├─ 创建 IManualSendMessageContext
       │     → 首轮：填充用户原始 prompt
       │     → 重试轮：填充用户 prompt 为空（纠错消息已在会话历史中）
       │
       ├─ 订阅 AssistantChatMessage.TextAppended
       │     → extractor.Append(text)
       │     → fragments = extractor.TryExtractFragments()
       │     → foreach fragment:
       │         merger.AcceptFragment(fragment, context)
       │         if context.Errors.Count > 0:
       │           foreach error:
       │             if controller.ReportTolerableError(error):
       │               break  ← 连续错误超阈值，触发中断
       │         else:
       │           controller.ResetErrorCount()  ← 成功片段，重置计数
       │           ★ renderService.TryRender(merger.GetMergedXml(), context)
       │             → 节流通过 → RenderAsync → 更新 LatestState
       │             → 触发 SlideRendered → UI 实时刷新
       │         if controller.IsInterruptionRequested:
       │           break
       │
       ├─ 构建 ChatOptions
       │     → Tools = renderService.CreateTools()
       │       （get_slide_state + get_slide_preview）
       │
       ├─ using context.StartChatting()
       ├─ await context.AppendMessagesToSessionAsync()
       │
       ├─ try
       │     await foreach (var update in agent.RunStreamingAsync(
       │         messages, session, options, controller.Token))
       │     {
       │       context.AppendResponseUpdate(update);
       │       // ↑ 内部触发 TextAppended → 上面的回调执行实时提取/合并/渲染
       │       // LLM 可在 await foreach 挂起期间调用 get_slide_state / get_slide_preview
       │     }
       │     → 流正常结束 → renderService.FinalRender() 确保最后一次渲染 → break
       │
       │  catch (OperationCanceledException) when (controller.IsInterruptionRequested)
       │     → 进入纠错流程 ↓
       │  catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
       │     → 用户主动取消，直接返回当前渲染结果
       │
       ├─ 流正常结束？
       │     ├─ 是 → break（跳出 while 循环）
       │     └─ 否（被中断）→
       │
       ├─ 纠错流程:
       │     ├─ 收集错误信息（context.Errors）
       │     ├─ 收集当前合并 XML（merger.GetMergedXml()）
       │     ├─ 可选：附上 renderService 的最后一次渲染状态
       │     ├─ 构建纠错反馈消息
       │     ├─ AppendMessageAsync(纠错消息)  ← 追加到会话
       │     ├─ controller.CanRetry() ?
       │     │     ├─ 是 → continue（回到 while 循环顶部，新一轮）
       │     │     └─ 否 → break（达到最大重试次数）
       │
       └─ 循环结束
            │
            ▼
       返回 renderService 的最终 SlideMlRenderResult
       触发 SlideRendered 事件
```

#### 步骤 6c：SlideStreamRenderService（实时渲染层 + AI Tool 提供者）

**文件**：`Code/PptxGenerator.Core/Streaming/SlideStreamRenderService.cs`（新建）

**职责**：在流式接收过程中，每次片段合并成功后触发实时渲染（带节流），维护最新状态供 LLM 工具读取，并提供 `get_slide_state` / `get_slide_preview` 两个 AI Tool。

**核心 API**：

```csharp
/// <summary>
/// 流式实时渲染服务，负责在片段合并后触发渲染、维护最新状态、提供 AI Tool。
/// </summary>
public sealed class SlideStreamRenderService
{
    private readonly ISlideMlRenderPipeline _renderPipeline;
    private readonly IMainThreadDispatcher _dispatcher;
    private readonly TimeSpan _minRenderInterval;
    private DateTimeOffset _lastRenderTime;
    private SlideMlRenderResult? _latestResult;

    /// <summary>
    /// 最近一次渲染的回填 XML。
    /// </summary>
    public string? LatestRenderedXml => _latestResult?.OutputXml;

    /// <summary>
    /// 最近一次渲染的警告列表。
    /// </summary>
    public IReadOnlyList<string> LatestWarnings
        => _latestResult?.Warnings ?? Array.Empty<string>();

    /// <summary>
    /// 最近一次渲染的预览图。
    /// </summary>
    public IPreviewImage? LatestPreviewImage => _latestResult?.PreviewImage;

    /// <summary>
    /// 尝试渲染（带节流）。距上次渲染不足 <see cref="_minRenderInterval"/> 时跳过。
    /// </summary>
    /// <param name="mergedXml">当前合并后的 XML。</param>
    /// <param name="context">渲染上下文。</param>
    /// <returns>是否实际执行了渲染。</returns>
    public async Task<bool> TryRenderAsync(string mergedXml, SlideMlPipelineContext context);

    /// <summary>
    /// 强制渲染（忽略节流），用于流结束后的最终渲染。
    /// </summary>
    public Task<SlideMlRenderResult> FinalRenderAsync(string mergedXml, SlideMlPipelineContext context);

    /// <summary>
    /// 创建供 LLM 使用的 AI Tool 列表。
    /// </summary>
    /// <returns>包含 get_slide_state 和 get_slide_preview 的工具列表。</returns>
    public IReadOnlyList<AIFunction> CreateTools();
}
```

**渲染节流策略**：

- `MinRenderInterval`（默认 500ms）：两次渲染之间的最小间隔
- 距上次渲染不足间隔时跳过（返回 `false`），但不影响合并流程
- 流结束时的 `FinalRenderAsync` 忽略节流，确保最终状态一定被渲染
- 每次渲染通过 `IMainThreadDispatcher.InvokeAsync` 调度到 UI 线程（与现有 `SlideMlRenderPipeline` 一致）

**AI Tool 定义**：

| 工具名 | 签名 | 返回 | 说明 |
|--------|------|------|------|
| `get_slide_state` | 无参数 | `string` | 返回当前回填后的 XML + 警告列表。LLM 可随时调用查看排版效果和引擎反馈 |
| `get_slide_preview` | 无参数 | `AIContent`（PNG） | 返回当前页面预览图。LLM 可从视觉层面评估颜色、间距、对齐等 |

**`get_slide_state` 返回格式**：

```text
[get_slide_state] 当前页面状态：

回填后的 XML：
<Page Background="#0F0F23" ActualWidth="1280" ActualHeight="720">
  <Panel Id="hero" X="0" Y="0" Width="1280" Height="360" ActualWidth="1280" ActualHeight="360">
    <TextElement Id="hero-title" X="80" Y="120" Width="1120" ActualWidth="1120" ActualHeight="67" ActualLineCount="1"
                 Text="SlideML V3" FontSize="56" IsBold="True"
                 Foreground="#FFFFFF" TextAlignment="Center" />
  </Panel>
</Page>

警告列表：
- [Warning] hero-title: ActualLineCount=1，文本可能偏短

渲染状态：实时渲染（第 3 次渲染，距上次 612ms）
```

**与现有 `SlideMlRenderTool` 的区别**：

| 对比项 | `SlideMlRenderTool`（一次性模式） | `SlideStreamRenderService`（流式模式） |
|--------|-----------------------------------|---------------------------------------|
| 触发方式 | LLM 主动调用 `render_slide` 工具 | 管线自动在每次片段合并后触发 |
| 渲染输入 | LLM 提供完整 XML 字符串 | 管线从 `SlideMlStreamingMerger` 获取当前合并 XML |
| 渲染时机 | LLM 输出完整 XML 后调用 | 每个 XML 片段合并成功后立即触发（带节流） |
| 工具语义 | `render_slide`（执行渲染）+ `get_render_preview`（获取截图） | `get_slide_state`（读取已渲染状态）+ `get_slide_preview`（读取预览图），LLM 不负责触发渲染 |
| 状态维护 | `LatestPreviewImage` / `LatestRenderedXml` 等 | 相同，但跨重试轮次保留 |

**关键设计决策**：流式模式下 LLM **不负责触发渲染**（不提供 `render_slide` 工具），渲染由管线自动完成。LLM 只能通过 `get_slide_state` 和 `get_slide_preview` **读取**当前状态。这样避免了 LLM 在流式输出中途调用 `render_slide` 导致的语义混乱。

### 纠错消息格式

```text
[系统纠错反馈]
你在流式输出过程中产生了以下错误，已中断：

- [Error] card1: Id "card1" 已存在于 PanelA 下，不能同时出现在 PanelB 下
- [Error] dup: 同一片段内出现两个相同 Id "dup"

已成功接收的部分内容：
<Page Background="#0F0F23">
  <Panel Id="hero" X="0" Y="0" Width="1280" Height="360">
    <TextElement Id="hero-title" X="80" Y="120" Width="1120"
                 Text="SlideML V3" FontSize="56" IsBold="True"
                 Foreground="#FFFFFF" TextAlignment="Center" />
  </Panel>
</Page>

请基于已有内容修正错误并继续输出。
注意：不要重复已成功的片段，只输出修正和后续内容。
你可以调用 get_slide_state 查看当前回填状态，调用 get_slide_preview 查看预览图。
```

### 中断后上下文保留的关键

| 上下文组件 | 保留方式 | 说明 |
|------------|----------|------|
| 用户原始 prompt | 会话 `ChatMessages` 中的 `UserChatMessage` | 已通过 `AppendMessagesToSessionAsync` 追加 |
| LLM 部分输出 | 会话 `ChatMessages` 中的 `AssistantChatMessage` | 已通过 `AppendResponseUpdate` 积累部分文本 |
| AgentSession 历史 | `AgentSession` 内部的 `InMemoryChatHistoryProvider` | 跨多次 `RunStreamingAsync` 调用保持历史，新请求自动携带之前所有对话 |
| 已合并的 DOM 树 | `SlideMlStreamingMerger` 内部 `XDocument` | 不重置，跨重试轮次保留已成功合并的片段 |
| 已注册的 Id 索引 | `SlideMlStreamingMerger` 内部 `Dictionary<string, XElement>` | 不重置，后续片段可以继续匹配已有元素 |
| 最近渲染状态 | `SlideStreamRenderService` 的 `LatestRenderedXml` / `LatestPreviewImage` | 不重置，LLM 工具可读取中断前的最后一次渲染结果 |

**关键决策**：重试时 **不创建新的 `SlideMlStreamingMerger`**，保留已合并的 DOM 树状态。LLM 在纠错消息中看到"已成功接收的部分内容"，只需输出修正和后续片段。Merger 继续从当前状态合并新片段。

**关键决策**：重试时 **复用同一个 `AgentSession`**。`AgentSession` 内部的 `InMemoryChatHistoryProvider` 自动携带之前所有对话历史（包括被中断的部分输出和纠错反馈），LLM 能看到完整上下文。

**关键决策**：重试时 **复用同一个 `SlideStreamRenderService`**。中断前的最后一次实时渲染结果（回填 XML + 预览图）仍然可用，LLM 可通过 `get_slide_state` / `get_slide_preview` 工具读取，了解中断时的页面状态。

### 区分用户取消与系统中断

两种取消场景需要区分处理：

| 场景 | CancellationToken 来源 | 处理方式 |
|------|------------------------|----------|
| **用户主动取消** | 外部传入的 `cancellationToken` | 直接返回当前合并结果（可能不完整），不触发纠错 |
| **系统中断（错误检测）** | `SlideStreamInterruptionController` 内部的 CTS | 触发纠错流程，构建纠错消息并重试 |

实现方式：使用 `CancellationTokenSource.CreateLinkedTokenSource(externalCt, interruptCt)` 链接两个令牌，在 `catch` 中通过 `IsInterruptionRequested` 区分。

### 新增测试

#### SlideStreamInterruptionControllerTests

| 测试 | 场景 |
|------|------|
| `ReportTolerableError_BelowThreshold_DoesNotInterrupt` | 连续错误未达阈值，不触发中断 |
| `ReportTolerableError_AtThreshold_TriggersInterrupt` | 连续错误达阈值，触发中断 |
| `ResetErrorCount_OnSuccess_ClearsCount` | 成功片段后重置连续错误计数 |
| `ReportFatalError_ImmediatelyInterrupts` | 不可容错错误立即触发中断 |
| `CanRetry_BelowMaxRetries_ReturnsTrue` | 未达最大重试次数，可重试 |
| `CanRetry_AtMaxRetries_ReturnsFalse` | 达到最大重试次数，不可重试 |
| `StartRound_CreatesNewCts` | 每轮创建新的 CTS |

#### SlideStreamRenderServiceTests

| 测试 | 场景 |
|------|------|
| `TryRender_FirstCall_RendersImmediately` | 首次调用立即渲染，不节流 |
| `TryRender_WithinInterval_SkipsRender` | 距上次渲染不足间隔时跳过 |
| `TryRender_AfterInterval_Renders` | 距上次渲染超过间隔时渲染 |
| `FinalRender_IgnoresThrottle` | 最终渲染忽略节流 |
| `GetSlideState_ReturnsLatestRenderedXml` | get_slide_state 工具返回最新回填 XML |
| `GetSlideState_ReturnsLatestWarnings` | get_slide_state 工具返回最新警告 |
| `GetSlidePreview_ReturnsLatestImage` | get_slide_preview 工具返回最新预览图 |
| `GetSlidePreview_NoRenderYet_ReturnsErrorMessage` | 尚未渲染时返回错误提示 |
| `CreateTools_ReturnsTwoTools` | CreateTools 返回 get_slide_state + get_slide_preview |

#### SlideStreamingPipeline 纠错测试

| 测试 | 场景 |
|------|------|
| `StreamSlideAsync_FatalError_TriggersRetry` | 不可容错错误触发重试 |
| `StreamSlideAsync_ConsecutiveErrors_TriggersRetry` | 连续可容错错误达阈值触发重试 |
| `StreamSlideAsync_RetryPreservesContext` | 重试时保留已合并 DOM、会话历史和渲染状态 |
| `StreamSlideAsync_RetryPreservesRenderService` | 重试时复用 SlideStreamRenderService，LLM 工具可读取中断前的渲染状态 |
| `StreamSlideAsync_MaxRetriesReached_ReturnsPartialResult` | 达到最大重试次数返回部分结果 |
| `StreamSlideAsync_UserCancel_DoesNotRetry` | 用户取消不触发纠错 |
| `StreamSlideAsync_CorrectedOutput_RendersSuccessfully` | 纠错后 LLM 输出正确内容，最终渲染成功 |
| `StreamSlideAsync_RealTimeRenderDuringRetry` | 重试轮次中片段合并仍然触发实时渲染 |

---

## 风险与开放问题

1. **片段切分时机**：LLM 的 token 输出可能在一个 XML 标签中间断开（如 `<Panel Id="hea` + `der">`）。`SlideMlFragmentExtractor` 使用深度计数器判断何时一个顶层元素完整闭合。需要处理 XML 注释 `<!-- -->` 和 CDATA 的干扰。

2. **Tool 调用与流式输出的交织**：流式模式下 LLM 可随时调用 `get_slide_state` / `get_slide_preview` 工具。AgentFramework 在 `RunStreamingAsync` 的 `await foreach` 挂起期间自动处理 tool call，调用对应 `AIFunction` 并将结果作为 `FunctionResultContent` 追加到历史，LLM 继续生成。需要确保 `SlideStreamRenderService` 的 `LatestState` 在 tool call 发生时是最新的——由于实时渲染在 `TextAppended` 回调中同步触发，tool call 读取时状态已是最新。

3. **实时渲染频率与性能**：每个片段合并后触发渲染可能导致频繁渲染（LLM 输出速度快时每秒可能产生多个片段）。通过 `MinRenderInterval`（默认 500ms）节流，避免渲染管线过载。同时需注意渲染在 UI 线程执行（通过 `IMainThreadDispatcher.InvokeAsync`），不能阻塞 `TextAppended` 回调链。方案：`TryRenderAsync` 异步执行渲染，不阻塞回调线程；渲染完成后通过事件通知 UI。

3. **Id 唯一性校验复杂度**：每次 `AcceptFragment` 时检查 Id 重复通过 `Dictionary` 索引实现 O(1) 查找，总体 O(N)。

4. **Page 元素的 Merge**：`<Page>` 可以作为片段反复出现。在 `SlideMlStreamingMerger` 中特殊处理：第一次 `<Page>` 创建 `XDocument` 根节点，后续 `<Page>` 片段合并属性和子元素到根节点。

5. **StyleFrom 依赖顺序**：源元素必须在引用元素之前出现（或在同一片段中先出现）。如果源元素尚不存在，报 `[Error]` 但保留元素（去掉 StyleFrom 处理）。

6. **现有 `SlideMlPromptProvider` 的 prompt 指导 LLM 输出完整 XML 并调用 `render_slide`**。流式模式需要独立的提示词，指导 LLM 按片段序列输出、每个元素必须带 Id、可以随时调用 `get_slide_state` / `get_slide_preview` 查看当前状态、不需要主动调用渲染（管线自动渲染）。

7. **中断时机的选择**：可容错错误升级为不可容错的阈值（`MaxConsecutiveErrors`）需要根据实际 LLM 输出质量调优。阈值过低会导致频繁中断，阈值过高会导致大量无效片段堆积。建议初始值 3，通过实际使用反馈调整。

8. **重试时的 DOM 树一致性**：重试时不重置 `SlideMlStreamingMerger`，但被中断的最后一个片段可能已经部分合并到 DOM 树。需要在 `AcceptFragment` 中保证原子性——片段完全成功后才提交到 DOM 树，失败时回滚。方案：先在临时副本上合并，成功后替换；或记录操作日志，失败时逆序回滚。

9. **AgentSession 复用与上下文膨胀**：每次重试都复用同一个 `AgentSession`，历史会持续增长。如果 LLM 在纠错后仍然持续出错，多轮重试后上下文可能过长导致 token 超限。建议在纠错消息中只包含"已成功的部分 XML"而非完整历史，由 `InMemoryChatHistoryProvider` 的 `ChatReducer` 自动压缩。

10. **用户取消与系统中断的竞争**：用户取消和系统中断可能同时发生。通过 `CancellationTokenSource.CreateLinkedTokenSource` 链接两个令牌，在 `catch` 中优先检查 `IsInterruptionRequested` 判断是系统中断，否则检查外部令牌判断是用户取消。

---

## 与现有规范的兼容性

- 所有 V2/V3 标签（Page、Panel、Rect、TextElement、Image、Span、Fill、Stroke、Shadow、LinearGradient、Stop）在流式输出中完全可用
- 所有 V2/V3 属性在流式输出中完全可用
- 引擎回填属性（`ActualWidth`、`ActualHeight`、`ActualLineCount`）在流式输出中同样适用，渲染后回填到最终合并的 XML 中
- 渲染反馈（Warning、Error、截图）与 V2/V3 保持一致
- 唯一差异：`Id` 从可选变为必填，且要求全局唯一
