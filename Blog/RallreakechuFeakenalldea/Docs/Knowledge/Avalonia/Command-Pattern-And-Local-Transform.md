# 命令模式扩展与 Copilot 本地转换约定

## 场景

`SimpleWrite` 的编辑器右键菜单支持按文本内容动态提供命令。当前这一层除了把内容发送给 Copilot，也承担两类“本地即可完成”的能力：

1. 识别命令文本并提供“在终端运行”；
2. 识别 URL、Base64、二进制文本并提供直接处理入口。

## 命令模式入口

- 命令注册入口：`SimpleWrite/Business/PluginCommandPatterns/PluginCommandPatternProvider.cs`
- 命令匹配与菜单生成：`SimpleWrite/Business/TextEditors/SimpleWriteTextEditor.cs`
- Copilot 侧边栏能力注入：`SimpleWrite/Views/Components/RightSlideBar.axaml.cs`
- 主协调桥接：`SimpleWrite/ViewModels/SimpleWriteMainViewModel.cs`

`SimpleWriteTextEditor.OnRaisePrepareContextMenuEvent(...)` 会在右键菜单弹出前拿到当前选区文本，或者在没有选区时拿到命中的单段文本，然后依次调用 `CommandPatternManager.CommandPatternList` 中每个模式的 `IsMatchAsync(...)`。

因此新增能力时优先考虑两点：

1. 是否允许在“无选区，仅命中当前段落”时触发；
2. `IsMatchAsync(...)` 是否足够严格，避免普通正文误匹配。

目前的组织方式是：

- `RightSlideBar` 只负责检查 Copilot 配置，并把“在侧边栏显示一组用户/助手对话”的能力封装成 `ISidebarConversationPresenter` 回填给 `SimpleWriteMainViewModel`；
- `PluginCommandPatternProvider` 只注册与模型无关的命令，例如 URL、本地转换、终端运行；
- 与模型相关的命令仍留在 `RightSlideBar` 侧注册，避免 `PluginCommandPatternProvider` 反向承载模型提示词逻辑。

## cmd 启动方式

“在终端运行”不会把命令拼进 `cmd /c` 或 `cmd /k` 参数，而是：

1. 直接启动 `cmd.exe`；
2. 打开标准输入重定向；
3. 把命令文本写入标准输入；
4. 不主动关闭标准输入，让终端窗口保持打开，交给用户自行关闭。

实现原因是：这样可以直接把多行命令原样写入终端，而不用额外处理复杂的命令行转义。

当前实现还有两个额外约定：

1. Windows 下通过单个 `cmd` 进程复用标准输入，把连续命令持续写入同一终端窗口。
2. Linux 与 macOS 下分别走各自的终端程序启动方式，不再直接忽略非 Windows 平台。

## “在终端运行”的匹配约定

`RunCommandLineCommandPattern.IsMatchAsync(...)` 现在按下面顺序收敛：

1. 先确认当前平台存在对应的终端运行器，而不是写死只有 Windows 可用。
2. 对文本做轻量归一化：去掉首尾空白、单反引号包裹、Markdown fenced code block、常见命令提示符前缀。
3. 取首个非空命令行的第一个块，要求它看起来像合法的路径或命令名，不能以 `>`、`!`、`#`、`|` 等明显无效符号开头。
4. 如果首个块本身是路径，则直接检查该路径是否存在；Windows 下无扩展名时会结合 `PATHEXT` 补全查找，Unix 平台会额外检查执行权限。
5. 如果首个块不是路径，则按当前平台的 `PATH` 规则查找命令；Windows 下还会补充 `cmd` 内建命令，Linux/macOS 下也会补充常见 shell 内建命令。

这样做的目的不是“穷举所有命令”，而是把误判率压低：普通正文、符号片段、环境变量赋值残片不应出现“在终端运行”。

## 经验修正

本次实现里有两个需要明确记住的经验：

1. `PluginCommandPatternProvider` 不应通过多个委托拼装能力，而应直接感知 `SimpleWriteMainViewModel`，再从主链路拿到侧边栏展示能力。
2. 与模型相关的命令不要搬进 `PluginCommandPatternProvider`；该提供器只保留本地工具型命令。
3. `RunCommandLineCommandPattern` 的匹配规则不能写死成单一平台特征，更不能把 Windows 经验直接套给整个仓库；必须先做平台感知，再决定后续判定方式。
4. `OpenUrlCommandPattern` 与 `RunCommandLineCommandPattern` 的语义不同，不能为了省代码而共享同一套“文本帮助类”；URL 和命令的匹配边界应各自独立维护。

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

这些能力现在放在 `PluginCommandPatternProvider` 里统一注册，并且每一种转换都拆成独立的命令类型。

执行时不直接调用 `CopilotViewModel.AddLocalConversationAsync(...)`，而是先经过：

`命令模式` → `PluginCommandPatternProvider` → `SimpleWriteMainViewModel.SidebarConversationPresenter` → `CopilotViewModel.AddLocalConversationAsync(...)`

这样做有两个结果：

1. 转换结果仍然会出现在当前聊天会话里，方便用户在侧边栏直接查看与复制；
2. 这些消息会被标记为本地预设信息，不参与后续模型历史上下文。
3. `PluginCommandPatternProvider` 不需要直接依赖 `RightSlideBar` 或 `CopilotViewModel`，只依赖主 ViewModel 暴露出来的侧边栏展示桥接能力。

## 本地会话写入约定

`SimpleWrite` 侧新增了 `ISidebarConversationPresenter`，由 `RightSlideBar` 在配置有效时创建实现并注入到 `SimpleWriteMainViewModel`。这一层的职责是：

- 向主界面暴露“把一组用户/助手内容显示到侧边栏”的能力；
- 让本地工具型命令只依赖主 ViewModel，不直接依赖侧边栏控件。

`AvaloniaAgentLib/ViewModel/CopilotViewModel.cs` 里的 `AddLocalConversationAsync(...)` 仍然负责：

- 直接把用户消息和助手结果追加到 `SelectedSession`；
- 同时写入 `ChatLogger`；
- 默认标记为不参与模型历史。

如果后续还有“本地即可完成，但希望显示到侧边栏”的能力，优先复用这一方法，而不是再走 `SendMessageAsync(...)`。

## 适用场景

- 扩展右键菜单内容匹配规则；
- 给选中文本增加“本地工具型”处理能力；
- 排查“为什么会出现打开链接/在终端运行/转换文本”这类菜单项；
- 调整 Copilot 侧边栏里哪些消息需要进入模型上下文。
