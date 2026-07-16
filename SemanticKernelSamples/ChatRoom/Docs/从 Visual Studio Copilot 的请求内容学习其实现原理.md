# 从 Visual Studio Copilot 的请求内容学习其实现原理

本文介绍一份新版 Visual Studio Copilot 发给大语言模型的完整请求。通过分析其中的消息分层、系统提示词、工具定义和请求参数，可以看到 Copilot 已经不只是一个“把代码发给模型再等待回答”的聊天插件，而是一套包含上下文装配、任务规划、工具调度、子智能体、代码编辑、验证闭环和完成协议的智能体系统

本文内容由人类主导 AI 辅助编写

## 前言

我此前拿到过一份 Visual Studio Copilot 的请求，并根据那份请求写下了本文的第一版。当时的样本包含两条 system 消息、一条 user 消息和 26 个工具，代码编辑主要依赖 `replace_string_in_file`。

这次拿到的新请求已经发生了明显变化。新版样本包含：

- 2 条 system 消息
- 3 条 user 消息
- 36 个 function 工具
- 一套更完整的计划状态机
- 两种子智能体调用方式
- 交互式澄清问题的能力
- 基于补丁的代码编辑协议
- 面向性能分析、应用现代化和 Azure 的专项能力
- 显式的 Autopilot 完成握手

因此，这次更新不是简单地替换几个工具名称，而是需要重新理解 Copilot 如何构造一次智能体请求。

本文的事实来源是同目录下的 `Request.json`。请求样本中的本地绝对路径、内部远程仓库地址和自定义模型路由标签均不在文中原样展示。

本文更新于 2026 年 7 月 15 日。Copilot 的提示词、工具和产品交互都可能频繁变化，文中的结论只对应这一次请求样本，不能视为所有版本都固定如此。

## 请求内容的整体结构

先看最外层的请求信封。省略具体消息正文和工具 schema 后，可以整理成下面的结构：

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
  "model": "GPT-5.5 [自定义路由标签已脱敏]"
}
```

这里有几个直接可见的事实：

1. 请求使用流式输出，Visual Studio 可以边接收边展示模型回复或工具调用
2. `include_usage` 被启用，说明流式响应结束时还会返回 token 使用量
3. `tool_choice` 是 `auto`，由模型决定直接回答还是调用工具
4. 最大输出 token 数为 8192
5. 请求没有在样本中显式携带 `temperature`、`top_p` 等采样参数，至于服务端是否有默认值，仅凭这份请求无法判断

更重要的变化在 `messages`。新版请求不再只有“系统提示词 + 用户问题”，而是把指令、项目特征和 IDE 状态分层注入，再把真正的用户输入放在最后。

## 五条消息构成的上下文装配流水线

这五条消息依次承担不同职责：

| 顺序 | role | 内容 | 作用 |
| --- | --- | --- | --- |
| 1 | system | 核心智能体提示词 | 定义角色、工作流、工具策略、编辑与验证规则 |
| 2 | system | 指令文件内容 | 注入仓库级和用户级编码规范 |
| 3 | user | `COPILOTWORKSPACE CONTEXT` | 提供目标框架等工作区特征 |
| 4 | user | `IDESTATE CONTEXT` | 提供 IDE、解决方案、当前文件、终端和 Git 状态 |
| 5 | user | 用户实际输入 | 本次真正需要回答或执行的任务 |

这说明 Visual Studio Copilot 在发起请求前存在一个“上下文装配器”。它至少会收集以下几类信息：

- 产品预置的智能体规则
- 仓库和用户的 Copilot 指令文件
- 项目或解决方案特征
- 当前 IDE 状态
- 用户本轮输入
- 当前可用工具及其 JSON Schema

这些信息不是简单拼成一段长文本，而是利用 system 和 user 两种消息角色做分层。特别是编码规范文件被放在第二条 system 消息中，而项目状态被放在 user 消息中。这种组织方式使“应该怎样工作”和“这次工作环境是什么”保持分离。

从代码智能体的设计角度看，这比维护一份不断膨胀的万能提示词更合理：稳定策略由产品维护，团队规范来自仓库，个人偏好来自用户配置，易变化的 IDE 状态则在每次请求时动态生成。

## 工作区上下文不是模型自己猜出来的

第三条消息是工作区特征，样本中包含目标框架信息：

```text
# COPILOTWORKSPACE CONTEXT

The current workspace includes the following specific characteristics:
- Projects targeting: '.NET 10'
Consider these characteristics when generating or modifying code, but only if they are directly relevant to the task.
```

最后一句很重要：这些特征只有在与任务直接相关时才应被考虑。这是在避免模型看见 `.NET 10` 后，无论用户问什么都强行使用最新语言特性。

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

这意味着模型在调用任何工具之前，就已经知道当前解决方案、活动文件、终端类型以及 Git 仓库状态。工具负责进一步读取真实内容，而 IDE 上下文负责给工具调用提供起点。

由此也能解释系统提示词为什么反复强调“不要猜文件路径”：既然 IDE 已经提供了解决方案和当前文件，模型应当先利用这些可靠信息，而不是凭训练数据臆测仓库结构。

## 第二条 system 消息：把团队规范提升为执行约束

第二条 system 消息会将发现的指令文件直接纳入请求。样本中包括：

- 仓库级 `.github/copilot-instructions.md`
- 用户级 Copilot 指令文件

这些内容涵盖日志方式、JSON 库选择、命名规则、测试要求、文档编写方式以及隐私限制等。

值得注意的是，这些文件并非在执行过程中由工具临时读取，而是在请求组装阶段就已注入 system 消息。因此，模型从第一轮推理开始就能看到相关规则。对代码智能体而言，这是很有价值的分层：

- 产品规则决定智能体的基础行为
- 仓库规则决定团队惯例
- 用户规则决定个人偏好
- 本轮用户要求决定当前任务目标

当然，仅凭请求内容无法确认 Visual Studio 如何处理不同指令文件之间的冲突，也无法得知全部文件的发现范围和优先级。至少在该样本中，可以确认指令文件被序列化为高优先级上下文。

## 角色定义依然简洁

新版请求开头的角色定义与旧版基本一致：

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

它负责设定几项最基本的边界：

- 身份是 GitHub Copilot
- 只处理软件开发主题
- 遵守用户要求和 Microsoft 内容政策
- 避免侵犯版权
- 回答应当简洁、非人格化
- 使用 `zh-CN` 回复

真正复杂的部分并没有堆积在角色说明中，而是放进 `<preamble>`、`<context_gathering_strategy>`、`<tool_use_guidance>`、`<editing_files>` 和 `<testing_guidance>` 等 XML 风格的区段。这样更便于按模板维护不同职责的规则。

## 从“读取上下文”升级为分层检索

上下文收集策略依然强调先判断已有信息，再选择工具：

```text
- If the user's request includes specific file names or code snippets, prioritize reading those files directly
- If the user's request requires knowledge of a symbol's usage, definition, or implementation in the workspace, use find_symbol to find results
- If the user mentions specific functionality or errors, use code_search for semantic searches
- Use get_projects_in_solution and get_files_in_project when you need to understand the overall structure of the workspace
```

结合新版工具列表可以看到，检索能力已经形成多个层级。

### 已知文件：`get_file`

知道确切路径时，可以按行直接读取文件。该工具可指定起止行，并支持返回行号，从而避免把整个大型文件不加筛选地塞入上下文。

### 已知文件名：`file_search`

知道文件名或路径片段、但不知道完整位置时，可以搜索路径。它只返回相对路径，并限制最大结果数量。

### 已知文本：`grep_search`

已有错误码、配置键或确切文本时，可以基于 ripgrep 搜索文本或正则表达式，并支持 glob 范围与结果数量限制。

### 已知符号：`find_symbol`

已有类、方法或接口名称时，可以按定义、引用或实现查询符号树。它比普通文本搜索更权威，因为它理解符号关系，依据的不是字符匹配，而是编译器掌握的符号关系。

### 已知概念：`code_search`

只知道“数据库连接”“命令行参数”等自然语言概念时，可以使用语义搜索。提示词还明确要求，不要用它替代精确的符号查询，也不要并行调用它。

### 需要广泛探索时：`search_agent`

如果任务只需要阅读和搜索代码，可以把完整的探索目标交给专用搜索智能体。它只有读取和搜索能力，不能编辑、运行终端或构建，因此权限边界更小。

这套层级可以概括为：

```text
路径明确 -> 直接读取文件
文件名明确 -> 文件搜索
文本明确 -> grep
符号明确 -> 编译器符号查询
概念明确 -> 语义搜索
探索范围较广 -> 搜索子智能体
```

这也是代码智能体减少幻觉的关键手段：不依赖某一种包打天下的搜索能力，而是根据问题中已知信息的精确程度选择合适的数据源。

## “不要请求许可”与“遇到歧义时提问”并不矛盾

提示词中同时存在两条看似冲突的规则。

一条规则要求模型自主收集上下文：

```text
Do not ask the user for confirmation before doing so.
Never ask permission to use a tool.
```

另一条规则要求遇到歧义时使用 `ask_question`：

```text
When the user's request is ambiguous or lacks detail, always use the ask_question tool before proceeding with any investigation or implementation.
Do not guess on ambiguous design decisions — ask.
```

二者约束的是两类不同的动作：

- 不要询问“可以读取项目吗？”“可以执行搜索吗？”这类工具权限问题
- 当框架、架构、测试范围等选择会真正改变实现时，需要澄清用户需求

新版请求还提供了两种结构化提问工具：

- `clarify_requirements`：在探索之后、创建计划之前登记澄清问题
- `ask_question`：在一般场景下让用户从 2～5 个具体选项中选择

两者都会把问题展示为单选卡片，并保留自由输入选项。这说明 Copilot 不只是通过普通聊天文本向用户提问，还能在 IDE 中以需求澄清表单的形式进行结构化交互。

## 计划不是简单文本，而是可见的状态机

新版请求的计划机制比旧版更加完整。系统提示词首先设置了一道“计划门槛”：

- 跨越后端与前端、API 与测试、代码与配置等多个区域
- 需要调查根因、性能问题或不稳定测试
- 影响共享契约、Schema 或横切模式

满足这些条件时才应创建计划。简单任务即使需要修改几个文件，也应直接执行。

`plan` 工具需要生成包含理解、假设、方案、关键文件、风险和步骤的 Markdown。主要步骤通常为 5～12 个，并映射到 `step-1`、`step-2` 这类稳定标识符。

建立计划后，状态大致按下面的方式流转：

```text
pending -> in-progress -> completed
                       -> failed
                       -> skipped
```

按照 `update_plan_progress` 的说明，执行过程受到以下约束：

- 第一步开始时，要显式标记为 `in-progress`
- 每个主要步骤完成后立即更新
- 不要等所有工作结束后再批量补填状态
- 步骤进入最终状态后，可以自动启动下一个等待中的步骤
- 子步骤只是内部检查清单，不单独更新状态

如果执行中发现事实与计划不符，则使用以下机制：

1. 使用 `record_observation` 记录带有证据的错误、发现、决策或风险
2. 只有在确实必要时，才使用 `adapt_plan` 调整计划结构
3. 保留已完成步骤；被替代的步骤不删除，而是标记为 `skipped`

只有所有步骤都已完成并经过验证，而且不存在阻塞性观察时，才能调用 `finish_plan`。

这不只是“模型先写一段 Todo 列表”，而是一套可由 IDE 持久化和展示的任务状态机。提示词明确说明计划文件会实时呈现给用户，因此用户可以在执行过程中查看并修改计划。

## 两种计划交互可能服务于不同工作模式

工具列表中同时存在 `finish_plan` 和 `signal_plan_ready`：

- `finish_plan` 用于关闭计划
- `signal_plan_ready` 用于在计划已展示并获得用户批准后，将其移交给执行流程

结合 `clarify_requirements`，可以推测同一套工具注册表可能服务于不止一种产品模式：

- 当前智能体生成计划并立即执行
- 只负责研究与规划，由用户决定何时开始实现
- 计划智能体与执行智能体协作分工

这里需要强调“推测”二字。请求只展示了工具能力和调用规则，并不包含实际调用轨迹，因此无法仅凭这份 JSON 确认 Visual Studio 的完整 UI 流程。

## 从单一智能体扩展为分层智能体系统

新版请求最显眼的变化之一，是加入了多种智能体或任务转交能力。

### `search_agent`

这是一个只读的快速搜索智能体，适合查找文件、搜索关键词以及说明代码库结构。它不能编辑、构建或运行终端，符合最小权限原则。

### `run_subagent`

这是一个通用的自主子智能体，适合复杂的多步骤研究或实现任务。工具说明明确规定：

- 子智能体不会在后台异步运行，主智能体需要等待其结果
- 每次调用都是无状态的
- 子智能体只能返回一次最终报告
- 报告不会自动展示给用户，而是由主智能体进行总结
- 调用时必须说明是否需要编写代码
- 如果用户指定了智能体名称，必须原样准确传递

这就像主智能体委派一个自包含的工作包，再对返回结果进行整合。

### `profiler_agent`

性能问题不会被当作普通重构处理。只有涉及慢代码、瓶颈、内存泄漏、基准测试或指标驱动优化时，才会转交性能分析智能体；转交之前，还必须向用户说明为什么需要测量数据。

### `start_modernization`

当用户提出 .NET 升级、框架迁移、包更新、转换为 SDK 风格或迁移到 Azure 等要求时，会进入专门的 .NET 应用现代化流程，而不是由普通编码智能体直接处理。

### `query_azure_resource_graph`

针对 Azure 资源查询，模型可以把自然语言转换为 Resource Graph 查询并执行；如果结果过多，则只展示前 10 条，以控制上下文长度。

这些能力体现了一种路由思路：

```text
一般代码任务 -> 当前编码智能体
只读代码探索 -> 搜索智能体
复杂自主任务 -> 通用子智能体
性能诊断 -> Profiler Agent
.NET/Azure 现代化 -> 现代化智能体
Azure 资源查询 -> 专用查询工具
```

与其要求同一个模型同时把所有流程都做好，不如根据任务性质把工作路由到受限工具或专用智能体，这样更容易建立稳定的行为边界。

## 文件编辑从字符串替换改为结构化补丁

旧版样本使用的是 `replace_string_in_file` 和 `multi_replace_string_in_file`。在新版样本中，这两个工具已经消失，主要编辑工具改为 `apply_patch`，并以 `edit_file` 作为后备方案。

核心规则如下：

```text
Use the `apply_patch` tool to edit files.
Don't try to edit an existing file without reading it first.
Use `edit_file` as fallback if `apply_patch` cannot accomplish the required changes.
```

`apply_patch` 接收结构化 diff：

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

它还提出了以下要求：

- 默认提供修改前后各 3 行上下文
- 如果上下文不足以唯一定位，则使用 `@@` 指定类或方法
- 多处修改分别使用更新区段表示
- 严格保持原文件的空格或制表符缩进风格
- 新增行和删除行必须分别带有 `+` 或 `-` 标记
- 补丁必须包含明确的开始和结束标记

与精确字符串替换相比，补丁协议更接近开发者熟悉的 diff，而且一次调用就能表达同一文件中的多处局部修改。它的可靠性不再依赖整段旧字符串完全匹配，而是依赖上下文匹配。

`edit_file` 是语义编辑的后备方案。模型只需提供必要的代码片段，未修改区域可以用 `// ...existing code...` 表示。提示词要求先尝试补丁编辑，只有在确实无法应用时才退回 `edit_file`。这表明产品更偏好可审查、确定性更强的补丁操作。

编辑协议还设置了失败上限。如果针对同一文件连续三次仍无法修复相关错误，智能体就必须向用户请求指导，避免陷入无限修补循环。

## 工具说明本身也是提示词的一部分

`apply_patch` 的详细格式同时出现在 system 消息和工具的 `description` 中。计划、子智能体、观察记录等工具也都带有很长的行为说明。

这说明在智能体系统中，工具 schema 不只是 API 签名，也承担了部分提示词的作用。模型每次决定是否调用某个工具时，都能同时看到：

- 这个工具能做什么
- 什么情况下应当使用
- 什么情况下不应使用
- 调用前后需要完成哪些动作
- 失败时应当如何恢复

新版工具参数通常采用严格的 JSON Schema，并设置为：

```json
{
  "additionalProperties": false
}
```

这样可以防止模型随意添加未定义字段。一些逻辑上可选的参数采用“字段必须存在，但值允许为 `null`”的设计，例如后台终端输出的窗口参数。这样既能保持调用对象形状稳定，也能表达未指定的值。

请求中还保留了 `{ToolNames.CodeSearch}` 这类模板占位符的痕迹。这看起来像提示词生成模板的实现细节，也说明我们看到的只是一次动态组装的产物，而不是一份静态提示词。

## 代码修改规则强调修复根因，而不是掩盖问题

新版 `<code_changes>` 新增了两项重要约束：

```text
- Do not take shortcuts that merely hide problems. Address root causes of errors, warnings, and failures rather than working around them.
- If you cannot determine the correct fix after multiple attempts, explain what you tried and ask the user for guidance rather than applying a workaround.
```

这两条规则针对的是 AI 编程中常见的不良做法：

- 为了让构建通过而删除出错代码
- 通过禁用警告来“解决”问题
- 用空 `catch` 吞掉异常
- 跳过失败的测试
- 在未理解根因的情况下不断叠加补丁

Copilot 被要求进行最小修改，但“最小”并不等于“表面上的改动量最少”。更准确的含义是：在解决问题根因的前提下，不扩大无关范围。

## 构建、诊断与测试形成验证闭环

新版请求保留并细分了验证工具：

- `get_errors`：查看特定文件的编译错误，适合局部修改后的快速反馈
- `run_build`：构建整个工作区，用于任务结束前的最终验证
- `get_output_window_logs`：读取构建、CMake、调试、Git、NuGet 和测试等 Visual Studio 输出窗口日志
- `get_tests`：发现测试及其当前状态
- `run_tests`：按程序集、项目、完全限定名、类型或方法运行测试

测试指南建议先发现相关测试，再精确运行。如果普通筛选不够准确，就应使用 `FullyQualifiedName`。

由此形成典型闭环：

```text
读取代码 -> 修改文件 -> 查看局部错误 -> 运行相关测试 -> 构建工作区 -> 报告结果
```

终端工具依然存在，但提示词要求只在没有专用工具时才使用终端，并且不得并行执行多个终端命令。这说明 Visual Studio 原生的编译器、测试资源管理器和输出窗口能力优先于通用 Shell。

## Git 更改也成为智能体工作流的一部分

新版增加了两个 Git 工具：

- `git_view_changes`：打开 Git 更改窗口，管理待处理的更改
- `git_review_local_changes`：审查当前本地更改

结合注入了当前分支和远程仓库信息的 IDE 状态消息，可以看出 Copilot 已开始把“修改代码后如何查看变更”纳入工作流。

不过，样本中没有提供提交、推送、切换分支或重写历史的工具。因此，在这套工具集中，Git 能力更侧重查看和审查，而不是替用户完成高风险的仓库操作。

## `detect_memories` 连接本轮上下文与长期偏好

`detect_memories` 的规则非常明确。当用户做出以下行为时，应当调用它：

- 纠正智能体的行为或输出
- 明确编码标准、格式规则或团队惯例
- 表达个人开发偏好或身份
- 要求记住某件事，或写入指令文件
- 详细说明代码风格、模式或架构偏好

智能体需要调用该工具，并提供具体的记忆内容和置信度。

这与第二条 system 消息中的指令文件形成了闭环：

1. 请求开始时注入已保存的规则
2. 用户在对话中提供新的稳定偏好
3. `detect_memories` 识别值得长期保存的内容
4. 产品可以建议把它写入仓库级或用户级指令
5. 后续请求再次加载这些指令

因此，Copilot 的个性化并不只是依赖聊天记录，还可以沉淀为显式、可管理的规则文件。

## 面向 Autopilot 的显式完成握手

核心 system 消息的末尾新增了 `<Autopilot>` 区段：

```text
When you have fully completed the task, call the task_complete tool to signal that you are done.
IMPORTANT: Before calling task_complete, you MUST provide a brief text summary of what was accomplished in your message.
The task is not complete until both the summary and the task_complete call are present.
```

与之对应，工具列表的最后提供了一个无参数的 `task_complete`。

这意味着“模型停止生成”并不等同于“任务完成”。完整的结束需要两个信号：

1. 向用户提供简短的结果摘要
2. 调用 `task_complete`，通知宿主任务已经进入最终状态

这是代码智能体设计中很值得借鉴的一点。自然语言适合解释结果，结构化工具调用则适合让程序可靠地更新状态。两者同时存在时，IDE 就能区分：

- 模型正在暂时等待工具结果
- 模型需要向用户提问
- 计划仍在执行中
- 模型因错误而提前停止
- 整个任务已经成功结束

换句话说，完成并不只是文本语气，而是一种协议。

## 36 个工具的完整分类

新版请求共注册了 36 个 function 工具，并全部设置为由模型自动选择。按职责分类如下。

### Git 与变更审查（2 个）

- `git_view_changes`：打开 Git 更改窗口
- `git_review_local_changes`：审查当前本地更改

### 专用智能体与云能力（3 个）

- `profiler_agent`：转交性能诊断和指标驱动优化任务
- `query_azure_resource_graph`：使用自然语言查询 Azure Resource Graph
- `start_modernization`：启动 .NET 或 Azure 应用现代化流程

### 代码理解与上下文检索（8 个）

- `code_search`：自然语言语义搜索
- `file_search`：按文件名或相对路径搜索
- `get_files_in_project`：列出指定项目的文件
- `get_projects_in_solution`：列出解决方案中的项目
- `find_symbol`：查找符号定义、引用或实现
- `get_web_pages`：读取用户明确引用的网页
- `grep_search`：使用文本或正则表达式搜索工作区
- `get_file`：按行读取文件

### 构建与诊断（3 个）

- `get_errors`：获取指定文件的编译错误
- `run_build`：构建工作区
- `get_output_window_logs`：读取 Visual Studio 输出窗口日志

### 文件操作（4 个）

- `remove_file`：删除文件并移除项目引用
- `create_file`：创建新文件
- `apply_patch`：应用结构化补丁
- `edit_file`：执行语义文件编辑

### 终端（2 个）

- `run_command_in_terminal`：执行 PowerShell 命令
- `get_background_terminal_output`：读取或终止后台命令

### 子智能体（2 个）

- `run_subagent`：运行通用自主子智能体
- `search_agent`：运行只读搜索智能体

### 计划与需求交互（8 个）

- `plan`：创建执行计划
- `update_plan_progress`：更新主要步骤状态
- `finish_plan`：完成并关闭计划
- `record_observation`：记录影响执行的事实
- `adapt_plan`：前提条件发生变化时调整计划
- `signal_plan_ready`：标记计划已经准备好移交执行流程
- `clarify_requirements`：在规划前登记澄清问题
- `ask_question`：向用户展示结构化选择题

### 用户偏好（1 个）

- `detect_memories`：识别值得保存的用户或团队规则

### 测试（2 个）

- `run_tests`：在 Visual Studio 测试资源管理器中运行测试
- `get_tests`：发现测试

### 任务生命周期（1 个）

- `task_complete`：显式标记 Autopilot 任务已完成

合计：

```text
2 + 3 + 8 + 3 + 4 + 2 + 2 + 8 + 1 + 2 + 1 = 36
```

旧版文章提到的 NuGet 专用工具、Microsoft Learn 工具和字符串替换工具没有出现在这次请求中。这并不意味着产品永久删除了这些能力，但说明工具集可能会根据版本、功能开关、模型、工作区或工作模式动态变化。

## 新版请求与旧版请求的主要差异

| 观察点 | 旧版样本 | 新版样本 |
| --- | --- | --- |
| 消息数量 | 2 条 system + 1 条 user | 2 条 system + 3 条 user |
| 用户侧上下文 | 主要是用户问题 | 分层注入工作区上下文、IDE 状态和实际问题 |
| 工具数量 | 26 | 36 |
| 主要编辑方式 | 精确字符串替换 | `apply_patch`，失败时退回 `edit_file` |
| 检索能力 | 文件、语义和符号搜索 | 新增 `grep_search` 和只读 `search_agent` |
| 智能体结构 | 以当前编码智能体为主 | 增加通用子智能体和多个专用智能体 |
| 用户交互 | 主要依赖普通对话 | 增加结构化澄清问题卡片 |
| 计划机制 | 已有计划和进度工具 | 状态、失败流程、自动推进和完成前提更加严格 |
| Git | 没有专用工具 | 增加查看和审查本地更改的能力 |
| 任务完成 | 以模型响应结束为主 | 摘要 + `task_complete` 双重完成握手 |
| 个性化 | 注入指令文件 | 注入指令文件，并通过 `detect_memories` 发现新规则 |

保持不变的核心原则包括：

1. 不要凭空猜测工作区内容
2. 存在专用工具时，优先使用专用工具
3. 简单任务直接执行，复杂任务先制定计划
4. 编辑前读取文件，编辑后立即做局部诊断，再进行完整验证
5. 进行最小但聚焦的修改
6. 使用构建和测试验证结果
7. 不要在聊天中粘贴文件修改，而应在 IDE 中应用并展示变更

## 从这份请求中还原出的概念架构

根据这份请求，可以抽象出下面的概念架构。它不是 Visual Studio Copilot 的官方架构图，而是基于请求内容推导出的合理分层。

```text
Visual Studio
├─ 上下文收集
│  ├─ 解决方案和项目特征
│  ├─ 当前文件和 IDE 状态
│  ├─ Git 状态
│  └─ 指令文件
├─ 请求组装
│  ├─ 产品 system 提示词
│  ├─ 用户/仓库规则
│  ├─ 动态工作区上下文
│  ├─ 用户输入
│  └─ 当前可用工具注册表
├─ 模型推理
│  ├─ 直接回答
│  ├─ 调用本地工具
│  ├─ 创建并执行计划
│  ├─ 向用户澄清
│  └─ 委派给子智能体或专用智能体
├─ 执行与验证
│  ├─ 搜索与读取
│  ├─ 补丁编辑
│  ├─ 编译与测试
│  └─ 审查 Git 更改
└─ 生命周期
   ├─ 计划状态
   ├─ 观察与计划调整
   ├─ 用户记忆建议
   └─ task_complete 完成握手
```

这里最重要的并不是模型本身，而是围绕模型的“宿主系统”。如果没有可靠的上下文收集、严格的工具 schema、可恢复的状态机和验证能力，再强大的模型也可能只会生成看起来合理的文本。

## 自己设计代码智能体时可以借鉴的经验

如果根据这份请求设计自己的代码智能体，至少可以参考以下原则。

### 1. 把上下文编排做成独立模块

不要让业务代码到处拼接提示词。应当收集稳定策略、团队规范、项目状态、当前任务和工具说明，再按优先级组装消息。

### 2. 为不同精确度的信息提供不同检索工具

文件路径、文本、符号和自然语言概念并不是同一种检索问题。工具越能利用已有的精确信息，结果越可靠，上下文浪费也越少。

### 3. 工具 schema 同时定义接口与行为边界

不仅要明确参数类型，还应写清使用条件、禁用条件、前置条件、错误恢复方式以及返回结果如何展示。通过严格禁止额外参数，减少模型随意构造调用对象的空间。

### 4. 使用补丁，不要让模型重写整个文件

补丁更便于审查、回滚和定位冲突。编辑前先读取文件，编辑后立即进行局部诊断，再执行完整验证。

### 5. 持久化复杂任务的计划

计划应当具有稳定的步骤 ID、明确的状态和实时进度，而不是只存在于模型的一次回复中。用户应当能够查看、修改和中断计划。

### 6. 区分“发现事实”与“修改计划”

先记录带有证据的观察，再决定是否调整计划，可以避免模型因为普通编译错误而频繁重写整个解决方案。

### 7. 子智能体应当无状态、权限受限且任务自包含

只读搜索智能体不需要编辑权限。通用子智能体应接收完整的任务说明，并只返回结构化结果；由主智能体负责与用户沟通。

### 8. 优先路由专项任务，不要把一切堆给万能智能体

性能分析、框架现代化和云资源查询有着不同的证据要求与风险边界。为它们提供专用智能体或工具，比单纯把要求追加到基础提示词中更可控。

### 9. 区分自然语言摘要与机器可读的完成状态

既要向用户提供摘要，也要向宿主程序发送完成事件。不要依赖“看起来已经完成”的语气来判断智能体生命周期。

### 10. 在内容进入模型前过滤隐私信息

这份请求说明，IDE 可能注入本地路径、远程仓库地址和用户级指令。构建自己的智能体时，需要明确哪些信息可以发送、如何脱敏、保存多久，以及日志应如何记录这些信息。

## 仅凭这份请求无法确定的内容

分析请求内容很有价值，但也应避免过度推测。仅凭 `Request.json` 无法确认：

- Visual Studio 在什么条件下动态添加或移除工具
- 模型服务是否还会追加不可见的提示词
- 每个工具在宿主侧的具体实现代码
- 工具调用是否还要经过额外权限确认或沙箱
- 上下文裁剪、缓存和 token 预算算法
- 子智能体实际使用什么模型
- 计划文件存储在哪里、生命周期有多长
- 模型在真实任务中最终选择了哪些工具
- 工具执行结果如何写回后续模型请求

此外，工具“存在”只表示模型可以调用它，并不意味着本轮请求一定会调用。样本中的真实用户输入只是“你好”，因此很可能完全不需要搜索、计划或编辑工具。

如果要进一步研究完整执行原理，还需要同时捕获后续流式响应、工具调用、工具返回值以及下一轮请求。当前这篇文章主要揭示的是“Copilot 在开始推理前向模型提供了什么”。

## 总结

新版请求所展示的 Visual Studio Copilot，已经是一套相当完整的代码智能体运行时。

1. 通过多条消息分层注入产品规则、指令文件、工作区特征、IDE 状态和用户问题
2. 根据已知信息的精确程度，在文件、文本、符号、语义搜索和搜索智能体之间进行选择
3. 使用结构化问题卡片处理真正影响实现的歧义
4. 通过可见、可修改、可恢复的计划状态机管理复杂任务
5. 通过通用子智能体和专用智能体进行能力路由
6. 使用 `apply_patch` 在 IDE 中完成可审查的局部编辑
7. 通过构建、测试和输出日志形成验证闭环
8. 通过指令文件和 `detect_memories` 连接团队规则与长期偏好
9. 通过用户摘要和 `task_complete` 完成任务生命周期握手

与旧版相比，最值得关注的变化不是工具从 26 个增加到 36 个，而是工具之间开始形成分层和流程：先组装上下文，再判断是否需要澄清，然后执行搜索、计划、委派、编辑和验证，最后显式完成。

这说明代码智能体的核心竞争力并不只来自某一个大语言模型。模型之外的上下文工程、工具工程、状态管理、权限边界和验证机制，才真正决定了体验上限。

---

## 附录：新版核心 system 消息的区段索引

新版第一条 system 消息很长，完整原文保存在 `Request.json` 中。这里不再重复粘贴，只列出主要区段及其职责。

| 区段 | 职责 |
| --- | --- |
| 顶层角色定义 | 身份、主题边界、合规、语言和回答风格 |
| `<preamble>` | 自动化编码智能体的目标和基础规划原则 |
| `<context_gathering_strategy>` | 文件、符号、语义搜索和项目结构工具的选择 |
| `<maximize_context_understanding>` | 控制上下文范围，避免猜测和无关修改 |
| `<tool_use_guidance>` | 工具调用、计划、澄清、终端和记忆规则 |
| `<code_changes>` | 最小修改、根因修复和失败退出原则 |
| `<code_style>` | 注释、依赖和现有代码风格约束 |
| `<editing_files>` | `apply_patch`、`edit_file` 和错误修复协议 |
| `<testing_guidance>` | 测试发现、筛选和执行方式 |
| `<Autopilot>` | 摘要与 `task_complete` 的完成握手 |


通过指令文件发现团队规范或个人设置，并在整个请求生命周期中持续使用这些规则，这一思路非常令人印象深刻，也为代码生成 AI 未来的发展方向提供了很多启示。
