# SimpleWrite — AI 协作指南

> 本文件用于存储 **每个会话都必须遵循** 的核心开发规范。
> 如果本文件内容超过 200 行，请将扩展内容（详细实现示例、完整代码片段、故障排查手册等）移至 `/Docs` 文件夹，并在此保留关键要点和文档链接。
> 开始开发之前，请阅读 `/Docs` 文件夹内的文档内容，按需阅读 `/Docs/Knowledge` 知识经验文档
> 每次任务完成之后，请整理文档，将学到的知识和总结的经验写入到 `/Docs/Knowledge` 文件夹

## 开始前先看

1. `Docs/README.md`：文档入口与阅读顺序。
2. `Docs/Knowledge/README.md`：按主题定位经验文档。
3. 当前任务相关的 `Docs/Knowledge/Avalonia/*.md` 或 `Docs/Knowledge/Workflow/*.md`。

任务完成后，记得把新增经验补充到 `Docs/Knowledge/`。

## 项目速览

- `SimpleWrite/`：核心 Avalonia UI 与 ViewModel。
- `RallreakechuFeakenalldea/`：宿主应用。
- `RallreakechuFeakenalldea.Desktop/`：桌面入口。
- `LightTextEditorPlus/`：底层文本编辑器库，只读。
- `Docs/`：项目文档与知识库。

目标框架为 `.NET 9`，开启了 AOT 发布。

## 必守规则

### 分层

- 严格遵循 MVVM。
- View 只做界面和事件转发，不直接承载业务。
- `SimpleWriteMainViewModel` 负责跨 ViewModel 协调。

### TextEditor 相关

- 当前编辑器实例由 `MainEditorView` 管理生命周期。
- 需要从子控件访问当前编辑器时，统一走 `TextEditorInfo` 附加属性。
- 扩展编辑器能力时，优先修改 `SimpleWriteTextEditor : TextEditor`。

### 文件与快捷键

- 文件选择器通过 `IFilePickerHandler` 注入，不让 ViewModel 直接依赖 `TopLevel`。
- 默认快捷键统一在 `ShortcutManagerHelper.AddDefaultShortcut` 注册。

### 异步与平台

- 文件读写、文件选择器等 I/O 一律使用 `async Task`。
- 涉及平台能力时，至少考虑 Windows 与 Linux。

### AOT 与 XAML

- 避免无限制反射、动态代码生成。
- 共享配色统一放在 `SimpleWrite/Styles/Brushes.axaml`。
- 共享控件样式统一从 `SimpleWrite/Styles/MainStyles.axaml` 聚合。
- 已声明 `x:Name` 的控件直接使用生成字段，不再通过 `FindControl` 查找。

### 插件命令模式

- 在此仓库中，`PluginCommandPatternProvider` 不应通过多个委托注入能力，而应直接感知 `SimpleWriteMainViewModel`。
- 仅与模型无关的命令放在该提供器中。
- 用于侧边栏显示对话的接口命名应强调侧边栏会话展示，而非 Copilot。

## 常见扩展入口

| 场景 | 入口 |
|---|---|
| 打开/保存/另存为 | `EditorViewModel` |
| 多标签切换 | `EditorViewModel.EditorModelList` 与 `CurrentEditorModel` |
| 快捷键 | `ShortcutManagerHelper` |
| 当前编辑器相关 UI | `MainEditorView`、`TextEditorInfo` |
| 侧边栏目录树 | `FolderExplorerViewModel`、`SimpleWriteSideBar` |
| 状态栏信息 | `StatusViewModel` |
| 共用主题资源 | `Brushes.axaml`、`MainStyles.axaml` |

## 推荐阅读

- 目录树与文件夹查找：`Docs/Knowledge/Avalonia/Folder-Explorer-And-Folder-Find.md`
- 目录树选中联动与主题：`Docs/Knowledge/Avalonia/Folder-TreeView-Selection-And-Theme.md`
- 快捷键与文件选择器：`Docs/Knowledge/Avalonia/Shortcut-Defaults-And-FilePicker.md`
- 交付自检：`Docs/Knowledge/Workflow/Feature-Delivery-Checklist.md`
