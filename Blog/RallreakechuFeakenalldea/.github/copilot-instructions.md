# SimpleWrite — AI 协作指南

## 项目概览

**SimpleWrite** 是一款基于 [Avalonia UI](https://avaloniaui.net/) 与 [LightTextEditorPlus](../LightTextEditorPlus) 构建的跨平台轻量文本编辑器，目标框架为 `.NET 9`，启用了 AOT 发布。

### 解决方案结构

```
SimpleWrite/                        # 核心 UI 库（Avalonia UserControl，可复用）
RallreakechuFeakenalldea/           # Avalonia 宿主应用（Shell）
RallreakechuFeakenalldea.Desktop/   # 桌面平台入口
Docs/                               # 项目文档与待办
LightTextEditorPlus/                # 富文本编辑器底层库（外部依赖，只读）
```

---

## 架构分层

项目严格遵循 **MVVM** 模式，各层职责如下：

### 1. View 层（`SimpleWrite/Views/`）
- 只负责 UI 渲染与用户交互事件转发。
- 通过 **Avalonia 附加属性** `TextEditorInfo` 将 `MainEditorView` 的 `TextEditor` 实例向下传递给子控件（如 `StatusBar`），避免直接依赖父级控件。
- `SimpleWriteMainView` 是顶层 `UserControl`，承载所有子视图。
- 文件对话框等平台能力通过 `ISaveFilePickerHandler` 接口注入，不在 View 层直接实现业务逻辑。

### 2. ViewModel 层（`SimpleWrite/ViewModels/`）
- `SimpleWriteMainViewModel`：根 ViewModel，组合 `EditorViewModel` 与 `StatusViewModel`，负责跨 ViewModel 的事件桥接（如 `SaveStatusChanged`）。
- `EditorViewModel`：编辑核心逻辑，管理 `EditorModel` 列表（多标签）、文件打开/保存/另存为流程、快捷键注册。
- `StatusViewModel`：状态栏数据（保存状态文本、光标位置文本）。
- `ViewModelBase`：提供 `INotifyPropertyChanged` 的标准实现，所有 ViewModel 继承此基类。

### 3. Model 层（`SimpleWrite/Models/`）
- `EditorModel`：单个编辑标签的数据模型，持有 `FileInfo`、`TextEditor` 引用、`SaveStatus`，并对外暴露 `SaveStatusChanged` 事件。
- `SaveStatus`（enum）：`Draft` / `Saving` / `Saved` / `Error`。

### 4. Business 层（`SimpleWrite/Business/`）
- `SimpleWriteTextEditor`：继承自 `LightTextEditorPlus.TextEditor`，用于将来扩展编辑器行为。
- **ShortcutManagers/**：快捷键系统，三层分离：
  - `ShortcutKeyBind`（record）：按键组合 → 命令名 的映射数据。
  - `ShortcutCommand`：命令名 → `Action<ShortcutExecuteContext>` 的执行数据。
  - `ShortcutManager`：统一管理 `KeyBind` 与 `Command` 列表，提供查找接口。
  - `ShortcutExecutor`：在键盘事件中查找并执行匹配命令，返回是否命中。
  - `ShortcutManagerHelper`：注册所有默认快捷键（`Ctrl+S` 保存、`Ctrl+Shift+S` 另存为）。
- **FileHandlers/**：`ISaveFilePickerHandler` 接口 + `SaveFilePickerHandler` 实现，封装平台文件选择对话框。

---

## 关键约定

### 命名与可见性
- 内部实现类用 `internal`，公开 API 才用 `public`。
- ViewModel 属性变更使用 `OnPropertyChanged()` / `SetField()`，不绕过基类。
- 快捷键命令名（`string name`）在 `ShortcutManagerHelper` 中集中定义，保持一致。

### 异步规范
- 所有 I/O 操作（文件读写、文件选择器）均为 `async Task`，方法名以 `Async` 结尾。
- 不使用 fire-and-forget；`ShortcutManagerHelper` 中触发的保存命令目前为同步调用，如需改为 async 请确保调用方也是 `async`。

### TextEditorInfo 附加属性模式

新增需要访问 `TextEditor` 的子控件时，沿用以下模式，不通过构造函数或属性直接传递 `TextEditor` 实例：

```csharp
// 在 SimpleWriteMainView 中注册
TextEditorInfo.SetTextEditorInfo(this, new TextEditorInfo(MainEditorView));

// 在子控件（如 StatusBar）中获取，无需持有父控件引用
var textEditor = TextEditorInfo.GetTextEditorInfo(this).CurrentTextEditor;
```

### LightTextEditorPlus（外部库，只读）
- 不修改 `LightTextEditorPlus/` 目录下的任何文件。
- 通过继承 `SimpleWriteTextEditor : TextEditor` 来扩展编辑器能力。
- 事件订阅使用库提供的 `CurrentSelectionChanged`、`LayoutCompleted` 等事件。

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
| 新增平台对话框（打开文件等） | 新建接口，参考 `ISaveFilePickerHandler` + `SaveFilePickerHandler` 模式 |
| 新增状态栏信息 | 在 `StatusViewModel` 中添加属性，通过 `StatusText` 组合输出 |
| 多标签切换 | 操作 `EditorViewModel.EditorModelList` 和 `CurrentEditorModel` |

---

## 待办功能（`Docs/README.md`）

- [ ] 支持打开加密文档，弹出加密对话框
