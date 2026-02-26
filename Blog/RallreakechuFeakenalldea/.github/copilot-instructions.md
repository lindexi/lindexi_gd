# SimpleWrite — AI 协作指南

> 本文件用于存储 **每个会话都必须遵循** 的核心开发规范。
> 如果本文件内容超过 200 行，请将扩展内容（详细实现示例、完整代码片段、故障排查手册等）移至 `/Docs` 文件夹，并在此保留关键要点和文档链接。
> 开始开发之前，请阅读 `/Docs` 文件夹内的文档内容，按需阅读 `/Docs/Knowledge` 知识经验文档

## 项目概览

**SimpleWrite** 是一款基于 [Avalonia UI](https://avaloniaui.net/) 与 [LightTextEditorPlus](../LightTextEditorPlus) 构建的跨平台轻量文本编辑器，目标框架为 `.NET 9`，启用了 AOT 发布。

### 解决方案结构

```
SimpleWrite/                        # 核心 UI 库（Avalonia UserControl，可复用）
RallreakechuFeakenalldea/           # Avalonia 宿主应用（Shell）
RallreakechuFeakenalldea.Desktop/   # 桌面平台入口
Docs/                               # 项目文档
LightTextEditorPlus/                # 富文本编辑器底层库（外部依赖，只读）
```

---

## 架构分层

项目严格遵循 **MVVM** 模式，各层职责如下：

### 1. View 层（`SimpleWrite/Views/`）

- 只负责 UI 渲染与用户交互事件转发。
- `SimpleWriteMainView` 是顶层 `UserControl`，承载所有子视图，并在构造函数中向附加属性树注册 `TextEditorInfo`。
- 通过 **Avalonia 附加属性** `TextEditorInfo`（可继承）将 `MainEditorView` 的 `CurrentTextEditor` 向下传递给子控件（如 `StatusBar`），避免直接依赖父级控件。

#### 子组件（`SimpleWrite/Views/Components/`）

| 组件 | 职责 |
|---|---|
| `MainEditorView` | 编辑区核心视图，管理 `TextEditor` 实例的生命周期，绑定 `EditorViewModel`，内含 `SimpleWriteTextEditorHandler` |
| `StatusBar` | 状态栏，显示保存状态与光标位置，绑定 `StatusViewModel` |
| `TabBar` | 标签栏，展示多标签（`EditorModelList`） |
| `SimpleWriteSideBar` | 侧边栏 |

### 2. ViewModel 层（`SimpleWrite/ViewModels/`）

- `SimpleWriteMainViewModel`：根协调类（**不继承** `ViewModelBase`），持有 `EditorViewModel` 和 `StatusViewModel`，负责跨 ViewModel 事件桥接（将 `EditorModel.SaveStatusChanged` 同步到 `StatusViewModel.IsSaving`），并对外暴露 `OpenFileAsync`。
- `EditorViewModel`：编辑核心逻辑，管理 `EditorModel` 列表（多标签）、文件打开/保存/另存为流程、`ShortcutManager` 数据、`SaveFilePickerHandler` 引用。
- `StatusViewModel`：状态栏数据，组合保存状态文字与光标位置文字输出为 `StatusText`；光标信息通过 `SetCurrentCaretInfoText()` 方法更新。
- `ViewModelBase`：提供 `INotifyPropertyChanged` 的标准实现，`EditorViewModel` / `StatusViewModel` 继承此基类。

### 3. Model 层（`SimpleWrite/Models/`）

- `EditorModel`：单个编辑标签的数据模型，持有 `FileInfo`、`TextEditor` 引用、`SaveStatus`、`Title`，并对外暴露 `SaveStatusChanged` 事件。`Title` 默认为 `"无标题"`，在无文件名时由 `MainEditorView` 根据第一段内容自动生成（超 5 字截断，保留前 5 后 3）。
- `SaveStatus`（enum）：`Draft` / `Saving` / `Saved` / `Error`。

### 4. Business 层（`SimpleWrite/Business/`）

- `SimpleWriteTextEditor`：继承自 `LightTextEditorPlus.TextEditor`，用于将来扩展编辑器行为。
- **ShortcutManagers/**：快捷键系统，职责分离：
  - `ShortcutKeyBind`（record）：按键组合 → 命令名 的映射数据。
  - `ShortcutCommand`：命令名 → `Action<ShortcutExecuteContext>` 的执行数据。
  - `ShortcutExecuteContext`：快捷键执行上下文（当前为空占位）。
  - `ShortcutManager`：统一管理 `KeyBind` 与 `Command` 列表，提供查找接口；数据存放在 `EditorViewModel`。
  - `ShortcutExecutor`：在键盘事件中查找并执行匹配命令，返回是否命中；实例在 `MainEditorView` 中创建并注入 `SimpleWriteTextEditorHandler`。
  - `ShortcutManagerHelper`：在 `EditorViewModel` 构造时注册所有默认快捷键（`Ctrl+S` 保存、`Ctrl+Shift+S` 另存为）。
- **FileHandlers/**：`ISaveFilePickerHandler` 接口 + `SaveFilePickerHandler` 实现，封装平台文件选择对话框；`SaveFilePickerHandler` 实例在 `MainEditorView.OnLoaded()` 中创建并注入 `EditorViewModel.SaveFilePickerHandler`。

---

## 关键约定

### 命名与可见性

- 内部实现类用 `internal`，公开 API 才用 `public`。
- ViewModel 属性变更使用 `OnPropertyChanged()` / `SetField()`，不绕过基类。
- 快捷键命令名（`string name`）在 `ShortcutManagerHelper` 中集中定义，保持一致。

### 异步规范

- 所有 I/O 操作（文件读写、文件选择器）均为 `async Task`，方法名以 `Async` 结尾。
- `ShortcutManagerHelper` 中触发的保存命令目前为同步调用（fire-and-forget），如需改为 `async` 请确保调用链完整。

### TextEditor 生命周期（`MainEditorView`）

`MainEditorView` 负责为每个 `EditorModel` 按需创建 `SimpleWriteTextEditor`，并在切换标签时更新 `TextEditorBorder.Child` 与 `CurrentTextEditor`：

```csharp
// MainEditorView 中：切换/创建编辑器
private void UpdateCurrentEditorMode(EditorModel editorModel)
{
    if (editorModel.TextEditor is null)
    {
        editorModel.TextEditor = CreateTextEditor(editorModel);
        // 若已有文件路径，立即加载内容
    }
    TextEditorBorder.Child = editorModel.TextEditor;
    CurrentTextEditor = editorModel.TextEditor;
}
```

键盘事件由 `SimpleWriteTextEditorHandler`（`MainEditorView.axaml.cs` 中的内部类）拦截，优先交给 `ShortcutExecutor` 处理，命中后不再向下传递。

### TextEditorInfo 附加属性模式

新增需要访问 `TextEditor` 的子控件时，沿用以下模式，不通过构造函数或属性直接传递 `TextEditor` 实例：

```csharp
// 在 SimpleWriteMainView 构造函数中注册（一次）
TextEditorInfo.SetTextEditorInfo(this, new TextEditorInfo(MainEditorView));

// 在任意子控件（如 StatusBar）的 OnLoaded 中获取，无需持有父控件引用
var textEditor = TextEditorInfo.GetTextEditorInfo(this).CurrentTextEditor;
```

`TextEditorInfo.CurrentTextEditor` 委托给 `MainEditorView.CurrentTextEditor`，始终返回当前激活的编辑器实例。

### LightTextEditorPlus（外部库，只读）

- 通过继承 `SimpleWriteTextEditor : TextEditor` 来扩展编辑器能力。
- 事件订阅使用库提供的 `CurrentSelectionChanged`、`DocumentChanged`、`LayoutCompleted` 等事件。
- 键盘处理通过继承 `TextEditorHandler` 并重写 `OnKeyDown` 实现（见 `SimpleWriteTextEditorHandler`）。

对 LightTextEditorPlus 文本库的使用，可参阅文本库里面的 `使用说明文档.md` 文件

### AOT 兼容

- 项目开启了 `<PublishAot>true</PublishAot>`，新增代码需注意：
  - 避免无限制反射（`Type.GetMethod` 等）。
  - 避免动态代码生成（`Emit`、`Expression.Compile` 需评估 AOT 支持情况）。
  - Avalonia 编译绑定已默认开启（`AvaloniaUseCompiledBindingsByDefault`），AXAML 绑定需确保类型可在编译期解析。

---

## 扩展点

| 场景 | 建议做法 |
|---|---|
| 新增快捷键 | 在 `ShortcutManagerHelper.AddDefaultShortcut` 中调用 `shortcutManager.AddShortcut(...)` |
| 新增文件格式支持 | 在 `EditorViewModel.LoadFileToTextEditorAsync` / `SaveEditorModelToFileAsync` 中扩展 |
| 新增平台对话框（打开文件等） | 新建接口，参考 `ISaveFilePickerHandler` + `SaveFilePickerHandler` 模式，在 `MainEditorView.OnLoaded()` 中注入 |
| 自定义编辑器初始化（字体、颜色等） | 在 `MainEditorView.CreateTextEditor()` 中扩展 |
| 新增状态栏信息 | 在 `StatusViewModel` 中添加属性，通过 `StatusText` 组合输出；光标信息调用 `SetCurrentCaretInfoText()` |
| 多标签切换 | 操作 `EditorViewModel.EditorModelList` 和 `CurrentEditorModel`；`MainEditorView` 会自动响应 `EditorModelChanged` 事件 |
| 新增需访问 `TextEditor` 的子控件 | 在 `OnLoaded` 中通过 `TextEditorInfo.GetTextEditorInfo(this).CurrentTextEditor` 获取，不持有父控件引用 |