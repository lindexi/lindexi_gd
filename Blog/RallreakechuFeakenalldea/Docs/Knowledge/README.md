# Knowledge Base

该目录用于沉淀开发与协作中的可复用经验，帮助开发者与智能体快速理解已有实践并复用。

## 分类

- `Avalonia/`：Avalonia UI 设计与实现经验
- `Workflow/`：需求落地、协作与交付流程经验

## 现有文档

### `Avalonia/TabBar-ContextMenu-And-Explorer.md`

- **主题**：TabBar 右键菜单与资源管理器联动
- **解决问题**：统一标签页操作入口，明确“定位文件/打开上下文菜单”等交互行为
- **适用场景**：需要为编辑器或多标签界面补齐右键交互、文件定位能力时
- **关键词**：`TabBar`、`ContextMenu`、`Explorer`、`交互一致性`

### `Avalonia/Shortcut-Defaults-And-FilePicker.md`

- **主题**：快捷键默认策略与文件选择器行为约定
- **解决问题**：避免快捷键冲突、明确默认绑定；统一文件打开/保存选择器的体验
- **适用场景**：新增命令、调整快捷键体系、接入文件选择器时
- **关键词**：`Shortcut`、`默认值`、`FilePicker`、`用户体验`

### `Avalonia/Xaml-Naming-Notes.md`

- **主题**：XAML 命名规范与约束说明
- **解决问题**：降低命名歧义，提升代码可读性与可维护性，便于自动化分析
- **适用场景**：新增/重构视图、统一控件命名、进行样式与模板梳理时
- **关键词**：`XAML`、`命名规范`、`可维护性`、`一致性`

### `Avalonia/Markdown-Url-Highlighting.md`

- **主题**：Markdown 正文 URL 高亮与命中信息记录
- **解决问题**：为正文裸链接补齐蓝色下划线样式，并为后续点击打开链接保留范围与地址
- **适用场景**：需要在 Markdown 编辑态识别 URL、实现链接命中测试或点击打开行为时
- **关键词**：`Markdown`、`URL`、`Highlighter`、`HitTest`

### `Avalonia/TextEditor-Selection-To-Copilot-ContextMenu.md`

- **主题**：文本编辑器选中内容发送到 Copilot 的右键菜单交互
- **解决问题**：让选中文本可直接通过编辑器右键菜单进入共享 Copilot 会话
- **适用场景**：需要在编辑区与 AI 侧栏之间建立快速选区提问入口时
- **关键词**：`TextEditor`、`ContextMenu`、`Copilot`、`Selection`

### `Workflow/Feature-Delivery-Checklist.md`

- **主题**：功能交付检查清单
- **解决问题**：确保需求从实现到验证、文档与回归检查完整闭环
- **适用场景**：功能开发、提测、自测、代码评审、发布前检查
- **关键词**：`Feature Delivery`、`Checklist`、`质量保障`、`交付流程`

## 使用建议（面向智能体）

- 在实现功能前，先按关键词匹配相关文档，提取可复用约束。
- 在生成代码或交互方案时，优先遵循文档中的既有约定。
- 如存在冲突，以最新的流程类文档与团队明确约定为准。
- 在提交结果时，可引用对应文档路径，说明本次实现依据。
