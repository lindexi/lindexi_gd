# SlideML V2 实施计划

## 架构总览

当前项目结构（路径相对于 `Pptx\PptxGenerator\Code\`）：

| 层次 | 文件 | 职责 |
|------|------|------|
| 数据模型 | `PptxGenerator.Core\Models\SlideDocument.cs` | `SlideMlElement` 及其子类、画刷接口、辅助类型 |
| 解析器 | `PptxGenerator.Core\Models\SlideMlParser.cs` | XML→模型，含属性校验、未知属性 Warning |
| 布局引擎接口 | `PptxGenerator.Core\Rendering\ISlideLayoutEngine.cs` | PreLayout / FinalLayout 接口 |
| 布局引擎实现 | `PptxGenerator.Core\Rendering\SlideLayoutEngine.cs` | 纯数学坐标计算，含流式布局 |
| 渲染引擎接口 | `PptxGenerator.Core\Rendering\ISlideMlRenderEngine.cs` | PreMeasure / Render 接口 |
| 渲染流水线接口 | `PptxGenerator.Core\Rendering\ISlideRenderPipeline.cs` | Parse→PreLayout→PreMeasure→FinalLayout→Render |
| 上下文 | `PptxGenerator.Core\Models\SlideMlPipelineContext.cs` | 画布尺寸 + 诊断收集 |
| 测量结果 | `PptxGenerator.Core\Models\SlideDocument.cs`（含 `SlideMlElementMeasurements`、`SlideMlMeasureResult`） | 元素尺寸测量 |
| AI 工具 | `PptxGenerator.Core\Pipeline\SlideRenderTool.cs` | render_slide + get_render_preview |
| 生成流水线 | `PptxGenerator.Core\Pipeline\SlideGenerationPipeline.cs` | 生成→渲染→评估 编排 |
| 提示词 | `PptxGenerator.Core\Prompt\SlideMlPromptProvider.cs` | 系统提示词（当前仍为 V1 版本） |
| 提示词接口 | `PptxGenerator.Core\Prompt\IPromptProvider.cs` | ISlideMlPromptProvider 接口 |
| 评估器 | `PptxGenerator.Core\Evaluation\` | 幻灯片评估相关 |
| WPF 渲染引擎 | `PptxGeneratorWpfDemo\Core\SlideMl\Rendering\SlideRenderEngine.cs` | WPF DrawingContext 绘制（799 行） |
| WPF 渲染流水线 | `PptxGeneratorWpfDemo\Core\SlideMl\Rendering\SlideRenderPipeline.cs` | WPF 版流水线实现 |
| Avalonia 渲染引擎 | `PptxGeneratorAvaloniaDemo\Core\SlideMl\Rendering\AvaloniaSlideRenderEngine.cs` | Avalonia DrawingContext 绘制（394 行） |
| Avalonia 渲染流水线 | `PptxGeneratorAvaloniaDemo\Core\SlideMl\Rendering\SlideRenderPipeline.cs` | Avalonia 版流水线实现 |
| 单元测试 | `PptxGenerator.Tests\SlideLayoutEngineTests.cs` | MSTest，覆盖流式布局核心场景（9 个测试） |

---

## 阶段一：流式布局引擎 + 单元测试 ✅ 已完成

> 涉及文件：`SlideDocument.cs`、`SlideMlParser.cs`、`SlideLayoutEngine.cs`、`PptxGenerator.Tests\SlideLayoutEngineTests.cs`

### 已实现内容

**数据模型（`SlideDocument.cs`）：**
- `SlideMlLayoutDirection` 枚举：`Absolute`（默认）、`Horizontal`、`Vertical`
- `SlideMlThickness` 记录结构：`Left, Top, Right, Bottom`，支持 `"0,0,0,8"` 逗号格式解析
- `SlideMlElement` 基类：含 `Margin`（`SlideMlThickness?`）属性
- `SlideMlPanelElement`：含 `Layout`（`SlideMlLayoutDirection`）、`Gap`（`double`）属性

**解析器扩展（`SlideMlParser.cs`）：**
- `_panelKnownAttributes` 加入 `Layout`、`Gap`、`Margin`
- 所有元素的 `_*KnownAttributes` 加入 `Margin`
- `ParsePanel`：解析 `Layout`（枚举）、`Gap`（double）、`Margin`（`SlideMlThickness`）
- 解析 `Margin` 逗号格式（1~4 个值）

**流式布局核心算法（`SlideLayoutEngine.cs`）：**
- `PreLayout`：流式布局的 Panel 先用子元素声明的尺寸计算排列位置
- `FinalLayout`：结合 `PreMeasure` 测量结果，重算流式布局下子元素实际位置
- `LayoutPanel`：若 `Layout == Absolute` 走原有逻辑；若 `Horizontal`/`Vertical` 走流式排列
- 排列轴上的 X/Y 被忽略，由引擎自动计算
- 实际间距 = `max(Gap, 前元素下Margin + 后元素上Margin)`
- 跨轴方向（水平布局中的 Y）仍使用显式 Y 或 VerticalAlignment
- 溢出检测：超出 Panel 尺寸产生 Warning
- Panel 不设置 Width/Height 时自动包裹子元素（ActualWidth/ActualHeight）

**单元测试（`SlideLayoutEngineTests.cs`，9 个测试用例）：**
- 水平布局：子元素按声明宽度依次排列，间距为 Gap
- 垂直布局：子元素按声明高度依次排列
- Margin 参与间距计算
- 跨轴对齐（VerticalAlignment in Horizontal layout）
- 溢出 Warning
- 空子元素列表
- 混合布局（Panel 内嵌 Panel）
- Absolute 布局兼容性（现有行为不变）
- FinalLayout 使用测量尺寸

---

## 阶段二：数据模型层 + 解析器（V2 数据结构和 XML 解析）✅ 已完成

> 涉及文件：`SlideDocument.cs`、`SlideMlParser.cs`

### 已实现内容

**新增枚举和基础类型（`SlideDocument.cs`）：**
- `SlideMlCornerRadius` 记录结构：`TopLeft, TopRight, BottomRight, BottomLeft`，支持从单个值隐式转换（代码保留四值解析能力，但规范文档只对外声明单值）
- `SlideMlGradientStop` 类：`Offset`（0~1）、`Color`（string）
- `SlideMlLinearGradientBrush` 类：`X1, Y1, X2, Y2`（0~1）、`Stops`（`IReadOnlyList<SlideMlGradientStop>`）
- `SlideMlSolidColorBrush` 类：`Color`（string）
- `ISlideMlBrush` 接口：统一纯色和渐变画刷
- `SlideMlShadow` 类：`OffsetX, OffsetY, Blur, Color, Opacity`，支持 `Parse(string?)` 解析属性形式
- `SlideMlSpan` 类：`Text, FontSize?, FontName?, Foreground?, IsBold?, IsItalic?, TextDecoration?`

**扩展现有模型类（`SlideDocument.cs`）：**
- `SlideMlRectElement`：
  - `CornerRadius`（`SlideMlCornerRadius?`，兼容旧 double 解析）
  - `Fill`（`ISlideMlBrush?`）、`Stroke`（`ISlideMlBrush?`）
  - `Shadow`（`SlideMlShadow?`）、`ShadowString`（`string?` 属性形式）
  - `StrokeDashArray`（`IReadOnlyList<double>?`）
- `SlideMlTextElement`：
  - `IsBold`（`bool?`）
  - `IsItalic`（`bool?`）
  - `Spans`（`IReadOnlyList<SlideMlSpan>?`）
  - `Style`（`string?`，引用 Page.Styles 中定义的 TextStyle）

**扩展解析器（`SlideMlParser.cs`，702 行）：**
- 更新所有 `_*KnownAttributes` 哈希表，加入新属性名
- `ParseRect`：
  - 解析 `Shadow` 属性形式（如 `"0 4 12 #00000033"`）
  - 解析 `<Fill><LinearGradient>...</LinearGradient></Fill>` 子元素
  - 解析 `<Stroke><LinearGradient>...</LinearGradient></Stroke>` 子元素
  - 解析 `<Shadow OffsetX="..." .../>` 子元素（OffsetX 默认 0，OffsetY 默认 4，Blur 默认 12，Color 默认 `#00000033`，Opacity 默认 1）
  - 解析 `CornerRadius` 支持逗号分隔四值（如 `"8,16,8,16"`）和单值（如 `"8"`）
  - 解析 `StrokeDashArray`（逗号分隔数值）
- `ParseTextElement`：
  - 解析 `IsBold`（布尔）
  - 解析 `IsItalic`（布尔）
  - 解析 `<Span>` 子元素（`Text` 属性必填）
- `ParseBackground`：Panel 的 `Background` 也支持 `<Fill>` 渐变子元素
- `ParseFill`/`ParseStroke`：统一通过 `ParseGradientChild` 方法处理渐变子元素
- `ParseLinearGradient`：解析 `<LinearGradient>` 元素的 X1/Y1/X2/Y2 和 `<Stop>` 子元素
- `ParseShadowElement`：解析 `<Shadow>` 子元素的精细属性
- 未知属性/子标签产生 Warning 而非 Exception

**Page.Styles 支持（`SlideMlParser.cs`）：**
- 解析 `<Page.Styles>` 子元素，含 `<TextStyle>` 定义
- TextStyle 含 `Id, FontSize, IsBold, Foreground, FontName, TextAlignment`
- TextElement 可通过 `Style` 属性引用 TextStyle Id

---

## 阶段三：渲染引擎 V2 能力 ✅ WPF 版已完成 / ⚠️ Avalonia 版部分完成

> 涉及文件：`PptxGeneratorWpfDemo\Core\SlideMl\Rendering\SlideRenderEngine.cs`（799 行）、`PptxGeneratorAvaloniaDemo\Core\SlideMl\Rendering\AvaloniaSlideRenderEngine.cs`（394 行）

### WPF 渲染引擎（✅ 已完成）

已实现以下 V2 渲染能力：

- **渐变填充渲染**：`CreateWpfBrush(ISlideMlBrush?)` 方法，通过模式匹配分派 `SlideMlSolidColorBrush` 和 `SlideMlLinearGradientBrush`；`DrawRect` 和 `DrawPanel` 均支持渐变
- **阴影渲染**：`DrawShadow` 方法，在元素下方绘制偏移+模糊的阴影矩形
- **IsBold/IsItalic 支持**：`PreMeasureText` 中根据 `IsBold` 构建 `FontWeight`，根据 `IsItalic` 构建 `FontStyle`
- **富文本 Span 渲染**：`DrawText` 支持逐段绘制 Span，每段独立样式
- **CornerRadius**：`DrawRect` 支持圆角矩形（单值，四角统一）
- **StrokeDashArray**：`DrawRect` 支持虚线描边

### Avalonia 渲染引擎（⚠️ 部分完成）

已实现：
- **渐变画刷**：`CreateGradientBrush(SlideMlLinearGradientBrush)` 方法，创建 Avalonia `LinearGradientBrush`
- **IsBold / IsItalic**：`PreMeasureText` 中构建 `FontWeight` 和 `FontStyle`
- **统一画刷分派**：`CreateAvaloniaBrush(ISlideMlBrush?)` 方法，通过模式匹配分派

待实现：

| 功能 | 状态 | 说明 |
|------|------|------|
| CornerRadius | ❌ | `DrawRect` 未处理圆角，当前使用 `dc.FillRectangle` / `dc.DrawRectangle` 绘制直角矩形 |
| Shadow 阴影 | ❌ | `DrawRect` 未处理阴影绘制 |
| StrokeDashArray 虚线 | ❌ | `DrawRect` 未处理虚线描边 |
| Span 富文本 | ❌ | `DrawText` 仅使用 TextLayout 渲染纯文本，未支持 Span 逐段样式混排 |

---

## 阶段四：measure_text 工具 + 提示词更新 ❌ 未实现

> 涉及文件：`SlideRenderTool.cs`、`SlideMlPromptProvider.cs`、`SlideRenderPipeline.cs`

### 4.1 提示词更新（最高优先级）

**当前状态**：`SlideMlPromptProvider.BuildDefaultSystemPrompt()` 仍是 V1 版本，完全缺失 V2 新功能说明。

**待实现内容：**
- 在系统提示词中加入 V2 新标签/属性说明：
  - 流式布局：`Layout`（Horizontal/Vertical）、`Gap`、`Margin`
  - 渐变：`<Fill><LinearGradient><Stop/></LinearGradient></Fill>` 子元素形式
  - 阴影：`Shadow` 属性形式和 `<Shadow>` 子元素形式
  - 字体控制：`IsBold`、`IsItalic`
  - 富文本：`<Span>` 子元素
  - 圆角：`CornerRadius`（仅单值）
  - 虚线：`StrokeDashArray`
  - Page.Styles：TextStyle 定义和引用
- 更新排版规则，说明流式布局的行为和约束
- 更新标签与属性表，确保与规范文档一致

### 4.2 measure_text 工具

**待实现内容：**
- 在渲染引擎中新增 `MeasureText(string text, string fontName, double fontSize, bool? isBold, bool? isItalic, double? maxWidth)` 方法
- 返回 `MeasureTextResult { Width, Height, LineCount, Overflow }`
- 在 `SlideRenderTool` 中新增 `CreateMeasureTextTool()` 方法
- 使用 `AIFunctionFactory.Create` 注册为 `measure_text` 工具
- 参数：`text`, `fontName`, `fontSize`, `maxWidth`
- 返回 JSON 格式结果
- 在 `SlideRenderPipeline` 中暴露 `MeasureTextAsync` 公共方法

### 4.3 流水线集成

- `SlideRenderTool` 通过 `SlideRenderPipeline` 调用 `MeasureTextAsync`
- 确保工具在 AI 对话中可被模型调用

---

## 阶段五：高级功能 ❌ 未实现

### 5.1 图片检索 SubAgent

V1 中 `Source` 只是占位 ID。计划实现搜索图片 SubAgent，模型可检索并引用图片 ID。

### 5.2 多模态截图反馈增强

`get_render_preview` 工具已存在，确认其工作正常。评估器可调用此工具获取截图进行视觉评估。

---

## 实施顺序

1. **阶段四 — 提示词更新**（最高优先级）：当前提示词仍是 V1 版本，模型不知道 V2 能力存在，这是阻碍 V2 功能实际使用的最大瓶颈
2. **阶段四 — measure_text 工具**：暴露文本测量能力，减少排版试错
3. **阶段三 — Avalonia 渲染引擎补全**：补全 CornerRadius/Shadow/StrokeDashArray/Span
4. **阶段五 — 图片检索 SubAgent**：实现图片资源检索能力

每个阶段是可独立交付的增量。阶段一~三（WPF 版）已完成，可逐步测试验证。
