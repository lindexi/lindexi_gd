# SlideML 架构梳理文档

> 本文档梳理 PptxGenerator.Core 项目中与 SlideML 生成、渲染、评估相关的所有类型及其关系，旨在为后续概念合并与重构提供参考。

---

## 一、命名空间与分层概览

```
PptxGenerator              # 顶层：SlideChatManager（适配器）、SlideCliRunner
├── Pipeline                # 编排层：生成管道、迭代管道、流式生成器、渲染工具
├── Prompt                  # 提示词层：提示词提供者接口与实现
├── Rendering               # 渲染层：渲染管道、布局引擎、渲染引擎、素材管理
│   └── Materials           # 渲染素材：图片资源、资源管理器
├── Streaming               # 流式层：片段提取、合并、流式管道、流式渲染服务
├── Evaluation              # 评估层：Slide评估、提示词评估、提示词优化
└── Models                  # 模型层：数据模型、解析器、渲染结果、上下文
    └── SlideDocuments      # SlideML 文档对象模型（DOM）
```

---

## 二、核心类型按职责分类

### 2.1 对外入口层（顶层 `PptxGenerator` 命名空间）

| 类型 | 职责 | 依赖 |
|------|------|------|
| `SlideChatManager` | 对 `SlideGenerationPipeline` 的**适配器**。保留旧 API（`SendSlideRequestAsync`），内部全部委托给 Pipeline。还管理模型选择。 | `SlideGenerationPipeline`、`CopilotChatManager` |
| `SlideCliRunner` | CLI 入口，调用 `SlideChatManager` 生成幻灯片并保存文件。 | `SlideChatManager` |

> **问题**：`SlideChatManager` 和 `SlideGenerationPipeline` 职责高度重叠（都暴露 `SendSlideRequestAsync`、`PreviewImage`、`CurrentSlideXml` 等），`SlideChatManager` 主要是属性转发 + 模型选择。

---

### 2.2 编排层（`PptxGenerator.Pipeline` 命名空间）

| 类型 | 职责 | 定位 |
|------|------|------|
| **`SlideGenerationPipeline`** | **核心编排器**。管理"生成 → 渲染 → 评估"完整生命周期。持有 `CopilotChatManager`、`SlideMlRenderTool`、评估器、优化器。支持普通模式、流式模式、迭代模式。 | 中枢 |
| `PromptIterationPipeline` | **迭代编排器**。循环"评估 → 优化提示词 → 重新生成"，直到收敛。 | 由 `SlideGenerationPipeline.RunPromptIterationAsync` 内部创建 |
| `StreamingSlideGenerator` | **流式生成器**。逐片段接收 LLM 输出并实时渲染，错误时重试（最多 100 次）。 | 由 `SlideGenerationPipeline.SendMessageAsync(useStreaming:true)` 内部创建 |
| `SlideMlRenderTool` | **AI Tool 封装**。将 `ISlideMlRenderPipeline.RenderAsync` 封装为 `render_slide` 和 `get_render_preview` 两个 AI Function，供 LLM 在对话中调用。缓存 Latest* 属性。 | 被 `SlideGenerationPipeline` 持有 |
| `SlideStreamingState` | 流式生成的可变状态，跨重试轮次和跨轮对话复用。组合 `SlideStreamingPipeline` + `SlideMlPipelineContext`。 | 流式模式的跨轮状态容器 |
| `PipelineContext` | 一次生成流程的**完整中间结果快照**（SlideXml、RenderedXml、Warnings、PreviewImage、评估结果）。 | 被 `EvaluateContextAsync` 使用 |
| `PipelineConfiguration` | 配置对象，聚合 `ISlideEvaluator`、`IPromptEvaluator`、`IPromptOptimizer`。 | 减少构造函数参数 |

> **概念混乱点**：
> - `PipelineContext` vs `SlideMlPipelineContext`：前者是一次生成的业务快照，后者是渲染管道的诊断信息 + 资源管理。**名字相似但职责完全不同**。
> - `SlideGenerationPipeline` 做了太多事：普通生成、流式生成、评估、迭代——它既是编排器也是门面。
> - `StreamingSlideGenerator` 和 `SlideStreamingPipeline` 都在 `Pipeline` 命名空间下，但后者在 `Streaming` 子命名空间下，概念边界模糊。

---

### 2.3 流式层（`PptxGenerator.Streaming` 命名空间）

| 类型 | 职责 |
|------|------|
| `SlideStreamingPipeline` | **流式渲染编排管道**。串联 `SlideMlFragmentExtractor` → `SlideMlStreamingMerger` → `SlideStreamRenderService`。接收增量文本，提取完整 XML 片段，合并到 DOM 树，每合并一个片段后立即尝试实时渲染（带节流）。 |
| `SlideMlFragmentExtractor` | 从 LLM token 流中**增量切分**完整 XML 片段。使用标签名栈算法识别完整顶层元素。 |
| `SlideMlStreamingMerger` | 维护 XDocument DOM 树与 Id 索引，**逐片段合并** SlideML XML。支持 Page/Panel/Rect/TextElement/Image/Remove 等元素类型的增删改。 |
| `SlideStreamRenderService` | 封装 `ISlideMlRenderPipeline`，提供**带节流**的实时渲染和最终强制渲染。 |
| `SlideStreamInterruptionController` | 流式**中断控制器**，管理错误分级、中断信号和重试逻辑。 |
| `SlideStreamRenderResult` | 流式渲染结果 DTO。与 `SlideMlRenderResult` 结构几乎相同。 |

> **概念混乱点**：
> - `SlideStreamingPipeline` 和 `SlideMlRenderPipeline`：名字都叫"Pipeline"，但前者是流式编排（片段提取→合并→渲染），后者是四阶段渲染（Parse→Layout→Measure→Render）。**它们是不同层次的 Pipeline**。
> - `SlideStreamRenderResult` 与 `SlideMlRenderResult`：两个 DTO **字段几乎完全相同**。`StreamingSlideGenerator` 中甚至有 `new SlideMlRenderResult { ... }` 手动从 `SlideStreamRenderResult` 转换的代码。

---

### 2.4 渲染层（`PptxGenerator.Rendering` 命名空间）

| 类型 | 职责 |
|------|------|
| **`ISlideMlRenderPipeline`** | 渲染管道**抽象接口**。单一方法 `RenderAsync(slideXml) → SlideMlRenderResult`。 |
| **`SlideMlRenderPipeline`** | 渲染管道**实现**。编排四阶段：Parse → PreLayout → PreMeasure → FinalLayout → Render。持有 `SlideMlParser`、`ISlideMlLayoutEngine`、`ISlideMlRenderEngine`。 |
| **`ISlideMlLayoutEngine`** | 布局引擎接口。纯数学计算，无 UI 依赖。方法：`PreLayout`、`FinalLayout`。 |
| `SlideMlLayoutEngine` | 布局引擎实现。负责坐标、尺寸、对齐计算。 |
| **`ISlideMlRenderEngine`** | 渲染引擎接口。有 UI 框架依赖。方法：`PreMeasure`、`Render`、`RenderErrorPreview`。 |
| `SlideMlMaterialResourceManager` | 素材资源管理器（图片加载、缓存）。 |

> **说明**：WPF 和 Avalonia 各自实现 `ISlideMlRenderEngine`（`WpfSlideMlRenderEngine` / `AvaloniaSlideRenderEngine`）。

---

### 2.5 提示词层（`PptxGenerator.Prompt` 命名空间）

| 类型 | 职责 |
|------|------|
| **`ISlideMlPromptProvider`** | 提示词提供者接口。方法：`BuildSystemPrompt`、`BuildInitialUserPrompt`、`BuildStreamingSystemPrompt`、`BuildStreamingUserPrompt`。 |
| `SlideMlPromptProvider` | 默认实现。支持运行时覆盖提示词（`UpdatePrompts`/`ResetToDefault`），用于迭代优化。 |

---

### 2.6 评估层（`PptxGenerator.Evaluation` 命名空间）

| 类型 | 职责 |
|------|------|
| **`ISlideEvaluator`** | SlideML 评估器接口。输入用户需求 + SlideML + 渲染结果 + 预览图，输出 `SlideEvaluationResult`。 |
| `AiSlideEvaluator` | AI 驱动的 SlideML 评估实现。 |
| **`IPromptEvaluator`** | 提示词评估器接口。输入 SystemPrompt + UserPromptTemplate，输出 `PromptEvaluationResult`。 |
| `AiPromptEvaluator` | AI 驱动的提示词评估实现。 |
| **`IPromptOptimizer`** | 提示词优化器接口。输入评估结果 + 当前提示词，输出 `PromptOptimizationResult`（改进后的提示词）。 |
| `AiPromptOptimizer` | AI 驱动的提示词优化实现。 |
| `SlideEvaluationResult` | SlideML 评估结果 DTO（7 个维度评分 + 改进建议）。 |
| `PromptEvaluationResult` | 提示词评估结果 DTO（5 个维度评分 + 优化建议）。 |
| `PromptOptimizationResult` | 提示词优化结果 DTO（优化后的提示词 + 变更说明）。 |

---

### 2.7 模型层（`PptxGenerator.Models` 命名空间）

| 类型 | 职责 |
|------|------|
| `SlideMlParser` | SlideML XML 解析器，将 XML 字符串解析为 `SlideMlPage` 对象树。 |
| `SlideMlPage` | SlideML 页面根对象。 |
| `SlideMlElement`（及子类） | SlideML 元素基类。子类：`SlideMlPanelElement`、`SlideMlRectElement`、`SlideMlTextElement`、`SlideMlImageElement` 等。 |
| `SlideMlRenderResult` | 渲染结果 DTO（InputXml、OutputXml、Warnings、Errors、PreviewImage）。 |
| `SlideMlPipelineContext` | 渲染管道**共享上下文**。包含诊断信息（Warnings/Errors）、`SlideDocumentContext`、`SlideMlMaterialResourceManager`。 |
| `SlideDocumentContext` | 文档级上下文，承载画布尺寸（CanvasWidth/CanvasHeight）。 |
| `SlideMlMeasureResult` / `SlideMlElementMeasurements` | 测量结果数据。 |
| `SlideMlRect` / `SlideMlPoint` / `SlideMlSize` | 基础几何类型。 |
| `IPreviewImage` / `FilePreviewImage` | 预览图抽象。 |

> **概念混乱点**：
> - `SlideMlPipelineContext` 命名中有 "Pipeline" 但它在 `Models` 命名空间，与 `Pipeline` 命名空间中的 `PipelineContext` 不同。前者是渲染管道的诊断容器，后者是生成管道的业务快照。

---

## 三、数据流与调用关系图

```
用户输入
  │
  ▼
SlideChatManager (适配器)  /  SlideCliRunner (CLI入口)
  │
  ▼
SlideGenerationPipeline (核心编排器)
  │
  ├─ 普通模式 ─────────────────────────────────────┐
  │  1. 构建提示词 (ISlideMlPromptProvider)         │
  │  2. 发送消息给 LLM (CopilotChatManager)         │
  │  3. LLM 调用 render_slide tool                 │
  │     └─ SlideMlRenderTool                       │
  │         └─ ISlideMlRenderPipeline.RenderAsync() │
  │             └─ SlideMlRenderPipeline            │
  │                 ├─ SlideMlParser.Parse()        │
  │                 ├─ ISlideMlLayoutEngine         │
  │                 │   ├─ PreLayout()              │
  │                 │   └─ FinalLayout()            │
  │                 └─ ISlideMlRenderEngine         │
  │                     ├─ PreMeasure()             │
  │                     └─ Render()                 │
  │  4. (可选) 自动评估                             │
  │     └─ ISlideEvaluator.EvaluateAsync()          │
  │                                                 │
  ├─ 流式模式 ─────────────────────────────────────┤
  │  1. 创建 StreamingSlideGenerator               │
  │  2. 流式接收 LLM token                         │
  │     └─ SlideStreamingPipeline                   │
  │         ├─ SlideMlFragmentExtractor (切分片段)  │
  │         ├─ SlideMlStreamingMerger (合并DOM)     │
  │         └─ SlideStreamRenderService (节流渲染)  │
  │             └─ ISlideMlRenderPipeline           │
  │  3. 错误时重试 (最多100次)                      │
  │                                                 │
  └─ 迭代模式 ─────────────────────────────────────┤
     1. 创建 PromptIterationPipeline                │
     2. 循环: 生成 → 评估 → 优化提示词 → 重新生成  │
        ├─ SlideGenerationPipeline (skipAutoEval)   │
        ├─ ISlideEvaluator                          │
        └─ IPromptOptimizer                         │
            └─ SlideMlPromptProvider.UpdatePrompts()│
```

---

## 四、同名/近名概念对比

| 概念 A | 概念 B | 关系 |
|--------|--------|------|
| `PipelineContext`（业务快照） | `SlideMlPipelineContext`（渲染诊断） | **不同**。前者是生成流程的一次结果快照，后者是渲染管线的警告/错误/资源容器 |
| `SlideGenerationPipeline`（生成编排） | `SlideMlRenderPipeline`（渲染管道） | **不同层次**。前者编排 LLM 对话→渲染→评估，后者只做 XML→图片的渲染 |
| `SlideGenerationPipeline` | `SlideStreamingPipeline`（流式管道） | **不同层次**。前者是业务编排，后者是流式文本→渲染的技术管道 |
| `SlideGenerationPipeline` | `PromptIterationPipeline`（迭代管道） | **组合关系**。前者创建后者来运行迭代 |
| `SlideStreamingPipeline` | `SlideMlRenderPipeline` | **组合关系**。前者内部调用后者的接口 |
| `SlideStreamRenderResult` | `SlideMlRenderResult` | **几乎重复**。字段基本相同，存在手动转换代码 |
| `SlideChatManager` | `SlideGenerationPipeline` | **适配器关系**。前者是后者的薄封装 + 模型管理 |

---

## 五、"Pipeline"一词的 6 种用法

| 类名 | 命名空间 | 实际含义 | 建议 |
|------|----------|----------|------|
| `SlideGenerationPipeline` | `Pipeline` | 生成编排器 | 可保留，或改为 `SlideGenerationOrchestrator` |
| `PromptIterationPipeline` | `Pipeline` | 迭代编排器 | 可保留，它是特定子流程 |
| `SlideStreamingPipeline` | `Streaming` | 流式渲染管道 | 可改为 `SlideStreamingRenderPipe` |
| `SlideMlRenderPipeline` | `Rendering` | 四阶段渲染管道 | 命名合理，可保留 |
| `ISlideMlRenderPipeline` | `Rendering` | 渲染管道接口 | 命名合理 |
| `SlideMlPipelineContext` | `Models` | 渲染诊断上下文 | 应改名，建议 `SlideMlRenderContext` |

---

## 六、潜在合并/简化建议（待讨论）

1. **`SlideStreamRenderResult` → `SlideMlRenderResult`**：两个 DTO 字段几乎相同，可合并为一个。

2. **`SlideChatManager` → `SlideGenerationPipeline`**：`SlideChatManager` 主要是属性转发适配器，如果不需要保持旧 API 兼容，可以去掉这一层，由 `SlideGenerationPipeline` 直接暴露模型选择功能。

3. **`PipelineContext` → `SlideGenerationPipeline`**：`PipelineContext` 只是数据容器 + `SnapshotFromRenderTool` 方法，可以考虑内联到 `SlideGenerationPipeline` 中（或改为 record）。

4. **`SlideMlPipelineContext` 改名**：建议改为 `SlideMlRenderContext`，避免与 `Pipeline` 命名空间混淆，同时更准确反映其"渲染诊断上下文"的职责。

5. **`SlideStreamingState` 位置**：它在 `Pipeline` 命名空间，但组合的 `SlideStreamingPipeline` 在 `Streaming` 命名空间。要么一起移到 `Streaming`，要么保持现状（它是给 `SlideGenerationPipeline` 用的状态容器）。

6. **`StreamingSlideGenerator` vs `SlideStreamingPipeline`**：前者负责 LLM 交互（发送消息、接收流、错误重试），后者负责纯技术处理（片段提取→合并→渲染）。职责边界清晰，但"Generator"和"Pipeline"的命名暗示了不同层次的抽象。

---

## 七、类型统计

| 命名空间 | 类型数 |
|----------|--------|
| `PptxGenerator`（顶层） | 2 |
| `PptxGenerator.Pipeline` | 10 |
| `PptxGenerator.Streaming` | 6 |
| `PptxGenerator.Rendering` | 4（接口 + 实现 + 素材管理） |
| `PptxGenerator.Rendering.Materials` | 5 |
| `PptxGenerator.Prompt` | 2 |
| `PptxGenerator.Evaluation` | 9 |
| `PptxGenerator.Models` | ~20（含 DOM 模型） |
| **总计** | **~58** |

---

> **总结**：当前架构的核心问题不是类型太多，而是：
> 1. "Pipeline" 一词被过度使用，指代了 4 种不同层次的流程（生成编排、迭代编排、流式管道、渲染管道）；
> 2. `PipelineContext` 和 `SlideMlPipelineContext` 命名高度相似但职责完全不同；
> 3. `SlideStreamRenderResult` 和 `SlideMlRenderResult` 存在重复；
> 4. `SlideChatManager` 作为适配器层增加了间接性但未增加实质价值。
>
> 建议优先解决命名问题，再考虑是否需要合并类型。
