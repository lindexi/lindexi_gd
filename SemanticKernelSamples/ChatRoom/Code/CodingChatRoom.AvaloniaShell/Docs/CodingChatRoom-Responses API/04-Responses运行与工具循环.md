# Responses 运行与工具循环

## 请求构造

每轮请求使用 `CreateResponseOptions`：

- `Model`：当前选中模型名；
- `Instructions`：统一编程提示词；
- `InputItems`：本轮用户输入或工具结果 Items；
- `PreviousResponseId` 或 conversation：来自会话协议状态；
- `ReasoningOptions`：映射现有思考强度并请求 reasoning summary；
- `ParallelToolCallsEnabled=true`；
- `Tools`：本地函数工具和 Web Search；
- `MaxOutputTokenCount`：沿用模型定义；
- `Metadata`：只使用任务、会话和运行标识。

不增加配置开关。Responses 客户端复用现有模型 Endpoint、Key 和模型选择。Web Search 作为 Responses 分支内建能力加入请求。

用户文本和图片分别映射为 input text 与 input image content part，并保持原始顺序。

## 流式事件映射

按 `SequenceNumber` 处理事件：

| Responses 更新 | UI 投影 |
| --- | --- |
| `response.created/queued/in_progress` | 更新运行状态和 response id |
| `output_text.delta` | 追加回答文本 |
| `reasoning_summary_text.delta` | 追加“思考摘要” |
| function call item | 创建并完成现有工具消息项 |
| Web Search 状态与结果 | 展示搜索过程、来源和引用 |
| `completed` | 更新终态、usage 和 response id |
| `incomplete` | 返回包含具体 reason 的错误 |
| `failed/error` | 保留 code、param、status 和 response id |

流读取、Item 投影和状态更新分离，避免单个事件处理器同时承担网络、工具与 UI 职责。

## 本地函数工具循环

1. 发起流式 response。
2. 收集完成的 function call Items。
3. 按 call id 创建 UI 工具项。
4. 按 API 返回的并行工具语义调用对应 `AIFunction`。
5. 每个结果生成 function call output Item。
6. 使用刚完成的 response id 发起后续 response。
7. 直到没有待执行的本地函数调用。

并行结果按原 `OutputIndex` 稳定排序，保证展示和日志一致。

## 工具结果

- 普通返回值转为稳定文本或 JSON；
- 多模态结果继续投影到现有消息模型，并给模型提供可解释文本；
- 工具异常按既有工具失败语义展示并返回给模型；
- `OperationCanceledException` 终止整次运行。

## Web Search

Web Search 是本方案唯一要求第一等支持的 SDK 托管工具：

- 请求中注册 SDK 原生 Web Search 工具；
- 流式展示搜索开始、进行和完成状态；
- 保留答案中的 URL annotation、标题和引用关系；
- 搜索结果只作为模型上下文和引用来源，不自动写入工作区；
- 工具失败保留服务端错误，不用本地 HTTP 搜索伪装成功。

File Search、Code Interpreter、Remote MCP、Computer Use、Image Generation 和 Apply Patch 不属于本方案承诺范围，也不预留 UI 或设置占位。Responses Item 投影器和工具注册边界允许未来按相同模式局部增加，无需重写运行循环。

## 取消与运行结束

- 用户停止时取消当前流和本地工具调用；
- 已取得 response id 且 SDK 支持服务端取消时，调用服务端 cancel；
- completed、incomplete、failed 和 canceled 都更新内存中的协议状态，并由现有会话保存时机持久化；
- 不为异常退出另建高频检查点、后台恢复守护或额外事务层。

取消设计保持端到端可取消，同时不引入与当前产品使用方式无关的过度恢复机制。
