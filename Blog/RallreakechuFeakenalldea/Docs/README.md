# SimpleWrite Docs

`Docs/` 只负责给出阅读入口；具体实现经验统一放进 `Docs/Knowledge/`。

## 推荐阅读顺序

1. `.github/copilot-instructions.md`
   - 会话级约束与高频入口。
2. `Knowledge/README.md`
   - 按问题类型定位知识文档。
3. 当前任务相关的 `Knowledge/Avalonia/*.md`
   - 看具体控件、交互和 MVVM 组织方式。
4. `Knowledge/Workflow/*.md`
   - 做开发自检与交付收口。

## 常用入口

| 主题 | 文档 |
|---|---|
| 编辑器右键命令模式与本地转换 | `Knowledge/Avalonia/Command-Pattern-And-Local-Transform.md` |
| 左侧目录树与文件夹查找 | `Knowledge/Avalonia/Folder-Explorer-And-Folder-Find.md` |
| 目录树选中联动与深色主题 | `Knowledge/Avalonia/Folder-TreeView-Selection-And-Theme.md` |
| 编辑器选区发送到 Copilot | `Knowledge/Avalonia/TextEditor-Selection-To-Copilot-ContextMenu.md` |
| 快捷键与文件选择器 | `Knowledge/Avalonia/Shortcut-Defaults-And-FilePicker.md` |
| 功能交付自检 | `Knowledge/Workflow/Feature-Delivery-Checklist.md` |

## 维护原则

- 入口文档保持短小，只保留索引和阅读顺序。
- 同一主题优先补充原有知识文档，避免重复说明。
- 新增经验写入 `Knowledge/` 后，记得同步更新索引。
