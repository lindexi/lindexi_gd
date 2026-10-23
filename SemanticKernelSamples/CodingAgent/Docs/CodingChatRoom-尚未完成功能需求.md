# CodingChatRoom 尚未完成功能需求

## 1. 文档目的与当前状态

本文记录 PrismCodingAgent 当前尚未完成或仅部分实现的需求，并保留已实现事项的简要索引，避免重复开发。

当前应用项目为 `CodingAgent/Code/App/PrismCodingAgent.AvaloniaShell`，不再使用旧项目路径 `CodingChatRoom.AvaloniaShell`。

状态依据当前代码及已有测试内容核对；本次为文档整理，未运行测试，也不代表真实模型服务或 Windows 实机验收已经完成。已存在测试与测试已通过是不同结论。

| 事项 | 当前状态 | 后续范围 |
| --- | --- | --- |
| LSP 工作目录按需设置与项目范围隔离 | 部分基础能力已实现，核心需求未完成 | 已有延迟启动、停止和重启；仍需显式设置独立 LSP 目录与范围隔离 |
| 文件工具以返回值传递错误信息 | 部分完成 | 已有参数、缺失路径、读取前置条件及替换匹配错误返回；仍需统一处理预期的路径、权限和 I/O 失败 |
| 从指定消息开始新建对话 | 未发现实现 | 消息入口、上下文复制、协议合法边界及独立保存 |
| 技能功能 | 底层有基础设施，当前应用尚未完整接入 | 不应把现有单选“能力”模板视为完整技能功能 |
| CodingAgent 思考强度设置 | 核心实现已完成，不再作为待开发功能 | 保留实现索引与验证边界；详见第 6 节 |

本文不要求修改业务代码，也不将实现核查中发现的测试覆盖缺口自动扩大为新的产品功能。

## 2. LSP 工作目录按需设置与项目范围隔离

### 2.1 已有能力与剩余问题

当前已有按需启动语言服务器、停止和再次启动的基础能力，但这不等于已经支持独立的 LSP 工作文件夹。

`CodingWorkspaceCache` 仍将同一工作区路径交给普通文件工具和 Roslyn 工具；语言服务器首次查询时使用该路径启动。尚未提供供 Agent 显式设置 LSP 工作文件夹的工具。

当工作路径包含多个文件夹或多个项目时，仍存在加载范围过大、内存占用和同名符号污染等问题。

### 2.2 尚待实现的要求

- 新增一个可供 Agent 调用的“设置 LSP 工作文件夹”工具，参数为项目目录或文件夹路径；
- 应用初始化和工作区初始化不直接将整个应用工作路径设置为 LSP 分析范围；
- 设置成功后，依赖 LSP 的符号定义、引用、接口实现等工具只在该文件夹范围内工作；
- Agent 可以按当前任务切换 LSP 工作文件夹；
- 切换时释放或替换旧范围对应的 LSP 状态，避免旧项目符号污染新项目；
- 所有依赖 LSP 的工具调用前检查是否已设置目录，未设置时返回清晰错误，不能隐式使用整个工作路径或自行猜测目录；
- 普通文件读取、查找等工具继续使用应用工作路径，不受 LSP 目录选择限制。

未设置时的错误信息应明确提示：

> 尚未设置 LSP 工作文件夹。请先设置需要进行符号分析的项目目录，再调用此工具。

### 2.3 验收标准

- 打开多项目工作路径时，不会自动把整个工作路径加载到 LSP；
- 未设置目录时，LSP 工具返回可读的前置条件错误；
- 设置项目目录后，符号结果只来自该范围；
- 切换项目目录后，不再返回旧项目符号；
- 普通文件工具仍能访问允许范围内的其他文件夹；
- 内存占用和符号结果污染的改善需实际验证，不能只根据延迟启动判断完成。

### 2.4 实现与测试索引

- `AgentLib/AgentLib.Coding/CodingWorkspaceCache.cs`
- `AgentLib/AgentLib.Coding/RoslynAgentTools.cs`
- `AgentLib/AgentLib.Coding/RoslynLspClient.cs`
- `AgentLib/AgentLib.Coding.Tests/CodingWorkspaceToolProviderTests.cs`

已有测试涉及启动失败、停止和再次启动，不代表显式目录设置及多项目隔离已经验收。

## 3. 文件工具以返回值传递错误信息

### 3.1 已完成部分

当前普通文件工具已对多类预期失败返回可读文本，包括：

- 多类参数错误、文件或目录不存在、工作区越界；
- 写入或替换前未读取文件、文件在读取后被外部修改；
- 替换文本未匹配或匹配不唯一。

因此，不再将文件工具统一描述为“只能抛出异常，Agent 看不到具体原因”。

### 3.2 尚待完成的要求

继续核查读取文本、按行读取、列出目录、查找文件或目录、搜索文件内容、写入和替换等同类工具，保持错误处理一致：

- 将预期的非法路径、访问被拒绝和 I/O 失败转换为可读返回结果；部分实际读写调用仍可能直接抛出异常；
- 递归枚举遇到权限或 I/O 失败时，不应只跳过而完全不告知 Agent；
- 文件内容搜索读取失败时，不应将失败等同于“没有匹配项”；
- 成功与失败结果应具有稳定、可辨识的格式；
- 错误包含操作类型、目标路径和具体原因，不能只写“调用失败”，也不直接暴露完整异常堆栈；
- 程序缺陷等不可恢复异常不要求伪装成普通输入错误；
- 修复某个工具时同时检查同类工具，避免只修单一入口。

图片文件工具仍存在通过异常报告缺失、越界或格式错误的情况；纳入文件工具错误一致性核查时，应明确其返回方式。

### 3.3 验收标准

- Agent 能从返回结果区分文件不存在、权限不足、路径错误和匹配不唯一等原因；
- 失败结果能指导 Agent 修正路径或参数；
- 部分目录或文件读取失败不会被误报为完整搜索成功且无命中；
- 不把所有失败统一解释为“文件不可访问”；
- 成功场景的现有行为保持不变。

### 3.4 实现与测试索引

- `AgentLib/AgentLib/Tools/WorkspaceTools_/WorkspaceToolProvider.cs`
- `AgentLib/AgentLib/Tools/WorkspaceTools_/WorkspaceFileLineReader.cs`
- `AgentLib/AgentLib/Tools/WorkspaceTools_/PatternMatch_/WorkspaceFilePatternMatcher.cs`
- `AgentLib/AgentLib/Tools/WorkspaceTools_/Replace_/WorkspaceFileStringReplacer.cs`
- `AgentLib/AgentLib.Coding/CodingWorkspaceContentTools.cs`
- `AgentLib/AgentLib.Tests/WorkspaceToolProviderTests.cs`
- `AgentLib/AgentLib.Tests/WorkspaceFileStringReplacerTests.cs`
- `AgentLib/AgentLib.Coding.Tests/CodingWorkspaceContentToolsTests.cs`

已有参数校验和替换匹配测试，不能据此认定权限、非法路径及 I/O 失败的返回值已完整覆盖。

## 4. 从指定消息开始新建对话

### 4.1 当前状态

未发现消息级“从此处新建对话”入口及对应分支流程。现有新建、打开和保存会话不等于已经实现消息分支。

### 4.2 尚待实现的要求

- 每条适合分支的消息提供“从此处新建对话”入口；
- 新对话复制原对话从开始到所选消息为止的有效上下文，不包含后续内容；
- 原对话不删除、不截断、不覆盖；
- 新对话获得独立的会话标识、保存记录和后续消息序列；
- 创建后切换到新对话，允许继续输入；
- 工具调用请求与结果保持完整配对，不从协议结构不完整的位置创建分支；
- 对工具调用过程中的消息，选择最近的合法边界，或明确提示该位置不能分支；
- 继承必要的系统提示、模型配置和工作区上下文；
- 新对话后续修改不能反向影响原对话。

### 4.3 验收标准

- 可以从普通用户消息或 Agent 消息创建新对话；
- 新对话仅包含选中位置及之前的有效上下文；
- 原对话及后续消息保持不变；
- 新旧对话可以独立续聊和保存；
- 不生成缺少工具请求或工具结果的无效历史。

## 5. 技能功能

### 5.1 已有基础与功能边界

`AgentLib` 已有 `AddSkillFolder`、`AgentSkillsProvider` 和请求级技能目录基础设施，并有 `CopilotChatManagerSkillTests`；但尚未发现当前 PrismCodingAgent 应用完整接入这些技能入口。

当前应用的“能力”功能从本地文件读取提示词模板，选择后展开为本次用户输入。它是相邻能力，不等于已经完成技能发现、多选、按需读取、来源优先级和会话追踪。

### 5.2 尚待实现的要求

- 支持发现和列出可用技能，并查看名称、说明和内容；
- 支持发起任务前选择一个或多个技能，以及对话过程中按需启用；
- Agent 能获知可用技能，并在任务匹配时读取或使用；
- 技能内容按需加载，不把所有技能全文默认加入每轮上下文；
- 明确应用级、用户级或工作区级等来源与作用范围；
- 同名技能具有稳定的优先级和来源提示；
- 技能无效、缺失或读取失败时有清晰错误，不影响其他技能；
- 不绕过现有工具权限、工作区边界和沙盒限制；
- 会话记录能表达本次任务启用了哪些技能，便于恢复和追踪；
- 界面能够查看当前已启用的技能。

### 5.3 首期范围与验收标准

首期至少支持本地文本或 Markdown 技能的发现、列表、内容读取和向 Agent 提供选中技能；不要求技能市场、在线下载、自动更新或脚本直接执行。

验收时需确认用户能查看技能说明、选中技能能实际影响任务、未使用技能不会无条件占用大量上下文、来源和优先级可辨识，且不突破原有安全边界。

## 6. 已实现：CodingAgent 思考强度设置

### 6.1 已落地能力

此项核心实现已完成，不再作为待开发功能重复安排：

- 聊天界面在模型选择器附近提供固定的“默认、低、中、高”选项；
- 默认值为 `null`，不主动设置主运行的思考强度；
- 运行期间模型和思考强度选择器禁用；
- 使用 `Microsoft.Extensions.AI.ReasoningEffort`，通过轻量 `CodingChatRunOptions` 合并传递自动压缩、`dotnet run` 和思考强度；
- 每次运行创建请求选项，不通过修改共享模型客户端设置强度；
- 循环主运行继续传递同一组选项，活动运行中的插话注入当前运行；
- 自动和手动压缩不接收主运行的思考强度选项；
- 已有 Chat Completions 和 Responses 运行链路的请求选项处理。

当前调用链为：

`ChatViewModel → CodingWorkTaskController.SendMessageAsync → ICodingChatRunner.RunAsync → 对应 CodingAgent / Responses 运行链路 → 模型请求`。

### 6.2 已变更的持久化与协议口径

原文“思考强度仅进程内生效、不持久化”的要求已过时：当前按工作任务将思考强度保存至 `WorkTasks.json`，并在任务恢复时还原；这不等于写入模型定义或 Shell 全局模型配置。

所有模型使用固定强度选项，不根据模型名称猜测能力。默认不主动指定强度；显式选择强度后，模型服务不接受该值时应保留明确错误，不把旧文档中的“不支持模型不发送字段”理解为要求新增模型能力推断或静默降级。

### 6.3 实现、测试索引与验证边界

主要实现：

- Shell 的 `ViewModels/ChatViewModel.cs`、`Views/ChatView.axaml`；
- Shell 的 `Services/WorkTasks/CodingWorkTaskController.cs`、`WorkTaskStore.cs`；
- Shell 的 `ViewModels/MainViewModel.cs`、`App.axaml.cs`；
- `AgentLib/AgentLib.Coding/CodingChatRunOptions.cs`、`CodingAgent.cs`；
- `AgentLib/AgentLib.Coding/Responses/ResponsesCodingAgentRunner.cs`。

已有测试内容包括：

- Shell 的 `ShellStructureTests.cs`：选择入口、固定选项和默认值；
- Shell 的 `CodingChatSendingTests.cs`：思考强度传至运行器；
- Shell 的 `WorkTaskStoreTests.cs`：包含思考强度的任务元数据保存与加载；
- `AgentLib/AgentLib.Coding.Tests/ResponsesCodingAgentTests.cs`：原生请求中的显式强度；
- Shell 的 `ResponsesWorkflowLiveTests.cs`：真实环境下的界面到请求链路验证入口，需要环境配置，不能仅凭测试存在认定已验收。

最终协议默认字段完全省略、普通 Chat Completions 工具调用前后强度一致，以及循环、插话、压缩隔离等场景，未发现完整的专项断言覆盖。这里记录的是验证边界，不代表核心功能未实现，也不声明所有模型供应商均已实测通过。

历史设计参见[归档的思考强度设置方案](归档/CodingChatRoom-CodingAgent思考强度设置方案.md)；其中旧约束不覆盖当前任务持久化行为。
