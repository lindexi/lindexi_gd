# Copilot 派生命令的新会话行为

## 问题答案

当前项目里，侧栏输入框发送与编辑器/命令模式触发的 Copilot 操作，已经分成了两种会话策略：

1. 侧栏输入框发送：继续使用当前选中的聊天会话。
2. 编辑器右键“发送内容到 Copilot 聊天”、翻译、润色、本地转换结果展示、自定义能力等派生命令：先新建会话，再把文本作为用户消息发送出去。

这样做的目标，是避免临时任务污染用户正在进行中的聊天上下文，同时又保留完整的历史记录。

## 当前链路

### 1. 普通输入框仍然走当前会话

`CopilotSlideBar` 的发送按钮和回车发送，仍然调用 `CopilotViewModel.SendMessageAsync(inputText, withHistory: true, ...)`。

这表示：

- 用户正在看的会话，就是要继续追问的会话。
- 当前会话里的非预设消息会继续作为历史上下文参与后续模型请求。

对应文件：

- `AvaloniaAgentLib/View/CopilotSlideBar.axaml.cs`

### 2. 派生命令统一先新建会话

`CopilotViewModel` 新增了两类能力：

- `SendMessageInNewSessionAsync`
- `AddConversationAsync(..., createNewSession: true, ...)`

前者用于“需要真的发给模型”的情况，后者用于“直接把一问一答写进聊天历史”的情况。

对应文件：

- `AvaloniaAgentLib/ViewModel/CopilotViewModel.cs`

### 3. 右侧栏接线层决定入口策略

`RightSlideBar.axaml.cs` 里现在约定：

- “发送内容到 Copilot 聊天” 使用 `SendMessageInNewSessionAsync`
- “翻译为计算机英文” 使用 `SendMessageInNewSessionAsync`
- XML 配置加载出的 Copilot 能力使用 `SendMessageInNewSessionAsync`
- 本地转换类命令通过 `SidebarConversationPresenter` 调用 `AddConversationAsync(..., createNewSession: true)`

这意味着接线层负责声明“这个入口到底是继续当前会话，还是单独开新会话”。

对应文件：

- `SimpleWrite/Views/Components/RightSlideBar.axaml.cs`
- `SimpleWrite/Business/PluginCommandPatterns/*.cs`

### 4. 润色命令也走独立会话

`PolishSelectedTextCommandPattern` 在请求模型润色选区时，也会先新建会话。

如果后续出现以下结果：

- 模型没有通过工具回传结果
- 原始选区已变化
- 选区定位失败

这些失败说明同样会被写到一个新的聊天会话中，而不是作为当前会话里的预设提示。

对应文件：

- `SimpleWrite/Business/CopilotCommandPatterns/PolishSelectedTextCommandPattern.cs`

## 为什么不用预设消息

之前本地展示类命令通过 `AddLocalConversationAsync` 把用户问题和结果都标记成 `IsPresetInfo = true`。

这样虽然能显示在右侧栏里，但会带来两个问题：

1. 这些内容不会参与标题生成。
2. 语义上更像“系统展示”，不是真正的聊天历史输入输出。

现在改成普通用户/助手消息后：

- 会话标题可以从首条用户消息自动生成。
- 日志里能看到完整且语义正确的一问一答。
- 用户后续若切回该会话，看到的是可追溯的任务记录。

## 关键约束

- 侧栏输入框发送仍然保留当前会话语义，不自动新建会话。
- 派生命令默认不复用当前聊天上下文，避免干扰连续对话。
- 真正属于引导性质的欢迎语、配置提示，仍然使用预设消息。
- 新会话创建后，欢迎语会先加入该会话，再追加真实用户消息与助手回复。

## 适用场景

- 需要调整某个 Copilot 入口是否应该污染当前上下文时。
- 需要排查“为什么这条命令跑到了一个新会话里”时。
- 需要继续扩展右键菜单、命令模式、侧边栏对话展示能力时。
