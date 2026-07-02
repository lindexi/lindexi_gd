# SlideML 回填属性重构计划

## 变更概述

将 `ActualWidth` / `ActualHeight` / XY 坐标回写 改为 `RenderSize` + `RenderLocation`，解决当前流式布局下 XY 坐标被回填覆盖导致无法区分用户输入与引擎计算值的问题。

### 核心问题

当前实现中，`FinalLayout` 调用 `SyncLayoutBoundsToXY` 将布局计算结果回写到 `X`/`Y` 属性，然后 `FormatRenderedXml` 再将 `X`/`Y` 序列化回 XML。在流式布局场景下，这导致：

- 用户无法区分 `X`/`Y` 是原始输入还是引擎回填值
- 回填后的 XML 中 `X`/`Y` 与原始输入不一致，影响后续排版分析
- `ActualWidth`/`ActualHeight` 两个独立属性不如一个 `RenderSize` 简洁

### 变更方案

| 变更前 | 变更后 |
|--------|--------|
| `ActualWidth="100"` `ActualHeight="50"` | `RenderSize="100x50"`（宽 x 高，保留两位小数） |
| 回写 `X="100"` `Y="50"` 到原始属性 | 新增 `RenderLocation="100x50"`（XxY，保留两位小数） |
| `ActualLineCount="3"` | `ActualLineCount="3"`（**不变**） |

### 优势

- `X`/`Y` 属性保持用户输入值不变，语义清晰
- `RenderSize` 一个属性替代两个属性，更简洁
- `RenderLocation` 明确标识这是引擎计算的实际布局位置
- 所有回填属性以 `Render` 前缀开头，语义统一且易于识别

---

## 影响范围分析

### 1. 数据模型层（Core）

| 文件 | 变更内容 |
|------|----------|
| `SlideMlElement.cs` | 移除 `ActualWidth`/`ActualHeight`；新增 `RenderSize`(string?)、`RenderLocation`(string?) |
| `SlideMlTextElement.cs` | `ActualLineCount` 保持不变 |
| `SlideMlPage.cs` | 无变更 |
| `SlideMlPanelElement.cs` | 无变更 |
| `SlideMlRectElement.cs` | 无变更 |
| `SlideMlImageElement.cs` | 无变更 |
| `SlideMlRenderResult.cs` | 无变更 |
| `SlideMlMeasureResult.cs` | 无变更（测量结果仍是 `MeasuredWidth`/`MeasuredHeight`，与回填无关） |
| `SlideMlRenderedMetrics.cs` | 移除 `ActualWidth`/`ActualHeight`；新增 `RenderSize`、`RenderLocation` |
| `SlideMlXmlUtilities.cs` | `FormatRenderedXml` 改为输出 `RenderSize`/`RenderLocation`，不再回写 `X`/`Y` |
| `SlideMlPoint.cs` | 无变更（纯数据点，仍被 LayoutBounds 使用） |
| `SlideMlSize.cs` | 无变更 |
| `SlideMlRect.cs` | 无变更 |

### 2. 布局引擎（Core）

| 文件 | 变更内容 |
|------|----------|
| `SlideMlLayoutEngine.cs` | 所有 `child.ActualWidth =` / `child.ActualHeight =` 改为设置 `child.RenderSize`；所有 `panel.ActualWidth =` / `panel.ActualHeight =` 改为设置 `panel.RenderSize`；移除 `SyncLayoutBoundsToXY` 方法；`FinalLayout` 不再调用 `SyncLayoutBoundsToXY`；改为在 `FinalLayout` 末尾调用 `SyncRenderLocation` 设置 `RenderLocation` |

### 3. 渲染引擎（WPF / Avalonia）

| 文件 | 变更内容 |
|------|----------|
| `WpfSlideMlRenderEngine.cs` | 无变更（渲染引擎只负责 PreMeasure 和 Render，不涉及回填属性） |
| `AvaloniaSlideRenderEngine.cs` | 无变更 |

### 4. 流式处理（Core）

| 文件 | 变更内容 |
|------|----------|
| `SlideMlStreamingMerger.cs` | 无变更（处理 XML 片段，不直接操作数据模型） |
| `SlideMlMergeState.cs` | 无变更 |
| `SlideMlFragmentExtractor.cs` | 无变更 |

### 5. 管线（Core）

| 文件 | 变更内容 |
|------|----------|
| `SlideMlRenderPipeline.cs` | 无变更（只调用 LayoutEngine 和 RenderEngine） |
| `SlideMlRenderTool.cs` | 无变更（只使用 RenderResult） |

### 6. 测试

| 文件 | 变更内容 |
|------|----------|
| `SlideLayoutEngineTests.cs` | 将 `panel.ActualWidth`/`panel.ActualHeight` 断言改为 `panel.RenderSize`/`panel.RenderLocation` |
| `SlideMlElementMeasurementsTests.cs` | 无变更 |
| `SlideMlMeasureResultTests.cs` | 无变更 |
| `SlideMlRenderedMetricsTests.cs` | 更新为测试 `RenderSize`/`RenderLocation` |
| `SlideMlXmlUtilitiesFormatRenderedXmlTests.cs` | 将 `ActualWidth`/`ActualHeight` 断言改为 `RenderSize`/`RenderLocation` |
| `SlideMlRenderPipelineBackfillTests.cs` | 更新断言 |
| `SlideMlRenderPipelineBasicTests.cs` | 更新断言 |
| `SlideMlRenderPipelineContextTests.cs` | 更新断言 |
| `SlideMlRenderPipelineEngineTests.cs` | 更新断言 |
| `SlideMlRenderPipelineWarningTests.cs` | 更新断言 |
| `SlideMlRenderPipelineErrorTests.cs` | 更新断言 |
| `SlideMlLayoutEngine*.cs` (多个) | 更新断言 |
| `SlideStreamRenderServiceTests.cs` | 更新 `ActualWidth` 引用 |
| `SlideMlIntegrationTests.cs` | 更新断言 |

### 7. 文档

| 文件 | 变更内容 |
|------|----------|
| `SlideML V2 规范文档.md` | 更新「引擎回填属性」章节和「渲染反馈」章节 |
| `SlideML V3 规范文档.md` | 更新「§24 引擎回填属性」和「§25 渲染反馈」 |
| `SlideML 流式输出规范.md` | 更新回填属性引用 |
| `SlideML 单元测试编写指南.md` | 更新所有 `ActualWidth`/`ActualHeight` 引用 |
| `SlideML 项目结构探索报告.md` | 更新属性列表 |
| `SlideML V2 实施计划.md` | 更新引用 |
| `流式生成状态延续修复计划.md` | 更新引用 |

---

## RenderSize / RenderLocation 格式说明

### RenderSize

格式：`"{Width}x{Height}"`，其中 Width 和 Height 使用 `FormatNumber` 格式化（保留两位小数，去除多余零）。

示例：
- `RenderSize="1280x720"`（Page 根元素）
- `RenderSize="340x260"`（固定尺寸 Rect）
- `RenderSize="120.5x48.3"`（测量后的文本元素）

### RenderLocation

格式：`"{X}x{Y}"`，其中 X 和 Y 使用 `FormatNumber` 格式化（保留两位小数，去除多余零）。

表示元素在**父容器内容区**中的实际布局位置（即 `LayoutBounds.X` 和 `LayoutBounds.Y` 相对于父容器内容区原点的偏移）。

Page 根元素**不需要** `RenderLocation`（因为它是根）。

示例：
- `RenderLocation="80x400"`
- `RenderLocation="216.5x120"`

---

## 实现步骤

### 步骤 1：更新规范文档

先更新 V3 和 V2 规范文档中的回填属性章节，确保文档先行。

### 步骤 2：修改 SlideMlElement 基类

```csharp
// 移除：
public double ActualWidth { get; set; }
public double ActualHeight { get; set; }

// 新增：
public string? RenderSize { get; set; }
public string? RenderLocation { get; set; }
```

### 步骤 3：修改 SlideMlLayoutEngine

- 移除 `SyncLayoutBoundsToXY` 方法及其在 `FinalLayout` 中的调用
- 新增 `SyncRenderMetadata` 方法，遍历元素树设置 `RenderSize` 和 `RenderLocation`
- 所有设置 `ActualWidth`/`ActualHeight` 的地方改为设置 `RenderSize`
- `FinalLayout` 末尾调用 `SyncRenderMetadata`

### 步骤 4：修改 SlideMlXmlUtilities.FormatRenderedXml

- 不再回写 `X`/`Y` 属性
- 改为输出 `RenderSize` 和 `RenderLocation` 属性
- Page 根元素输出 `RenderSize`（画布尺寸），不输出 `RenderLocation`

### 步骤 5：修改 SlideMlRenderedMetrics

- 移除 `ActualWidth`/`ActualHeight`
- 新增 `RenderSize`/`RenderLocation`

### 步骤 6：更新所有测试

批量更新测试文件中的断言，将 `ActualWidth`/`ActualHeight` 改为 `RenderSize`/`RenderLocation`。

### 步骤 7：更新其他文档

更新流式输出规范、单元测试编写指南、项目结构探索报告等文档。

### 步骤 8：编译验证 + 运行全量测试

---

## 风险评估

| 风险 | 等级 | 缓解措施 |
|------|------|----------|
| 测试文件众多，漏改 | 中 | 使用 grep 全量搜索，逐个文件确认 |
| 流式合并器中的回填属性处理 | 低 | 流式合并器处理 XML 字符串，不直接操作数据模型，但需确认 `SlideMlMergeState` 无影响 |
| `SlideMlRenderedMetrics` 类是否仍被使用 | 低 | 确认后决定是更新还是删除 |

---

## 待确认问题

1. **`ActualLineCount` 是否保留？** — 用户未提及，计划保留不变。
2. **`SlideMlRenderedMetrics` 类是否仍需要？** — 目前只在测试中使用，考虑是否将其也更新为 `RenderSize`/`RenderLocation`。
3. **`SlideMlElement` 基类中 `X`/`Y` 是否仍需要 `set`？** — 当前 `X`/`Y` 有 `set`，因为 `SyncLayoutBoundsToXY` 需要写入。移除后，`X`/`Y` 可以改为 `{ get; init; }`（纯输入属性），但需确认解析器和其他代码路径。

---

> **请审批此计划后再开始实施。** 如有任何调整需求，请在审批时告知。