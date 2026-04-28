# 文本编辑器选中内容发送到 Copilot 聊天的组织方式

## 问题答案

项目里“把编辑器选中的文本发送到 Copilot 聊天”不是由某一个类单独完成，而是拆成了 4 层协作：

1. `SimpleWriteTextEditor` 只负责暴露右键菜单入口，并在点击菜单时抛出“请求发送选区”的事件。
2. `EditorViewModel` 在创建编辑器实例时订阅该事件，把编辑器事件转成对 `SimpleWriteMainViewModel` 的调用。
3. `SimpleWriteMainViewModel` 只做桥接，统一提供 `SendMessageToCopilotAsync` 方法，不直接依赖右侧边栏控件。
4. `RightSlideBar` 在界面加载链路中创建 `ICopilotHandler`，把最终消息交给共享的 `CopilotViewModel`，真正进入聊天记录与流式回复逻辑。

换句话说：

`选区右键菜单` → `编辑器事件` → `EditorViewModel` → `SimpleWriteMainViewModel` → `RightSlideBar/CopilotHandler` → `CopilotViewModel.SendMessageAsync`

## 从入口开始看代码

### 1. 右侧 Copilot 面板始终挂在主界面上

`SimpleWriteMainView.axaml` 直接把 `RightSlideBar` 放在主布局右侧，因此编辑器和 Copilot 侧栏天然处于同一个页面树中：

- `SimpleWrite/Views/SimpleWriteMainView.axaml`
- `SimpleWrite/Views/Components/RightSlideBar.axaml`

这一步的意义是：Copilot 不是临时弹出来的对话框，而是主界面固定的一部分，所以编辑器只需要把消息转发出去，不需要自己创建聊天 UI。

### 2. `RightSlideBar` 负责把侧栏 UI 接到 Copilot 逻辑

`RightSlideBar.axaml` 里面只有一个 `CopilotSlideBar` 控件，真正的接线逻辑在 `RightSlideBar.axaml.cs`：

- 从 `SimpleWriteMainViewModel.ConfigurationManager` 读取模型配置。
- 取得 `CopilotSlideBar.ViewModel`，也就是右侧聊天面板实际使用的 `CopilotViewModel`。
- 配置有效时，创建 `CopilotHandler` 并回填给 `mainViewModel.CopilotHandler`。

这里形成了一个很关键的约定：**主 ViewModel 不拥有 Copilot 控件，但拥有一个可选的 `ICopilotHandler` 桥接口**。

对应文件：

- `SimpleWrite/Views/Components/RightSlideBar.axaml.cs`
- `SimpleWrite/Business/ICopilotHandler.cs`
- `AvaloniaAgentLib/View/CopilotSlideBar.axaml.cs`
- `AvaloniaAgentLib/ViewModel/CopilotViewModel.cs`

## 编辑器侧如何把选区变成消息

### 3. `EditorViewModel` 创建编辑器时注入发送链路

`EditorViewModel.CreateTextEditor` 会创建 `SimpleWriteTextEditor`，同时把它接入当前文档生命周期。

这里有一个专门的事件订阅：

- 订阅 `textEditor.RequestSendSelectionToCopilot`
- 收到选中文本后调用 `MainViewModel.SendMessageToCopilotAsync(selectedText)`

因此 `EditorViewModel` 承担的是“把编辑器内事件接到主协调 ViewModel”的责任，而不是直接操作聊天控件。

对应文件：

- `SimpleWrite/ViewModels/EditorViewModel.cs`

### 4. `SimpleWriteTextEditor` 只关心菜单和选区文本提取

真正把“发送选中内容到 Copilot 聊天”菜单加到右键菜单里的代码，在 `SimpleWriteTextEditor`：

- 构造函数里预先创建 `_sendSelectionToCopilotMenuItem`
- `OnRaisePrepareContextMenuEvent` 里根据 `CurrentSelection.IsEmpty` 动态添加或移除菜单项
- 菜单点击后读取 `CurrentSelection`
- 用 `GetText(in selection)` 取出选中文本
- 通过 `RequestSendSelectionToCopilot?.Invoke(this, selectedText)` 抛出事件

这部分设计说明：**编辑器本身并不知道 Copilot 是什么，它只知道“我这里有一段被选中的文本，需要交给外部处理”**。

对应文件：

- `SimpleWrite/Business/TextEditors/SimpleWriteTextEditor.cs`
- `LightTextEditorPlus/Build/Shared/API/Events/PrepareContextMenuEventArgs.cs`
- `LightTextEditorPlus/Build/Shared/API/Handlers/TextEditorHandler.Shared.cs`

其中 `LightTextEditorPlus` 的 `TextEditorHandler` 会在右键菜单准备阶段调用 `TextEditor.OnRaisePrepareContextMenuEvent(...)`，因此业务层只需要重写该虚方法即可插入菜单逻辑。

## 主链路如何收口

### 5. `SimpleWriteMainViewModel` 是编辑区与右侧边栏之间的桥

`SimpleWriteMainViewModel` 并不直接 new 一个 `CopilotViewModel`，而是暴露：

- `ICopilotHandler? CopilotHandler`
- `Task SendMessageToCopilotAsync(string text)`

`EditorViewModel` 只认 `MainViewModel.SendMessageToCopilotAsync(...)`，至于后面有没有真正接上 Copilot，由 `RightSlideBar` 决定。

这样做有两个好处：

1. 编辑器模块不依赖右侧边栏控件类型。
2. 当 Copilot 尚未完成配置时，发送调用会退化为 `Task.CompletedTask`，不会把编辑器链路打断。

对应文件：

- `SimpleWrite/ViewModels/SimpleWriteMainViewModel.cs`

### 6. 真正进入聊天记录的是 `CopilotViewModel`

`CopilotHandler.SendMessageToCopilotAsync` 最终调用 `CopilotViewModel.SendMessageAsync(text)`。

`CopilotViewModel` 会负责：

- 把用户消息加入 `ChatMessages`
- 创建聊天客户端
- 读取已有历史消息，组织成发送上下文
- 流式接收模型输出
- 将回复持续追加到同一个助手消息对象中

因此，编辑器右键发送与用户手动在侧栏输入，最终都会进入 `CopilotViewModel` 管理的聊天历史；只是两者的会话策略不同：

- 用户在侧栏输入框里发送时，继续使用当前选中的会话。
- 编辑器右键“发送内容到 Copilot 聊天”、翻译、润色等派生命令时，会先新建一个会话，再把文本作为用户消息发出去。

这也是当前实现里“编辑器派生操作不会污染正在进行中的聊天上下文，但仍然会进入历史记录”的根本原因。

## 职责拆分总结

| 层/类型 | 职责 | 不负责什么 |
|---|---|---|
| `SimpleWriteTextEditor` | 根据选区状态显示菜单，提取选中文本，抛出发送请求 | 不直接访问 `CopilotViewModel` |
| `EditorViewModel` | 创建编辑器并订阅发送事件 | 不直接操作侧栏控件 |
| `SimpleWriteMainViewModel` | 提供跨区域桥接方法 | 不管理聊天 UI 细节 |
| `RightSlideBar` / `CopilotHandler` | 将主 ViewModel 与共享聊天能力接通，并约定哪些入口要新建会话 | 不处理编辑器选区细节 |
| `CopilotViewModel` | 管理会话状态、消息集合和模型调用 | 不关心消息来自哪个具体 UI 控件 |

## 开发者排查顺序

如果后续有人想继续改这部分功能，建议按下面顺序阅读：

1. `SimpleWrite/Business/TextEditors/SimpleWriteTextEditor.cs`
2. `SimpleWrite/ViewModels/EditorViewModel.cs`
3. `SimpleWrite/ViewModels/SimpleWriteMainViewModel.cs`
4. `SimpleWrite/Views/Components/RightSlideBar.axaml.cs`
5. `AvaloniaAgentLib/ViewModel/CopilotViewModel.cs`

## 关键约束

- 菜单项只有在存在非空选区时才显示。
- 编辑器通过事件向外通知，避免直接耦合 Copilot 实现。
- 侧栏是否真正可用由 `RightSlideBar` 的配置校验结果决定。
- 聊天状态由 `CopilotViewModel` 统一管理，但编辑器派生操作会新建会话，避免把临时任务塞进当前正在聊天的上下文。
- 新建会话后的首条用户消息会参与标题生成，也会写入历史日志，不再作为仅展示用的预设消息。

## 适用场景

- 需要给编辑器增加“选区发给 AI”的入口时。
- 需要理解项目中编辑区与 AI 侧栏的边界时。
- 需要排查“菜单出现了但消息没发出去”这类问题时。
