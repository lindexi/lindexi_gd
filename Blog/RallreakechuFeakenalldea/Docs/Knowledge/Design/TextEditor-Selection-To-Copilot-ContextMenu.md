# 文本编辑器选中内容发送到 Copilot 聊天的组织方式

## 问题答案

当前项目里，“把编辑器选中的文本发送到 Copilot 聊天”已经不再走单独的 `ICopilotHandler` 桥接接口，而是统一并入命令模式体系。

现在的主链路是：

`SimpleWriteTextEditor` 右键菜单 → `CommandPatternManager` → `CopilotPatternProvider` 注册的命令 → `CopilotViewModel.SendMessageInNewSessionAsync(...)`

这意味着“发送到 Copilot”“翻译”“润色选中文本”以及 XML 能力文件加载出来的命令，都共享同一套文本命中、菜单生成和新会话策略。

## 当前实现分层

### 1. `SimpleWriteTextEditor` 只负责准备右键菜单

`SimpleWriteTextEditor.OnRaisePrepareContextMenuEvent(...)` 会在右键菜单弹出前：

1. 优先读取当前选区文本；
2. 无选区时退化为命中的当前段落文本；
3. 遍历 `CommandPatternManager.CommandPatternList`；
4. 对命中的命令动态生成 `MenuItem`，点击后执行对应 `DoAsync(...)`。

因此编辑器本身不直接感知 Copilot，也不维护“发送选区到 Copilot”的专用事件。它只负责把当前文本上下文交给命令模式判断。

对应文件：

- `SimpleWrite/Business/TextEditors/SimpleWriteTextEditor.cs`

### 2. `EditorViewModel` 负责给编辑器注入命令体系

`EditorViewModel.CreateTextEditor(...)` 创建 `SimpleWriteTextEditor` 时，会把 `MainViewModel.CommandPatternManager` 注入进去。

这一步的职责是：

- 保证每个编辑器实例都走同一套命令注册结果；
- 让编辑器只依赖命令管理器，不反向依赖具体 Copilot 组件；
- 继续复用 `ShortcutManager`、代码高亮、自动保存等既有编辑器初始化链路。

对应文件：

- `SimpleWrite/ViewModels/EditorViewModel.cs`

### 3. `SimpleWriteMainViewModel` 负责聚合命令提供器

`SimpleWriteMainViewModel` 构造时会先创建 `CommandPatternManager`，再注入两类提供器：

- `PluginCommandPatternProvider`：注册本地工具型命令，例如打开链接、终端运行、Base64/二进制转换；
- `CopilotPatternProvider`：在右侧栏完成模型配置后注册与模型相关的命令。

这表示主 ViewModel 负责命令聚合，但不直接负责右键菜单如何生成，也不自己持有某个“发送选区给 Copilot”的专用 API。

对应文件：

- `SimpleWrite/ViewModels/SimpleWriteMainViewModel.cs`
- `SimpleWrite/Business/PluginCommandPatterns/PluginCommandPatternProvider.cs`

### 4. `RightSlideBar` 负责把共享 Copilot 能力接进来

`RightSlideBar.axaml.cs` 在加载配置成功后：

- 取得共享的 `CopilotViewModel`；
- 创建 `CopilotPatternProvider`；
- 调用 `AddCopilotPatterns(...)` 把模型相关命令注册进 `CommandPatternManager`；
- 同时注入 `SidebarConversationPresenter`，给本地工具型命令提供“在侧边栏显示一组对话”的能力。

这里真正起作用的边界不是旧的 `ICopilotHandler`，而是：

- 模型型命令直接拿 `CopilotViewModel`；
- 本地工具型命令只拿 `ISidebarConversationPresenter`；
- 编辑器侧始终只依赖命令模式。

对应文件：

- `SimpleWrite/Views/Components/RightSlideBar.axaml.cs`
- `SimpleWrite/Business/AgentConnectors/CopilotPatternProvider.cs`

### 5. `CopilotPatternProvider` 决定“选中文本如何进入聊天”

当前与 Copilot 相关的入口主要包括：

- “发送内容到 Copilot 聊天”；
- “翻译为计算机英文”；
- “Json转C#类”；
- “润色选中文本”；
- 从 XML 能力文件加载的自定义命令。

其中：

- 普通 prompt 型命令统一调用 `CopilotViewModel.SendMessageInNewSessionAsync(...)`；
- “润色选中文本”会调用 `CopilotViewModel.SendMessageAsync(..., createNewSession: true, ...)`，并要求模型通过工具回传最终文本；
- 本地转换结果则不会发给模型，而是通过 `SidebarConversationPresenter` 直接写入侧边栏会话。

这也是当前“选区发给 Copilot”不再是一条专用链路，而是更大命令体系中的一个场景。

## 会话策略

当前项目有一个明确约定：

- 用户在右侧输入框里主动提问，继续使用当前会话；
- 编辑器右键触发的 Copilot 派生命令，默认新建会话；
- 本地转换类结果也会按独立会话展示，避免污染用户正在进行中的上下文。

因此编辑器选区进入 Copilot 的目标，不只是“发出去”，更重要的是保持派生任务与用户手动聊天分离。

## 职责拆分总结

| 层/类型 | 职责 | 不负责什么 |
|---|---|---|
| `SimpleWriteTextEditor` | 收集当前选区/命中文本并动态生成右键菜单 | 不直接调用 `CopilotViewModel` |
| `EditorViewModel` | 创建编辑器并注入共享的 `CommandPatternManager` | 不直接决定菜单项内容 |
| `SimpleWriteMainViewModel` | 聚合本地命令与模型命令注册入口 | 不处理右键菜单 UI 细节 |
| `RightSlideBar` | 在配置完成后接入 `CopilotViewModel`、注册 Copilot 命令、桥接侧边栏会话展示 | 不解析编辑器选区 |
| `CopilotViewModel` | 管理会话、消息、工具与模型调用 | 不关心消息来自哪个具体控件 |

## 开发者排查顺序

如果后续需要继续调整这条链路，建议按下面顺序阅读：

1. `SimpleWrite/Business/TextEditors/SimpleWriteTextEditor.cs`
2. `SimpleWrite/ViewModels/EditorViewModel.cs`
3. `SimpleWrite/ViewModels/SimpleWriteMainViewModel.cs`
4. `SimpleWrite/Business/AgentConnectors/CopilotPatternProvider.cs`
5. `SimpleWrite/Views/Components/RightSlideBar.axaml.cs`
6. `AvaloniaAgentLib/ViewModel/CopilotViewModel.cs`

## 关键约束

- 菜单项是否出现，取决于命令模式对当前文本的匹配结果。
- 模型相关命令与本地工具型命令应继续分开注册，避免职责混杂。
- 编辑器派生命令默认新建会话，避免污染正在进行中的聊天上下文。
- 如果需要“只在真实选区下出现”的命令，应通过 `SupportSingleLine = false` 控制，而不是额外造一条专用菜单链路。

## 适用场景

- 需要给编辑器增加新的 Copilot 派生命令时。
- 需要理解命令模式如何把选区文本接入右侧聊天时。
- 需要排查“菜单出现了，但没有进入新会话”这类问题时。
