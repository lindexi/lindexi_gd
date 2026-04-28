# Copilot 聊天日志注入与落盘方式

## 问题答案

`CopilotViewModel` 的聊天日志现在走可替换的 `ICopilotChatLogger`，默认实现是 `FileCopilotChatLogger`。

默认约定有 4 个：

1. 日志能力由 `CopilotViewModel.ChatLogger` 提供，外部可以在初始化阶段替换实现。
2. `FileCopilotChatLogger` 允许在构造时指定日志文件夹。
3. 每个会话按 `CurrentSessionId` 单独落一个文件，文件里顺序追加“时间 + 说话人 + 内容”。
4. 编辑器右键发送、翻译、本地转换结果展示等派生操作会先新建会话，再把文本按正常用户/助手消息写入该会话历史。

## 当前接线位置

在 `SimpleWrite` 里，右侧栏初始化时会把应用数据目录下的 `CopilotChatLogs` 注入给 `CopilotViewModel`：

- `SimpleWrite/Foundation/AppPath.cs`
- `SimpleWrite/Views/Components/RightSlideBar.axaml.cs`

也就是说，项目层决定“日志写到哪里”，而 `AvaloniaAgentLib` 只负责“怎么写”。

## 文件组织方式

默认文件实现使用：

- 文件夹：`AppPathManager.CopilotChatLogDirectory`
- 文件名：`{CurrentSessionId:N}.log`

单个文件内容是可读文本，按时间顺序追加，例如：

- `SessionId: ...`
- `[2025-03-27 21:15:32.123 +08:00]`
- `我:` / `Copilot:`
- 实际消息正文

这样排查单次会话时，不需要反序列化额外结构，直接打开文本文件即可看见来回消息。

## 记录时机

当前实现只在真正进入聊天链路时记录：

- 用户消息加入 `ChatMessages` 后立即记录。
- 助手流式回复结束后，按最终汇总内容记录一次。
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

## 适用场景

- 需要替换聊天日志存储方式时。
- 需要排查某个 `CurrentSessionId` 对应的完整会话时。
- 需要把日志目录改到应用专属数据路径时。
