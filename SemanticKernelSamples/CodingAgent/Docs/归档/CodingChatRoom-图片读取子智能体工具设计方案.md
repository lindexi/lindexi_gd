# CodingChatRoom 图片读取子智能体工具设计方案

## 1. 概述

为 `CodingChatRoom.AvaloniaShell` 承载的主编程智能体新增 `AnalyzeImage` 工具：工具接收图片路径数组与分析指令，内部启动一次独立的多模态子智能体运行，子智能体通过专用结果提交工具返回结论。

核心约束：

1. 子智能体使用界面当前选中的主模型，不单独保存模型配置，也不提供模型选择界面；
2. 子智能体复用 `CodingAgent` 的编程 System 提示词（通过同一初始化方法共享而非复制）；
3. 在编程提示词之后追加一条“图片子智能体模式”System 消息；
4. 用户消息由要求文本（`TextContent`）和全部图片（`DataContent`）组成；
5. 最终结果必须通过专用提交工具返回，普通助手文本不作为结果；
6. 子智能体工具集合只包含结果提交工具，不包含任何编程工具；
7. 图片路径直接读取，不附加工作区边界、路径授权或文件大小限制；
8. 每次调用使用全新、短生命周期的上下文，不共享、不持久化 `AgentSession`。

## 2. 目标

### 2.1 功能目标

- 工具名：`AnalyzeImage`
  - 输入：`filePath`（图片文件路径数组，可为一个或多个）、`AnalysisInstruction`（分析指令）
  - 输出：子智能体提交的文本结论

典型用途：

- 阅读界面截图并定位异常信息；
- 分析设计稿、流程图、架构图或 UML 图；
- 从日志截图中提取关键信息；
- 对照代码任务理解图片中的交互或布局；
- 识别图片中的文本、控件、状态和错误提示。

### 2.2 非目标

以下内容不属于本功能，均为确定的设计边界，不存在“首版暂缓、后续再做”的含义。每一项不做都有明确理由，且由本设计的结构直接保证，不依赖任何版本阶段：

- 图片编辑、生成，以及视频、音频或文档处理：本功能只做图片读取与分析，编辑、生成与其他媒体处理属于独立领域；
- 独立的子智能体模型选择界面：子智能体跟随主模型是固定决策（见第 5 章），不存在独立的模型选择；
- 子智能体会话持久化：子智能体是短生命周期执行，不产生可持久化的会话（见 9.1）；
- 与主会话共享 `AgentSession`：上下文隔离是设计约束（见 9.1）；
- 把子智能体的普通文本输出直接当作最终结论：结果必须以提交工具返回（见 4.2）；
- 递归调用图片分析工具：子智能体工具集合只包含提交工具，不存在可递归的入口（见 9.2）；
- 图片路径安全策略、工作区范围限制或路径授权系统：路径直接读取，不附加安全边界（见第 8 章）。

### 2.3 演进能力

“不做”不意味着未来可以靠推翻本设计来补做。对可能成为未来方向的内容，本设计给出明确的承载路径，保证演进无需推倒重来：

- **并行分析多个独立图片任务**：每次 `AnalyzeImage` 调用都是独立的短生命周期子智能体运行，状态互不共享；未来若要并行分析多个独立任务，在外部并发发起多次 `AnalyzeImage` 调用即可，子智能体内部结构无需修改；
- **图片路径安全策略**：路径在 `AnalyzeImage` 工具入口被接收并直接加载（见第 8 章）；未来若要增加工作区范围或授权校验，只需在该入口插入校验，不影响子智能体运行与结果提交；
- **子智能体独立模型选择**：模型来源只有“调用时读取 `PrimaryModel`”一处（见第 5 章）；未来若要独立指定模型，替换该来源即可，不涉及消息组装与结果提交逻辑。

其余条目（图片编辑与生成、视频/音频/文档处理、会话持久化、共享 `AgentSession`、普通文本作为结果）在任何版本中都不是本功能的目标，属于永久边界。

## 3. 实现基础与架构边界

### 3.1 现有能力

当前代码已具备大部分基础设施：

- `CodingAgent` 支持 `IReadOnlyList<AIContent>` 多模态输入；
- `DataContent.LoadFromAsync` 可从文件路径加载图片；
- `CodingWorkspaceContentTools.LoadImageAsync` 已有图片工具实现，保持原样，不在本功能改动范围内；
- `CodingWorkspaceToolLease` 为一次运行提供稳定的工具快照（用于主工具装配）；
- `AgentApiEndpointManager.PrimaryModel` 随 `ChatViewModel.SelectedModel` 切换；
- `GeneratedTextSubmissionTool` 提供模型提交文本的基础能力；
- `SubAgentToolProvider` 已实现子智能体流式运行、工具结果采集与未调用提交工具时的纠正重试；
- 聊天消息模型已支持展示子智能体过程与工具调用。

因此无需设计通用子智能体框架，只需补充图片输入、当前模型选择、编程提示词初始化与结果提交约束。

### 3.2 架构边界

功能入口虽在 `CodingChatRoom.AvaloniaShell`，核心实现应放在 `AgentLib.Coding`，避免落入 Avalonia ViewModel：

```text
CodingChatRoom.AvaloniaShell
  └─ 组合根提供 AgentApiEndpointManager

AgentLib.Coding
  ├─ CodingAgent
  ├─ CodingWorkspaceToolProvider
  ├─ CodingImageSubAgentTool
  └─ CodingImageSubAgentExecutor
```

职责划分：

- Shell 负责模型配置与界面模型切换；
- `AgentLib.Coding` 负责图片加载、子智能体运行与结果提交；
- 工具作为编程智能体的运行时能力装配，不进入会话数据或 Shell 设置；
- Avalonia UI 不直接创建或调用图片子智能体。

由 `CodingAgent` 或图片工具持有 `AgentApiEndpointManager`，在工具实际调用时读取当前模型，不引入复杂的模型解析接口体系。

## 4. 工具契约

### 4.1 AnalyzeImage（主智能体工具）

```text
AnalyzeImage
  描述：使用独立的多模态子智能体读取并分析一张或多张图片。适用于截图、设计稿、
        流程图、架构图和图片文字提取。输入图片路径数组和明确的分析指令，返回
        子智能体提交的最终文本结论。
  参数：
    filePath    图片文件路径数组，可包含一个或多个文件。
    AnalysisInstruction 给子智能体的分析指令，明确需要观察、提取、比较或判断的内容，
                        以及期望的输出格式。
```

`AnalysisInstruction` 是给子智能体的任务指令，不是对图片本身的处理要求；本工具不进行图片编辑或处理。
```

返回值直接使用提交工具收集到的文本，不包装复杂 DTO。

### 4.2 SubmitImageAnalysisResult（子智能体专用提交工具）

```text
SubmitImageAnalysisResult
  参数：
    result 根据分析指令和图片得到的最终完整结论。
```

- 只在图片子智能体内部可见，不暴露给主智能体；
- 工具本身返回简短确认文本；真正返回给主智能体的是提交收集器保存的 `result`。

现有 `load_image` 工具保持原样，不在本功能改动范围内。

## 5. 模型选择

子智能体在每次工具调用开始时读取 `AgentApiEndpointManager.PrimaryModel`，自然跟随界面 `ChatViewModel.SelectedModel` 的切换，不保存第二份模型状态。

时序：

1. 用户在界面切换模型；
2. `SelectedModel` 更新 `AgentApiEndpointManager.PrimaryModel`；
3. 主智能体运行并调用图片工具；
4. 工具读取当前 `PrimaryModel`；
5. 本次子智能体固定使用该模型直到结束。

约束：

- 不在工具创建时捕获模型，避免长期使用旧模型；
- 当前模型必须支持图片输入与工具调用，否则直接报告无法完成调用，不静默切换到其他模型。

## 6. 提示词与消息顺序

推荐消息顺序：

```text
System：CodingAgent 通用 SystemPrompt
System：CodingAgent CodePrompt
System：CodingAgent SandboxPrompt
System：图片子智能体模式提示词
User：TextContent(AnalysisInstruction) + DataContent(全部图片)
```

图片子智能体模式提示词：

```text
你当前运行在图片读取子智能体工具模式中。
你的任务是严格按照用户要求分析随用户消息提供的图片。
图片是当前任务的主要输入，不要假设未在图片中出现的信息。
完成分析后，必须调用 SubmitImageAnalysisResult 工具提交最终完整结论。
不要仅通过普通助手文本返回最终结果；只有提交工具中的内容会返回给上一级智能体。
```

用户消息内容：

```text
TextContent：请根据以下分析指令处理随本消息提供的图片：{AnalysisInstruction}
DataContent：从 filePath 数组加载的全部图片
```

“必须调用提交工具”属于运行模式约束，应放在额外 System 消息中，而不是只写在用户要求里。

## 7. 编程 System 提示词初始化

`CodingAgent` 的 `SystemPrompt`、`CodePrompt`、`SandboxPrompt` 当前是私有常量，由 `CodingAgent` 在创建 `AgentSession` 时写入。图片子智能体需要同样的编程 System 消息。

在 `CodingAgent` 上提供一个 `internal` 方法 `EnsureSystemPromptInSession(AgentSession)`：为传入的 `AgentSession` 确保已初始化编程 System 消息（`SystemPrompt`、`CodePrompt`、`SandboxPrompt`）。

`CodingAgent` 自身与图片子智能体共用该方法，提示词内容保持单一来源，不复制字符串，不额外提取提示词提供器类型。该方法保持 `internal`，不增加公共 API；提示词内容不在本功能中重写，只调整初始化方式与复用入口。

## 8. 图片读取

子智能体读取图片的逻辑很简单，直接在 `AnalyzeImage` 工具执行器内处理，不抽取共用加载方法，不改动现有 `load_image` 工具：

1. 校验 `filePath` 数组非空；
2. 遍历数组，将每个路径直接传给 `DataContent.LoadFromAsync`；
3. 文件不存在时保留文件读取异常；
4. 加载结果不是图片时，向调用方返回自然语言说明，例如“该文件不是受支持的图片，请提供 PNG、JPG 等常见图片格式”；
5. 读取过程传递取消令牌。

明确不做：

- 工作区路径拼接、范围判断、绝对路径限制、`..` 越界判断；
- 路径授权或白名单、文件大小上限、额外的路径规范化层。

## 9. 子智能体运行

### 9.1 上下文隔离

每次工具调用均使用全新、短生命周期的上下文：

- 不读取主会话历史，不写入主会话 `AgentSession`，不持久化自己的 `AgentSession`；
- 只接收编程 System 提示词、图片模式提示词、要求与全部图片；
- 工具执行过程作为 `CopilotChatSubAgentItem` 展示；
- 最终提交结果作为主工具调用结果进入主智能体上下文。

这样可避免把整段主会话历史与图片一起发送，降低 Token 消耗，并减少旧指令对图片任务的干扰。

### 9.2 工具集合

子智能体工具集合只包含提交工具，不装配任何编程工具：

```text
SubmitImageAnalysisResult
```

因此：

- 子智能体没有 `AnalyzeImage` 或其他图片工具，天然无法递归；
- 子智能体没有通用子智能体入口，天然无法扩散子任务；
- 工具集合与主运行的 `CodingWorkspaceToolLease` 快照无关，不读取主运行的工具。

### 9.3 执行流程

```text
主智能体调用 AnalyzeImage(filePath, AnalysisInstruction)
  → 校验 filePath 数组、AnalysisInstruction 非空
  → 遍历 filePath 加载全部 DataContent 并确认都是图片
  → 读取 AgentApiEndpointManager.PrimaryModel
  → 检查模型支持图片输入与工具调用
  → 创建独立 IChatClient / ChatClientAgent
  → 装配 SubmitImageAnalysisResult 工具
  → 创建独立消息列表（不复用主 AgentSession）
  → 通过 EnsureSystemPromptInSession 初始化编程 System 消息 + 图片模式 System 消息
  → 添加包含 AnalysisInstruction 与全部 DataContent 的 User 消息
  → 流式执行并投影到主消息中的子智能体项
  → 已提交：返回提交内容
  → 未提交：追加纠正 User 消息并重试一次
  → 第二次仍未提交：返回说明未提交的字符串
```

纠正消息：

```text
你尚未调用 SubmitImageAnalysisResult。必须立即调用该工具提交最终完整结论；普通文本不会返回给上一级智能体。
```

### 9.4 取消与异常

取消令牌端到端传递至：图片文件读取、`GetChatClientAsync`、子智能体流式运行、纠正重试。

异常策略：

| 场景 | 异常 |
| --- | --- |
| 空分析指令 / 空路径数组 | `ArgumentException` |
| 文件不存在或无法读取 | 保留对应 I/O 异常 |
| 非图片文件 | `InvalidDataException` |
| 模型不支持图片或工具调用 | `InvalidOperationException` |
| 用户取消 | 保留 `OperationCanceledException` |

两次运行均未提交结果不属于异常：第二次仍未提交时，直接返回说明未提交的字符串作为工具结果，由主智能体自行判断后续处理。不得捕获并吞掉异常，也不得在失败时返回空字符串伪装成功。

## 10. 展示与交互

沿用现有子智能体展示模型：

```text
图片分析
  ├─ 输入：filePath（数组）+ AnalysisInstruction
  ├─ 思考内容
  ├─ 普通文本增量
  ├─ 子工具调用
  └─ 输出：提交的最终结论
```

- 主工具展示名建议为“分析图片”或“读取图片”；
- 输入摘要展示 `filePath` 数组与分析指令的截断摘要；
- 不在 UI 中展示图片二进制内容；
- 现有消息模板已能显示 `CopilotChatSubAgentItem`，无需增加独立界面。

## 11. 代码改动范围

### AgentLib.Coding

1. 在 `CodingAgent` 上增加 `internal` 的 `EnsureSystemPromptInSession(AgentSession)`，为 `AgentSession` 确保初始化编程 System 消息；
2. 增加图片子智能体工具执行器（含直接加载 `filePath` 数组为 `DataContent` 的逻辑）；
3. 将 `AnalyzeImage` 注册到 `CodingWorkspaceToolProvider` 创建的 Lease；
4. 保留工具展示信息；
5. 执行器在调用时获取当前主模型；
6. 子智能体只装配 `SubmitImageAnalysisResult`，不装配其他工具；
7. 不改动现有 `load_image` 工具。

### CodingChatRoom.AvaloniaShell

1. 组合根创建 `CodingAgent` 时提供 `AgentApiEndpointManager` 或当前模型解析委托；
2. 不增加新的模型设置，不在 `ChatViewModel` 中加入图片子智能体调用代码；
3. 如有需要，仅补充工具展示资源字符串。

### Tests

- `AgentLib.Coding.Tests`：图片加载、消息顺序、模型选择、提交重试；
- `CodingChatRoom.AvaloniaShell.Tests`：验证切换 `SelectedModel` 后工具读取新的 `PrimaryModel`（若核心测试无法完整覆盖该行为）。

## 12. 测试方案

使用现有 MSTest 与 `FakeChatClient`，不依赖真实模型服务、不发网络请求。至少覆盖：

1. `filePath` 数组（单个与多个）中的绝对路径图片可加载为 `DataContent`；相对路径按进程正常文件路径语义处理；
2. 工作区外路径不会被额外拒绝；文件不存在返回 I/O 错误；非图片文件被拒绝；
3. 空数组与空分析指令被拒绝；
4. 消息顺序为三个编程 System、图片模式 System、User；User 同时包含 `TextContent` 与全部图片的 `DataContent`；
5. 子智能体工具集合只包含提交工具，无其他工具；
6. 模型调用提交工具后，主工具返回提交文本；
7. 首次未提交时追加纠正消息并重试；第二次仍未提交时返回未提交说明文本；
8. 运行中取消会终止图片读取或模型执行；
9. 工具调用开始时读取当前 `PrimaryModel`，而非创建时捕获的旧模型；
10. 不支持图片输入或不支持工具调用的模型会明确失败；
11. 子智能体不读取主运行工具快照，工具集合固定只有提交工具。

## 13. 实施顺序

1. 在 `CodingAgent` 上增加 `EnsureSystemPromptInSession`；
2. 在 `AnalyzeImage` 执行器内实现 `filePath` 数组的图片加载；
3. 实现结果提交收集器与图片模式提示词；
4. 实现独立多模态子智能体运行；
5. 注册 `AnalyzeImage` 主工具；
6. 接入调用时的 `PrimaryModel` 解析；
7. 接入 `CopilotChatSubAgentItem` 展示；
8. 增加单元测试；
9. 构建并运行相关测试项目。

## 14. 验收标准

1. 主智能体可传入一个或多个可读取的图片路径与分析指令调用图片分析工具；
2. 子智能体收到的是全部图片的二进制多模态内容，而非路径文本；
3. 图片读取不附加工作区边界或路径授权限制；
4. 子智能体使用界面当前选中的模型，切换模型后下一次工具调用立即生效；
5. 子智能体包含与主编程智能体一致的 System 提示词，并明确运行在图片工具模式；
6. 最终结果只能通过专用提交工具返回；未提交时自动纠正一次，仍未提交则返回未提交说明文本；
7. 子智能体工具集合只包含提交工具，无法调用任何编程工具或递归图片分析；
8. 停止主运行时可以取消图片子智能体；
9. 子智能体不污染主会话 `AgentSession`；
10. 工具过程可在现有子智能体消息区域观察；
11. 相关项目构建通过，新增测试全部通过。

## 15. 关键设计决策

- 图片以 `DataContent` 进入用户消息，而不是只传路径文本；
- `filePath` 是路径数组，一次调用可承载一张或多张图片；
- 编程提示词通过 `EnsureSystemPromptInSession` 单一入口初始化，而非复制字符串；
- 结果通过专用提交工具收集，普通文本不作为结果；第二次未提交时返回说明文本而非抛异常，不掩盖设计缺失；
- 图片路径直接读取，不增加路径安全防御，也不改动现有 `load_image`；
- 子智能体工具集合只包含提交工具，无编程工具，天然阻止递归与子任务扩散。
