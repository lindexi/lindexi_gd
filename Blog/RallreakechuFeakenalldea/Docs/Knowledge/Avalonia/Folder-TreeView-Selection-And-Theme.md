# 目录树跟随当前文档与深色主题约定

## 场景

`SimpleWriteSideBar` 中的 `FolderTreeView` 需要在文档切换后自动定位到当前打开文件，同时保持深色主题下的目录树可读性。

## 当前实现

### 1. 选中同步入口

- 同步逻辑放在 `FolderExplorerViewModel`。
- `FolderExplorerViewModel` 在构造时订阅 `EditorViewModel.EditorModelChanged`。
- 当当前文档变化或重新打开文件夹时，会调用 `SyncSelectedEditorFile()`。

### 2. 节点状态来源

`FolderTreeItemViewModel` 持有两个 UI 状态：

- `IsSelected`
- `IsExpanded`

它们只描述目录树展示状态，不参与文件打开逻辑。

### 3. 递归选择规则

- 若当前文档不在已打开文件夹内，则清空目录树选中态。
- 若当前文档在目录树中：
  - 选中文件节点；
  - 递归展开它的所有父目录；
  - 保留用户对其他目录的展开状态，不主动收起。

### 4. 路径比较

- 路径比较放在 `FolderExplorerViewModel` 内部统一处理。
- Windows 使用 `OrdinalIgnoreCase`。
- 非 Windows 使用 `Ordinal`，避免大小写敏感平台误判。

## 视图与样式约定

### `SimpleWriteSideBar`

- 目录树继续只负责交互事件，例如双击打开文件。
- 不在视图里自己查找当前文档对应节点。

### `TreeViewStyles.axaml`

- `TreeViewItem.IsSelected` 与 `TreeViewItem.IsExpanded` 通过绑定映射到 `FolderTreeItemViewModel`。
- 深色主题下统一覆盖：
  - `ChevronPath`
  - 悬停背景
  - 选中背景
  - 选中边框
  - 选中前景色

### `Brushes.axaml`

目录树相关颜色统一放到共享画刷里，避免在模板里硬编码颜色。

### 共享控件样式补充

- 新增 `CheckBox`、`ToggleButton` 这类交互控件时，不要直接使用默认主题外观。
- 若界面仍走项目自定义深色风格，需要把样式收敛到 `SimpleWrite/Styles/*.axaml`，并在 `MainStyles.axaml` 中聚合后再在视图里通过 `Classes` 显式启用。
- 新增控件样式时优先复用 `Brushes.axaml` 中已有画刷；确实缺少时再补共享画刷，避免局部硬编码颜色导致风格漂移。

## 适用场景

- 排查“目录树没有跟随当前文档”的问题。
- 调整目录树选中态或展开策略。
- 修改深色主题下的 `TreeView`、`TreeViewItem`、`ChevronPath` 配色。
