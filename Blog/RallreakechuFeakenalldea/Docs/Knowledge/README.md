# Knowledge Base

该目录用于沉淀可复用的开发经验。阅读时先找“当前问题属于哪一类”，再进入对应文档，不需要一次性通读全部内容。

## 快速入口

| 我想了解什么 | 先看哪篇 |
|---|---|
| 编辑器选区如何发送到 Copilot 聊天 | `Avalonia/TextEditor-Selection-To-Copilot-ContextMenu.md` |
| 聊天气泡模板与 MVVM 边界怎么拆 | `Avalonia/MVVM-Layer-Responsibilities-And-ChatTemplateSelector.md` |
| Markdown 链接为何能高亮并支持命中测试 | `Avalonia/Markdown-Url-Highlighting.md` |
| 默认快捷键和文件选择器在哪里接入 | `Avalonia/Shortcut-Defaults-And-FilePicker.md` |
| 标签页右键菜单如何定位到文件资源管理器 | `Avalonia/TabBar-ContextMenu-And-Explorer.md` |
| XAML 命名有哪些额外约束 | `Avalonia/Xaml-Naming-Notes.md` |
| 功能开发完成后如何自检 | `Workflow/Feature-Delivery-Checklist.md` |

## 分类索引

### `Avalonia/`

| 文档 | 主题 | 适用场景 |
|---|---|---|
| `MVVM-Layer-Responsibilities-And-ChatTemplateSelector.md` | 聊天消息模板选择与 MVVM 职责边界 | 调整 Copilot 聊天 UI、梳理 Model/View/ViewModel 责任时 |
| `Markdown-Url-Highlighting.md` | Markdown 正文 URL 高亮与命中信息记录 | 需要识别链接、命中链接、准备点击打开能力时 |
| `Shortcut-Defaults-And-FilePicker.md` | 快捷键默认方案与文件选择器注入 | 新增命令、调整快捷键、接入打开/保存对话框时 |
| `TabBar-ContextMenu-And-Explorer.md` | 标签栏右键菜单与文件资源管理器联动 | 扩展标签交互入口、定位文件时 |
| `TextEditor-Selection-To-Copilot-ContextMenu.md` | 文本选区发送到 Copilot 聊天的完整链路 | 理解编辑器与 Copilot 侧栏的协作边界时 |
| `Xaml-Naming-Notes.md` | XAML 命名补充约束 | 新增或重构视图、补 `x:Name` 时 |

### `Workflow/`

| 文档 | 主题 | 适用场景 |
|---|---|---|
| `Feature-Delivery-Checklist.md` | 功能交付检查清单 | 实现完成后做构建、自测、回归与文档收口时 |

## 阅读建议

1. 先读与当前问题直接相关的那一篇。
2. 如果改动跨越 UI、ViewModel、交互流程，再补读同分类下的关联文档。
3. 如果文档约定和代码现状不一致，以代码当前实现为准，并补充更新文档。
