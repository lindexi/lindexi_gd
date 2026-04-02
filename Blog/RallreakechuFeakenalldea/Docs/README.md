# SimpleWrite Docs

`Docs/` 是项目文档入口，重点用于帮助开发者快速理解现有实现，而不是堆叠大段背景描述。

## 建议阅读顺序

1. `.github/copilot-instructions.md`
   - 会话级开发约束、分层约定、AOT 注意事项。
2. `Knowledge/README.md`
   - 知识库索引，按问题类型定位对应经验文档。
3. `Knowledge/Avalonia/*.md`
   - 具体到某个交互、控件或分层设计的实现经验。
4. `Knowledge/Workflow/*.md`
   - 功能开发、自测、交付阶段的流程约定。

## 当前重点文档

- `Knowledge/Avalonia/TextEditor-Selection-To-Copilot-ContextMenu.md`
  - 说明编辑器选区如何进入右侧 Copilot 聊天。
- `Knowledge/Avalonia/MVVM-Layer-Responsibilities-And-ChatTemplateSelector.md`
  - 说明聊天区域的 MVVM 分层与模板选择方式。
- `Knowledge/Avalonia/Shortcut-Defaults-And-FilePicker.md`
  - 说明快捷键与文件选择器接入点。

## 维护原则

- 文档优先回答“代码在哪里、职责怎么分、从哪开始读”。
- 同一主题只保留一个主入口，避免重复写相同说明。
- 新增经验优先放到 `Knowledge/`，并同步补索引。