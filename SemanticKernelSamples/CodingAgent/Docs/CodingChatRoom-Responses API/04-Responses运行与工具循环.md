# Responses 请求、历史与工具调用

## 本章边界

本章描述当前原生 SDK 接入方法。请求与工具循环已通过 API 用法实验验证，不等于 Shell、完整协议持久化或全部 UI 已完成。后续产品目标与当前实现必须分别说明。

## 创建请求

- 使用 `CreateResponseOptions`，`Model` 填配置中的调用模型 ID，不是提供商名或展示名。
- `Instructions` 设置本轮系统提示词；每轮显式提供需要的提示词和工具定义，不假定配置随历史自动继承。
- `InputItems` 使用 SDK 原生 Items，包含所需历史及本轮新增输入。
- `StoredOutputEnabled = true` 表达保存请求；它与提示词缓存是两件事，不替代客户端历史管理。
- 原生 `ResponsesClient` 不绑定模型，每个请求都设置模型。
- Endpoint 为 API 基础地址，SDK 追加 `/responses`；需要 `/v1/responses` 时基础地址包含 `/v1`，不重复填写完整路径。
- `CreateResponseStreamingAsync(options, token)` 使用的 options 必须设置 `StreamingEnabled = true`。
- 不新增配置格式、能力探测、隐式协议降级。

## 多轮上下文

当前应用使用显式历史：依次保存用户输入、模型返回的原生 `OutputItems`、工具结果和后续输入，保持原始顺序。不要只提取回答文本；工具调用标识和推理项也属于续接内容。

`PreviousResponseId` 引用的是上一轮返回的 `id`，不是其返回的 `previous_response_id`。当前显式历史路线不依赖该字段补齐上下文，不混用两种方式制造隐藏依赖。请求带 `store:true` 不意味着客户端可以删除历史；返回提示词缓存字段也不代表服务端已保存可恢复的对话。

## 本地函数工具

`ResponsesClient` 负责 HTTP、序列化和事件解析，不自动执行本地函数。`AIFunctionFactory.Create()` 负责包装函数，也不会自动启动调用循环。

1. 将真实本地函数包装为 `AIFunction`，维护函数名与实例的映射。
2. 使用 `ResponseTool.CreateFunctionTool` 把名称、参数 Schema 和描述加入 `request.Tools`。
3. 发起请求，从返回 `OutputItems` 获取 `FunctionCallResponseItem`；流式路径等待完整参数后执行。
4. 按返回名称找到本地函数，将 JSON 参数传给 `InvokeAsync`，传递执行取消令牌。
5. 使用原调用的 `CallId` 创建 `FunctionCallOutputResponseItem`，内容为真实执行结果，不手写假结果。
6. 保存本轮原生输出，追加工具结果，再发起下一轮请求，继续提供本轮需要的工具和提示词。
7. 直到模型不再请求本地工具。

并行调用由执行层按请求和实际输出组织，不在每个请求中无条件开启并行。工具异常与取消遵循现有执行语义；不转成伪造成功结果。

已有 `Microsoft.Agents.AI.OpenAI` 提供 `ResponsesClient.AsAIAgent(...)` 等更高层入口，但返回 `ChatClientAgent`，与当前直接处理原生 Responses 的设计不同。不为省略工具循环擅自切换架构。

## 内置 Web Search

Web Search 是服务端托管工具，与本地函数调用不同：

- 显式执行 `request.Tools.Add(ResponseTool.CreateWebSearchTool())`。可以在 options 初始化之后添加，发送的是同一个对象。
- 提示词要求实际搜索、优先官方来源，并给出新闻日期、模型名称、链接和检索时点。仅在提示词写“使用 WebSearchTool”不等于注册工具。
- 发起请求后，由服务端执行搜索；客户端不为它实现本地函数，也不制造 `function_call_output`。
- 从原生 `OutputItems` 中检查 `WebSearchCallResponseItem`，这才是实际工具调用记录；不能仅凭回答声称搜索过或带了链接判断。
- 完整原生结果保留搜索 action、状态及回答引用。当前没有给普通文本项增加 Annotations 集合；原生记录可读与 UI 引用渲染完成不是同一件事。
- 不固定最新新闻或最新模型名称为断言常量；检索内容会更新，测试核验真实工具调用、请求注册、终态和非空回答。

## 消息接入与用量

通过 `CreateManualSendMessageContextAsync()` 获得上下文；从 `LanguageModel` 的 `IResponsesClientProvider` 获取客户端。保留上下文原有 Chat 能力，不让 `CopilotChatManager` 承担 Responses 循环。

消息的 `ResponseInfo` 属性延迟创建，直接接收 `AppendResponse`、`AppendResponseUpdate`、`AppendToolResult`。片段定位在信息对象中，文本/推理消息项负责追加及增量事件，消息汇总既有通知。

用量映射至消息原有当前值和累计值，同一响应终态不重复累计。缓存量读取 `usage.input_tokens_details.cached_tokens`，命中率计算与稳定前缀规则见 05。原生 SDK 结果不经 UI 文本重建。

## 当前未完成的产品能力

Shell、普通多轮、思考强度、工具、停止后发送、协议存储及 Runtime 重建续聊已接通并有定向测试。插话使用原生用户 Item 队列，在工具批次后或无工具终态追加，不取消重发。

原生 Compact 已实现，无回退，完整 output 替换历史；自动触发与 CodingAgent 阈值对齐且插话优先，但压缩后语义续聊未通过。完整异常事件、引用 UI、取消后不完整工具历史续接仍需完善。

annotations:null 的特定 completed 解析异常按用户明确授权完成处理，保留之前的原生 item.done；其他错误不因此吞掉，终态用量可能缺失。实际能力边界见 10，调查见 14。
