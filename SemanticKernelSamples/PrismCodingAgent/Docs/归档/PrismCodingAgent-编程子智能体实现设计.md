# PrismCodingAgent 编程子智能体实现设计

## 1. 文档用途与状态

本文记录经过讨论和用户修正后的实现设计，供下一轮对话直接据此实施。用户修正优先于早先方案；第 12 节列出的撤回方案不得重新引入。

当前状态：已阅读相关源码及部分 Agent Framework API，尚未实施本功能，未执行构建或测试。本文中的设计与验收要求不代表实现已经通过验证。实施前应核对当前工作区源码，不假定源码与本文记录时完全一致。

本功能的完整范围为 Chat 协议下的编程子智能体。不增加 Responses API 下的编程委托能力；对共享代码的修改仍须保证既有 Responses 行为不回归。这是明确的功能边界，不是分版本交付或未完成内容的推迟。

## 2. 原则与绝对门禁

### 2.1 长期维护与最小设计

- 项目长期维护，优先保证代码可读性和整体设计质量。
- 采用最佳实践，禁止补丁、特殊入口绕行和复制简化版 Coding Agent。
- 每项设计都必须检查是否已有类似概念或机制，是否重复，是否把通用职责错误地当作子智能体业务处理。
- 完整完成本文范围内的实现和验证，不使用“首期不做”“PoC”“后续扩展”“后续版本”解释遗漏。
- 不因追求所谓完整性引入范围外的机制，禁止过度设计、过度防御。

### 2.2 本功能专属约束

- 编程子智能体复用同一个 CodingAgent 和现有编程工具实例。
- 不增加独立 CodingAgent 实例、独立工作区缓存或独立 Roslyn Language Server。
- DotNetCliTools 等工具直接复用。不为父子构建日志互相覆盖等假设场景增加处理。
- 新增子智能体相关代码禁止新增任何锁或同步门，不增加并行调度机制。
- 按普通、串行、被等待的工具调用执行；不后台启动子运行，不创建 fire-and-forget 工作。
- 系统提示词仅允许在现有沙箱说明之后新增一条：“最终结果必须通过 `ReturnOutputToParent` 提交”。禁止新增其他子运行角色说明。
- 不考虑重复提交，不检测中途提交，不以提交动作提前终止 Agent 执行。
- 不设计审批、安全隔离、权限框架、资源租约、父子运行树、持久化或故障恢复机制。
- 保留 AnalyzeImage；禁止子智能体调用 InvokeCodingSubAgent，也不向 Coding 工具集中引入 InvokeSubAgent。
- 不改动或污染现有通用 SubAgentToolProvider 的执行逻辑。
- UI 展示与存储只是旁节，不作为执行架构的设计驱动力。

## 3. 核心设计

> 编程子智能体是现有编程执行器的一次普通工具内调用：同一执行机制、同一批编程工具，不同的模型会话、输出对象和结果提交工具。

主入口：

用户发送 → CodingWorkTaskController → CodingAgentChatRunner → CodingAgent.RunAsync

子入口：

主 Agent 调用 InvokeCodingSubAgent → Coding 委托工具提供者 → 同一个 CodingAgent.RunAsync

汇合点是 CodingAgent 的编程执行入口，而不是按钮、ViewModel、工作任务 Controller 或 Shell Runner。

主运行等待委托工具；委托工具等待子运行；子运行完成后返回普通工具结果，主运行继续。主运行可以依次委托子智能体 1 和子智能体 2；这不代表给子智能体开放递归编程委托能力。

## 4. 已有源码基础

以下是研究时确认的事实，实施时应再次核对。

| 文件或类型 | 现有职责及复用意义 |
| --- | --- |
| `AgentLib/AgentLib.Coding/CodingAgent.cs` | 装配编程工具、消息注入、自动压缩、流式运行及输出；当前依赖 IManualSendMessageContext |
| `PrismCodingAgent/Code/PrismCodingAgent.AvaloniaShell/Services/Chat/CodingAgentChatRunner.cs` | 从主聊天取得上下文，管理主运行 _activeRun 和日志收尾 |
| `PrismCodingAgent/Code/PrismCodingAgent.AvaloniaShell/Services/WorkTasks/CodingWorkTaskController.cs` | 已有活动运行时把新发送识别为插话；不适合作为编程委托入口 |
| `AgentLib/AgentLib/Model/SendMessages_/IManualSendMessageContext.cs` | 混合模型客户端、模型会话、主展示消息、StartChatting 和消息追加职责 |
| `AgentLib/AgentLib.Coding/CodingPromptProvider.cs` | 提供现有系统提示词、编程规则及项目指令、沙箱说明 |
| `AgentLib/AgentLib.Coding/CodingSystemPrompt.cs` | 初始化系统提示词；当前依赖固定身份文本判断是否已初始化 |
| `AgentLib/AgentLib/AgentSessionStreamingHelper.cs` | RunWithHistoryCompletionAsync 提供流式执行、历史补全及既有重试行为 |
| `AgentLib/AgentLib/Reducers/ToolCallAwareChatReducer.cs` | 压缩时维护工具调用历史的完整性 |
| `AgentLib/AgentLib/Tools/ToolRegistration.cs` | 将工具与展示配置绑定 |
| `AgentLib/AgentLib/Tools/ToolRegistrationRegistry.cs` | 根据工具注册生成展示信息，并检查重复工具名称 |
| `AgentLib/AgentLib.Coding/CodingWorkspaceCache.cs` | 已有工作区工具缓存，以及包含工具和展示注册表的 CodingRunWorkspaceContext |
| `AgentLib/AgentLib/Tools/GeneratedTextSubmissionTool.cs` | 已有提交函数 SubmitText、SubmittedText 和 HasSubmittedText；可直接用于 ReturnOutputToParent |
| `AgentLib/AgentLib.Coding/ICodingAgentRunResult.cs` | 已有活动运行结果、输出消息、完成任务与消息注入契约 |
| `AgentLib/AgentLib.Coding/Images/CodingImageAnalysisTools.cs` | 有限补交流程可作参考；不复制其模型执行循环作为编程执行底座 |
| `AgentLib/AgentLib/Tools/SubAgentToolProvider.cs` | 现有通用子智能体机制；仅作参考，不修改、不作为本功能执行底座 |

### 4.1 为什么不复用主发送入口

主任务活动时，CodingWorkTaskController.SendMessageAsync 会把输入交给 InjectMessageAsync。子智能体若走这里，就会变成主运行插话。

Shell Runner 还持有主 _activeRun，并取得选中会话上下文。不能通过 isSubAgent 分支或临时切换 SelectedSession 修补。

### 4.2 框架与项目约束

研究时 AgentLib.Coding 库目标框架为 net6.0、net9.0，Nullable 启用，现有 LangVersion 为 latest；测试项目为 net10.0，采用 MSTest，并已有 FakeChatClient 测试基础。不要为本功能修改目标框架、SDK、语言版本或升级依赖。

## 5. 统一执行边界

### 5.1 最小运行上下文

用一个具体的、小型 CodingRunContext 承接 Coding Chat 执行实际需要的数据，替换 Coding Chat 路径对 IManualSendMessageContext 的依赖：

- IChatClient：创建和配置本次 Agent。
- AgentSession：保持本次模型历史；首次运行尚未创建时，由统一执行入口创建并关联到该上下文。
- CopilotChatMessage：承接本次文本、推理、工具调用、工具结果和用量。
- 可选 IMainThreadDispatcher：输出绑定 UI 时沿用现有线程调度方式。

继续使用现有 CodingChatRunOptions、ToolRegistration、ICodingAgentRunResult。提示词与工具作为明确的执行输入，不再分别制造 Definition、Request、Profile 等类型。

上下文不承载 SelectedSession、StartChatting、主用户展示消息、日志或持久化接口、父 RunId、运行树、结果提交状态。

结果提交状态由本次委托创建的 GeneratedTextSubmissionTool 承担，不污染所有 Coding 运行的上下文。

### 5.2 主聊天宿主职责

由 CodingAgentChatRunner 承接当前混在 CodingAgent 中的宿主逻辑：

- 填充主用户展示消息，保留多模态内容顺序。
- 将主用户消息和主助手消息加入明确捕获的主会话。
- 管理 StartChatting 生命周期。
- 将执行上下文中的 AgentSession 绑定、回写到该主会话，保持现有多轮历史和插话行为。
- 保留 _activeRun 与现有日志收尾。

不在子运行中实现这些操作，也不提供带大量空操作的 IManualSendMessageContext 假实现。

### 5.3 编程执行职责

CodingAgent 的唯一 Chat 执行路径共同负责：

- Agent 创建与配置。
- 系统提示词初始化。
- 最终工具集合装配。
- 自动压缩及既有压缩展示。
- 消息注入。
- 流式执行及模型历史维护。
- 向本次输出对象追加内容。
- 现有取消传播与执行收尾。

继续使用 RunWithHistoryCompletionAsync 和既有压缩器，不复制模型—工具循环。

删除 Coding Chat 的旧 IManualSendMessageContext 兼容入口，调整所有对应调用方和测试；不保留两个入口背后的两份实现。

`CodingAgentRunExtensions.cs` 当前通过包装整个手动发送上下文设置 ReasoningEffort。改为统一执行入口直接使用 CodingChatRunOptions 设置推理选项，删除该包装机制。

通用 AgentLib 的手动发送接口及既有通用子智能体逻辑保持不变。Responses 的既有入口不因 Chat 改造被删除或转换成新的子智能体执行路径。

### 5.4 展示对象与模型消息的区别

子智能体不需要主聊天的用户展示消息对象。但委托提示词仍作为协议中的 User 消息发送，助手消息与工具消息仍进入独立 AgentSession 的模型历史。

子运行的 CopilotChatMessage 只是输出目标，不意味着加入主会话、创建主用户消息或创建新工作任务。不要为了类型名称另造事件总线或另一套输出模型。

## 6. 工具装配与复用

增加一个 Coding 层工具提供者，例如 CodingSubAgentTools。它本身就是委托入口，不另加委托服务或转发层。

### 6.1 主运行工具

现有编程工具 + InvokeCodingSubAgent。

主运行在取得现有编程工具后，委托工具捕获这批工具、展示注册信息，以及明确的模型客户端和编程运行选项。

### 6.2 子运行工具

同一批现有编程工具 + 本次 ReturnOutputToParent。

- DotNetCliTools、Roslyn 和其他已有工具实例直接复用，不克隆、不重新建工作区缓存。
- AnalyzeImage 保留；不把“禁止编程委托”扩大成“禁止所有内部使用智能体的工具”。
- 子运行不注册 InvokeCodingSubAgent。
- 不合并 IManualSendMessageContext.DefaultTools，因此不引入 InvokeSubAgent。
- 沙箱等宿主工具按主运行已经装配的配置复用。
- 不从调用时的 SelectedSession、输入框、当前模型选择或其他可变 UI 状态重新推断所属环境。

工具执行列表和工具展示注册表来自同一最终注册集合。优先在现有 CodingRunWorkspaceContext 与工具装配位置完成组合，不新增能力授权机制、工具源接口或独立执行器。

未知工具调用沿用框架的现有处理行为，通过测试确认不可执行，不新增拦截框架。

### 6.3 串行执行与取消

委托工具直接 await 子执行，完成后返回父模型。全部新增子智能体代码禁止新增 lock、SemaphoreSlim 或其他同步门；不增加并行调度器、后台任务和任务树。

把父运行的取消令牌传递给委托工具、子执行及已有工具调用。取消和异常沿既有执行链传播，不转换为正常提交结果。

不为本功能扩展全局 DisposeAsync、资源等待、资源引用计数或工作区切换管理。不将其他生命周期问题混入本功能。

## 7. InvokeCodingSubAgent 契约

模型可调用的入口名称为 InvokeCodingSubAgent，参数仅为本次任务提示词，例如 prompt。

- 提示词参数原样作为 User 消息发送。
- 不增加 systemPrompt、instructions、角色配置、子模型选择、协议选择、工作区覆盖等参数。
- 模型客户端、工作区工具及 CodingChatRunOptions 从明确的父运行环境取得。
- 每次调用创建独立 CodingRunContext、独立 AgentSession、独立输出对象和独立 GeneratedTextSubmissionTool。
- 不创建新的 CodingAgent；在同一个 CodingAgent 上调用同一个编程执行入口。
- 不把子模型历史或过程文本混入父 AgentSession；父模型只获得普通工具调用的输入及最终输出。

## 8. 系统提示词与模型输入顺序

### 8.1 初始顺序

继续使用 CodingPromptProvider.BuildAsync 的现有内容和顺序：

1. 现有 SystemPrompt。
2. 现有 CodePrompt，以及已有方式加入的项目指令。
3. 现有 SandboxPrompt。
4. 新增一条 System 消息，内容严格为：最终结果必须通过 `ReturnOutputToParent` 提交。
5. InvokeCodingSubAgent 的 prompt 参数，以 User 身份原样发送。

除第 4 条外，不新增任何子运行系统说明，不修改、复制或替换公共编程规则。

禁止添加：仅完成委托任务、不得再次委托、结构化结果协议、报告验证真实性、说明修改与失败、身份说明等额外角色文本。禁止通过工具描述绕道加入这些额外角色说明；工具描述仅说明必要的调用参数和提交功能。

### 8.2 初始化与继续运行

- 新会话初始化一次该运行的系统提示词。
- 第二轮补交使用同一上下文与 AgentSession，不重复插入系统提示词。
- 主多轮聊天继续使用已有主会话历史；恢复历史保留已有系统消息，缺少系统提示词时补入主运行提示词。
- 不使用固定身份文本推断角色，不新增 PromptId、提示词身份注册表或版本机制。
- 不在同一个会话中切换主运行与子运行角色。

## 9. ReturnOutputToParent 与两轮检查流程

### 9.1 复用现有提交工具

每次 InvokeCodingSubAgent 创建一个 GeneratedTextSubmissionTool，通过 CreateTool 将提交函数注册为 ReturnOutputToParent。

现有底层函数名为 SubmitText，已有 SubmittedText 与 HasSubmittedText。无需新增结果收集器、提交状态机或结构化结果协议。

### 9.2 确定的执行流程

1. 使用初始消息执行一轮完整编程对话，等待 CompletionTask 完成。
2. 仅在该轮完成之后检查 HasSubmittedText。
3. 已提交：直接返回 SubmittedText。
4. 未提交：在同一个模型会话追加一条 User 消息，内容为“要求调用 ReturnOutputToParent 方法”，通过同一个执行入口再执行一轮。
5. 第二轮完整结束后再次检查。
6. 如仍未提交，宿主直接调用同一个底层提交函数 SubmitText("子智能体没有返回任何内容")，等价于手动调用 ReturnOutputToParent("子智能体没有返回任何内容")。
7. 返回提交工具中的最终文本。

第二轮不重新发送原始任务，不清空历史，不重新插入系统提示词，不创建第三轮。

### 9.3 明确禁止的分支

- 不在流式执行中途检查提交状态。
- 不因中途调用提交函数而打断流式枚举、取消执行或提前返回。
- 不增加多次提交的检测、限制、优先级、合并或覆盖策略。
- 不将普通助手文本当作提交结果。
- 不无限重试。
- 不回滚已经发生的文件修改。
- 不模拟一次模型工具调用来完成宿主兜底；直接调用已注册工具对应的底层函数。
- 取消或执行异常不进入“正常结束但未提交”的补交流程或兜底流程。

## 10. UI 与存储旁节：已核对的事实

此节仅说明既有机制，不要求建设新的子智能体 UI、存储或恢复功能。

实时显示不会天然触发持久化。是否被既有存储收集，取决于是否将子运行输出挂进主会话消息对象。

源码依据：

- `PrismCodingAgent/Code/PrismCodingAgent.AvaloniaShell/ViewModels/MessageItemViewModel.cs` 直接向 UI 暴露主消息的 MessageItems。
- `PrismCodingAgent/Code/PrismCodingAgent.AvaloniaShell/Views/ChatMessageItemTemplateSelector.cs` 已支持 CopilotChatSubAgentItem。
- `AgentLib/AgentLib/Logging/CopilotChatHistoryXmlCodec.cs` 对 CopilotChatSubAgentItem.MessageItems 已有递归序列化和读取逻辑。
- `PrismCodingAgent/Code/PrismCodingAgent.AvaloniaShell/Services/Sessions/FileCodingChatSessionStore.cs` 保存的是主会话的 AgentSession 状态。

| 输出接法 | 既有结果 |
| --- | --- |
| 输出对象独立，不加入主会话消息对象 | 不会被主会话消息序列化自动收集 |
| 挂入主消息的 CopilotChatSubAgentItem.MessageItems | 保存主消息时可由现有逻辑连同子片段保存，不需要新增存储机制 |

展示片段被保存不等于子运行 AgentSession 被保存，也不意味着父模型看到全部子过程。

本功能不要求新增实时展开 UI。保持主侧普通工具调用展示即可，独立输出对象承接子过程。不要为此增加存储过滤机制、子会话记录、父子关联格式或故障恢复。

## 11. 实施范围与验证门禁

### 11.1 修改范围

| 区域 | 职责 |
| --- | --- |
| AgentLib.Coding/CodingAgent | 最小运行上下文、唯一 Chat 执行入口、提示词和工具组合 |
| CodingPromptProvider / CodingSystemPrompt | 公共内容保持不变；落实初始化和唯一提交说明 |
| 新增 Coding 委托工具提供者 | 独立模型会话、复用工具、提交工具、最多两轮和指定兜底 |
| Shell CodingAgentChatRunner | 主聊天消息、主状态、主会话绑定与既有日志收尾 |
| CodingAgentRunExtensions | 将推理选项交给统一入口，删除手动发送上下文包装 |
| AgentLib.Coding.Tests 与相关 Shell 测试 | 新功能及主聊天回归验证 |
| 现有通用子智能体 | 不改 |
| Responses | 不增加编程委托能力；共享改动须保证既有行为 |
| UI / 存储 / Runtime 资源管理 | 不新增子运行机制，不进行范围外重构 |

### 11.2 验收要求

使用既有 MSTest、FakeChatClient 与现有测试基础，通过公开入口验证，不为测试新增抽象或改变生产代码可见性。

- 主聊天多轮历史、用户多模态消息顺序、流式输出、工具展示、插话、自动压缩、推理选项及日志收尾不回归。
- 主、子运行进入同一个 Coding 执行路径，子运行不经主 Controller 或 Shell Runner。
- 父子模型会话独立，子过程不进入父模型历史。
- 子运行复用现有工具实例，不创建新 CodingAgent 或工作区工具资源。
- 子系统提示词顺序正确，只新增指定的一条 System 说明；任务参数作为 User 消息原样发送。
- 子工具集合有编程工具、AnalyzeImage 和 ReturnOutputToParent，没有 InvokeCodingSubAgent 或 InvokeSubAgent。
- 首轮已提交：只运行一轮，返回提交内容。
- 首轮未提交、第二轮提交：同会话补一轮，只追加补交 User 消息，返回提交内容。
- 两轮均未提交：调用同一个底层提交函数，返回“子智能体没有返回任何内容”，无第三轮。
- 即使已在执行中调用提交函数，也等待整轮正常完成后才检查和返回。
- 取消令牌传递到子执行和工具；异常和取消不触发正常未提交兜底。
- 主运行依次委托不同子任务时，每次模型会话、输出与提交对象独立，主 _activeRun 和会话选择不被替换。
- 新增子智能体相关代码无锁、无并行调度、无后台执行、无第二套模型—工具循环。
- AgentLib.Coding 的 net6.0、net9.0 构建通过，相关新增测试与现有回归测试通过，包括共享改动影响到的 Responses 测试。

不增加重复提交处理、父子日志隔离或并发运行等被明确排除的验收需求。全部范围内实现和验证完成后才能宣称功能完成；真实阻塞必须如实报告，不以绕过或延期掩盖。

## 12. 已撤回方案：禁止重新引入

以下内容来自早先讨论，已被用户明确修正或撤回，不属于本文实现设计：

- 给子智能体创建独立 CodingAgent、独立 DotNetCliTools、独立工作区缓存或独立 Roslyn 实例。
- 为构建日志互相覆盖等假设情况实施隔离和防御。
- 增加资源租约、引用计数、子运行资源管理器、活动运行管理体系或全局生命周期重构。
- 增加锁来串行化整个编程执行过程。
- 增加只完成委托任务、不可再次委托、验证真实性、结构化结果等子系统角色约束。
- 在 InvokeCodingSubAgent 上提供额外的角色或系统提示词参数。
- 设计多次提交策略或提交即终止语义。
- 两轮未提交后直接返回错误而不调用指定提交函数。
- 将 AnalyzeImage 作为禁止的委托工具过滤掉。
- 使用事件总线、父子运行树、Definition/Request/Profile 等多层新概念。
- 保留旧 Coding Chat 上下文入口作为兼容层。
- 为实时展示新增子运行持久化、恢复、存储过滤或父子关联机制。

下一轮应以本文为实现依据，保持变更最小、职责明确，并在本文范围内完整实施和验证。

## 13. 第二步实施记录：编程子运行实时输出

### 当前工具详情设计（覆盖下方历史记录）

工具调用提供可选 `Details`，复用现有消息片段承载持续更新的详情。概要、输入与工具完成后的输出返回内容保留原有职责，详情不展示用量，也不被当作输出返回内容。

- 普通流式调用与工具自身通过统一 `CreateToolItem` 获取同一调用项；不增加展示类型、配对集合、关联 ID、工具名称特判或执行管线。
- 编程委托与已有通用子智能体均将其流式内容写入工具项的 `Details`，不再创建同级过程卡。UI 直接绑定 `Details.MessageItems`，不扫描或隐藏重复项。
- 保留原消息 ViewModel 和注释；通知、深克隆与既有 XML 嵌套消息编码同步支持详情，旧历史子片段继续可以展示。
- 本轮共享模型及详情定向 net10.0 36 项通过；net6.0 宿主缺少 Google.Protobuf，不能运行。Coding 全量 175 通过、1 个已有缓存引用断言失败；Shell 构建通过。
- 真实 UI 测试仍超时，旧通用子智能体完整执行回归尚未完成；本轮不能认定完整交付。下方是历史结果，不是当前代码验收依据。

### 历史实现记录

本节记录后续用户授权的第二步，覆盖第 10 节原先“不要求实时接入”的范围限制；前文历史设计状态不代表当前代码状态。

- `CodingRunContext` 可指定 `ICopilotChatOutput`，默认仍为 `AssistantChatMessage`。后者继续承担宿主及 completion 结果契约；子运行的过程不写入该兼容消息，委托结果始终来自 `GeneratedTextSubmissionTool`。
- `CodingSubAgentTools` 捕获明确父上下文，在执行前通过父 `Output` 创建独立 `CopilotChatSubAgentItem`。同一次委托两轮共用片段、上下文及 session；串行多次委托分别创建片段。
- 子运行复用同一 `CodingAgent` 和唯一流式循环，直接向子片段追加文本、推理、工具过程及用量，不共享或复制 `MessageItems`。
- 捕获的 `MainThreadDispatcher` 用于可等待的片段创建、流式更新、用量及占位清理；无 dispatcher 时直接执行。压缩展示通过 reducer 包装等待事件产生的输出任务，不再丢弃调度任务。
- 按用户修正，通用消息模型不判断 `InvokeCodingSubAgent` 名称，不提供 `SetOutputText`，不额外写入子片段的最终结果。委托保留普通工具调用和结果展示，子片段只接收过程输出，父模型仍接收正常工具结果。
- 未修改通用 `SubAgentToolProvider`、Responses 执行路径、工具权限边界或 AnalyzeImage；未引入锁、调度器、运行树或复制执行循环。

验证记录：

- AgentLib.Coding 的 net6.0、net9.0 构建通过；Shell 构建通过。
- 新增 12 项 MSTest，使用公开 API 与现有 `FakeChatClient` 验证实时文本、推理及工具过程、同输出补交、串行独立片段、取消不兜底、UI 调度、普通工具结果、用量及空响应占位清理。移除名称特判和额外结果 API 后，相应断言验证普通工具结果，不要求隐藏委托工具卡。
- Coding 全量：164 通过、1 失败；失败为此前已知的 `CodingWorkspaceCacheTests.CreateRunContextShouldReuseCachedState` 集合引用断言，未修改。
- 共享输出模型定向回归：net10.0 的 34 项通过；net6.0 testhost 因缺少 Google.Protobuf 3.30.2 中止，未修改环境或依赖。
- Shell 全量：244 通过、1 跳过、2 失败。失败为 Responses 在线压缩测试 `RawHttpCompactionShouldPreserveContextWithoutSdkAsync` 与 `CompressionCommandShouldBeAvailableForResponsesHistoryAsync`，模型未返回预期历史标识；未修改这些范围外测试或执行逻辑。
- 本次未执行命令行、脚本或沙盒程序。
