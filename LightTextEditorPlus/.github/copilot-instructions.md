# AI 智能体协作指南

本文档用于约束 AI 在本仓库中的协作方式，帮助 AI 快速理解项目结构、查找项目文档，并将每次协作过程中的知识沉淀到知识库。

## 目标

- 让 AI 在开始任务前快速了解仓库结构。
- 让 AI 使用统一的文档入口，避免重复询问和重复探索。
- 让 AI 在每次任务后输出可复用知识，持续降低后续协作成本。

## 项目结构

以下为当前解决方案中的主要项目分布（按功能分组）：

### 核心与平台层

- `LightTextEditorPlus.Core`：核心排版与编辑能力，平台无关。
- `LightTextEditorPlus.Wpf`：WPF 平台对接与编辑处理。
- `LightTextEditorPlus.Avalonia`：Avalonia 平台对接与编辑处理。
- `LightTextEditorPlus.Skia`：Skia 渲染对接（主要用于渲染能力）。
- `LightTextEditorPlus.Skia.AotLayer`：Skia AOT 相关层。
- `LightTextEditorPlus.MauiGraphics`：MAUI 图形渲染相关对接。

### 构建与分析工具

- `Analyzers/TextEditorInternalAnalyzer`：内部分析器，用于 API 约束与一致性保障。
- `Build/Shared/API`：多平台共享 API 代码与约束输入。

### 测试与基准

- `Tests/LightTextEditorPlus.Core.Tests`
- `Tests/LightTextEditorPlus.Core.TestsFramework`
- `Tests/LightTextEditorPlus.Tests`
- `Tests/LightTextEditorPlus.Avalonia.Tests`
- `Tests/LightTextEditorPlus.Core.BenchmarkTests`
- `Tests/TextVisionComparer`

### 示例与文档

- `Demos/LightTextEditorPlus.Demo`
- `Demos/AvaloniaDemo`
- `Docs`：项目设计、行为、实现、维护文档。
- `知识库`：AI 每次协作后沉淀的知识总结目录。

## 项目文档链接

建议 AI 优先阅读以下文档：

- 根说明：`README.md`
- 使用说明：`Docs/使用说明文档.md`
- 维护说明：`Docs/维护文档.md`
- 行为定义：`Docs/行为定义.md`
- 实现定义：`Docs/实现定义.md`
- 术语表：`Docs/术语表.md`
- 参考资料：`Docs/参考文档.md`

## AI 协作流程约束

### 1. 任务开始前

- 先阅读 `README.md` 与本文件。
- 根据任务范围，再定向阅读 `Docs` 中对应文档。
- 尽量复用既有实现与既有测试模式。

### 2. 任务执行中

- 修改尽量最小化，避免扩大变更面。
- 对齐现有命名、目录结构和代码风格。
- 涉及平台差异时，优先保持跨平台 API 一致性。

### 3. 任务完成后（强制）

每次 AI 完成任务后，必须将本次协作知识总结写入 `知识库` 目录。

要求：

- 每次任务产出一份独立文档。
- 文件名建议：`知识库/YYYY-MM-DD-任务简述.md`。
- 内容至少包括：
  - 背景与目标
  - 关键文件与模块
  - 主要决策与原因
  - 修改点摘要
  - 验证方式（构建/测试/手工验证）
  - 后续建议

## 知识条目模板

可使用以下模板创建知识条目：

```markdown
# <任务标题>

## 背景与目标

## 关键文件

- `路径A`
- `路径B`

## 主要决策

## 修改摘要

## 验证记录

## 风险与后续建议
```
