# 文本编辑器选中内容发送到 Copilot 右键菜单经验

## 场景

在 `SimpleWriteTextEditor` 中，当用户选中了文本后，希望通过右键菜单直接把当前选中内容发送到右侧 `Copilot Chat` 面板继续对话。

## 实现要点

1. 由 `SimpleWriteMainViewModel` 持有唯一的 `CopilotViewModel`，避免编辑器和右侧边栏各自维护聊天状态。
2. `RightSlideBar` 在 `DataContext` 变更时，将 `CopilotSlideBar.DataContext` 绑定到 `MainViewModel.CopilotViewModel`。
3. `EditorViewModel` 创建 `SimpleWriteTextEditor` 时，注入一个 `SendSelectionToCopilotAsync` 回调。
4. `SimpleWriteTextEditor.OnRaisePrepareContextMenuEvent` 根据 `CurrentSelection.IsEmpty` 动态添加或移除“发送选中内容到 Copilot 聊天”菜单项。
5. 菜单点击后读取 `CurrentSelection` 对应文本，并调用注入回调转发到 Copilot。

## 关键约束

- 右键菜单项只在存在选区时显示，避免空操作入口。
- 发送逻辑复用共享 `CopilotViewModel`，这样从编辑器发出的消息会直接进入现有聊天记录。
- 若 Copilot 正在聊天，可在上层回调中直接忽略新的发送请求，避免并发对话状态冲突。

## 适用场景

- 需要把编辑器选中文本快速交给 AI 侧栏继续解释、润色或问答。
- 需要让多个入口复用同一个聊天会话状态时。
