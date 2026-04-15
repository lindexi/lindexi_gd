# VirtualFileExplorer 设计文档

## 总体设计

项目分为三层：

1. 领域层：`VirtualFileInfo`、`VirtualFolderInfo`、`VirtualFileSystemEntry` 等对象负责表达文件系统实体和能力状态。
2. 管理层：`VirtualFileManager` 定义统一的枚举与操作接口，`VirtualFileManagerAsyncAdapter` 提供异步包装，`PhysicalFileManager` 提供物理文件系统实现。
3. 表现层：`FileExplorerViewModel` 负责导航、选择、命令、排序、筛选与搜索；`FileExplorerUserControl` 负责整体布局；`BreadcrumbBarControl` 与 `FileExplorerContentControl` 负责可复用子界面。

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
- `IconGlyph`

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

同步能力继续保留，用于兼容原有实现。

### `VirtualFileManagerAsyncAdapter`

通过包装已有的同步 `VirtualFileManager` 提供以下异步入口：

- `GetRootFolderAsync`
- `GetEntriesAsync`
- `GetFilesAsync`
- `GetFoldersAsync`
- `ResolveFolderAsync`
- `RenameEntryAsync`
- `DeleteEntryAsync`
- `CopyEntryAsync`
- `MoveEntryAsync`
- `CreateFolderAsync`

这样可以在不破坏原同步模型的情况下，让 WPF 界面端切换为异步刷新与异步文件操作。

### `PhysicalFileManager`

设计为以物理根目录为边界的实现：

- 构造时传入根目录。
- 所有虚拟目录 `Id` 存放完整物理路径。
- 根目录之外的路径操作一律拒绝。
- 枚举时把 `FileInfo`、`DirectoryInfo` 转换为虚拟模型。
- 重命名、复制、移动、删除时分别处理文件和目录分支。

## 视图模型设计

新增 `FileExplorerViewModel`、`RelayCommand` 与 `AsyncRelayCommand`：

### 状态

- `FileManager`
- `CurrentFolder`
- `Entries`
- `_sourceEntries`
- `Breadcrumbs`
- `SelectedEntry`
- `SelectedEntries`
- `DisplayMode`
- `SearchText`
- `EntryFilter`
- `SortField`
- `SortDirection`
- `SelectionMode`
- `StatusText`

### 命令

- `RefreshCommand`
- `NavigateUpCommand`
- `OpenEntryCommand`
- `NavigateBreadcrumbCommand`
- `SwitchToTileViewCommand`
- `SwitchToDetailsViewCommand`

### 交互策略

- 控件以依赖属性接收 `VirtualFileManager`，再通过异步包装层驱动视图模型。
- `FileManager` 变化后，视图模型自动异步加载根目录。
- 复制和移动通过 `FolderPickerWindow` 选择目标目录，而不是手输路径。
- 搜索、筛选和排序都在视图模型层统一作用于 `_sourceEntries`。
- 操作完成后刷新当前目录，并尽量恢复合理的选中项。

## 控件设计

### 布局

控件包含四个区域：

1. 顶部工具栏：刷新、返回上级、创建文件夹、显示模式切换、操作按钮。
2. 面包屑区域：由独立的 `BreadcrumbBarControl` 呈现当前路径链并允许点击跳转。
3. 内容区域：由独立的 `FileExplorerContentControl` 呈现条目。
4. 状态区域：显示当前目录与数量统计。

### 视图切换

- 平铺模式使用 `WrapPanel + ListBox`。
- 详细模式使用 `ListView + GridView`，列宽可直接拖拽调整。
- 两种视图共享同一批 `Entries` 数据，并同步多选状态。

### 可定制入口

控件暴露依赖属性：

- `TileEntryTemplate`
- `TileFileTemplate`
- `TileFolderTemplate`
- `ToolBarContent`
- `NavigationContent`
- `EmptyContent`

若宿主未提供，则使用控件内默认模板。

## 示例应用设计

`MainWindow` 在启动时：

1. 创建演示根目录。
2. 准备若干子目录和文件。
3. 构造 `PhysicalFileManager`。
4. 将 `FileExplorerUserControl.TileFileTemplate` 替换为带图标的自定义模板。
5. 赋值给 `FileExplorerUserControl.FileManager`。

## 风险与折中

- `VirtualFileManagerAsyncAdapter` 仍基于同步 I/O 包装，后续若性能需要可继续演进为原生异步实现。
- 为保持改动可控，本轮优先提供文件夹选择器，独立文件选择器留待后续补充。
- 浅色主题已经抽离到资源字典，但更完整的主题切换系统仍有继续演进空间。
