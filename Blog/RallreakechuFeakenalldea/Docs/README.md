# SimpleWrite Docs

`Docs/` 只负责给出阅读入口；具体实现经验统一放进 `Docs/Knowledge/`。

## 推荐阅读顺序

1. `.github/copilot-instructions.md`
   - 会话级约束与高频入口。
2. `Knowledge/README.md`
   - 按问题类型定位知识文档。
3. 当前任务相关的 `Knowledge/Avalonia/*.md` 或 `Knowledge/Design/*.md`
   - 前者看 Avalonia 控件、样式、交互与平台接线；后者看业务设计、Copilot 会话与命令链路。
4. `Knowledge/Workflow/*.md`
   - 做开发自检与交付收口。

## 常用入口

| 主题 | 文档 |
|---|---|
| 编辑器右键命令模式与本地转换 | `Knowledge/Design/Command-Pattern-And-Local-Transform.md` |
| 代码着色三分类与扩展名路由 | `Knowledge/Avalonia/Code-Highlighting-Categories-And-Extensions.md` |
| Copilot 聊天历史 XML 落盘 | `Knowledge/Design/Copilot-Chat-History-Xml.md` |
| Copilot 流式消息片段与工作区工具异步化 | `Knowledge/Design/Copilot-Streaming-Message-Items-And-Workspace-Tools-Async.md` |
| Copilot 人类会话与 AgentSession 分层 | `Knowledge/Design/Copilot-AgentSession-And-HumanSession-Split.md` |
| 左侧目录树与文件夹查找 | `Knowledge/Avalonia/Folder-Explorer-And-Folder-Find.md` |
| Copilot 默认工作路径工具 | `Knowledge/Design/Copilot-Workspace-Default-Tools.md` |
| Copilot 主副工作区路径 | `Knowledge/Design/Copilot-Workspace-Primary-And-Secondary-Paths.md` |
| 目录树选中联动与深色主题 | `Knowledge/Avalonia/Folder-TreeView-Selection-And-Theme.md` |
| 标签关闭确认与临时快照自动保存 | `Knowledge/Avalonia/Unsaved-Tab-Close-And-Temp-Autosave.md` |
| 编辑器选区发送到 Copilot | `Knowledge/Design/TextEditor-Selection-To-Copilot-ContextMenu.md` |
| Copilot 派生命令为何会新建会话 | `Knowledge/Design/Copilot-New-Session-Command-Behavior.md` |
| 快捷键与文件选择器 | `Knowledge/Avalonia/Shortcut-Defaults-And-FilePicker.md` |
| 功能交付自检 | `Knowledge/Workflow/Feature-Delivery-Checklist.md` |
| AgentLib 单元测试补齐 | `Knowledge/Workflow/AgentLib-Tests-Coverage-And-MSTest-Notes.md` |

## 维护原则

- 入口文档保持短小，只保留索引和阅读顺序。
- 同一主题优先补充原有知识文档，避免重复说明。
- 新增经验写入 `Knowledge/` 后，记得同步更新索引。
