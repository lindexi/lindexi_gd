# 编辑器标签页 AI 工具

## 场景

让 Copilot AI 能够感知编辑器中已打开的标签页，支持列出所有标签（含行数）以及按需读取标签页内容。这使 AI 能够跨标签页理解用户当前的工作上下文。

## 架构决策

- **不改基础库**：不在 AgentLib 的 `CopilotChatManager` 或 `CopilotToolManager` 层做任何修改。
- **注册方式**：在 `CopilotViewModel` 上添加 `AdditionalDefaultTools` 属性。`RightSlideBar.axaml.cs` 在初始化时创建 `EditorTabToolProvider`，将工具注册到该属性。
- **工具合并**：`CopilotViewModel` 用 `new` 隐藏了基类的 `SendMessageAsync` 和 `SendMessageInNewSessionAsync`，在调用基类之前将 `AdditionalDefaultTools` 合并到工具的 `tools` 参数中。
- **标签 ID 策略**：Title + 重名后缀（如 `"文档1"`、`"文档1 #2"`），对 AI 友好且可读。
- **UI 线程访问**：通过 `Dispatcher.UIThread.InvokeAsync` 切回 UI 线程读取 `TextEditor.Text`。
- **只读工具**：这两个工具均为只读，不需要快照保护。

## 当前实现

### 1. `CopilotViewModel.AdditionalDefaultTools`

`CopilotViewModel` 新增了 `AdditionalDefaultTools` 属性（`List<AITool>`），调用方可在创建 ViewModel 后向此集合添加工具。

`SendMessageAsync` 和 `SendMessageInNewSessionAsync` 用 `new` 关键字隐藏基类方法，在调用基类前合并额外工具：

```
用户工具列表(可为空) + AdditionalDefaultTools → 合并后传入 base.SendMessageAsync
```

### 2. `EditorTabToolProvider`

位于 `SimpleWrite/Business/AgentConnectors/EditorTabToolProvider.cs`，依赖 `EditorViewModel`。

提供两个 AI 工具：

1. **`ListOpenTabs`** — 列出所有打开的标签页，附带行数：
   - 参数：`maxResults`（可选，默认 100）
   - 返回：带 `[当前]` 标记的标签列表

2. **`ReadTabContent`** — 读取标签内容，支持行范围截取：
   - 参数：`tabId`（可选，不传则读当前标签）、`startLine`（1-based，默认 1）、`endLine`（默认起始行+400）、`includeLineNumbers`（默认 true）
   - 限制：单次最多 400 行

### 3. `RightSlideBar` 注册

在 `RightSlideBar.OnDataContextChanged` 中：

```csharp
var editorTabToolProvider = new EditorTabToolProvider(mainViewModel.EditorViewModel);
copilotViewModel.AdditionalDefaultTools.AddRange(editorTabToolProvider.CreateTools());
```

## 相关文件

| 文件 | 说明 |
|---|---|
| `AvaloniaAgentLib/ViewModel/CopilotViewModel.cs` | 添加 `AdditionalDefaultTools` 属性，隐藏 `SendMessageAsync` 合并工具 |
| `SimpleWrite/Business/AgentConnectors/EditorTabToolProvider.cs` | 标签页工具提供器 |
| `SimpleWrite/Views/Components/RightSlideBar.axaml.cs` | 注册 EditorTabToolProvider |