# 命令模式扩展与 Copilot 本地转换约定

## 场景

`SimpleWrite` 的编辑器右键菜单支持按文本内容动态提供命令。当前这一层除了把内容发送给 Copilot，也承担两类“本地即可完成”的能力：

1. 识别命令文本并提供“在终端运行”；
2. 识别 URL、Base64、二进制文本并提供直接处理入口。

## 命令模式入口

- 命令注册入口：`SimpleWrite/Business/PluginCommandPatterns/PluginCommandPatternProvider.cs`
- 命令匹配与菜单生成：`SimpleWrite/Business/TextEditors/SimpleWriteTextEditor.cs`
- Copilot 侧边栏扩展入口：`SimpleWrite/Views/Components/RightSlideBar.axaml.cs`

`SimpleWriteTextEditor.OnRaisePrepareContextMenuEvent(...)` 会在右键菜单弹出前拿到当前选区文本，或者在没有选区时拿到命中的单段文本，然后依次调用 `CommandPatternManager.CommandPatternList` 中每个模式的 `IsMatchAsync(...)`。

因此新增能力时优先考虑两点：

1. 是否允许在“无选区，仅命中当前段落”时触发；
2. `IsMatchAsync(...)` 是否足够严格，避免普通正文误匹配。

## cmd 启动方式

“在终端运行”不会把命令拼进 `cmd /c` 或 `cmd /k` 参数，而是：

1. 直接启动 `cmd.exe`；
2. 打开标准输入重定向；
3. 把命令文本写入标准输入；
4. 不主动关闭标准输入，让终端窗口保持打开，交给用户自行关闭。

实现原因是：这样可以直接把多行命令原样写入终端，而不用额外处理复杂的命令行转义。

## URL 打开约定

`OpenUrlCommandPattern` 会优先识别：

- 纯 `http/https` 链接；
- Markdown 链接格式 `[title](https://example.com)`；
- 尖括号包裹的链接 `<https://example.com>`。

命中后使用系统默认方式打开，不走终端。

## Copilot 本地转换约定

以下转换不需要调用模型：

- 文本转 Base64；
- Base64 转文本；
- 文本转二进制（UTF-8）；
- 二进制转文本（UTF-8）。

这些能力放在 `RightSlideBar.axaml.cs` 的 `CopilotPatternProvider` 里注册，但执行时不调用 `CopilotViewModel.SendMessageAsync(...)`，而是改为调用 `CopilotViewModel.AddLocalConversationAsync(...)`。

这样做有两个结果：

1. 转换结果仍然会出现在当前聊天会话里，方便用户在侧边栏直接查看与复制；
2. 这些消息会被标记为本地预设信息，不参与后续模型历史上下文。

## 本地会话写入约定

`AvaloniaAgentLib/ViewModel/CopilotViewModel.cs` 新增了 `AddLocalConversationAsync(...)`，职责是：

- 直接把用户消息和助手结果追加到 `SelectedSession`；
- 同时写入 `ChatLogger`；
- 默认标记为不参与模型历史。

如果后续还有“本地即可完成，但希望显示到侧边栏”的能力，优先复用这一方法，而不是再走 `SendMessageAsync(...)`。

## 适用场景

- 扩展右键菜单内容匹配规则；
- 给选中文本增加“本地工具型”处理能力；
- 排查“为什么会出现打开链接/在终端运行/转换文本”这类菜单项；
- 调整 Copilot 侧边栏里哪些消息需要进入模型上下文。
