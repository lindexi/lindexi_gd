# Copilot 聊天日志注入与落盘方式

## 问题答案

`CopilotViewModel` 的聊天日志现在走可替换的 `ICopilotChatLogger`，默认实现是 `FileCopilotChatLogger`。

默认约定有 5 个：

1. 日志能力由 `CopilotViewModel.ChatLogger` 提供，外部可以在初始化阶段替换实现。
2. `FileCopilotChatLogger` 允许在构造时分别指定文本日志文件夹与聊天历史文件夹。
3. 每个会话按 `CurrentSessionId` 单独落一个可读文本日志文件，文件里顺序追加“时间 + 说话人 + 内容”；如果助手消息同时带有思考链和正文，则按统一组合文本一起写入；若带有 `UsageDetails`，则继续追加用量摘要。
4. 同一个会话还会在 `CopilotChatHistory` 文件夹里维护一份 XML 文件，文件名为 `yyyyMMdd_HHmmss_{SessionId:N}.xml`，内容按会话聚合全部消息，并在可用时额外保存机器侧 `AgentSessionState`，方便后续恢复机器记忆与查阅历史。
5. 编辑器右键发送、翻译、本地转换结果展示等派生操作会先新建会话，再把文本按正常用户/助手消息写入该会话历史。

## 当前接线位置

在 `SimpleWrite` 里，右侧栏初始化时会把应用数据目录下的 `CopilotChatLogs` 与 `CopilotChatHistory` 一起注入给 `CopilotViewModel`：

- `SimpleWrite/Foundation/AppPath.cs`
- `SimpleWrite/Views/Components/RightSlideBar.axaml.cs`

也就是说，项目层决定“日志写到哪里”，而 `AvaloniaAgentLib` 只负责“怎么写”。

## 文件组织方式

默认文件实现使用两套文件：

- 文本日志文件夹：`AppPathManager.CopilotChatLogDirectory`
- 文本日志文件名：`{创建首条消息时间:yyyyMMdd_HHmmss}_{CurrentSessionId:N}.log`
- 聊天历史文件夹：`AppPathManager.CopilotChatHistoryDirectory`
- 聊天历史文件名：`{创建首条消息时间:yyyyMMdd_HHmmss}_{CurrentSessionId:N}.xml`

单个文件内容是可读文本，按时间顺序追加，例如：

- `SessionId: ...`
- `[2025-03-27 21:15:32.123 +08:00]`
- `我:` / `Copilot:`
- 实际消息正文；若存在思考链，则会先写“思考”部分，再写分隔线和正文
- 若存在用量统计，则继续写入当前可用的总计、输入、输出、音频、思考、缓存等项目

这样排查单次会话时，可以直接打开文本文件查看原始往返消息；如果需要按结构恢复整段会话，则读取 XML 文件即可。

当前会话模型也分成了两层：

- `CopilotChatSession`：继续承载面向人的标题、消息列表和 UI 展示状态；
- `AgentSession`：承载面向模型的对话上下文与记忆；当前日志链路至少会把可稳定恢复的机器侧会话状态写入 `AgentSessionState`。

## 记录时机

当前实现只在真正进入聊天链路时记录：

- 用户消息加入 `ChatMessages` 后立即记录。
- 助手流式回复结束后，按最终汇总内容记录一次，并同步刷新当前会话对应的机器侧会话状态。
- 取消或异常时，会把最终显示给用户的助手消息一并写入日志。

预设欢迎语和“请先设置模型连接”这类引导文案，不属于正式会话输入输出，不在 `CopilotViewModel` 的默认日志路径里自动落盘。

需要注意的是，项目里有两类消息来源：

- 侧栏输入框里的手动提问：继续追加到当前选中的会话。
- 编辑器或命令模式触发的派生操作：新建会话后写入首条用户消息与后续助手回复。

因此现在排查“为什么某条翻译/转换消息没有出现在当前聊天里”时，需要先确认它是不是被设计成进入了独立的新会话。

## 修改建议

如果后续还要扩展这块，优先保持下面两个边界：

1. 不要让 `AvaloniaAgentLib` 直接依赖 `SimpleWrite` 的路径类型。
2. 不要在流式输出每个分片都落盘，否则很容易把单次回复拆成大量碎片日志。
3. 如果要调整 XML 结构，优先保持元素名和属性名稳定，避免后续反序列化历史时需要兼容太多版本。
4. 调整 AI 用量统计前，先核对 `Microsoft.Agents.AI.OpenAI` 及其传递依赖 `Microsoft.Extensions.AI*` 的实际解析版本，再决定使用哪些 `UsageDetails` 字段。

## 适用场景

- 需要替换聊天日志存储方式时。
- 需要排查某个 `CurrentSessionId` 对应的完整会话时。
- 需要把日志目录改到应用专属数据路径时。
- 需要增加可反序列化的 Copilot 聊天历史存档时。
