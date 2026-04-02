# 左侧文件夹树与文件夹查找的实现方式

## 场景

为 `SimpleWriteSideBar` 增加“打开文件夹”入口，并让 `FindReplaceBar` 支持在当前已打开文件夹中查找内容时，需要同时满足：

- 目录树可以树形展开文件夹内容；
- 点击结果能够继续复用现有的 `OpenFileAsync` 打开文档链路；
- `FindReplaceBar` 的“当前文档查找替换”职责不被打乱；
- 仍然沿用现有的文件选择器注入方式，不让 ViewModel 直接依赖 `TopLevel`。

## 模块拆分

### 1. `FolderExplorerViewModel`

- 负责左侧边栏的当前文件夹状态。
- 对外暴露：
  - `RootItems`
  - `CurrentFolderPath`
  - `HasOpenedFolder`
  - `PickAndOpenFolderAsync()`
  - `OpenFileAsync(...)`
- 自己不直接创建 Avalonia 文件选择器，只接受 `IFilePickerHandler` 注入。

### 2. `FolderExplorerService`

- 负责把磁盘目录递归转换成目录树数据。
- 目录与文件统一排序，目录排在前面。
- 对无权限或瞬时失效的子目录，当前实现选择跳过，避免整个侧边栏加载失败。

### 3. `FindReplaceViewModel`

- 继续保留“当前文档查找/替换”的核心职责。
- 新增 `IsFolderSearchScope` 与 `FolderSearchResultList`，用于承接“当前文件夹查找”。
- 文件夹模式只做查找，不做跨文件替换；因此 `IsReplaceAvailable` 需要显式约束为“替换模式且不是文件夹模式”。

### 4. `FolderSearchService`

- 负责递归搜索当前文件夹下的文件内容。
- 返回的是“按文件聚合”的结果：
  - 相对路径
  - 命中次数
  - 第一处命中的偏移
  - 预览片段
- `FindReplaceViewModel` 再把这些结果转成 UI 需要的 `FolderSearchResultViewModel`。

## 交互约定

### 左侧边栏

- `SimpleWriteSideBar` 只处理按钮点击和树节点双击。
- 双击文件节点时，调用 `FolderExplorerViewModel.OpenFileAsync(...)`。
- 不要在视图里自己解析路径或直接 new `FileInfo` 后操作编辑器控件。

### 查找栏

- 在 `FindReplaceBar` 顶部增加“当前文件夹”开关。
- 切换到文件夹模式后：
  - “上一个/下一个”不再可用；
  - 用单独的“搜索文件夹”按钮或 `Enter` 触发查找；
  - 搜索结果列表展示在 `FindReplaceBar` 下方；
  - 点击“打开”后，仍然走 `EditorViewModel.OpenFileAsync(...)`，然后用已记录的偏移恢复选区。

## 本次踩坑记录

1. `FolderTreeItemViewModel` 虽然需要给 XAML 使用，但其接收内部目录树模型 `FolderTreeEntry` 的构造函数必须保持 `internal`，否则会触发可访问性不一致。
2. `FindReplaceBar` 的焦点逻辑不能只看 `IsReplaceMode`，否则切到文件夹模式时会错误聚焦已经隐藏的替换输入框，应该改用 `IsReplaceAvailable`。
3. 文件夹搜索再次执行前要先清空旧结果，否则用户会在“搜索中”阶段看到上一轮结果，误以为没有刷新。
4. 任何新增 `x:Name`、`TreeView` 模板、事件处理之后，都应立即构建一次，优先抓出 Avalonia XAML 编译错误。

## 适用场景

- 继续扩展左侧资源管理器，如加入刷新、右键菜单、过滤器时。
- 继续增强文件夹查找，如增加忽略大小写、仅搜索特定扩展名、结果高亮时。
- 排查“目录树能显示但打开文件不工作”或“文件夹搜索有结果但没有定位到文本选区”时。
