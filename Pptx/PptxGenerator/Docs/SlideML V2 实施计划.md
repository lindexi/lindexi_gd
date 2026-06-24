# SlideML V2 完整实施计划（WPF 版）

## 架构总览

当前 WPF 版核心文件清单（路径相对于 `PptxGeneratorWpfDemo\PptxGeneratorWpfDemo\`）：

| 层次 | 文件 | 职责 |
|------|------|------|
| 数据模型 | `Core\SlideMl\SlideDocument.cs` | `SlideElement` 及其子类 |
| 解析器 | `Core\SlideMl\SlideMlParser.cs` | XML→模型，含属性校验 |
| XML 工具 | `Core\SlideMl\SlideXmlUtilities.cs` | 回填 ActualWidth/Height/LineCount |
| 渲染结果 | `Core\SlideMl\SlideRenderResult.cs` | 结果 DTO |
| 布局引擎接口 | `Core\ISlideLayoutEngine.cs` | PreLayout / FinalLayout |
| 布局引擎实现 | `Core\SlideLayoutEngine.cs` | 纯数学坐标计算 |
| 渲染引擎接口 | `Core\ISlideRenderEngine.cs` | PreMeasure / Render |
| 渲染引擎实现 | `Core\SlideRenderEngine.cs` | WPF DrawingContext 绘制 |
| 渲染流水线 | `Core\SlideRenderPipeline.cs` | Parse→PreLayout→PreMeasure→FinalLayout→Render |
| 上下文 | `Core\SlidePipelineContext.cs` | 画布尺寸 + 诊断收集 |
| 测量结果 | `Core\SlideMeasureResult.cs` + `SlideElementMeasurements.cs` | 元素尺寸测量 |
| AI 工具 | `Core\SlideMl\SlideRenderTool.cs` | render_slide + get_render_preview |
| 生成流水线 | `Core\SlideMl\Pipeline\SlideGenerationPipeline.cs` | 生成→渲染→评估 编排 |
| 提示词 | `Core\SlideMl\Prompt\SlideMlPromptProvider.cs` | 系统提示词 |

还有一份 Avalonia 版代码在 `PptxGenerator\Code\PptxGenerator\Core\SlideMl\`，本计划不修改。

**合并策略**：当前 `Core\SlideMl\SlideDocument.cs`（被 `SlideMlParser` 引用）和 WPF 项目内的类型定义实际上是同一份。本计划所有新类型和属性统一添加到 `Core\SlideMl\SlideDocument.cs`。

---

## 阶段一（原阶段三，优先）：流式布局引擎 + 单元测试 ⭐

> 目标：在 `SlideLayoutEngine` 中实现 `Layout="Horizontal"/"Vertical"` 的单向流式布局，并用 MSTest 覆盖核心布局算法。
> 
> 涉及文件：`SlideDocument.cs`、`SlideMlParser.cs`、`SlideLayoutEngine.cs`、`PptxGenerator.Tests\SlideLayoutEngineTests.cs`

### 1.1 数据模型准备

- 新增 `SlideLayoutDirection` 枚举：`Absolute`（默认）, `Horizontal`, `Vertical`
- 新增 `SlideThickness` 记录结构：`Left, Top, Right, Bottom`，支持 `"0,0,0,8"` 逗号格式解析
- `SlideElement` 基类：新增 `Margin` 属性（`SlideThickness?`）
- `SlidePanelElement`：新增 `Layout`（`SlideLayoutDirection`）、`Gap`（`double`）

### 1.2 解析器扩展

- `_panelKnownAttributes` 加入 `Layout`、`Gap`
- 所有元素的 `_*KnownAttributes` 加入 `Margin`
- `ParsePanel`：解析 `Layout`（枚举）、`Gap`
- 解析 `Margin` 逗号格式

### 1.3 流式布局核心算法

- 修改 `LayoutPanel`：
  - 若 `Layout == Absolute`：走现有逻辑（不变）
  - 若 `Layout == Horizontal`：子元素沿 X 轴依次排列，排列轴上的 X 忽略，使用 `Gap` 和 `Margin` 控制间距
  - 若 `Layout == Vertical`：子元素沿 Y 轴依次排列，排列轴上的 Y 忽略
- 新增辅助方法：`LayoutFlowChildren(List<SlideElement>, Rect, SlideLayoutDirection, double gap, ...)`
- 溢出检测：超出 Panel 尺寸产生 Warning
- 实际间距 = `max(Gap, 前元素下Margin + 后元素上Margin)`
- 跨轴方向（水平布局中的 Y）仍使用显式 Y 或 VerticalAlignment

### 1.4 双向布局（PreLayout + FinalLayout）

- `PreLayout`：流式布局的 Panel 先用子元素声明的尺寸计算排列位置
- `FinalLayout`：结合 `PreMeasure` 测量结果，重算流式布局下子元素实际位置

### 1.5 单元测试

- 测试项目：`PptxGenerator.Tests`（MSTest, net10.0-windows）
- 需要添加 `InternalsVisibleTo("PptxGenerator.Tests")` 或在 csproj 中添加
- 新增 `SlideLayoutEngineTests` 测试类，覆盖以下场景：
  - 水平布局：子元素按声明宽度依次排列，间距为 Gap
  - 垂直布局：子元素按声明高度依次排列
  - Margin 参与间距计算
  - 跨轴对齐（VerticalAlignment in Horizontal layout）
  - 溢出 Warning
  - 空子元素列表
  - 混合布局（Panel 内嵌 Panel）
  - Absolute 布局兼容性（现有行为不变）

---

## 阶段二（原阶段一）：数据模型层 + 解析器（其余 V2 数据结构和 XML 解析）

> 目标：完成剩余 V2 功能的数据模型定义和 XML 解析。解析器对未知标签/属性产生 Warning 而非崩溃。

### 2.1 新增枚举和基础类型 (`SlideDocument.cs`)

- `SlideMlTextElement` 和 `SlideMlSpan` 使用 `IsBold`（`bool?`）和 `IsItalic`（`bool?`）控制字体粗细和斜体
- `SlideCornerRadius` 记录结构：`TopLeft, TopRight, BottomRight, BottomLeft`，支持从单个值隐式转换
- `SlideGradientStop` 类：`Offset`（0~1）、`Color`（string）
- `SlideLinearGradientBrush` 类：`X1, Y1, X2, Y2`（0~1）、`Stops`（`IReadOnlyList<SlideGradientStop>`）
- `SlideShadow` 类：`OffsetX, OffsetY, Blur, Color, Opacity`
- `SlideSpan` 类：`Text, FontSize?, FontName?, Foreground?, IsBold?, IsItalic?, TextDecoration?`
### 2.2 扩展现有模型类 (`SlideDocument.cs`)

- `SlideRectElement`：
  - `CornerRadius` 改为 `SlideCornerRadius?`（兼容旧 double 解析）
  - 新增 `FillElement`（`SlideLinearGradientBrush?`）、`StrokeElement`（`SlideLinearGradientBrush?`）
  - 新增 `Shadow`（`SlideShadow?`）、`ShadowString`（`string?` 属性形式 "0 4 12 #00000033"）
  - 新增 `StrokeDashArray`（`IReadOnlyList<double>?`）
- `SlideTextElement`：
  - 新增 `IsBold`（`bool?`）
  - 新增 `IsItalic`（`bool?`）
  - 新增 `Spans`（`IReadOnlyList<SlideSpan>?`）


### 2.3 扩展解析器 (`SlideMlParser.cs`)

- 更新所有 `_*KnownAttributes` 哈希表，加入新属性名
- `ParseRect`：
  - 解析 `Shadow` 属性形式（如 `"0 4 12 #00000033"`）
  - 解析 `<Fill><LinearGradient>...</LinearGradient></Fill>` 子元素
  - 解析 `<Stroke><LinearGradient>...</LinearGradient></Stroke>` 子元素
  - 解析 `<Shadow OffsetX="..." .../>` 子元素
  - 解析 `CornerRadius` 支持逗号分隔四值（如 `"8,16,8,16"`）
  - 解析 `StrokeDashArray`（逗号分隔数值）
- `ParseTextElement`：
  - 解析 `IsBold`（布尔）
  - 解析 `IsItalic`（布尔）
  - 解析 `<Span>` 子元素
- `ParseElement`：识别新的子元素标签（`Span`、`Fill`、`Stroke`、`Shadow`、`LinearGradient`、`Stop`）
- 整体策略：未知属性/子标签产生 Warning 而非 Exception

### 2.4 扩展 XML 工具 (`SlideXmlUtilities.cs`)

- `FormatRenderedXml` 的标签白名单加入新元素名
- 不因未知标签而崩溃

---

## 阶段三（原阶段二）：渲染引擎 V2 能力

> 目标：在 `SlideRenderEngine` 中实现渐变、阴影、IsBold/IsItalic、富文本 Span、增强 CornerRadius、StrokeDashArray。

### 3.1 渐变填充渲染

- 新增 `CreateGradientBrush(SlideLinearGradientBrush, Rect)` 方法，创建 WPF `LinearGradientBrush`
- 修改 `DrawRect`：`Fill` 子元素（渐变）优先于 `Fill` 属性（纯色）；`Stroke` 同理
- 修改 `DrawPanel`：支持渐变 Background

### 3.2 阴影渲染

- 新增 `DrawShadow(DrawingContext, SlideShadow, Rect, double cornerRadius)` 方法
- 在元素下方先绘制偏移+模糊的阴影矩形（使用 `BlurBitmapEffect` 或 `RectangleGeometry` + `DrawingVisual` 模糊）
- 与元素自身的 `Opacity` 独立

### 3.3 IsBold/IsItalic 支持

- 修改 `PreMeasureText`：根据 `IsBold` 构建 WPF `FontWeight` 值，根据 `IsItalic` 构建 `FontStyle` 值
- 修改 `DrawText`：传递正确的 `FontWeight` 和 `FontStyle` 到 `FormattedText`

### 3.4 富文本 Span 渲染

- 修改 `PreMeasureText`：若 `Spans` 非空，按 Span 逐段测量，拼接 `FormattedText`
- 修改 `DrawText`：若 `Spans` 非空，逐段绘制，每段独立样式（FontSize, FontName, Foreground, IsBold, IsItalic, TextDecoration）

### 3.5 增强 CornerRadius

- 修改 `DrawRect`：支持四角独立圆角，使用 `StreamGeometry` 构建 `PathGeometry`
- 向后兼容单值圆角

### 3.6 StrokeDashArray

- 修改 `DrawRect`：解析 `Pen.DashArray` 设置虚线描边

---

## 阶段四（原阶段四）：measure_text 工具

> 目标：新增独立的 AI Tool，暴露文本测量能力给模型。

### 4.1 文本测量核心逻辑

- 在 `SlideRenderEngine` 中新增 `MeasureText(string text, string fontName, double fontSize, bool? isBold, bool? isItalic, double? maxWidth)` 方法
- 返回 `MeasureTextResult { Width, Height, LineCount, Overflow }`

### 4.2 AI Tool 封装

- 在 `SlideRenderTool` 中新增 `CreateMeasureTextTool()` 方法
- 使用 `AIFunctionFactory.Create` 注册为 `measure_text` 工具
- 参数：`text`, `fontName`, `fontSize`, `maxWidth`
- 返回 JSON 格式结果

### 4.3 流水线集成

- 在 `SlideRenderPipeline` 中暴露 `MeasureTextAsync` 公共方法
- `SlideRenderTool` 通过 `SlideRenderPipeline` 调用

### 4.4 提示词更新

- 更新 `SlideMlPromptProvider.BuildSystemPrompt()`：
  - 添加 V2 新标签/属性说明（流式布局、渐变、阴影、IsBold/IsItalic、Span、Margin、增强 CornerRadius、StrokeDashArray）
  - 添加 `measure_text` 工具的使用指引

---

## 阶段五（原阶段六）：高级功能

> 目标：图片 SubAgent/RAG 集成、多模态截图反馈增强。

### 6.1 图片检索 SubAgent

（若 V2 中不做，可标记为后续）

### 6.2 多模态截图反馈增强

- `get_render_preview` 工具已存在，确认其工作正常
- 评估器可调用此工具获取截图进行视觉评估

---

## 实施顺序

1. **阶段一**（流式布局+单元测试）：Panel Layout="Horizontal"/"Vertical" 工作，MSTest 覆盖
2. **阶段二**（其余数据模型+解析器）：渐变、阴影、IsBold/IsItalic、Span 等结构落地，解析器可无崩溃解析 V2 XML
3. **阶段三**（渲染引擎）：渐变、阴影、IsBold/IsItalic、Span、CornerRadius、StrokeDashArray 可视
4. **阶段四**（measure_text 工具）：模型可调用文本测量
5. **阶段五**（高级功能）：图片检索、多模态增强

每个阶段是可独立交付的增量，阶段一完成后可逐步测试验证。
