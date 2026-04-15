# VirtualFileExplorer 设计文档

## 总体设计

项目分为三层：

1. 领域层：`VirtualFileInfo`、`VirtualFolderInfo`、`VirtualFileSystemEntry` 等对象负责表达文件系统实体和能力状态。
2. 管理层：`VirtualFileManager` 定义统一的枚举与操作接口，`PhysicalFileManager` 提供物理文件系统实现。
3. 表现层：`FileExplorerViewModel` 负责导航、选择、命令和显示模式；`FileExplorerUserControl` 负责展示和模板扩展。

## 领域模型设计

### `VirtualFileSystemEntry`

新增公共基类表达文件和文件夹的共性：

- `Id`
- `Name`
- `OwnerFolder`
- `EntryType`
- `CreatedTime`
- `LastWriteTime`
- `LastAccessTime`
- `CanRename`
- `CanMove`
- `CanCopy`
- `CanDelete`

这样视图层可以用统一集合承载文件和文件夹，减少两套视图同步逻辑。

### `VirtualFileInfo`

在基类之上增加：

- `Extension`
- `Length`

### `VirtualFolderInfo`

在基类之上增加：

- `UpperLevelFolder`
- 面包屑构建能力

### `RelativeFolderLink`

改造为面包屑项模型，直接保存目标 `VirtualFolderInfo`，避免界面通过字符串回查目录。

## 管理器设计

### `VirtualFileManager`

统一定义以下能力：

- `RootFolder`
- `GetFiles`
- `GetFolders`
- `GetEntries`
- `RenameEntry`
- `DeleteEntry`
- `CopyEntry`
- `MoveEntry`
- `CreateFolder`

默认实现使用虚方法包装，让具体实现只需重写必要成员。

### `PhysicalFileManager`

设计为以物理根目录为边界的实现：

- 构造时传入根目录。
- 所有虚拟目录 `Id` 存放完整物理路径。
- 根目录之外的路径操作一律拒绝。
- 枚举时把 `FileInfo`、`DirectoryInfo` 转换为虚拟模型。
- 重命名、复制、移动、删除时分别处理文件和目录分支。

## 视图模型设计

新增 `FileExplorerViewModel` 与 `RelayCommand`：

### 状态

- `FileManager`
- `CurrentFolder`
- `Entries`
- `Breadcrumbs`
- `SelectedEntry`
- `DisplayMode`
- `StatusText`

### 命令

- `RefreshCommand`
- `NavigateUpCommand`
- `OpenEntryCommand`
- `SwitchToTileViewCommand`
- `SwitchToDetailsViewCommand`
- `RenameCommand`
- `CopyCommand`
- `MoveCommand`
- `DeleteCommand`
- `CreateFolderCommand`

### 交互策略

- 控件以依赖属性接收 `VirtualFileManager`。
- `FileManager` 变化后，视图模型自动加载根目录。
- 对需要补充名称或目标目录的操作，先用简易对话框收集输入，再执行管理器操作。
- 操作完成后刷新当前目录，并恢复合理的选中项。

## 控件设计

### 布局

控件包含四个区域：

1. 顶部工具栏：刷新、返回上级、创建文件夹、显示模式切换、操作按钮。
2. 面包屑区域：显示当前路径链并允许点击跳转。
3. 内容区域：使用 `ListBox` 展示条目。
4. 状态区域：显示当前目录与数量统计。

### 视图切换

- 使用同一个 `ListBox` 数据源。
- 通过切换 `ItemsPanel` 与 `ItemTemplate` 风格实现平铺和详细模式。
- 详细模式使用 `GridViewRowPresenter` 风格模板近似列表详情展示。

### 可定制入口

控件暴露依赖属性：

- `EntryTemplate`
- `DetailsEntryTemplate`
- `ToolBarContent`
- `EmptyContent`

若宿主未提供，则使用控件内默认模板。

## 示例应用设计

`MainWindow` 在启动时：

1. 创建演示根目录。
2. 准备若干子目录和文件。
3. 构造 `PhysicalFileManager`。
4. 赋值给 `FileExplorerUserControl.FileManager`。

## 更改

当前的 `VirtualFileManager` 为同步的 API 版本，需要将其更改为异步版本

## 风险与折中

- WPF 原生没有跨平台输入对话框，本轮使用轻量窗口收集重命名和复制移动目标，保证库内自包含。
- 为保持改动可控，本轮不引入完整资源字典主题拆分，而是在控件内提供基础样式和模板入口。