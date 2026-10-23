# CodingChatRoom LSP 服务停止与按需重启

## 交互约束

“结束 LSP 服务”按钮仅在应用空闲时可用：

- Agent 正在运行时禁用；
- 对话正在压缩时禁用；
- 运行和压缩均结束后恢复可用。

通过界面约束保证停止操作不会与符号工具调用并发，不在 LSP 层增加锁、等待或状态机。

## 停止行为

停止调用沿真实对象所有权关系执行：

```text
ChatViewModel
  -> CodingChatApplication
  -> CodingAgent
  -> CodingWorkspaceCache
  -> RoslynAgentTools
  -> RoslynLspClient.DisposeAsync
```

`CodingChatApplication` 在生产组合根中直接持有 `CodingAgent`，不通过 `ICodingChatRunner` 转发停止操作。

停止时，`RoslynAgentTools`：

1. 保存当前 `RoslynLspClient`；
2. 将客户端字段设置为 `null`；
3. 释放保存的客户端；
4. 没有客户端时返回正常空操作结果。

## 按需重启

以下符号工具执行前获取当前 LSP 客户端：

- `code_search`；
- `find_symbol`；
- `find_all_references`。

客户端为空时，使用缓存的工作区路径和 Language Server 命令重新启动。启动失败返回原有的 `roslyn_language_server_unavailable` 错误；后续调用仍可再次尝试。

项目目录工具不依赖 LSP，不会触发启动：

- `get_projects_in_solution`；
- `get_files_in_project`。

## 用户反馈

- 成功停止：`LSP 服务已结束，将在下次调用符号工具时重新启动`；
- 没有活动服务：`当前没有正在运行的 LSP 服务`；
- 停止失败：`结束 LSP 服务失败：<错误信息>`。

反馈显示在输入区状态文本和当前会话的系统消息中。
