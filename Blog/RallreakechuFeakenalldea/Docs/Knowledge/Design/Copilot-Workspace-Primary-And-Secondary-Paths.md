# Copilot 主副工作区路径约定

## 场景

Copilot 默认文件工具原先只依赖一个 `WorkspacePath`。当用户没有打开左侧文件夹，或者当前文档来自其他外部目录时，相对路径解析会落到错误目录。

这次调整后，工作区拆成两层：

- 主工作区：当前打开文件夹；
- 副工作区：当前文档所在目录。

## 规则

1. `WorkspacePath` 继续表示主工作区优先的默认路径。
2. `SecondaryWorkspacePath` 表示当前文档目录。
3. 目录类工具只允许使用主工作区：
   - `ListDirectory`
   - `FindEntriesByName`
   - `FindFilesContainingText`
4. 文件读取类工具按以下顺序解析相对路径：
   1. 主工作区
   2. 副工作区
5. 任一路径参与解析时，都必须保证目标文件仍在各自根目录内部。

## UI 桥接

桥接点在 `SimpleWrite/Views/Components/RightSlideBar.axaml.cs`：

- `FolderExplorerViewModel.CurrentFolderChanged` -> `WorkspacePath`
- `EditorViewModel.EditorModelChanged` -> `SecondaryWorkspacePath`

这样可以保持：

- 左侧目录树继续只表达“已打开文件夹”；
- Copilot 仍能围绕当前文档附近的文件做只读分析；
- MVVM 分层没有额外倒灌到 ViewModel 里。

## 回归测试

`AgentLib.Tests/WorkspaceToolProviderTests.cs` 当前覆盖：

- 主工作区为空时，`ReadFile` 会回退到副工作区；
- 主工作区为空时，`ListDirectory` 不会错误读取副工作区。
