# 从 Visual Studio Copilot 的请求内容学习其实现原理

本文分析一份新版 Visual Studio Copilot 发给大语言模型的请求体样本。通过观察其中的消息分层、系统提示词、工具定义和生成参数，可以了解 Copilot 这类代码智能体如何组织上下文、规划任务、调度工具、编辑代码并管理任务生命周期。

<!--more-->
<!-- CreateTime:2026/06/02 07:06:22 -->
<!-- UpdateTime:2026/07/15 -->

<!-- 发布 -->
<!-- 博客 -->

本文内容由人类主导 AI 辅助编写

## 前言

我此前拿到过另一份 Visual Studio Copilot 请求，并根据当时保存的样本写下了本文的第一版。旧样本包含两条 system 消息、一条 user 消息和 26 个工具，代码编辑主要依赖 `replace_string_in_file`。

这次拿到的新请求已经发生了明显变化。新版样本包含：

- 2 条 system 消息
- 3 条 user 消息
- 36 个 function 工具
- 更详细的计划状态管理规则
- 两种子智能体调用方式
- 交互式澄清问题的能力
- 基于补丁的代码编辑协议
- 面向性能分析、应用现代化和 Azure 的专项能力
- 显式的 Autopilot 完成握手

因此，这次更新不只是替换几个工具名称，而是需要重新理解 Copilot 如何构造一份面向代码智能体的模型请求。

本文的主要事实来源是同目录下的 `Request.json`。文章不会原样展示其中的本地绝对路径、内部远程仓库地址和模型字段中的个性化后缀。需要特别提醒的是：原始 `Request.json` 本身仍然包含环境信息，如果准备公开仓库或发布附件，应先另外制作脱敏副本。

本文更新于 2026 年 7 月 15 日。Copilot 的提示词、工具和产品交互都可能频繁变化，文中的观察只对应这一次请求体样本，不能视为所有版本都固定如此。

## 请求内容的整体结构

先看最外层的请求体。省略具体消息正文和工具 schema 后，可以整理成下面的结构：

```json
{
  "messages": [
    { "role": "system", "content": "核心智能体提示词" },
    { "role": "system", "content": "指令文件内容" },
    { "role": "user", "content": "COPILOTWORKSPACE CONTEXT" },
    { "role": "user", "content": "IDESTATE CONTEXT" },
    { "role": "user", "content": "用户实际输入" }
  ],
  "max_completion_tokens": 8192,
  "stream": true,
  "stream_options": {
    "include_usage": true
  },
  "tools": ["36 个 function 工具"],
  "tool_choice": "auto",
  "model": "GPT-5.5 [后缀已脱敏]"
}
```

这里有几个可以直接从请求中确认的事实：

1. 请求启用了流式响应
2. `include_usage` 被启用，请求要求流式响应包含 token 使用量信息
3. `tool_choice` 是 `auto`，允许模型自行选择直接回答或调用工具
4. `max_completion_tokens` 为 8192
5. 请求没有显式携带 `temperature`、`top_p` 等采样参数；服务端是否另有默认值，仅凭这份文件无法判断
6. `model` 字段包含模型名称和一个方括号后缀，但后缀的实际用途无法仅凭样本确定

需要注意，本文分析的是 JSON 请求体，不包含请求端点、HTTP 头、鉴权信息、流式响应内容、工具执行结果和后续轮次请求。因此，它适合用来研究“模型开始推理前收到了什么”，但不足以还原完整的网络协议和宿主端实现代码。

## 五条消息组成的分层上下文

这五条消息依次承担不同职责：

| 顺序 | role | 内容 | 作用 |
| --- | --- | --- | --- |
| 1 | system | 核心智能体提示词 | 定义角色、工作流、工具策略、编辑与验证规则 |
| 2 | system | 指令文件内容 | 注入仓库级和用户级编码规范 |
| 3 | user | `COPILOTWORKSPACE CONTEXT` | 提供目标框架等工作区特征 |
| 4 | user | `IDESTATE CONTEXT` | 提供 IDE、解决方案、当前文件、终端和 Git 仓库元数据 |
| 5 | user | 用户实际输入 | 本次真正需要回答或执行的任务 |

这说明请求生成端在发送请求前进行了上下文装配，至少收集了以下信息：

- 产品预置的智能体规则
- 仓库和用户的 Copilot 指令文件
- 项目或解决方案特征
- 当前 IDE 环境信息
- Git 仓库、分支和远程仓库元数据
- 用户本轮输入
- 当前可用工具及其 JSON Schema

这些信息没有简单拼成一段长文本，而是利用 system 和 user 两种消息角色分层表达。产品和团队规则被放在 system 消息中，易变化的工作区信息被放在 user 消息中，真正的用户输入则位于最后。

从代码智能体设计角度看，这是一种值得借鉴的组织方式：稳定策略由产品维护，团队规范来自仓库，个人规范来自用户配置，工作环境由 IDE 提供，本轮目标由用户输入决定。

## 第一条 system 消息：定义智能体的执行协议

### 角色定义仍然简洁

新版请求开头的角色定义与旧样本基本一致：

```text
You are an AI programming assistant.
When asked for your name, you must respond with "GitHub Copilot".
Follow the user's requirements carefully & to the letter.
Your expertise is strictly limited to software development topics.
Follow Microsoft content policies.
Avoid content that violates copyrights.
For questions not related to software development, simply give a reminder that you are an AI programming assistant.
Keep your answers short and impersonal.
Respond in the following locale: zh-CN
```

它确定了几个基础边界：

- 被问及名称时，必须回答 GitHub Copilot
- 只处理软件开发主题
- 遵循用户要求和 Microsoft 内容政策
- 避免版权违规
- 回答应简短、非个人化
- 使用 `zh-CN` 回复

真正复杂的规则没有全部堆在角色描述里，而是放进后面的 XML 风格区段，例如 `<preamble>`、`<context_gathering_strategy>`、`<tool_use_guidance>`、`<editing_files>` 和 `<testing_guidance>`。这让不同职责的规则可以分别维护。

### 上下文收集强调先判断已知信息

系统提示词规定：

```text
- If the user's request includes specific file names or code snippets, prioritize reading those files directly
- If the user's request requires knowledge of a symbol's usage, definition, or implementation in the workspace, use find_symbol to find results
- If the user mentions specific functionality or errors, use code_search for semantic searches
- Use get_projects_in_solution and get_files_in_project when you need to understand the overall structure of the workspace
```

它还明确指出一个反模式：不要跳过结构工具并直接猜测文件路径。原因也写得很直接——那样容易编造不存在的路径，或者错过项目真实的分层约定。

如果用户没有提供足够的工作区信息，提示词要求先调用一次 `get_projects_in_solution`，再有针对性地调用 `get_files_in_project`，之后才按需要增加搜索工具。这里的重点不是机械地多读文件，而是先建立可靠的结构认知。

### 搜索能力形成多个层次

结合新版工具清单，可以把检索策略整理为下面几个层次。

#### 已知文件：`get_file`

知道确切路径时，直接按行读取文件。工具要求给出起止行，并支持返回行号，避免把整个大文件无差别塞进上下文。

#### 已知文件名：`file_search`

知道文件名或路径片段，但不知道完整位置时，按路径搜索。它只返回相对路径，并限制最大结果数量。

#### 已知文本：`grep_search`

知道错误码、配置键或精确文本时，使用基于 ripgrep 的文本或正则搜索。它支持 glob 范围和结果数量上限。

#### 已知符号：`find_symbol`

知道类、方法或接口名称，并需要定义、引用或实现时，使用编译器和符号树。它理解符号关系，不只是匹配相同字符。

#### 已知概念：`code_search`

只知道“数据库连接”“命令行参数”之类的自然语言概念时，使用语义搜索。工具描述说明每个搜索最多返回 4 个相关片段；系统提示词还要求不要并行调用它，也不要用它替代精确的符号查询。

#### 需要广泛探索：`search_agent`

如果任务只是阅读和搜索代码，可以把完整搜索目标交给专用搜索智能体。该智能体只有读取和搜索能力，不能编辑、运行终端或构建，权限边界更小。

这套分层可以概括为：

```text
路径明确 -> 直接读文件
文件名明确 -> 搜文件
文本明确 -> grep
符号明确 -> 编译器符号查询
概念明确 -> 语义搜索
探索范围较大 -> 搜索子智能体
```

### “不要询问工具许可”和“有歧义必须提问”并不矛盾

提示词中同时存在两类规则。

一类要求模型自主收集上下文：

```text
Do not ask the user for confirmation before doing so.
Never ask permission to use a tool.
```

另一类要求遇到歧义时使用 `ask_question`：

```text
When the user's request is ambiguous or lacks detail, always use the ask_question tool before proceeding with any investigation or implementation.
Do not guess on ambiguous design decisions — ask.
```

它们约束的是两件不同的事：

- 不要问“我可以读取项目吗”“我可以运行搜索吗”这类工具许可问题
- 如果框架、架构、测试范围等选择会真实改变实现，则必须向用户澄清需求

新版请求提供了两个结构化提问工具：

- `clarify_requirements`：探索代码库之后、创建计划之前注册澄清问题
- `ask_question`：在一般场景中让用户从 2 到 5 个具体选项中选择，单次最多提交 5 个问题

二者都会生成带自由输入入口的单选问题卡片。请求没有展示实际 UI，但工具描述表明，需求澄清被设计为一种结构化交互，而不只是普通聊天文本。

### 计划不是一段 TODO，而是状态化协议

系统提示词设置了“计划门槛”。以下情况应创建计划：

- 跨后端与前端、API 与测试、代码与配置等多个区域
- 需要调查根因、性能问题或不稳定测试
- 影响共享契约、schema 或横切模式

简单任务即使修改几个文件，也应直接执行。

`plan` 工具要求计划说明理解、假设、方案、关键文件、风险和步骤。主步骤使用 `step-1`、`step-2` 这样的稳定标识。计划创建后，主步骤大致按下面的状态变化：

```text
pending -> in-progress -> completed
                       -> failed
                       -> skipped
```

`update_plan_progress` 规定：

- 开始第一步时显式标记 `in-progress`
- 每个主步骤完成后立即更新
- 不能等全部工作结束后再批量回填状态
- 步骤进入终态后可以自动启动下一个待处理步骤
- 子步骤只是内部清单，不单独更新状态

计划上下文中的 `narrative` 是具体名称、数值和规格的依据。如果出现 `userModifications`，说明用户直接编辑了计划，其内容应覆盖此前对话中的假设。若用户的修改影响已经完成的步骤，智能体必须先回头更新受影响的代码或文件，再继续后续步骤。

如果执行过程中发现事实与计划不符，则使用：

1. `record_observation` 记录带证据的错误、发现、决策或风险
2. 确有必要时调用 `adapt_plan` 调整计划结构
3. 保留已完成步骤，不删除被替代步骤，而是将其标为 `skipped`

所有步骤均进入 `completed`、`failed` 或 `skipped` 终态，所需编辑已经完成，适用的构建和测试已经通过或其失败已被明确接受，并且不存在阻塞观察后，才能调用 `finish_plan`。调用后计划的内存状态会被清除，且不应再继续搜索或编辑；计划文件则保留在临时目录中。

这里还有两个值得注意的内部不一致：

- system 提示词要求新计划生成 5 到 12 个步骤，但 `plan` 工具说明又允许简单任务使用 3 到 5 个步骤
- `plan` 工具主体要求固定章节，而参数描述又说标题后可以自由组织技术说明
- `update_plan_progress` 的 `autoAdvance` 在 schema 中是必填字段，但描述又提到默认值为 `true`

这说明提示词和工具描述可能由不同模板片段组合而成。仅凭请求无法确定冲突时的实际优先级，编写自己的智能体时应尽量避免这种重复规则不一致。

### 两种计划结束方式可能对应不同模式

工具中同时存在 `finish_plan` 和 `signal_plan_ready`：

- `finish_plan` 用于执行完计划后关闭当前计划
- `signal_plan_ready` 用于计划已经展示，且用户明确认可或没有提出修改后，标记它可以交给执行流程

再结合 `clarify_requirements`，可以合理推测同一份工具注册表可能服务于多种工作模式，例如当前智能体创建并执行计划，或者规划智能体只负责产出方案再交给执行智能体。

这只是基于工具契约的推测。请求没有实际调用轨迹，无法据此确认 Visual Studio 的完整 UI 流程。

### 代码修改强调修复根因

新版 `<code_changes>` 比旧样本多了两条重要约束：

```text
- Do not take shortcuts that merely hide problems. Address root causes of errors, warnings, and failures rather than working around them.
- If you cannot determine the correct fix after multiple attempts, explain what you tried and ask the user for guidance rather than applying a workaround.
```

这两条规则针对 AI 编程中常见的坏习惯：

- 为了让构建通过而删除报错代码
- 用禁用警告代替修复问题
- 用空的 `catch` 吞掉异常
- 跳过失败测试
- 在不了解根因时不断叠加补丁

Copilot 被要求做最小修改，但“最小”不等于“表面上改动最少”，而是在解决根因的前提下不扩大无关范围。

### 文件编辑从字符串替换变成结构化补丁

根据作者此前保存的旧样本，旧版使用 `replace_string_in_file` 和 `multi_replace_string_in_file`。新版请求中，这两个工具没有出现，主编辑工具变成 `apply_patch`，`edit_file` 作为后备。

核心规则是：

```text
Use the `apply_patch` tool to edit files.
Don't try to edit an existing file without reading it first.
Use `edit_file` as fallback if `apply_patch` cannot accomplish the required changes.
```

`apply_patch` 接收一种结构化 diff：

```text
*** Begin Patch
*** Update File: path/to/file
@@ class-or-function-context
 context before
-old code
+new code
 context after
*** End Patch
```

它还要求：

- 默认提供变更前后各三行上下文
- 上下文不足以唯一定位时，使用 `@@` 指明类或方法
- 多处修改分别给出更新区段
- 严格保持原文件的空格或 Tab 缩进风格
- 新增和删除行必须带 `+` 或 `-`
- 补丁必须有明确的开始与结束标记

与精确字符串替换相比，补丁协议更接近开发者熟悉的 diff，也能在一次调用中表达同一文件的多个局部变化。

`edit_file` 是语义化编辑后备方案。模型只需给出必要的代码片段，并用 `// ...existing code...` 表示未变化区域。提示词要求先尝试修正补丁，实在无法应用时才退回 `edit_file`。这可以确认工具的优先顺序，但产品选择这种顺序的内部原因无法仅凭请求确定。

`create_file` 只能用于创建新文件，不能拿来编辑已有文件。编辑协议还设置了失败上限：同一个文件如果连续三次仍无法修复相关错误，智能体应停止尝试并向用户寻求指导，而不是无限循环修改。

### 构建、诊断与测试形成验证闭环

新版请求包含以下验证工具：

- `get_errors`：查看指定文件的编译错误，适合局部修改后的快速反馈
- `run_build`：构建整个工作区，要求在任务结束前用于最终验证
- `get_output_window_logs`：读取构建、CMake、调试、Git、NuGet 和测试等 Visual Studio 输出窗格
- `get_tests`：发现测试及其当前状态
- `run_tests`：按程序集、项目、完全限定名、类型或方法运行测试

测试指南推荐先发现相关测试，再精确运行。若普通筛选不准确，应使用 `FullyQualifiedName`。

这形成了一个典型闭环：

```text
读取代码 -> 修改文件 -> 查看局部错误 -> 运行相关测试 -> 构建工作区 -> 汇报结果
```

终端仍然存在，但提示词要求有专用工具时优先使用专用工具，并且不能并行调用 `run_command_in_terminal`。前台命令输出超过 4000 个字符时只保留尾部；后台命令则通过 `get_background_terminal_output` 查询状态、读取输出、等待或发送 Ctrl+C 停止。

### Autopilot 使用显式完成握手

核心 system 消息最后新增了一个 `<Autopilot>` 区段：

```text
When you have fully completed the task, call the task_complete tool to signal that you are done.
IMPORTANT: Before calling task_complete, you MUST provide a brief text summary of what was accomplished in your message.
The task is not complete until both the summary and the task_complete call are present.
```

工具列表最后也注册了一个没有参数的 `task_complete`。

这意味着在 Autopilot 协议中，“模型停止生成”不等于“任务完成”。完整结束需要两个信号：

1. 给用户一段简短的结果摘要
2. 调用 `task_complete` 向宿主显式报告完成

请求只能证明宿主定义了这个完成条件。至于 IDE 内部如何区分等待工具、等待用户、执行失败等其他状态，还需要结合实际响应和产品实现进一步研究。

## 第二条 system 消息：注入团队与个人规范

第二条 system 消息把发现的 instruction files 直接放入请求。样本中包含：

- 仓库级 `.github/copilot-instructions.md`
- 用户级 Copilot 指令文件

这些内容包括日志方式、JSON 库选择、命名规范、测试要求、文档写作方式和隐私限制等。

值得注意的是，在这次可见的模型交互开始时，这些内容已经随 system 消息提供，模型无需在本轮再调用文件读取工具。因此，模型从本轮第一次推理开始就能看到相关规范。至于宿主在构造请求前通过什么内部机制读取这些文件，仅凭请求无法确定。

可以把这类规则分为几层：

- 产品规则决定智能体的基本行为
- 仓库规则表达团队实践
- 用户规则表达个人偏好
- 用户本轮要求决定当前任务目标

仅凭请求内容无法知道 Visual Studio 如何处理不同指令文件之间的冲突，也无法确认所有文件的发现范围和优先级。能确定的是：这份样本把两类指令文件序列化为第二条 system 消息。

## 第三条 user 消息：工作区特征

第三条消息包含工作区特征：

```text
# COPILOTWORKSPACE CONTEXT

The current workspace includes the following specific characteristics:
- Projects targeting: '.NET 10'
Consider these characteristics when generating or modifying code, but only if they are directly relevant to the task.
```

最后一句很重要：这些特征只有在与任务直接相关时才应被考虑。这能避免模型看见 `.NET 10` 后，无论用户问什么都强行套用最新语言特性。

## 第四条 user 消息：IDE 状态

第四条消息是更具体的 IDE 状态。脱敏后可以表示为：

```text
# IDESTATE CONTEXT
User's current development environment is: Microsoft Visual Studio Professional 2026 (18.7.1)
User's workspace root path is: [已脱敏]
User's solution file is: [已脱敏]
User's preferred terminal shell is: pwsh.exe
The user's current file: [当前文件]
The user has the following active Git repositories:
- Repository: [已脱敏]
- Current branch: [分支名]
- Remotes: [已脱敏]
```

这说明模型在调用工具之前已经收到解决方案位置、当前文件、终端类型以及活动仓库、当前分支和远程仓库等元数据。它不等同于完整的 Git 工作区状态，因为请求没有给出暂存区、未跟踪文件或工作树是否干净等信息。

这些 IDE 信息为后续工具调用提供了可靠起点，也与系统提示词中“不要猜文件路径”的规则相互配合。不过，这种组合是否就是该规则的设计原因，仍然只能视为合理解释，而不是请求直接声明的事实。

## 第五条 user 消息：真正的用户输入

最后一条消息才是用户本轮实际输入，样本内容只是一个简单问候。

这点很重要：工具注册表表示模型“可以调用什么”，不表示本轮“实际调用了什么”。对于一句简单问候，模型很可能直接回答，并不需要搜索、计划、编辑、构建或测试。

要研究完整执行链路，还需要继续抓取：

- 模型的流式响应
- tool call
- 工具返回值
- 工具结果被写回后的下一轮请求
- 最终完成消息

当前 `Request.json` 主要揭示的是一次推理开始前的输入契约。

## 从单智能体扩展为分层代理系统

新版请求增加了多种代理或转移能力。

### `search_agent`

这是只读的快速搜索智能体，适合查文件、搜关键字和解释代码库结构。它不能编辑、构建或运行终端，符合最小权限原则。

### `run_subagent`

这是通用子智能体，适合复杂、多步骤的研究或实施任务。工具描述明确规定：

- 子智能体不是后台异步执行，主智能体会等待结果
- 每次调用都是无状态的
- 子智能体只能返回一次最终报告
- 报告不会自动显示给用户，由主智能体负责总结
- 调用时必须明确说明是只研究还是需要写代码
- 如果用户指定了智能体名称，必须精确传入

这相当于主智能体把一个自包含的工作包委派出去，再负责整合结果。

### `profiler_agent`

只有慢代码、瓶颈、内存泄漏、基准测试或指标驱动优化等性能任务才应转给性能分析代理，而且转移前必须先向用户解释为什么需要测量数据。普通重构和可读性清理不应转交给它。

### `start_modernization`

当用户提出升级 .NET、迁移框架、更新包、转换为 SDK-style 或迁移 Azure 等需求时，提示词要求进入专门的 .NET 应用现代化体验。

### `query_azure_resource_graph`

对于 Azure 资源查询，模型可以把自然语言转换成 Resource Graph 查询并执行。结果过多时只展示前 10 条，以控制返回内容长度。

这些能力反映出一种任务路由思路：

```text
普通代码任务 -> 当前代码智能体
只读代码探索 -> 搜索智能体
复杂自治任务 -> 通用子智能体
性能诊断 -> Profiler Agent
.NET/Azure 现代化 -> Modernization Agent
Azure 资源查询 -> 专用查询工具
```

## 工具描述本身也是提示词

`apply_patch` 的详细格式既出现在 system 消息中，也出现在工具的 `description` 中。计划、子智能体、观察记录等工具同样带有很长的行为说明。

因此，工具 schema 不只是 API 参数定义，还承担了局部行为提示的作用。模型在决定是否调用工具时，同时能看到：

- 工具能做什么
- 什么时候应该使用
- 什么时候不应该使用
- 调用前后需要完成什么动作
- 失败时如何恢复

新版工具参数普遍采用严格 JSON Schema，并设置：

```json
{
  "additionalProperties": false
}
```

这能阻止模型随意添加未定义字段。部分逻辑上的可选参数采用“字段必须出现，但值允许为 `null`”的设计，例如后台终端输出的窗口参数。这样可以保持调用对象形状稳定，同时表达未指定值。

请求中还保留了类似 `{ToolNames.CodeSearch}` 的模板占位符痕迹。这可以视为提示词由模板动态拼装的证据之一，但无法据此还原具体的模板系统。

## Git 与长期偏好

### Git 工具偏向查看和审查

新版增加了两个 Git 专用工具：

- `git_view_changes`：打开 Git Changes 窗口管理待处理变更
- `git_review_local_changes`：审查当前本地变更

请求没有注册提交、推送、切换分支或重写历史的专用 Git 工具。不过，通用 PowerShell 终端理论上仍可能执行相应命令，所以不能据此断言这些操作在宿主中被禁止。能确定的只是：本次工具表为查看与审查提供了专用入口。

### `detect_memories` 识别可长期保存的规范

当用户纠正智能体行为、明确编码标准、表达个人偏好、要求记住某件事，或者详细描述代码风格和架构实践时，智能体应调用 `detect_memories`。工具参数要求提供具体记忆内容和置信度，且只有置信度不低于 0.6 时才应调用。第一条 system 消息还要求在用户任务完成后执行这一识别，因此工具描述负责定义触发条件，系统提示词又补充了调用时点。

工具描述说明 Copilot 可以据此提议把规则保存到仓库级或用户级指令。当前请求同时展示了指令文件被加载的结果，因此可以看到“读取既有规范”和“发现新规范”两端的能力。至于记忆最终保存到哪里、后续请求何时加载，仍取决于宿主实现，不能仅凭本样本确定。

## 36 个工具的完整分类

新版请求一共注册了 36 个 function 工具，`tool_choice` 为 `auto`。按职责分类如下。

### Git 与变更审查（2 个）

- `git_view_changes`：打开 Git Changes 窗口
- `git_review_local_changes`：审查当前本地变更

### 专项代理与云能力（3 个）

- `profiler_agent`：转交性能诊断和指标驱动优化任务
- `query_azure_resource_graph`：通过自然语言查询 Azure Resource Graph
- `start_modernization`：进入 .NET 或 Azure 应用现代化流程

### 代码理解与上下文检索（8 个）

- `code_search`：自然语言语义搜索
- `file_search`：按文件名或相对路径搜索
- `get_files_in_project`：列出指定项目文件
- `get_projects_in_solution`：列出解决方案项目
- `find_symbol`：查找符号定义、引用或实现
- `get_web_pages`：读取用户明确引用的网页；描述要求出现 URL 时调用
- `grep_search`：使用文本或正则表达式搜索工作区
- `get_file`：按行读取文件

### 构建与诊断（3 个）

- `get_errors`：获取指定文件编译错误
- `run_build`：构建工作区
- `get_output_window_logs`：读取 Visual Studio 输出窗口日志

### 文件操作（4 个）

- `remove_file`：删除文件并移除项目引用
- `create_file`：创建新文件，不能用于编辑已有文件
- `apply_patch`：应用结构化补丁
- `edit_file`：执行语义化文件编辑

### 终端（2 个）

- `run_command_in_terminal`：运行 PowerShell 命令
- `get_background_terminal_output`：查询、等待、读取或终止后台命令

### 子智能体（2 个）

- `run_subagent`：运行通用自治子智能体
- `search_agent`：运行只读搜索智能体

### 计划与需求交互（8 个）

- `plan`：创建实施计划
- `update_plan_progress`：更新主步骤状态
- `finish_plan`：完成并关闭计划
- `record_observation`：记录影响执行的事实
- `adapt_plan`：在前提变化时调整计划
- `signal_plan_ready`：标记计划已可交给执行流程
- `clarify_requirements`：在规划前注册澄清问题
- `ask_question`：向用户展示结构化选择题

这两个工具的 `questions` 参数在 schema 中是字符串，字符串内容才是 JSON 问题数组，而不是直接使用数组类型。

### 用户偏好（1 个）

- `detect_memories`：识别值得保存的用户或团队规则

### 测试（2 个）

- `run_tests`：运行 Visual Studio Test Explorer 中的测试
- `get_tests`：发现和筛选测试

### 任务生命周期（1 个）

- `task_complete`：显式标记 Autopilot 任务完成

合计：

```text
2 + 3 + 8 + 3 + 4 + 2 + 2 + 8 + 1 + 2 + 1 = 36
```

旧文章提到的 NuGet 专用工具、Microsoft Learn 工具和字符串替换工具，没有出现在这次请求中。这不能证明产品永久删除了它们，只能说明工具集合可能随版本、功能开关、模型、工作区或工作模式变化。

## 新旧样本的关键差异

旧样本信息来自作者此前保存的另一份请求，不是当前 `Request.json` 可以独立复核的内容。两份样本的主要差异如下：

| 观察点 | 旧样本 | 新样本 |
| --- | --- | --- |
| 消息数量 | 2 条 system + 1 条 user | 2 条 system + 3 条 user |
| 用户侧上下文 | 用户问题为主 | 工作区上下文、IDE 状态、实际问题分层注入 |
| 工具数量 | 26 | 36 |
| 主编辑方式 | 精确字符串替换 | `apply_patch`，失败时回退 `edit_file` |
| 搜索能力 | 文件、语义、符号搜索 | 新增 `grep_search` 和只读 `search_agent` |
| 智能体结构 | 以当前代码智能体为主 | 增加通用子智能体和多个专项代理 |
| 用户交互 | 主要依赖普通对话 | 新增结构化澄清问题卡片 |
| 计划机制 | 已有计划与进度工具 | 状态、失败流、用户中途修改和完成条件更详细 |
| Git | 未见专用工具 | 增加查看与审查本地变更的专用入口 |
| 任务完成 | 以模型回复结束为主 | 摘要 + `task_complete` 双重完成握手 |
| 个性化 | 注入指令文件 | 注入指令文件，并通过 `detect_memories` 发现新规则 |

仍然没有改变的核心原则包括：

1. 不凭空猜测工作区内容
2. 有专用工具时优先使用专用工具
3. 简单任务直接执行，复杂任务才创建计划
4. 先读文件再编辑
5. 做最小且聚焦的修改
6. 使用构建和测试验证结果
7. 不在聊天中粘贴文件修改，而是在 IDE 中应用并展示变更

## 从请求中可以抽象出的概念架构

下面的结构不是 Visual Studio Copilot 官方架构图，而是根据请求契约抽象出的概念分层：

```text
Visual Studio 宿主
├─ 上下文收集
│  ├─ 解决方案和项目特征
│  ├─ 当前文件与 IDE 环境
│  ├─ Git 仓库元数据
│  └─ instruction files
├─ 请求装配
│  ├─ 产品 system prompt
│  ├─ 用户/仓库规则
│  ├─ 工作区上下文
│  ├─ 用户输入
│  └─ 当前工具注册表
├─ 模型决策
│  ├─ 直接回答
│  ├─ 调用工具
│  ├─ 创建和执行计划
│  ├─ 向用户澄清
│  └─ 委派给子智能体或专项代理
├─ 执行与验证
│  ├─ 搜索和读取
│  ├─ 补丁编辑
│  ├─ 编译与测试
│  └─ 变更审查
└─ 生命周期
   ├─ 计划状态
   ├─ 观察与计划调整
   ├─ 用户偏好提议
   └─ task_complete 完成握手
```

这里最关键的不只是模型本身，还有模型周围的宿主能力：上下文收集、严格的工具 schema、可恢复的状态管理和验证工具共同决定了智能体能否可靠完成任务。

## 对自己编写代码智能体的启发

### 1. 把上下文装配做成独立模块

分别收集稳定策略、团队规范、项目状态、当前任务和工具描述，再按优先级组装消息，不要让业务代码到处拼接提示词。

### 2. 为不同精度的信息提供不同检索工具

文件路径、精确文本、程序符号和自然语言概念不是同一种搜索问题。工具越能利用已有确定信息，返回结果越可靠，上下文浪费越少。

### 3. 工具 schema 同时定义接口和行为边界

除了参数类型，还应写清使用条件、禁止条件、前置动作、错误恢复和结果展示方式，并关闭额外参数，减少模型自由发挥的空间。

### 4. 使用补丁而不是重写整个文件

补丁更容易审查、回滚和定位冲突。编辑前先读文件，编辑后先做局部诊断，最后再做全局验证。

### 5. 把复杂任务计划持久化

计划应有稳定步骤 ID、明确状态和实时进度，而不是只存在于模型的一次回复里。用户还应能够查看和修改计划。

### 6. 将“发现事实”和“修改计划”分开

先记录带证据的观察，再决定是否调整计划，可以避免模型因为普通错误频繁重写整个方案。

### 7. 子智能体应当无状态、权限受限、任务自包含

只读搜索代理不需要编辑权限。通用子智能体应收到完整任务说明，并只返回一次可整合的结果，由主智能体负责面向用户沟通。

### 8. 专项任务优先路由

性能分析、框架现代化和云资源查询有不同的证据要求与风险边界。为它们提供专用代理或工具，比把所有规则堆进一个万能代理更可控。

### 9. 区分自然语言摘要和机器可读完成状态

给用户看摘要，给宿主程序发送完成事件，不要依赖“文本看起来已经结束”来判断任务生命周期。

### 10. 在进入模型前做隐私筛选

这份样本说明 IDE 可能注入本地路径、仓库远程地址和用户级指令。自建智能体必须明确哪些信息允许发送、如何脱敏、保存多久，以及日志和抓包文件如何处理。

## 哪些内容不能从这份请求中确定

仅凭 `Request.json`，无法确定：

- Visual Studio 在什么条件下动态增减工具
- 模型服务端是否还叠加了不可见提示词
- 每个工具在宿主端的具体实现代码
- 工具调用是否经过额外权限确认或沙箱
- 上下文裁剪、缓存和 token 预算算法
- 子智能体实际使用什么模型
- 计划文件所在临时目录的具体位置，以及文件的保留时长和清理策略
- 本轮模型最终选择了哪些工具
- 工具执行结果如何写回后续模型请求
- 流式内容在 IDE 中如何呈现
- `detect_memories` 识别出的规则最终保存到哪里

这也是分析提示词时最需要注意的边界：请求中明确写出的内容是事实；根据工具组合还原出的架构只能算合理推测；具体宿主实现则需要更多日志、响应和代码证据。

## 总结

新版请求体展示出的 Visual Studio Copilot，已经具备一套完整度很高的代码智能体输入与工具契约：

1. 通过多条消息分层注入产品规则、指令文件、工作区特征、IDE 状态和用户问题
2. 根据已知信息的精确程度，在文件、文本、符号、语义搜索和搜索代理之间选择
3. 用结构化问题卡片处理真正影响实现的歧义
4. 用可见、可修改、可恢复的计划状态管理复杂任务
5. 通过通用子智能体和专项代理进行能力路由
6. 使用 `apply_patch` 进行局部编辑，并以 `edit_file` 作为后备
7. 通过编译、测试和输出日志形成验证闭环
8. 通过 instruction files 和 `detect_memories` 连接既有规范与新发现的偏好
9. 用用户摘要加 `task_complete` 完成 Autopilot 生命周期握手

与旧样本相比，最值得注意的变化不是工具从 26 个增加到 36 个，而是工具之间开始形成更清晰的层次和流程：先装配上下文，再判断是否澄清，随后搜索、计划、委派、编辑、验证，最后显式完成。

这也说明，代码智能体的能力不只来自大语言模型。模型外部的上下文工程、工具工程、状态管理、权限边界和验证机制，同样决定最终体验。

---

## 附录：新版核心 system prompt 的区段索引

新版第一条 system 消息很长，完整原文保存在 `Request.json`，这里不再重复粘贴，只列出主要区段及其职责：

| 区段 | 职责 |
| --- | --- |
| 顶层角色定义 | 名称回答规则、主题边界、合规、语言和回答风格 |
| `<preamble>` | 自动化代码智能体目标和计划基本原则 |
| `<context_gathering_strategy>` | 文件、符号、语义搜索和项目结构工具的选择 |
| `<maximize_context_understanding>` | 控制上下文范围，避免猜测和无关修复 |
| `<tool_use_guidance>` | 工具调用、规划、澄清、终端和记忆规则 |
| `<code_changes>` | 最小修改、根因修复和失败退出原则 |
| `<code_style>` | 注释、依赖和现有代码风格约束 |
| `<editing_files>` | `apply_patch`、`edit_file` 与错误修复协议 |
| `<testing_guidance>` | 测试发现、筛选和执行方式 |
| `<Autopilot>` | 摘要与 `task_complete` 的完成握手 |

如需逐字核对原文、参数 required 列表或每个工具的完整 JSON Schema，应直接查看经过妥善保护的请求样本。

更多技术博客，请参阅 [博客导航](https://blog.lindexi.com/post/%E5%8D%9A%E5%AE%A2%E5%AF%BC%E8%88%AA.html)
