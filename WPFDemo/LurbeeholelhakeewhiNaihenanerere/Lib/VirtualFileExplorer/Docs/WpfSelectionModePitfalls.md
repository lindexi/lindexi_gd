# WPF SelectionMode 选择机制经验总结

## 背景

本次问题出现在 `FileExplorerContentControl` 的选择同步逻辑中。控件需要同时兼容单选和多选模式，但代码直接访问了 `ListBox.SelectedItems`。当控件处于 `SelectionMode.Single` 时，WPF 会抛出异常：只能在多选模式中更改 `SelectedItems` 集合。

## 结论

在 WPF 中，`SelectedItem` 和 `SelectedItems` 不是可以随意混用的两套等价 API。

- 单选模式下，应使用 `SelectedItem`。
- 多选模式下，才允许读取和修改 `SelectedItems`。
- 当代码需要兼容两种模式时，必须先判断 `SelectionMode`，再决定读取或写入哪一条路径。

## 本次修复策略

### 1. 写入选择状态时先判断 `SelectionMode`

在 `SyncSelection` 中：

- 若为 `SelectionMode.Single`，只设置 `SelectedItem`。
- 若为多选模式，先清空 `SelectedItems`，再逐项加入，并同步主选中项。

### 2. 读取选择状态时先判断 `SelectionMode`

在 `OnSelectionChanged` 中：

- 单选模式下，从 `SelectedItem` 组装单项结果。
- 多选模式下，才从 `SelectedItems` 收集结果。

### 3. 将模式判断集中到公共方法

本次没有把判断逻辑散落在多个事件处理器里，而是提取成公共的选择读取方法和选择同步方法。这样后续如果新增 `ListView`、`ListBox` 或其他选择器控件，也能复用同一套机制，减少再次犯错的概率。

## 经验要点

### 1. 不要假设 `ListBox` 一定支持 `SelectedItems`

虽然 `ListBox` 和 `ListView` 都有 `SelectedItems`，但这不表示任何模式下都能安全访问。真正决定是否可用的是 `SelectionMode`。

### 2. 复用控件时要优先考虑模式切换

像文件浏览器这类复用控件，往往既会被主界面以多选方式使用，也会被文件夹选择器以单选方式使用。只在一种模式下验证，很容易遗漏另一种模式的异常。

### 3. 选择同步代码属于高风险区域

凡是涉及以下内容的代码，都需要单独审查：

- `SelectedItem`
- `SelectedItems`
- `SelectionChanged`
- 从视图模型回写 UI 选择状态
- 从 UI 读取选择结果回写视图模型

## 后续建议

1. 后续新增选择控件时，优先复用当前的模式判断方法，不要重新手写一套。
2. 与选择模式相关的逻辑应补充测试场景，至少覆盖单选和多选两条路径。
3. 以后在做 WPF 代码审查时，可以把“是否在单选模式下误用 `SelectedItems`”作为固定检查项。
