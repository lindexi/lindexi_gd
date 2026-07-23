# 主题分析 Agent 自愈闭环与产品化重构方案

> 状态：实施中。已完成 Phase 1 领域基线和 Phase 2 第一批 Draft 工具领域协议，尚未接入 Agent Framework 或切换旧主题分析链路。
>
> 范围：重新设计 `CoursewarePptxGeneratorWpfDemo` 的主题分析编排、Agent 工具协议、草稿模型、质量门、失败体验、持久化和测试体系。
>
> 兼容性：不保留现有主题分析 API、旧快照、旧 `CoursewareTheme` 投影或现有 WPF 页面状态语义。允许删除并重建相关项目代码。
>
> 核心验收口径：任何模型生成的可修复错误都必须在产品内部完成诊断、定向返修和重新验证；只有同一候选版本通过全部必需质量门并完成原子发布后，用户才能获得或进入该版本。

---

## 1. 执行摘要

本次异常不是一个应通过补充若干 Token、过滤空 `TemplateId` 或把重试次数从 2 改成 5 来解决的孤立问题。它暴露的是当前主题分析流程的产品职责边界错误：

1. 模型负责生成复杂设计系统，但宿主只给它一个短生命周期、固定次数的尝试窗口；
2. 草稿允许无效实体先写入，很多错误直到最终 `complete_courseware_design_system` 才被发现；
3. 当前工具协议不能读取完整草稿，也不能删除、重命名或回滚错误实体；
4. 模板编译和真实 WPF 渲染发生在设计系统已经冻结以后，失败结果无法再反馈给 Agent；
5. 即使模板质量门失败，分析服务仍然发布“完成”状态并允许进入工作台；
6. 固定两轮未通过后，内部结构错误被转换为异常并原样交给用户，要求用户整包重试；
7. 用户重试会从头运行整个随机生成过程，而不是利用已有有效分区和精确诊断继续修复，因此会反复得到不同错误。

正确做法不是简单增加“聊天轮数”，而是建立双层闭环：

```text
内层 Agent 工具循环
    负责：工具参数错误、Revision 冲突、单实体字段错误的即时纠正

外层产品工作流循环
    负责：候选版本、完整质量门、诊断归一化、返修路由、回滚、预算、模型回退和最终发布
```

目标主链路应改为：

```text
不可变输入快照
    ↓
AnalysisRun（持久化工作流）
    ↓
初始设计 Authoring Work Item
    ↓
不可变 Draft Revision
    ↓
Candidate
    ↓
质量门 DAG
    ├── 设计图与引用
    ├── 模板静态编译
    ├── 真实 WPF 渲染
    ├── 布局、溢出、无障碍
    ├── 语义与视觉评审
    └── 产物回读验证
    ↓ 失败
结构化 Diagnostic
    ↓
Repair Router
    ↓
受限 Repair Work Item
    ↓
新 Draft Revision / 新 Candidate
    └───────────────回到质量门

全部必需质量门通过
    ↓
Qualified Artifact
    ↓
原子发布与回读
    ↓
Published Artifact
    ↓
用户结果页与页面美化工作台
```

这里最重要的产品原则是：

> 模型可以犯错，但产品不能把模型的可修复错误直接变成用户错误；模型输出只是候选，不是产物。

---

## 2. 当前代码事实与异常形成机制

### 2.1 当前调用链

当前主题分析主链路为：

```text
CoursewareWorkspaceViewModel
    ↓
CoursewareThemeAnalysisService
    ↓
CopilotCoursewareThemeAgent
    ↓
CoursewareDesignSystemToolSet
    ↓
CoursewareDesignSystemDraftBuilder
    ↓
CoursewareDesignSystemValidator
    ↓
冻结 CoursewareDesignSystem
    ↓
CoursewareTemplateStressTester
    ↓
CoursewareThemeAnalysisResult
```

主要实现位置：

- `Pptx/PptxGenerator/Code/CoursewarePptxGeneratorWpfDemo/Services/CopilotCoursewareThemeAgent.cs`
- `Pptx/PptxGenerator/Code/CoursewarePptxGeneratorWpfDemo/Services/CoursewareDesignSystemToolSet.cs`
- `Pptx/PptxGenerator/Code/CoursewarePptxGenerator.Core/Analysis/CoursewareDesignSystemDraftBuilder.cs`
- `Pptx/PptxGenerator/Code/CoursewarePptxGenerator.Core/Analysis/CoursewareDesignSystemValidator.cs`
- `Pptx/PptxGenerator/Code/CoursewarePptxGeneratorWpfDemo/Services/CoursewareTemplateStressTester.cs`
- `Pptx/PptxGenerator/Code/CoursewarePptxGeneratorWpfDemo/Services/CoursewareThemeAnalysisService.cs`
- `Pptx/PptxGenerator/Code/CoursewarePptxGeneratorWpfDemo/ViewModels/CoursewareWorkspaceViewModel.cs`

### 2.2 当前请求与会话语义

`CopilotCoursewareThemeAgent` 当前具有以下行为：

- `MaximumRequestCount = 2`，即初始请求加一次外层修复；
- 每轮都设置 `WithHistory = false`；
- 每轮都设置 `CreateNewSession = true`；
- 初始轮发送完整输入和可选截图；
- 修复轮重新发送完整文本输入、`DraftId`、`Revision` 和最后一次完成校验诊断；
- 修复轮不重新附加首轮截图；
- 两轮后仍未成功冻结时直接抛出 `InvalidOperationException`。

AgentLib 的 `RunStreamingAsync` 本身会在一次请求内部执行工具调用、工具结果返回和继续推理。因此当前实现并非完全没有 Agent 工具循环，而是存在以下边界：

1. 单次请求内部可以多次调用工具；
2. 模型一旦结束本轮输出，宿主才重新获得控制；
3. 冻结后的模板编译和 WPF 渲染不在这次内部工具循环中；
4. 宿主当前只额外允许一次新会话修复；
5. `SendMessageResult.RunTask` 返回的 `SendMessageRunState` 没有被主题 Agent 检查，模型调用异常、取消和“模型正常结束但未完成任务”没有被完整区分。

因此，问题不能只通过设置 `WithHistory = true` 解决。会话历史可以改善短期连续性，但不能替代可恢复、可审计、可回滚的产品状态。

### 2.3 本次异常如何形成

本次异常包含两类核心错误：

```text
组件引用未知设计令牌：clr-bg-analysis、clr-bg-page、……、ty-body-quote
pageTemplates: 稳定 ID 不能为空
```

其形成过程可以由当前代码直接解释：

1. 模型先通过 `set_*` 工具提交 Token 分区；
2. 随后通过 `upsert_design_component` 提交组件；
3. 组件写入时只检查 `DraftId + Revision`，不会立即检查 `TokenIds` 是否存在；
4. 模型通过 `upsert_page_template` 写入了空 `TemplateId`；
5. 模板写入时同样只检查 `DraftId + Revision`，不会立即拒绝空稳定 ID；
6. 最终调用 `complete_courseware_design_system` 时，完整验证器才一次性发现跨对象引用和空 ID；
7. 外层修复轮只知道诊断，不具有完整草稿读取工具；
8. 更严重的是，当前模板 `upsert` 以 `TemplateId` 为键。已经写入的空 ID 模板无法通过新增一个非空 ID 模板来覆盖，也没有删除工具可以移除它；
9. 因此当前草稿已经进入“诊断上可说明、协议上不可修复”的脏状态；
10. 第二轮结束后异常穿透到 `CoursewareWorkspaceViewModel`，由 UI 展示堆栈并要求用户从头重试。

这说明当前失败不是单纯“模型没有听懂错误”，而是工具协议没有提供完成修复所需的能力。

### 2.4 模板质量门位于错误的边界

`CoursewareThemeAnalysisService` 当前在 Agent 已返回冻结设计系统后才执行 `CoursewareTemplateStressTester.ValidateAsync`。

这会产生三个产品问题：

1. 模板静态编译、真实解析、布局和 WPF 渲染错误不能反馈给原 Agent；
2. 设计系统已经冻结，后续没有合法修改路径；
3. 即使 `templateValidation.IsValid == false`，服务仍继续发布 `Completed` 事件、构造结果并进入 `AnalysisReady`，只是把能力状态标为 `Failed`。

“结果中带有失败状态”和“阻止失败结果被消费”不是同一件事。当前实现只有状态说明，没有真正的发布门。

### 2.5 当前 UI 把内部错误变成用户责任

`CoursewareWorkspaceViewModel` 捕获任意异常后会：

- `LoadErrorMessage = ex.Message`；
- `LoadErrorDetails = ex.ToString()`；
- 设置 `WorkspaceState = AnalysisFailed`。

失败页随后提示用户检查模型、工具能力或输入并“重试主题分析”。这相当于要求用户承担以下内部工程职责：

- 判断哪个 Token 未定义；
- 判断哪个模板实体无效；
- 判断错误是否可局部修复；
- 判断是否应该换模型；
- 再次承担完整分析的时间和成本。

对于产品默认路径，这是不合理的。只有输入缺失、用户约束冲突、预算需要批准等确实需要用户决策的情况，才应中断并请求用户介入。

---

## 3. 根因分层

| 层级 | 当前问题 | 直接后果 |
| --- | --- | --- |
| 产品语义 | 把模型输出视为最终结果，而不是候选 | 模型错误直接暴露给用户 |
| 工作流 | 固定两轮请求，没有持久化状态机 | 无法基于错误类型决定继续、回滚、换模型或暂停 |
| 草稿模型 | 可变内存草稿，错误实体可先写入 | 无效状态污染后续修复 |
| 实体寻址 | 以模型生成的稳定 ID 作为唯一编辑键 | 空 ID、错误 ID 无法可靠删除或重命名 |
| 工具协议 | 有 upsert，无 read/delete/replace/rename/transaction | 诊断可见但修复动作不存在 |
| 校验时机 | 大量错误只在 `Complete` 发现 | 发现过晚，错误已扩散 |
| 质量门 | 模板编译和渲染发生在冻结以后 | 执行错误无法返修 |
| 发布门 | `TemplateValidation = Failed` 仍可返回完成结果 | 下游可能消费不合格产物 |
| 会话策略 | 修复轮没有当前草稿快照，也不附加截图 | 模型只能根据诊断猜测当前状态 |
| 错误分类 | 模型错误、基础设施错误、产品缺陷统一异常化 | 无法选择正确重试或责任方 |
| 收敛策略 | 只有固定次数，没有改善检测和预算账本 | 不是过早退出，就是盲目重试 |
| 可观测性 | UI 事件按 Stage 覆盖，原始异常直接展示 | 返修历史不可审计，用户体验差 |
| 测试 | 缺少完整工具链、自愈、回滚和发布门测试 | 工程上无法证明产品可用性 |

结论是：

> 当前缺陷是工作流和领域协议的系统性缺陷，不是提示词细节缺陷。

---

## 4. 产品不可违背的原则

### 4.1 候选与产物严格分离

- 模型输出只能形成 `DraftRevision` 或 `Candidate`；
- `Candidate` 不得被页面生成、快照发布或 UI 工作台直接消费；
- 只有 `QualifiedArtifact` 可以进入发布流程；
- 只有完成原子发布和回读验证的 `PublishedArtifact` 才能交付用户。

### 4.2 可修复错误默认内部闭环

以下错误默认不得中断用户：

- 必填字段缺失；
- ID 非法或重复；
- 未知 Token、组件、页面类型、模板或 ResourceId 引用；
- 页面覆盖不完整；
- 模板 XML、标签、属性、Token 或 Slot 错误；
- 渲染错误、越界、裁切、字号和对比度错误；
- 语义或视觉评审给出的可执行改进项；
- 模型忘记调用提交工具；
- 模型使用旧 Revision；
- 单个模型或端点短暂失败。

### 4.3 确定性门优先于模型判断

- 模型不能覆盖、忽略或把确定性错误降级为 Warning；
- LLM 评审不能批准一个确定性门未通过的候选；
- “模型认为可以”不构成发布依据；
- 每个发布条件必须能由版本化质量策略解释。

### 4.4 不允许假成功

以下状态不得与可交付结果并存：

- `DesignSystemValidation = Failed`；
- `TemplateValidation = Failed`；
- 必需视觉门为 `NotSupported`；
- 必需资源无法解析；
- 关键渲染 Warning；
- 产物指纹与验证报告不一致。

### 4.5 会话不是源真相

- 聊天历史可以保留，也可以丢弃；
- 任何工作流都必须能在新进程、新会话或更换模型后恢复；
- `DraftRevision`、诊断、预算、质量门和发布状态必须显式持久化；
- UI 聊天消息不得参与业务状态判断。

### 4.6 返修必须受约束

- 每次返修只允许修改诊断相关路径；
- 已通过且不受依赖影响的分区不得被随意重写；
- 修复分支没有改善时必须丢弃，而不是污染最佳候选；
- Agent 不能自行宣布最终成功。

---

## 5. 目标总体架构

### 5.1 推荐分层

建议不再把完整工作流放在 WPF Demo 项目中。可以重新组织为以下边界：

```text
CoursewarePptxGenerator.Analysis.Domain
    - AnalysisRun、DraftRevision、Candidate、Diagnostic、GateReport、Artifact

CoursewarePptxGenerator.Analysis.Application
    - WorkflowOrchestrator、RepairRouter、Budget、状态机、发布编排

CoursewarePptxGenerator.Analysis.Agent
    - Author/Repair Worker、工具协议、模型路由、上下文构建

CoursewarePptxGenerator.Analysis.Quality
    - 设计系统验证、模板编译、渲染、布局、无障碍、视觉评审门

CoursewarePptxGenerator.Analysis.Infrastructure
    - RunStore、DraftStore、模型端点、WPF 渲染适配、ArtifactPublisher

CoursewarePptxGeneratorWpf
    - 只负责用户输入、进度投影、已发布产物展示和工作台
```

项目名称可以调整，但职责必须分离。特别是：

- WPF ViewModel 不再拥有分析工作流真相；
- Agent 不再拥有草稿真相；
- Validator 不再负责决定是否发布；
- Snapshot Store 不再把“分析过程数据”和“已发布产物”混为一体。

### 5.2 核心组件

```text
CoursewareAnalysisWorkflowOrchestrator
    ├── ICoursewareAnalysisRunStore
    ├── ICoursewareDesignDraftStore
    ├── ICoursewareModelWorkerFactory
    ├── CoursewareRepairRouter
    ├── CoursewareQualityGateGraph
    ├── CoursewareBudgetLedger
    ├── CoursewareModelPolicy
    └── ICoursewareArtifactPublisher
```

Orchestrator 是唯一可以推进 AnalysisRun 状态的组件。Agent、工具、验证器和 Publisher 都只返回结构化结果，不直接修改 UI 状态。

---

## 6. AnalysisRun 状态机

### 6.1 主状态

```text
Created
  ↓
PreparingInput
  ↓
Authoring
  ↓
CandidateReady
  ↓
Evaluating
  ├── 全部门通过 → Qualified
  └── 存在可修复诊断 → RepairPlanning
                              ↓
                          Repairing
                              ↓
                       CandidateReady
  ↓
Publishing
  ↓
Published
```

### 6.2 旁路状态

| 状态 | 含义 | 是否可恢复 |
| --- | --- | --- |
| `NeedsUserAction` | 缺失输入、用户约束冲突或必须确认的策略 | 是，补充输入后继续 |
| `InfrastructureBlocked` | 模型、渲染器、磁盘或端点暂不可用 | 是，恢复后继续 |
| `Exhausted` | 可修复问题在预算内未收敛 | 是，增加预算或更换策略后继续 |
| `ProductBug` | Validator 崩溃、不可能状态或未分类代码缺陷 | 工程修复后继续 |
| `Canceled` | 用户取消当前 Run | 可从检查点恢复或新建 Run |
| `PublicationFailed` | 候选已合格，但发布失败 | 只重试发布，不重新调用模型 |
| `Superseded` | 被更新输入或新 Run 替代 | 否 |

### 6.3 状态不变量

1. `Published` 必须蕴含所有必需质量门为 `Passed`；
2. 所有质量门报告必须绑定同一 `CandidateFingerprint`；
3. `AnalysisReady` 只能是 `PublishedArtifact != null` 的 UI 投影；
4. `TemplateValidation = Failed` 与 `Published` 不能同时存在；
5. 新分析失败不能污染上一份已发布产物；
6. `PublicationFailed` 只能重试同一 `QualifiedArtifact` 的发布；
7. 每次状态迁移必须持久化并产生追加式事件；
8. 任意进程终止后都能从最后一个已提交状态继续。

---

## 7. 草稿、修订与候选模型

### 7.1 用不可变修订替换可变 Builder

建议替换当前 `CoursewareDesignSystemDraftBuilder`：

```text
CoursewareDesignDraft
CoursewareDesignDraftRevision
CoursewareDraftOperation
ICoursewareDesignDraftStore
```

每次成功操作产生一个新 Revision：

```text
Revision 12
    ↓ apply operation
Revision 13
```

旧 Revision 永不修改。其收益包括：

- 失败修复分支可以直接废弃；
- 可以对比修复前后差异；
- 可以重放并验证确定性；
- 可以在模型切换后继续；
- 可以安全恢复崩溃；
- 可以保留“最佳已知候选”，避免后续修复引入回归。

### 7.2 分离宿主实体键与领域稳定 ID

当前 upsert 直接以 `TemplateId`、`ComponentId` 等模型字段寻址，导致空 ID 实体无法删除。新模型必须分离：

- `EntityKey`：由宿主生成的不可变内部键；
- `StableId`：设计系统对外使用、由业务规则校验的 ID。

示例：

```text
EntityKey = entity-7f2a...   // 永远有效，由宿主生成
StableId = content-template // 可被校验、重命名
```

所有编辑、删除和替换操作以 `EntityKey` 定位。稳定 ID 重命名由宿主执行引用同步，不能依赖模型手工遍历所有引用。

正常情况下，空 `StableId` 应在写入前被拒绝，不产生新 Revision。`EntityKey` 仍可保证导入脏数据或部分失败恢复时具有可寻址性。

### 7.3 候选分支

质量门运行的对象不是可变草稿，而是某个不可变 Candidate：

```text
Candidate
    CandidateId
    BaseRevision
    CandidateFingerprint
    InputFingerprint
    QualityPolicyVersion
```

返修从当前最佳 Revision 建立分支：

```text
Revision 20（当前最佳）
    ├── Repair Attempt A → Revision 21 → 无改善 → 丢弃
    └── Repair Attempt B → Revision 22 → 诊断减少 → 提升为当前最佳
```

只有满足以下条件的分支才可以取代当前最佳候选：

- 目标阻断诊断减少或消失；
- 没有新增同级或更高级阻断诊断；
- 未破坏工作项范围外已经通过的质量门；
- 候选指纹没有进入已检测到的循环；
- 修改范围符合 Repair Work Item 权限。

---

## 8. Agent 与工具协议重建

### 8.1 双层循环

#### 内层：单次 Agent 工具循环

由 Agent Framework 执行，适合处理：

- 工具参数缺失；
- Revision 冲突；
- 非法 ID 格式；
- 单实体本地字段错误；
- 工作项越权修改；
- 工具调用顺序和操作失败。

工具返回结构化错误后，模型可以在同一次请求内继续调用工具修正。

#### 外层：工作流质量循环

由 `CoursewareAnalysisWorkflowOrchestrator` 执行，适合处理：

- 完整设计图引用；
- 模板编译；
- WPF 渲染；
- 布局和无障碍；
- 独立语义与视觉评审；
- 预算、收敛和模型回退；
- 产物资格与发布。

不要尝试把所有质量门强行塞入一次黑盒 `RunStreamingAsync`。宿主应在每个 Agent Turn 结束后运行质量门，再决定是否创建下一个 Repair Work Item。

### 8.2 Worker 职责

建议使用受控 Worker，而不是共享隐式历史的自由 Agent 群：

| Worker | 职责 | 可修改范围 |
| --- | --- | --- |
| `DesignSystemAuthorWorker` | 形成首个完整候选 | 全部设计分区 |
| `DesignGraphRepairWorker` | 修复 Token、组件、页面类型、覆盖和证据 | 诊断相关设计分区 |
| `TemplateRepairWorker` | 修复模板、槽位和必要 Token | 指定模板及其直接依赖 |
| `VisualRefinementWorker` | 根据渲染图和评审报告修订视觉设计 | 指定 Token、组件、模板 |
| `IndependentReviewWorker` | 只评审，不写草稿 | 无写权限 |

同一个底层模型可以承担多个 Worker，但 System Prompt、工具权限、上下文和预算必须按工作项隔离。

### 8.3 读取工具

新工具集至少提供：

- `read_draft_manifest`
  - 当前 Revision；
  - 分区完成度；
  - EntityKey 与 StableId 清单；
  - 分区指纹；
  - 当前开放诊断；
  - 当前允许修改范围。

- `read_draft_section`
  - 按分区、EntityKey 或 StableId 返回规范化快照。

- `read_source_evidence`
  - 按 SlideId、ResourceId 或布局簇读取脱敏 Markdown、结构化事实和视觉证据。

- `read_gate_report`
  - 返回指定质量门的结构化失败样本、测量值和建议修复动作。

- `read_open_diagnostics`
  - 返回本工作项负责的诊断，不泄露无关原始异常。

所有读取都携带期望 Revision。模型读取旧 Revision 后尝试写新 Revision时，宿主必须拒绝。

### 8.4 变更工具

继续使用强类型语义操作，不开放任意 JSON Patch。至少需要：

- 替换画布与网格；
- 替换 Token 分区；
- 新增或更新实体；
- 按 `EntityKey` 删除实体；
- 按 `EntityKey` 原子替换实体；
- 原子重命名 StableId 并同步全部引用；
- 清除或替换指定 SlideId 的页面类型映射；
- 批量事务提交相互引用的 Token、组件和模板；
- 从指定 Revision 建立修复分支；
- 放弃当前修复分支。

每次操作必须包含：

```text
OperationId          // 幂等键
WorkItemId           // 权限和责任范围
ExpectedRevision
TargetEntityKey / TargetSection
Payload
ExpectedEntityFingerprint（可选）
```

每次操作返回：

```text
Success
OperationId
BaseRevision
CurrentRevision
SnapshotFingerprint
ChangedPaths
InvalidatedGateIds
Diagnostics
RemainingRequiredSections
AllowedOperations
```

`AllowedOperations` 必须由宿主强制执行，不能像当前 `AllowedNextTools` 一样只做提示。

### 8.5 写入时局部校验

不再允许“先写入，最后 Complete 再统一发现所有错误”。

写入前至少检查：

- StableId 非空、格式和当前作用域唯一；
- 必填字段和值域；
- 工作项写权限；
- 已存在依赖的引用合法性；
- 同一事务内新建依赖的一致性；
- Revision 和实体指纹；
- 不允许绝对路径；
- 工具参数反序列化完整。

例如：

- Token 分区已确定后，组件引用未知 Token 应立即拒绝；
- 空 `TemplateId` 应立即拒绝，不创建 Revision；
- 如果允许一次事务同时新增 Token 和组件，应在事务整体校验通过后原子提交。

### 8.6 候选控制工具

删除模型可直接“冻结最终产物”的语义。模型只允许调用：

- `propose_candidate`
- `finish_repair_work_item`

其含义只是“当前 Revision 可以提交宿主评估”。最终是否合格由 Orchestrator 和质量门决定。

---

## 9. 结构化诊断与返修路由

### 9.1 统一诊断模型

建议将当前只有 `Code + Path + Message + Severity` 的诊断升级为：

```text
DiagnosticId
Code
GateId
Category
Severity
IsBlocking
Repairability
Owner
Path
EntityKeys
StableIds
SlideIds
EvidenceReferences
ModelMessage
UserMessage
SuggestedOperations
FirstSeenRevision
LastSeenRevision
OccurrenceCount
Fingerprint
InternalExceptionReference
```

### 9.2 Repairability

| 类型 | 说明 | 处理方式 |
| --- | --- | --- |
| `DeterministicAutoFix` | 宿主可无歧义修复 | 本地修复，不调用模型 |
| `ModelRepairable` | 需要设计或模板判断 | 创建受限 Repair Work Item |
| `InfrastructureRetry` | 网络、限流、Dispatcher、磁盘等 | 退避重试，不修改候选 |
| `UserActionRequired` | 缺失事实或用户约束冲突 | 进入 `NeedsUserAction` |
| `ProductBug` | Validator 崩溃或协议不可能状态 | 停止并记录工程缺陷 |
| `NonRepairable` | 当前策略下不可修复 | 停止，不发布 |

### 9.3 Owner

```text
Host
DesignAuthor
TemplateRepair
VisualReviewer
Infrastructure
User
Engineering
```

### 9.4 诊断指纹与收敛

`Fingerprint` 至少由以下信息稳定计算：

```text
Code + GateId + Path + EntityKey + 关键测量边界
```

系统据此判断：

- 同一问题是否复发；
- 修复后是否真正消失；
- 是否出现二周期或三周期循环；
- 是否需要换模型或停止当前策略；
- 哪些诊断可以合并为一个 Repair Work Item。

### 9.5 原始异常边界

WPF、XML、SDK、网络和文件异常必须先归一化为稳定诊断：

- 原始 `ex.ToString()` 仅保存在受保护的技术日志中；
- 给模型的是结构化、脱敏、可执行的诊断；
- 给用户的是安全、简洁、说明是否需要操作的消息；
- 未分类异常进入 `ProductBug`，不能让模型猜测如何修改设计系统。

---

## 10. 分层质量门 DAG

### 10.1 质量门定义

| Gate | 检查内容 | 失败责任方 |
| --- | --- | --- |
| `G0 InputQualification` | 页面完整覆盖、身份、输入指纹、路径隐私、上下文和附件预算 | Host / User / Infrastructure |
| `G1 MutationInvariants` | ID、字段和值域、Revision、权限、事务完整性 | 当前 Agent Turn 即时纠正 |
| `G2 DesignGraph` | Token、组件、页面类型、assignment、fallback、证据、ResourceId | DesignGraphRepairWorker |
| `G3 TemplateStaticCompile` | XML、SlideML 标签/属性、Token、Slot、每类模板覆盖 | TemplateRepairWorker |
| `G4 RuntimeRender` | 真实解析、布局、WPF 渲染、资源解析和异常分类 | TemplateRepair / Infrastructure |
| `G5 LayoutAccessibility` | 越界、裁切、溢出、安全区、最小字号、实际对比度、关键 Warning | TemplateRepair / VisualRefinement |
| `G6 SemanticVisualReview` | 页面类型适配、教学层级、跨页节奏、品牌与视觉质量 | Design / Template / Visual Worker |
| `G7 ArtifactVerification` | 序列化、回读、Schema、报告指纹、资源清单和发布一致性 | Publisher / Engineering |

### 10.2 产品默认必需门

产品默认模式下：

- `G0` 至 `G5`、`G7` 必须通过；
- `G6` 必须在生产发布模式通过；
- 如果所选模型不支持必需视觉评审，应路由到支持图片的模型；
- 如果没有任何可用视觉模型，不得把 `NotSupported` 当作成功，应进入 `InfrastructureBlocked`；
- 开发模式可以由显式质量策略降低门槛，但结果必须带有独立 Artifact 类型，不能冒充生产合格产物。

### 10.3 模板质量门改造

当前 `CoursewareTemplateStressTester` 应拆为多个可定位、可缓存、可返修的 Gate：

```text
TemplateStaticCompileGate
WpfRenderGate
LayoutMeasurementGate
AccessibilityGate
VisualReviewGate
```

压力样本应同时包括：

- 页面类型证据页的真实内容绑定；
- 布局簇代表页；
- 密度最小、中位和最大真实页；
- 长内容边界样本；
- 特殊字符；
- 缺图和错误资源；
- 多画布；
- fallback 路径。

不应只证明“没有抛 Error”。还必须确定性测量：

- 元素边界；
- 文本裁切与溢出；
- 安全区；
- 字号下限；
- 颜色对比度；
- 图片是否为占位图；
- 必需槽位是否真实呈现；
- 页面类型是否至少有一个通过全部样本的模板。

### 10.4 独立语义与视觉评审

结构合法不等于产品质量优秀。建议使用独立 Reviewer，而不是让 Author 自我批准：

- Reviewer 只读 Candidate、事实、渲染预览和联系表；
- 输出固定量表、分项分数、阻断项和建议修复范围；
- Reviewer 无写草稿权限；
- 确定性门失败时不运行或不采信语义评审；
- Reviewer 低置信度时可由第二模型复核，而不是直接通过；
- LLM 评审不能覆盖确定性测量结果。

### 10.5 Gate 报告与候选指纹

每份报告必须记录：

```text
CandidateFingerprint
InputFingerprint
QualityPolicyVersion
ValidatorVersion
CompilerVersion
RendererVersion
StressCorpusFingerprint
ResourceCatalogFingerprint
ModelVersion（若使用 LLM Reviewer）
```

Candidate 发生修改后，根据依赖图失效质量门：

- 修改 assignment：重跑 `G2` 及相关样本选择；
- 修改颜色 Token：重跑 `G2` 至 `G6`；
- 修改单个模板：只重跑该模板相关的 `G3` 至 `G6`；
- 修改证据文字：通常重跑 `G2` 和 `G6`；
- 输入变化：全部报告失效。

---

## 11. 外层自愈循环算法

建议 Orchestrator 使用以下逻辑：

```text
创建或恢复 AnalysisRun
校验输入并建立预算账本

如果没有 Candidate：
    创建 Authoring Work Item
    调用 Author Worker
    将成功操作提交为 Draft Revision
    生成 Candidate

循环：
    对 Candidate 运行所有失效的必需质量门

    如果全部通过：
        创建 Qualified Artifact
        原子发布并回读
        标记 Published
        结束

    将失败报告归一化为 Diagnostics

    如果存在 ProductBug：
        标记 ProductBug
        结束

    如果存在 UserActionRequired：
        标记 NeedsUserAction
        暂停

    如果存在 InfrastructureRetry：
        根据基础设施预算退避、切端点或暂停
        不修改 Candidate
        继续

    根据 Repair Router 聚合 ModelRepairable Diagnostics
    创建受限 Repair Work Item

    如果无剩余模型预算或检测到不收敛：
        尝试模型/策略回退
        若无可用回退，标记 Exhausted
        结束

    从当前最佳 Revision 创建修复分支
    调用对应 Repair Worker
    对新分支运行受影响质量门

    如果目标诊断减少且未引入重大回归：
        提升新分支为当前最佳 Candidate
    否则：
        废弃分支，保留原 Candidate

    继续循环
```

该循环可以跨多个 Agent 会话、进程和模型运行。它是产品工作流，不依赖某一段聊天历史持续存在。

---

## 12. 本次异常在新架构中的处理示例

### 12.1 首次写入组件

模型尝试提交：

```text
Component TokenIds = [clr-bg-analysis, ty-body-analysis, ...]
```

工具宿主读取当前 Token Registry，发现这些 Token 不存在。`G1 MutationInvariants` 立即返回：

```text
Code: UnknownTokenReference
Repairability: ModelRepairable
SuggestedOperations:
  - add_missing_tokens
  - replace_component_token_references
```

此次操作不提交，不增加 Revision，不污染草稿。模型在同一 Agent Turn 内选择：

- 补齐 Token；或
- 把组件改为引用已经存在的语义 Token。

### 12.2 首次写入空 TemplateId

工具在写入前发现 `StableId` 为空：

```text
Code: StableIdRequired
Path: pageTemplate.stableId
```

操作被拒绝，不产生实体，不产生脏 Revision。模型在同一轮重新提交非空 ID。

### 12.3 如果错误只在 Candidate Gate 才能发现

假设模板字段合法，但实际引用了不存在的 Token 或渲染后越界：

1. Agent 调用 `propose_candidate`；
2. Orchestrator 生成不可变 Candidate；
3. `G3` 或 `G5` 产生结构化诊断；
4. Repair Router 创建 `TemplateRepairWorkItem`；
5. Work Item 只携带：
   - 失败模板快照；
   - 相关 Token；
   - 失败样本；
   - 元素测量；
   - 相关页面事实；
   - 可选渲染预览图；
   - 允许修改路径；
6. TemplateRepairWorker 创建新分支并定向修改；
7. 只重跑受影响模板的 `G3` 至 `G6`；
8. 通过后继续全局发布门。

用户看到的是：

```text
正在验证设计系统
发现 2 个可自动修复的模板问题
正在修订模板 content-analysis
正在重新验证 6 个压力样本
模板验证已通过
```

用户不会看到未知 Token 堆栈，也不需要手工重试整份主题分析。

---

## 13. 上下文与会话策略

### 13.1 初始 Authoring

初始 Author Worker 可以获得：

- 完整分析输入；
- 结构化事实；
- 资源逻辑清单；
- 受控视觉样本；
- 输出协议和质量策略摘要。

### 13.2 定向 Repair

Repair Work Item 不应每次重发整份课件。它应包含：

- 当前 Candidate 和 Revision 身份；
- 相关草稿分区的规范快照；
- 开放诊断；
- 受影响页面、事实和资源；
- 已通过且不得破坏的约束；
- 允许的操作；
- 本工作项预算；
- 需要时重新附加相关原图或渲染图。

如果修复涉及视觉观察，必须重新附加对应图片或使用可恢复的多模态 Session。当前修复轮只发送文本的做法不可保留。

### 13.3 是否保留会话历史

建议：

- 一个 Work Item 内可以保留短会话，便于连续工具调用；
- Work Item 结束后会话可以释放；
- 新模型或新进程必须能只凭显式状态重新执行；
- 对话过长时可以压缩，但压缩结果不是业务真相；
- 不保存或展示隐藏推理过程。

因此：

> `WithHistory = true` 可以是局部优化，但不是架构基础；显式状态才是架构基础。

---

## 14. 收敛、预算与模型回退

### 14.1 不使用单一固定请求次数

固定请求次数只能限制成本，不能描述任务是否收敛。建议建立：

```text
CoursewareBudgetPolicy
CoursewareBudgetLedger
```

分别记录：

- Authoring 请求次数；
- Repair 请求次数；
- 每类诊断修复次数；
- 每实体修复次数；
- 输入、输出和图片 Token；
- 金额成本；
- 总墙钟时间；
- WPF Gate 执行次数与缓存命中；
- 基础设施重试；
- 模型切换次数。

### 14.2 初始建议预算

以下值仅作为离线评测前的默认起点：

- 1 次初始 Authoring；
- 最多 4 次定向模型返修；
- 同一诊断指纹连续无改善最多 2 次；
- 同一实体最多 3 个修复分支；
- 每个基础设施端点最多 2 次指数退避重试；
- 最多 1 次模型升级或模型族切换；
- 明确的 Token、成本和总耗时上限。

基础设施重试不应消耗模型内容返修预算。

### 14.3 不收敛判定

以下任一情况出现时，停止当前策略：

- 目标阻断诊断集合没有减少；
- 诊断严重度加权分数连续两轮无改善；
- CandidateFingerprint 出现二周期或三周期循环；
- 模型提交 no-op；
- 同一诊断在相关字段已经修改后完全复发；
- 新增同级或更高级阻断错误；
- 修改越过 Work Item 允许范围；
- 模型连续不调用要求的工具；
- Token、成本或总时间耗尽。

停止当前策略后，可以：

1. 换更可靠的工具调用模型；
2. 换更擅长 XML/代码修复的模型；
3. 缩小 Work Item；
4. 使用宿主确定性自动修复；
5. 最终进入 `Exhausted`，保留可恢复检查点但不发布。

### 14.4 模型策略

当前所有工作负载固定到同一模型的做法应替换为能力注册表：

```text
ContextWindow
ToolCallReliability
ImageCapability
StructuredOutputReliability
XmlRepairCapability
Latency
Cost
Health
```

典型路由：

- 上下文不足：更大上下文模型或有覆盖证明的分批归并；
- 连续遗漏工具：工具调用可靠性更高的模型；
- 模板/XML 问题：代码与结构化输出更强的模型；
- 视觉门：支持图片输入的模型；
- 限流或网络失败：同等级备用端点；
- 语义质量无改善：切换模型族，而不是重复同一 Prompt。

不得通过截断页面、删除工具 Schema 或降低必需质量门来迁就模型。

---

## 15. UI 与用户体验

### 15.1 不再展示原始分析对话作为主产品界面

当前“分析对话”直接展示模型消息，容易把内部工具过程、错误和非最终文本暴露给用户。生产 UI 建议改为：

- 主界面：产品化进度时间线；
- 详情：质量门、自动返修次数和安全诊断；
- 开发者模式：脱敏后的工具轨迹和技术日志引用；
- 不展示隐藏推理；
- 不把模型自然语言输出当作业务完成证据。

### 15.2 进度事件必须追加而不是按 Stage 覆盖

当前 `CoursewareWorkspaceViewModel.UpdateAnalysisStage` 按 Stage 替换事件。同一阶段发生多轮返修时，历史会被覆盖。

新事件建议包含：

```text
SequenceNumber
RunId
AttemptId
CandidateId
RepairWorkItemId
GateId
DiagnosticId
EventType
State
Timestamp
Duration
UserMessage
```

底层事件追加保存，UI 可以聚合，但不能丢失审计轨迹。

### 15.3 用户可见状态

建议使用：

- 正在读取完整课件；
- 正在建立设计系统；
- 正在验证设计令牌与页面覆盖；
- 发现 3 个可自动修复的问题；
- 正在修订 2 个模板；
- 正在重新渲染代表页面；
- 正在执行视觉质量评审；
- 全部质量门已通过；
- 正在发布结果。

### 15.4 只有真正需要用户时才中断

允许要求用户介入的情况：

- 输入文件或关键资源缺失；
- 品牌规范存在互相冲突的要求；
- 产品策略要求用户批准额外预算；
- 没有任何可用模型具备必需能力；
- 需要用户选择 Preserve / Evolve / Redesign 等不可推断策略；
- 视觉评审处于高风险低置信度且策略要求人工确认。

### 15.5 旧发布版本与新 Run 分离

重新分析时：

- 旧 `PublishedArtifact` 保持可用；
- UI 明确显示“正在使用上一发布版本”；
- 新 Candidate 不得更新旧工作台；
- 新 Run 成功发布后再显式切换 ArtifactId；
- 新 Run 失败、取消或耗尽预算都不能污染旧产物。

工作台准入条件应为：

```text
PublishedArtifact != null
```

而不是当前 `WorkspaceState` 是否为分析中、失败或取消。

---

## 16. 可观测性、安全与隐私

### 16.1 每个 Run 必须记录

- RunId、InputFingerprint、CandidateFingerprint、ArtifactId；
- Prompt、工具 Schema、模型、Validator、Compiler、Renderer 和质量策略版本；
- 每次模型调用的耗时、Token、图片数和成本；
- 每个工具操作及拒绝原因；
- 每个 Gate 的耗时、结果和缓存命中；
- 诊断首次出现、复发和解决 Revision；
- Repair Router 决策；
- no-op、循环和回归次数；
- 模型切换和基础设施重试原因；
- 发布和回读结果；
- 最终终止原因。

### 16.2 不应默认记录

- 完整课件正文；
- 本地绝对路径；
- 密钥或模型配置；
- 隐藏推理；
- 未脱敏的原始异常；
- 无关图片二进制。

### 16.3 可重放性

对于同一 Run，应能够使用：

- 输入快照；
- 草稿操作日志；
- Revision；
- Gate 版本；
- 模型请求摘要；

重建每个 Candidate，并验证其 Fingerprint 与历史一致。LLM 本身不要求确定性重放，但已提交的领域操作和质量门结果必须可审计。

---

## 17. 持久化、恢复与发布

### 17.1 Run Store 与 Artifact Store 分离

建议新增：

```text
ICoursewareAnalysisRunStore
ICoursewareDesignDraftStore
ICoursewareArtifactPublisher
ICoursewareArtifactCatalog
```

Run Store 保存：

- 工作流状态；
- Draft Revision；
- Candidate；
- Diagnostics；
- Gate Reports；
- Budget Ledger；
- Work Items；
- 事件。

Artifact Store 只保存通过全部门的发布产物。

### 17.2 崩溃恢复

每次以下动作后都应持久化检查点：

- 状态迁移；
- Revision 提交；
- Candidate 生成；
- Gate Report 完成；
- Repair Work Item 创建或结束；
- Budget 消耗；
- Qualified Artifact 生成；
- 发布开始和结束。

恢复时不依赖聊天消息重建状态。

### 17.3 原子发布

可以保留当前快照服务中“临时目录写入、回读验证、原子移动”的正确模式，但发布对象必须改为 `QualifiedArtifact`。

发布步骤：

1. 将 Qualified Artifact、设计系统、全部必需 Gate Report 和 Manifest 写入临时目录；
2. 校验所有指纹一致；
3. 使用当前版本 Validator、Compiler 和 Artifact Verifier 回读；
4. 原子移动到正式 Artifact 目录；
5. 更新 Catalog 指针；
6. UI 切换到新 ArtifactId。

发布失败进入 `PublicationFailed`，只重试发布同一 Qualified Artifact，不重新调用模型。

### 17.4 不考虑旧快照兼容

本次明确不做兼容：

- 旧主题分析快照不迁移；
- 旧 `CoursewareThemeAnalysisResult` 不作为新 Run 恢复源；
- 旧快照可以直接拒绝并要求重新分析；
- 新版本只接受带完整 Candidate、Gate Report 和 Artifact Manifest 的格式。

---

## 18. 建议删除、替换与保留的代码

### 18.1 建议直接删除

以下类型已经属于旧协议或不应继续承担主链职责：

- `Services/CoursewareThemeSubmissionTool.cs`
- `Services/CoursewareThemeAgentProtocol.cs`
- `CoursewareThemeRepairEnvelope`
- `CoursewareThemeAgentJsonSerializerContext`
- 旧 `CoursewareThemeValidator`
- 旧 `CoursewareThemeValidationResult`

如果不再需要旧 UI 展示，可以同时删除：

- `Models/CoursewareTheme.cs`
- `CoursewareDesignSystemThemeAdapter`
- 旧固定四类页面建议投影。

### 18.2 建议整体替换

| 当前类型 | 建议替换为 |
| --- | --- |
| `CopilotCoursewareThemeAgent` | `CoursewareModelWorker` + Author/Repair Worker |
| `CoursewareDesignSystemToolSet` | `CoursewareDraftToolApi` |
| `CoursewareDesignSystemDraftBuilder` | 不可变 `DraftAggregate` + `DraftStore` |
| `CoursewareThemeAnalysisService` | `CoursewareAnalysisWorkflowOrchestrator` |
| `CoursewareTemplateStressTester` | 多个独立 Quality Gate |
| `CoursewareDesignSystemAgentResult` | `ModelWorkItemOutcome` |
| `CoursewareThemeAnalysisResult` | `AnalysisRunOutcome` + `PublishedArtifact` |
| `CoursewareAnalysisCapabilityStates` | `QualityGateSummary` |
| `CoursewareAnalysisEvent` | 追加式 `AnalysisRunEvent` |
| `CoursewareWorkspaceState` | `AnalysisRunState` 与 `PublishedArtifactState` 分离 |
| `CoursewareThemeAnalysisSnapshotStore` | `RunStore` + `ArtifactPublisher` |

### 18.3 建议保留并强化

以下能力方向正确，可以迁入新架构：

- `CoursewareAnalysisSourceSnapshotBuilder`；
- `CoursewareAnalysisInputBuilder`；
- `CoursewareAnalysisInputValidator`；
- `CoursewareStructuredFactBuilder`；
- `CoursewareVisualSampleSelector`；
- `CoursewareDesignSystemValidator` 的确定性规则；
- `CoursewareTemplateCompiler`；
- 当前 WPF `SlideMlRenderPipeline`；
- 快照临时写入、回读和原子移动模式。

但需要注意：

- 当前 `CoursewareDesignSystem` 虽然是 `record`，属性仍可变；新 Candidate 与 Artifact 应使用真正不可变的领域快照；
- Validator 应拆成可定位 Gate，并补充真实对比度、模板覆盖和布局测量；
- WPF 管线必须接入真实 `ResourceId -> 文件` 解析，不能把占位图 Warning 当作产品通过。

### 18.4 建议新增类型

#### 工作流

- `CoursewareAnalysisRun`
- `CoursewareAnalysisRunState`
- `CoursewareAnalysisWorkflowOrchestrator`
- `CoursewareAnalysisRunPolicy`
- `ICoursewareAnalysisRunStore`

#### 草稿与候选

- `CoursewareDesignDraftRevision`
- `CoursewareDraftEntityKey`
- `CoursewareDraftOperation`
- `CoursewareDraftMutationResult`
- `CoursewareCandidate`
- `CoursewareCandidateFingerprint`

#### 诊断与返修

- `CoursewareDiagnostic`
- `CoursewareDiagnosticRegistry`
- `CoursewareRepairWorkItem`
- `CoursewareRepairRouter`
- `CoursewareRepairAttempt`

#### 质量门

- `ICoursewareQualityGate`
- `CoursewareQualityGateContext`
- `CoursewareQualityGateReport`
- `CoursewareQualityPolicy`
- `CoursewareGateDependencyGraph`

#### 模型与预算

- `CoursewareModelPolicy`
- `CoursewareModelCapabilityProfile`
- `CoursewareModelAttemptRecord`
- `CoursewareBudgetPolicy`
- `CoursewareBudgetLedger`

#### 发布

- `QualifiedCoursewareArtifact`
- `PublishedCoursewareArtifact`
- `CoursewareArtifactManifest`
- `ICoursewareArtifactPublisher`
- `CoursewareArtifactVerifier`

#### 渲染与资源

- `ICoursewareResourceResolver`
- `CoursewareRenderDiagnosticNormalizer`
- `CoursewareLayoutMeasurementReport`

---

## 19. 实施路线

### 当前实施进度

当前已开始按本方案实施。第一轮工作没有直接修改旧 WPF 主题分析入口，而是先在 `CoursewarePptxGenerator.Core` 建立后续协议、工作流和发布链路共同依赖的领域边界。

已完成内容：

- 新增 `CoursewareAnalysisRun` 和 `CoursewareAnalysisRunState`：
  - 覆盖主链状态、可恢复旁路状态、终态和 `PublicationFailed`；
  - 强制合法迁移、恢复检查点、版本递增和时间单调性；
  - `PublicationFailed` 只能回到 `Publishing`，不能重新调用模型或重新评估 Candidate。
- 新增 `CoursewareDesignDraftRevision`：
  - 使用规范化 JSON 快照隔离外部可变 DTO；
  - 记录父 Revision 指纹和当前 Revision 指纹；
  - 拒绝无变化 Revision；
  - 同一 Revision 每次读取都返回隔离副本。
- 新增 `CoursewareCandidate`、`CoursewareQualityGateReport`、`QualifiedCoursewareArtifact`、`PublishedCoursewareArtifact` 和 `CoursewareArtifactManifest`：
  - Candidate 指纹绑定输入指纹、质量策略版本和 Draft Revision；
  - 缺失、失败或 `NotSupported` 的必需 Gate 不能生成 Qualified Artifact；
  - Gate Report 必须绑定同一 Candidate 指纹和质量策略版本；
  - Published Artifact 必须来自匹配的 Qualified Artifact 和已回读验证的 Manifest；
  - Manifest 指纹被篡改时拒绝发布对象构造。
- 新增 `ICoursewareAnalysisRunStore` 及线程安全内存实现：
  - 只允许创建 Version 0、状态为 `Created` 的 Run；
  - 使用预期 Version 执行乐观并发更新；
  - 拒绝重复创建、陈旧写入和非连续版本；
  - 取消操作不会修改已提交快照。
- 新增 `ICoursewareDesignDraftStore` 及线程安全内存实现：
  - 保存按 Revision 排序的不可变修订序列；
  - 通过 `OperationId` 支持幂等提交；
  - 拒绝陈旧 Revision、断链 Revision 和同一 `OperationId` 对不同 Revision 的复用；
  - 可读取最新 Revision 和完整 Revision 序列，为后续重放与崩溃恢复提供契约基础。
- 新增和扩展 `CoursewareAnalysisWorkflowDomainTests`：
  - 当前 Phase 1 领域测试共 18 项；
  - `CoursewarePptxGenerator.Core.Tests` 共 52 项测试全部通过；
  - 完整解决方案构建通过；
  - 新增实现同时通过 `net6.0` 和 `net10.0` Core 构建。

当前实现文件：

- `Pptx/PptxGenerator/Code/CoursewarePptxGenerator.Core/Models/CoursewareAnalysisRun.cs`
- `Pptx/PptxGenerator/Code/CoursewarePptxGenerator.Core/Models/CoursewareDesignDraftRevision.cs`
- `Pptx/PptxGenerator/Code/CoursewarePptxGenerator.Core/Models/CoursewareArtifactModels.cs`
- `Pptx/PptxGenerator/Code/CoursewarePptxGenerator.Core/Analysis/CoursewareAnalysisRunStore.cs`
- `Pptx/PptxGenerator/Code/CoursewarePptxGenerator.Core/Analysis/CoursewareDesignDraftStore.cs`
- `WPFDemo/CoursewarePptxGenerator/CoursewarePptxGenerator.Core.Tests/CoursewareAnalysisWorkflowDomainTests.cs`

尚未完成的 Phase 1 边界：

- 当前 Store 是用于固定领域语义和测试的内存实现，尚未提供跨进程持久化和崩溃恢复；
- 当前 `CoursewareDesignSystem` DTO 仍然可变，Revision 通过序列化副本实现快照隔离，尚未完成全部领域对象的真正不可变建模；
- Candidate、Qualified Artifact 和 Published Artifact 尚未接入 Orchestrator、Publisher、页面生成 API 和 WPF 工作台；
- 旧 `CoursewareDesignSystemDraftBuilder`、旧 Theme Agent、旧快照和旧 WPF 状态语义仍然存在；
- 因此目前只能认定“Phase 1 领域基线已完成”，不能认定 Phase 1 的产品切换完成，更不能开放新发布链路。

### 重构切换原则

本方案不建议在旧链路上长期边运行边打补丁。进入重构后应设置明确切换边界：

- 旧主题分析入口默认关闭，或明确标记为仅供开发诊断、不得发布产物；
- 在新 `QualifiedArtifact` 发布链路可用前，不新增旧快照、不扩展旧 `CoursewareThemeAnalysisResult`；
- 如果业务必须保留临时入口，必须采用失败封闭：设计系统、模板或渲染任一必需校验失败时，不保存快照、不进入 `AnalysisReady`、不创建或复用页面工作台；
- 新旧链路不得同时写入同一快照目录或共享“当前主题”指针；
- 正式切换以离线评测、故障注入、崩溃恢复和真实 WPF Gate 全部达到发布门槛为条件；
- 切换完成后直接删除旧链路，不维护双写、双读或兼容转换层。

这不是把临时阻断当作最终方案，而是避免在重构期间继续向用户交付已知不具备质量资格的结果。

### Phase 0：建立评测基线

- 固定一组真实课件和故障注入课件；
- 记录当前首轮成功率、最终成功率、错误分布、Token、耗时和人工质量评分；
- 保存本次未知 Token、空 TemplateId、模板编译失败、视觉不合格等复现样本。

完成条件：后续重构可以量化证明优于当前实现，而不是只证明“代码更复杂”。

### Phase 1：建立 Run、Revision 和 Artifact 资格边界

- [x] 新建 AnalysisRun 状态机；
- [x] 新建不可变 Draft Revision Store 契约和内存实现；
- [x] 新建 Candidate 和 Fingerprint；
- [x] 新建 Qualified/Published Artifact 领域边界；
- [ ] 从类型和 API 上禁止 Candidate 直接进入工作台。

当前结论：领域类型边界和并发语义已经建立；工作台准入与真实 Publisher 尚未切换，因此本 Phase 的产品完成条件仍未全部满足。

完成条件：任何未通过全部门的对象都无法被 Publisher 和页面生成 API 接收。

### Phase 2：重建工具协议

- [x] 增加 read、delete、replace、rename 和 batch transaction；
- [x] 引入宿主 `EntityKey`，并将身份映射纳入 Revision 指纹和重放；
- [x] 写入前执行 StableId、载荷类型和结构化直接引用校验；
- [x] 强制 `ExpectedRevision`、`OperationId` 幂等和 Work Item 修改范围；
- [x] 成功变更产生单一新 Revision，失败和 no-op 不产生 Revision；
- [x] 提供仅创建 `CoursewareCandidate` 的 `propose_candidate` 领域边界；
- [ ] 接入 AgentLib 工具特性、模型 Prompt 和新 Orchestrator。

当前结论：Phase 2 第一批 Core 领域协议已经完成，尚未接入具体 Agent Framework。重命名存在跨实体引用传播时，当前协议要求 Work Item 具有完整实体范围，避免隐式越权；Assignment 的 `SlideId` 属于不可变输入身份，不允许通过草稿操作重命名。

完成条件：空 ID、错误 ID、未知引用等故障在协议上都可被拒绝或修复，不存在不可寻址脏实体。

### Phase 3：建立质量门 DAG

- 将完整设计系统验证拆为 Gate；
- 将模板编译、WPF 渲染、布局、无障碍和视觉评审接入同一 Candidate；
- 建立 Gate 依赖和失效缓存；
- 所有报告绑定 CandidateFingerprint。

完成条件：任一必需 Gate 失败都无法生成 Qualified Artifact。

### Phase 4：实现诊断、返修路由与外层循环

- 统一诊断模型；
- 实现 Repairability 和 Owner；
- 根据 Gate 和诊断创建受限 Work Item；
- 实现分支评估、无改善回滚和循环检测；
- 重新附加视觉证据。

完成条件：故障注入的可修复问题能在内部自动闭环，且不会把底层异常展示给用户。

### Phase 5：实现预算与模型策略

- 建立 Budget Ledger；
- 区分模型返修预算和基础设施重试预算；
- 建立模型能力注册表和健康检查；
- 实现按错误类型切换模型或端点。

完成条件：系统能够解释为什么继续、为什么换模型、为什么暂停或为什么耗尽，而不是依赖固定次数。

### Phase 6：重做持久化与 UI

- Run Store 与 Artifact Store 分离；
- 支持任意状态崩溃恢复；
- 进度改为追加式事件；
- 主界面展示产品化时间线；
- 旧发布版本与新 Run 分离；
- 移除原始异常和模型对话作为默认用户界面。

完成条件：分析失败、取消或耗尽都不会污染上一有效版本；只有 Published Artifact 能进入工作台。

### Phase 7：删除旧链路

- 删除旧 Theme 协议和兼容投影；
- 删除固定两轮 `CopilotCoursewareThemeAgent`；
- 删除旧快照恢复；
- 删除把 `TemplateValidation = Failed` 当作可完成结果的代码；
- 更新所有测试和文档。

完成条件：仓库不存在两套并行的主题分析真相。

---

## 20. 测试与离线评测

### 20.1 Core 单元测试

必须覆盖：

- 状态机合法和非法迁移；
- Revision 不可变性；
- 操作幂等性；
- 并发 Revision 冲突；
- 分支创建、提升和废弃；
- 空 StableId 在写入前拒绝；
- 通过 EntityKey 删除脏实体；
- 原子重命名并同步引用；
- assignment 清除和替换；
- Work Item 写范围强制；
- 诊断稳定指纹和去重；
- Repair Router 分类；
- Gate 依赖和失效传播；
- 所有报告绑定同一 CandidateFingerprint；
- 任一 Gate 失败都不能创建 Qualified Artifact；
- no-op、二周期和三周期检测；
- Budget 消耗和终止；
- Revision 重放得到相同指纹。

### 20.2 Agent 协议测试

使用脚本化 Fake Model 覆盖：

- 初始一轮成功完成全部工具调用；
- 首轮引用未知 Token，同一 Turn 修复成功；
- 首轮空 TemplateId，工具拒绝后重提成功；
- 模型使用旧 Revision；
- 模型遗漏工具；
- 模型调用越权工具；
- 模型产生 no-op；
- 模型反复修改无关分区；
- Agent Session 丢失后从显式状态恢复；
- Repair Work Item 重新获得相关截图；
- 主模型不收敛后备用模型成功；
- 所有模型失败后进入 `Exhausted` 且不发布；
- `SendMessageRunState.IsSuccess == false` 被正确分类。

### 20.3 质量门集成测试

至少构造：

- 核心设计图通过、模板静态编译失败；
- 静态编译通过、WPF 渲染失败；
- 渲染无 Error 但元素越界；
- 文本被裁切；
- 关键 Warning 按策略阻断；
- 字号低于下限；
- 实际颜色对比度不足；
- 安全区越界；
- ResourceId 已登记但文件缺失；
- PageType 没有通过模板；
- fallback 循环；
- 模板与页面类型 Slot 不一致；
- 视觉观察与实际附件不一致；
- 修复后全部门通过并发布；
- 修改一个模板时不重复验证无关模板。

### 20.4 WPF 真实管线测试

在固定 STA、字体、DPI 和系统镜像下覆盖：

- XML Parse、Layout、Measure、Render；
- 多画布；
- 中英文混排；
- 长英文单词；
- 特殊字符；
- 超长文本；
- 表格和复杂组件；
- 真实 ResourceId 图片解析；
- 缺图和解码失败；
- Dispatcher 取消和异常；
- 预览图与联系表生成；
- 结构化几何回归和必要的黄金图评测。

### 20.5 持久化与恢复测试

- 每个状态落盘后模拟进程终止并恢复；
- `Repairing` 中恢复；
- `Evaluating` 中恢复；
- `Qualified` 后发布失败并重试；
- 未通过 Candidate 不得被工作区加载；
- Gate Report 与 Candidate 指纹不一致时拒绝；
- Validator 或 Renderer 版本变化时重新验证；
- 草稿、诊断或产物被篡改时拒绝；
- 原子发布失败不覆盖上一有效版本。

### 20.6 UI 测试

- 可修复错误显示为自动返修，不进入失败页；
- 无 Published Artifact 时工作台入口禁用；
- 有旧 Artifact 时明确显示仍在使用旧版本；
- 新 Run 失败不污染旧工作台；
- 默认界面不显示原始堆栈；
- 同一 Gate 多轮返修事件不会覆盖历史；
- `NeedsUserAction`、`InfrastructureBlocked`、`Exhausted` 使用不同文案和操作；
- `PublicationFailed` 只提供重试发布。

### 20.7 离线模型评测

固定语料至少包含：

- 简单和大型课件；
- 多画布；
- 高密度文本；
- 图文混排；
- 表格、公式和代码；
- 中英文混排；
- 多资源和缺失资源；
- 视觉样本；
- Prompt 注入文本；
- 各类故意注入的可修复设计系统错误。

统计指标：

- 首轮 Candidate 通过率；
- 预算内自动修复成功率；
- 最终全部必需 Gate 通过率；
- 平均和 P95 修复轮数；
- 平均和 P95 Token、成本和耗时；
- no-op 率；
- 循环率；
- 新增回归错误率；
- 模型协议失败率；
- 模型切换率；
- 用户可见失败率；
- 人工语义和视觉评分。

建议发布门槛：

- 未通过质量门却发布：`0`；
- 状态与 Revision 重放一致率：`100%`；
- Work Item 范围外修改：`0`；
- 确定性故障注入诊断命中率：`100%`；
- 可修复合成缺陷在预算内修复成功率：不低于 `95%`；
- 用户默认界面出现底层异常堆栈：`0`；
- 语义与视觉质量不得低于当前人工基线。

---

## 21. 不应采用的方案

| 方案 | 为什么不正确 |
| --- | --- |
| 把 `MaximumRequestCount` 从 2 改成 5 或 10 | 不可删除的脏实体仍不可修复；冻结后 Gate 仍无法反馈；只会增加成本和延迟 |
| 只加强 System Prompt | Prompt 不能凭空增加 read/delete/rename/rollback 工具，也不能改变发布门 |
| 只改成 `WithHistory = true` | 历史不是持久化领域状态，无法可靠恢复、审计和切模型 |
| 每轮重新生成整个设计系统 | 会破坏已通过分区、增加回归和 Token 消耗 |
| 只在最终 Complete 做完整校验 | 错误发现过晚，可能已经形成不可寻址脏状态 |
| 核心验证通过就冻结 | 模板、渲染、布局和视觉错误仍在冻结后发生 |
| 把异常吞掉或降级为 Warning | 会制造假成功并污染下游 |
| 失败时返回默认主题 | 与“全课件分析完成”语义冲突，掩盖真正问题 |
| 展示 `TemplateValidation = Failed` 但仍允许进入工作台 | 状态说明不能替代访问控制和产物资格 |
| 所有 Warning 永不阻断 | 占位图、资源缺失和裁切等 Warning 可能是产品级失败 |
| 多建几个 Agent 但共享聊天历史 | 只是把隐式状态分散到更多会话，仍不可恢复和重放 |
| 直接把 Validator 错误字符串拼进 Prompt | 缺少稳定诊断身份、修复责任、允许动作和收敛判断 |
| 用户点击“重试”时从头再跑 | 放弃已有有效分区和诊断，重复随机失败 |

---

## 22. 最终验收清单

### 领域与工作流

- [ ] AnalysisRun 可持久化、恢复并审计；
- [x] Draft Revision 不可变；
- [x] Candidate 与 Published Artifact 类型隔离；
- [ ] 新 Run 失败不污染旧 Artifact；
- [ ] 所有状态迁移具有明确原因和事件。

### 工具协议

- [ ] 写入前校验空 ID 和局部字段；
- [ ] Agent 可读取当前草稿；
- [ ] Agent 可删除、替换和重命名实体；
- [ ] 实体以宿主 EntityKey 寻址；
- [ ] 相互引用变更支持事务；
- [ ] Work Item 修改范围由宿主强制；
- [ ] 模型不能直接冻结或发布。

### 自愈与质量门

- [ ] 核心引用错误可自动修复；
- [ ] 模板编译错误可自动修复；
- [ ] WPF 渲染和布局错误可回馈 Repair Worker；
- [ ] 视觉修复轮能获得相关图片；
- [ ] 失败分支可丢弃；
- [ ] 无改善和循环可检测；
- [x] 必需 Gate 全通过前不能生成 Qualified Artifact。

### 预算与模型

- [ ] 模型返修和基础设施重试使用独立预算；
- [ ] 每次继续、切模型、暂停和终止都有可解释原因；
- [ ] 模型能力与健康状态可路由；
- [ ] 不通过截断输入或降低质量门适配模型。

### 用户体验

- [ ] 可修复内部错误不展示给用户；
- [ ] 默认 UI 不展示原始堆栈和隐藏推理；
- [ ] UI 展示真实 Gate 和返修进度；
- [ ] 只有 Published Artifact 能进入工作台；
- [ ] 只有确实需要决策时才请求用户操作。

### 工程质量

- [ ] 故障注入和脚本化模型测试覆盖完整闭环；
- [ ] 真实 WPF Gate 在稳定环境运行；
- [ ] 任意状态可崩溃恢复；
- [ ] 产物发布回读和指纹验证通过；
- [ ] 离线评测达到发布门槛。

---

## 23. 最终结论

当前主题分析已经拥有完整输入、结构化事实、设计系统 DTO、草稿工具、确定性 Validator、模板编译器和 WPF 渲染基础，这些能力值得保留。但它们目前被串成了“一次生成、一次补救、失败抛给用户”的调用链，而不是一个真正的产品工作流。

下一步不应继续围绕本次未知 Token 或空 `TemplateId` 做局部补丁。应直接重建主题分析的控制平面：

1. 以持久化 AnalysisRun 管理生命周期；
2. 以不可变 Draft Revision 和 Candidate 管理模型产物；
3. 以结构化 Diagnostic 和 Repair Work Item 驱动定向返修；
4. 以质量门 DAG 覆盖设计图、模板、渲染、布局、无障碍、语义和视觉；
5. 以预算、收敛检测和模型回退控制成本与停机；
6. 以 Qualified/Published Artifact 建立不可绕过的产品发布边界；
7. 只把真正通过全部门的结果交付用户。

最终产品语义应收敛为：

> Agent 的错误属于系统内部迭代过程；用户看到的只能是已通过质量门的结果，或确实需要用户决策的明确问题。

---

## 24. 建议下一轮对话的工作

下一轮建议继续执行 **Phase 2 的第一批工作：建立可测试的 Draft 工具领域协议**。不要立即修改 `CopilotCoursewareThemeAgent`、WPF ViewModel 或旧快照服务，也不要在新 Orchestrator 尚未存在时删除旧链路。

### 24.1 下一轮目标

在 `CoursewarePptxGenerator.Core` 中建立不依赖具体 Agent Framework 的强类型草稿操作协议，使以下错误在调用模型前后都具有确定、可测试的处理语义：

- 空 StableId 在写入前被拒绝，不产生 Revision；
- 模型字段不再作为宿主编辑寻址键；
- 实体可以通过宿主生成的 `EntityKey` 被读取、替换和删除；
- StableId 可以原子重命名，并由宿主同步直接引用；
- 同一事务可以提交相互引用的 Token、组件和模板；
- `OperationId`、`ExpectedRevision` 和 Work Item 权限由宿主强制，而不是只写进 Prompt；
- 每次成功操作都提交一个新 `CoursewareDesignDraftRevision`，失败操作不污染草稿。

### 24.2 建议实施范围

建议新增或完善：

- `CoursewareDraftEntityKey`
- `CoursewareDraftEntityKind`
- `CoursewareDraftOperation`
- `CoursewareDraftOperationScope`
- `CoursewareDraftWorkItemScope`
- `CoursewareDraftMutationResult`
- `CoursewareDraftManifest`
- `CoursewareDraftSectionSnapshot`
- `CoursewareDraftAggregate`
- `CoursewareDraftMutationService`

建议第一批只支持以下操作：

1. 读取 Draft Manifest；
2. 读取指定分区或实体；
3. 新增实体并由宿主分配 `EntityKey`；
4. 按 `EntityKey` 原子替换实体；
5. 按 `EntityKey` 删除实体；
6. 重命名 StableId 并同步引用；
7. 批量事务提交；
8. 提交 Candidate Proposal，但不允许冻结或发布。

暂不接入：

- AgentLib 工具特性和模型 Prompt；
- WPF 页面和进度事件；
- 模板编译、WPF Render Gate 和视觉评审；
- 文件持久化、Artifact Publisher 和旧快照迁移；
- 旧 Theme Agent 删除。

### 24.3 下一轮必须先验证的现有模型

开始编码前应先检查：

- `CoursewareDesignSystem`、Token、Component、PageType、Template 和 Assignment 的全部引用字段；
- 当前 `CoursewareDesignSystemValidator` 已有的 ID、引用和覆盖规则；
- `CoursewareDesignSystemDraftBuilder` 与 `CoursewareDesignSystemToolSet` 的全部调用点；
- 现有源生成 JSON 上下文能否序列化新增操作和快照模型；
- `net6.0` 兼容限制，避免使用目标框架缺少支撑类型的 `required` members API。

### 24.4 下一轮测试验收

至少新增以下 Core 单元测试：

- 宿主生成的 `EntityKey` 稳定且不依赖 StableId；
- 空 StableId 写入失败且 Revision 不变；
- 未知 Token 引用在组件写入时立即失败；
- 同一事务新增 Token 和引用该 Token 的组件可以原子成功；
- 事务中任一操作失败时全部回滚；
- 按 `EntityKey` 删除脏实体；
- StableId 重命名后组件、页面类型、模板和 assignment 引用同步更新；
- 陈旧 `ExpectedRevision` 被拒绝；
- 重复 `OperationId` 返回相同提交结果；
- Work Item 范围外修改被拒绝；
- no-op 操作不产生新 Revision；
- 完整 Revision 序列重放后得到相同指纹。

完成后必须运行：

- `CoursewarePptxGenerator.Core.Tests` 全部测试；
- `CoursewarePptxGenerator.Core` 的 `net6.0` 与 `net10.0` 构建；
- 完整解决方案构建；
- Git 差异审查，确保没有修改用户已有文档和无关旧链路。

### 24.6 本轮实施结果

已完成 Phase 2 第一批 Core 实现：

- 新增 `CoursewareDraftEntityKind`、`CoursewareDraftEntityKey`、操作、范围、Manifest、Section Snapshot、Mutation Result 和 Candidate Proposal 协议；
- `CoursewareDesignDraftRevision` 现在同时快照设计系统与宿主 EntityKey 身份映射，二者共同参与 Revision 指纹；
- 新增 `CoursewareDraftAggregate`，支持从任意 Revision 重建 Manifest，并按分区或 EntityKey 读取隔离快照；
- 新增 `CoursewareDraftMutationService`，支持事务化 add、replace、delete、StableId rename 和 batch commit；
- 同一事务可新增相互引用实体，事务末统一校验 StableId 唯一性和结构化直接引用，任一失败全部回滚；
- Token、Component、PageType 和 Template 重命名会同步全部结构化直接引用，EntityKey 保持不变；
- `ExpectedRevision`、`OperationId`、no-op 幂等登记和 Work Item 范围均由宿主强制；
- no-op 成功但不产生 Revision；Candidate Proposal 不冻结、不资格认证、不发布；
- 暂未修改 WPF、旧 Theme Agent、旧快照服务和质量门。

新增或修改的核心文件：

- `Pptx/PptxGenerator/Code/CoursewarePptxGenerator.Core/Models/CoursewareDraftProtocol.cs`
- `Pptx/PptxGenerator/Code/CoursewarePptxGenerator.Core/Models/CoursewareDesignDraftRevision.cs`
- `Pptx/PptxGenerator/Code/CoursewarePptxGenerator.Core/Analysis/CoursewareDraftAggregate.cs`
- `Pptx/PptxGenerator/Code/CoursewarePptxGenerator.Core/Analysis/CoursewareDraftMutationService.cs`
- `Pptx/PptxGenerator/Code/CoursewarePptxGenerator.Core/Analysis/CoursewareDesignDraftStore.cs`
- `Pptx/PptxGenerator/Code/CoursewarePptxGenerator.Core/Serialization/CoursewareDesignJsonSerializerContext.cs`
- `WPFDemo/CoursewarePptxGenerator/CoursewarePptxGenerator.Core.Tests/CoursewareDraftProtocolTests.cs`

验证结果：

- 新增 Draft 协议测试 16 项全部通过；
- `CoursewarePptxGenerator.Core.Tests` 共 68 项全部通过；
- `CoursewarePptxGenerator.Core` 的 `net6.0` 和 `net10.0` 构建通过；
- 完整解决方案构建通过；
- Git 差异审查未发现对 WPF、旧 Theme Agent、旧快照和质量门的修改。

### 24.5 建议下一轮对话开场指令

可以在下一轮对话中直接使用：

> 继续按照《主题分析 Agent 自愈闭环与产品化重构方案》执行 Phase 2 第一批工作。先读取文档第 24 节和当前 Phase 1 实现，建立不依赖 Agent Framework 的 Draft 工具领域协议，完成 EntityKey、读取、替换、删除、StableId 原子重命名、批量事务、ExpectedRevision、OperationId 幂等和 Work Item 范围强制。暂不修改 WPF、旧 Theme Agent、旧快照和质量门。补齐 Core 单元测试，并验证 Core 多目标构建、完整 Core 测试和解决方案构建。
