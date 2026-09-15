# Responses 能力覆盖矩阵

## 判定规则

“支持”必须具备请求构造、响应解析、消息投影、错误处理和测试。只有 Web Search 属于本方案要求第一等支持的 SDK 托管工具。

| 能力 | 产品级目标行为 |
| --- | --- |
| Instructions 与 Items | 原生使用，不转换成 Chat Completions messages |
| 文本流 | 按 delta 实时投影并处理 completed/incomplete/failed |
| 图片输入 | 使用 input image content part，保持与文本顺序 |
| `previous_response_id` | 用于多轮状态链 |
| Conversations | SDK 与服务支持时为本地会话绑定 conversation |
| `store=true` | 保存远端标识并支持重启续聊 |
| `store=false` | 保存继续上下文所需 Items 和 encrypted reasoning |
| Reasoning effort | 映射现有思考强度 |
| Reasoning summary | 请求并流式展示“思考摘要” |
| Cached/reasoning token | 映射到现有用量模型 |
| 自定义函数工具 | 复用 AgentLib `AIFunction`，完成 call/output 循环 |
| Parallel tool calls | 按 API 语义执行同批调用并稳定排序结果 |
| Web Search | 原生注册工具，投影状态、来源、annotations 和 citations |
| Annotations/citations | 保留 URL、标题及其与答案文本的引用关系 |
| Server-side cancel | 已取得 response id 且 SDK 支持时取消远端 response |
| Compact | 自动压缩开启时使用 Responses 可续接上下文管理能力 |
| 持久化 | 使用可选 ResponsesState，继续兼容旧 XML |
| 错误 | 保留 HTTP、code、param、status 和 response id |

## 非目标托管工具

以下 SDK 托管工具不属于本方案承诺范围：

- File Search；
- Code Interpreter；
- Remote MCP；
- Computer Use；
- Image Generation；
- Apply Patch。

不为这些能力增加设置项、界面占位、虚假开关或半成品实现。未来增加时沿用现有 `ResponsesStreamProjector` 与工具注册边界，以新增局部 handler 的方式接入，不修改 Toggle 分支、共享工作区 runtime、会话协调层和消息模型。

## 固定行为

- 不增加 Responses 设置或 capability 开关；
- Toggle 开启后直接使用当前模型的现有 Endpoint、Key 和模型定义；
- 端点不支持 Responses 时显示真实错误；
- 不自动回退 CodingAgent；
- 不把未收到的 reasoning summary 伪装为已完成；
- 不把未执行的 compact 伪装为压缩成功；
- 未知 Item 保留安全的类型信息；若影响工具闭环或最终状态，则运行不得标记成功。

## 自然演进证明

Responses 的网络读取、Item 投影、工具执行和会话状态彼此分离。新增 SDK Item 或托管工具时，只需增加对应注册与 projector handler；本地编程工具、Shell 分支、设置逻辑和 Avalonia 主界面均无需重做。
