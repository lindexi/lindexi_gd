# Shortcut 默认方案与文件选择器注入

## 场景

为 `SimpleWrite` 扩展编辑器快捷键时，需要保证：

- 快捷键定义集中管理；
- `ViewModel` 不直接依赖 Avalonia 视图对象；
- 打开/保存文件能力都可通过注入方式提供。

## 实现约定

1. 所有默认快捷键统一在 `ShortcutManagerHelper.AddDefaultShortcut` 注册。
2. 与 UI 相关的文件选择器能力通过 `FileHandlers` 抽象注入到 `EditorViewModel`：
   - `ISaveFilePickerHandler` / `SaveFilePickerHandler`
   - `IOpenFilePickerHandler` / `OpenFilePickerHandler`
3. 在 `MainEditorView.OnLoaded()` 中从 `TopLevel` 创建并注入处理器实例。

## 当前默认快捷键

- `Ctrl+S`：保存当前文档
- `Ctrl+Shift+S`：当前文档另存为
- `Ctrl+O`：打开文件
- `Ctrl+N`：新建文档
- `Ctrl+W`：关闭当前文档
- `Ctrl+Tab`：切换到下一个文档

## 细节建议

- 快捷键触发异步方法时，显式丢弃返回任务（`_ = ...`），避免编译器告警。
- `CloseCurrentDocument` 需保证始终至少保留一个可编辑标签，避免 UI 落入无文档状态。
