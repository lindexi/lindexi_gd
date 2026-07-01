# SlideML 项目结构探索报告

## 1. 项目概览

| 项目 | 路径 | TFM | 说明 |
|------|------|-----|------|
| PptxGenerator.Core | `Code/PptxGenerator.Core/PptxGenerator.Core.csproj` | 多目标 | 核心库，包含 SlideML 全部模型、解析器、布局引擎、渲染管道 |
| PptxGenerator.Tests | `Code/PptxGenerator.Tests/PptxGenerator.Tests.csproj` | net10.0-windows | 测试项目，使用 **MSTest 4.0.2** |
| PptxGeneratorWpfDemo | `Code/PptxGeneratorWpfDemo/` | net8.0-windows | WPF 演示 |
| PptxGeneratorAvaloniaDemo | `Code/PptxGeneratorAvaloniaDemo/` | 多目标 | Avalonia 演示 |

**测试框架**: MSTest，使用 `[TestClass]` / `[TestMethod]` / `[TestInitialize]` 特性。
命名规范：`方法名_场景_预期行为`，如 `HorizontalLayout_ChildrenPlacedSequentially`。

## 2. 核心数据模型

### 2.1 元素基类 — `SlideMlElement`

文件：`Models/SlideDocuments/SlideMlElement.cs`

```csharp
public abstract class SlideMlElement
{
    public string Id { get; init; }
    public double? X { get; init; }       // 水平位置（null 表示未指定）
    public double? Y { get; init; }       // 垂直位置
    public double? Width { get; init; }   // 宽度（null 表示自动）
    public double? Height { get; init; }  // 高度
    public SlideMlHorizontalAlignment? HorizontalAlignment { get; init; }
    public SlideMlVerticalAlignment? VerticalAlignment { get; init; }
    public double Opacity { get; init; } = 1;
    public SlideMlThickness? Margin { get; init; }

    // 布局引擎设置
    public SlideMlRect LocalBounds { get; set; }    // 元素自身坐标系边界
    public SlideMlRect LayoutBounds { get; set; }   // 父容器中的布局边界
    public string? RenderSize { get; set; }          // 渲染后的实际尺寸 "宽x高"
    public string? RenderLocation { get; set; }      // 渲染后的实际位置 "XxY"
}
```

### 2.2 Page — `SlideMlPage`

文件：`Models/SlideDocuments/SlideMlPage.cs`

```csharp
public sealed class SlideMlPage
{
    public string Background { get; init; } = "#FFFFFF";
    public List<SlideMlElement> Children { get; } = [];
    public IReadOnlyList<SlideMlTextStyle>? Styles { get; init; }
    public SlideMlRect LayoutBounds { get; set; }  // 默认 (0,0,1280,720)
}
```

### 2.3 Panel — `SlideMlPanelElement`

文件：`Models/SlideDocuments/SlideMlPanelElement.cs`

```csharp
public sealed class SlideMlPanelElement : SlideMlElement
{
    public double Padding { get; init; }              // 默认 0
    public ISlideMlBrush? Background { get; init; }    // 纯色或渐变
    public SlideMlLayoutDirection Layout { get; init; } // 默认 Absolute
    public double Gap { get; init; }                   // 流式布局间距，默认 0
    public List<SlideMlElement> Children { get; } = [];
}
```

### 2.4 Rect — `SlideMlRectElement`

文件：`Models/SlideDocuments/SlideMlRectElement.cs`

```csharp
public sealed class SlideMlRectElement : SlideMlElement
{
    public ISlideMlBrush? Fill { get; init; }             // 填充画刷
    public ISlideMlBrush? Stroke { get; init; }           // 描边画刷
    public double StrokeThickness { get; init; }           // 默认 0
    public SlideMlCornerRadius? CornerRadius { get; init; }// 圆角
    public SlideMlShadow? Shadow { get; init; }            // 阴影对象
    public string? ShadowString { get; init; }             // 阴影原始字符串
    public IReadOnlyList<double>? StrokeDashArray { get; init; } // 虚线
}
```

### 2.5 TextElement — `SlideMlTextElement`

文件：`Models/SlideDocuments/SlideMlTextElement.cs`

```csharp
public sealed class SlideMlTextElement : SlideMlElement
{
    public string Text { get; init; } = "";
    public string FontName { get; init; } = "Microsoft YaHei";
    public double FontSize { get; init; } = 16;
    public string Foreground { get; init; } = "#000000";
    public SlideMlTextAlignment TextAlignment { get; init; } = Left;
    public int ActualLineCount { get; set; }    // 渲染引擎回填
    public bool? IsBold { get; init; }
    public bool? IsItalic { get; init; }
    public IReadOnlyList<SlideMlSpan>? Spans { get; init; }
    public string? Style { get; init; }          // 引用 TextStyle
}
```

### 2.6 Image — `SlideMlImageElement`

文件：`Models/SlideDocuments/SlideMlImageElement.cs`

```csharp
public sealed class SlideMlImageElement : SlideMlElement
{
    public string Source { get; init; } = "";
    public SlideMlImageStretch Stretch { get; init; } = Uniform;
}
```

### 2.7 Span — `SlideMlSpan`

文件：`Models/SlideDocuments/SlideMlSpan.cs`

```csharp
public sealed class SlideMlSpan
{
    public string Text { get; init; }
    public double? FontSize { get; init; }
    public string? FontName { get; init; }
    public string? Foreground { get; init; }
    public bool? IsBold { get; init; }
    public bool? IsItalic { get; init; }
    public string? TextDecoration { get; init; }  // "Underline" 等
}
```

### 2.8 其他模型

| 类型 | 文件 | 说明 |
|------|------|------|
| `SlideMlCornerRadius` | `SlideMlCornerRadius.cs` | 四角独立圆角，支持 `Parse(string)` 1~4 值 |
| `SlideMlThickness` | `SlideMlThickness.cs` | 四边间距（Margin），支持 `Parse(string)` 1~4 值 |
| `SlideMlShadow` | `SlideMlShadow.cs` | 阴影效果，支持 `Parse(string)` 如 `"0 4 12 #00000033"` |
| `SlideMlLinearGradientBrush` | `SlideMlLinearGradientBrush.cs` | 线性渐变，X1/Y1/X2/Y2 (0~1)，Stops 列表 |
| `SlideMlSolidColorBrush` | `SlideMlSolidColorBrush.cs` | 纯色画刷，Color 属性 |
| `SlideMlGradientStop` | `SlideMlGradientStop.cs` | 渐变停止点，Offset (0~1) + Color |
| `ISlideMlBrush` | `ISlideMlBrush.cs` | 画刷抽象接口（标记接口） |
| `SlideMlTextStyle` | `SlideMlTextStyle.cs` | 文本样式定义，Id/FontSize/IsBold/Foreground/FontName/TextAlignment |
| `SlideMlRect` | `SlideMlRect.cs` | 纯数据矩形 (X,Y,Width,Height)，含 Right/Bottom/Center 属性 |
| `SlideMlPoint` | `SlideMlPoint.cs` | 纯数据点 (X,Y) |
| `SlideMlSize` | `SlideMlSize.cs` | 纯数据尺寸 (Width,Height) |

### 2.9 枚举

| 枚举 | 值 |
|------|----|
| `SlideMlLayoutDirection` | `Absolute`, `Horizontal`, `Vertical` |
| `SlideMlHorizontalAlignment` | `Left`, `Center`, `Right` |
| `SlideMlVerticalAlignment` | `Top`, `Center`, `Bottom` |
| `SlideMlTextAlignment` | `Left`, `Center`, `Right`, `Justify` |
| `SlideMlImageStretch` | `None`, `Fill`, `Uniform`, `UniformToFill` |

## 3. 解析器 — `SlideMlParser`

文件：`Models/SlideMlParser.cs`（704 行）

```csharp
public sealed class SlideMlParser
{
    public SlideMlPage Parse(string xml, SlideMlPipelineContext context);
}
```

**关键逻辑**：
- 使用 `XDocument.Parse` 解析 XML
- 根元素必须是 `Page`，否则抛 `SlideMlRootElementException`
- 未指定 Id 的元素自动分配 `elem_NNN`（递增计数器）
- 已知属性白名单校验：未知属性产生 Warning
- 已知标签白名单校验：未知标签产生 Warning 并跳过
- 支持解析子元素：`<Fill>` / `<Stroke>`（含 `<LinearGradient>` + `<Stop>`）、`<Shadow>`、`<Span>`
- 渐变子元素优先于同属性（如 `<Fill>` 优先于 `Fill` 属性）
- 必填校验：TextElement 必须有 Text 或 Span，Image 必须有 Source
- 数值解析使用 `CultureInfo.InvariantCulture`

**已知属性白名单**：
- Page: `Background`
- Panel: `Id, X, Y, Width, Height, HorizontalAlignment, VerticalAlignment, Opacity, Padding, Background, Layout, Gap, Margin`
- Rect: `Id, X, Y, Width, Height, HorizontalAlignment, VerticalAlignment, Opacity, Fill, Stroke, StrokeThickness, CornerRadius, Margin, Shadow, StrokeDashArray`
- TextElement: `Id, X, Y, Width, Height, HorizontalAlignment, VerticalAlignment, Opacity, Text, FontName, FontSize, Foreground, TextAlignment, Margin, IsBold, IsItalic, Style`
- Image: `Id, X, Y, Width, Height, HorizontalAlignment, VerticalAlignment, Opacity, Source, Stretch, Margin`

## 4. 布局引擎 — `SlideMlLayoutEngine`

文件：`Rendering/SlideMlLayoutEngine.cs`（462 行）

### 4.1 接口

```csharp
public interface ISlideMlLayoutEngine
{
    void PreLayout(SlideMlPage page, SlideMlPipelineContext context);
    void FinalLayout(SlideMlPage page, SlideMlPipelineContext context, SlideMlElementMeasurements measurements);
}
```

### 4.2 PreLayout 逻辑
- 使用声明的 Width/Height 进行布局
- TextElement 未声明尺寸时默认 (0, 0)
- Image 未声明尺寸时默认 (240, 180)
- Rect 未声明尺寸时默认 (0, 0)
- 设置 `page.LayoutBounds = (0, 0, CanvasWidth, CanvasHeight)`

### 4.3 FinalLayout 逻辑
- 使用 PreMeasure 产出的测量值（`SlideMlElementMeasurements`）进行布局
- 文本元素的 `ActualLineCount` 从测量结果回填
- 文本溢出容器高度时产生 Warning

### 4.4 布局算法关键逻辑

**绝对定位 Panel** (`LayoutAbsolutePanel`)：
1. 子元素按各自 X/Y 定位（相对于 Panel 内容区原点 + Padding）
2. 先做一遍临时布局计算内容尺寸
3. Panel 未指定 Width/Height 时自动包裹子元素（contentRight + Padding×2）
4. 再用最终尺寸做一遍布局
5. `ResolveOrigin` 处理 X/Y 优先 vs Alignment

**流式布局 Panel** (`LayoutFlowPanel`)：
1. 子元素沿排列轴依次排列
2. 间距 = `max(Gap, 前元素 trailingMargin + 后元素 leadingMargin)`
3. 跨轴方向使用 `VerticalAlignment`/`HorizontalAlignment` 或声明的 X/Y
4. Panel 未指定尺寸时自动包裹
5. 溢出固定宽度/高度时产生 Warning
6. 嵌套 Panel 会递归布局

**ResolveOrigin（坐标解析）**：
- 有显式 X/Y → `parentOrigin + offset`
- 否则按 Alignment：
  - `Center` → `parentOrigin + max(0, (parentSize - elementSize) / 2)`
  - `Right/Bottom` → `parentOrigin + max(0, parentSize - elementSize)`
  - `Left/Top`（默认） → `parentOrigin`

**ValidateBounds（边界校验）**：
- 右边界 > CanvasWidth → Warning
- 下边界 > CanvasHeight → Warning
- 左边界 < 0 → Warning
- 上边界 < 0 → Warning
- `clipToParent` 且元素超出父容器 → Warning

## 5. 渲染引擎 — `ISlideMlRenderEngine`

文件：`Rendering/ISlideMlRenderEngine.cs`

```csharp
public interface ISlideMlRenderEngine
{
    SlideMlElementMeasurements PreMeasure(SlideMlPage page, SlideMlPipelineContext context);
    IPreviewImage Render(SlideMlPage page, SlideMlPipelineContext context);
    IPreviewImage RenderErrorPreview(string message, SlideMlPipelineContext context);
}
```

- `PreMeasure`：框架相关测量（文本度量、图片加载），返回 `SlideMlElementMeasurements`
- `Render`：执行最终渲染，返回 `IPreviewImage`
- `RenderErrorPreview`：渲染错误预览图

### 5.1 测量结果

```csharp
public sealed class SlideMlElementMeasurements
{
    // 以元素 Id 为键的字典
    public bool TryGetValue(string elementId, out SlideMlMeasureResult result);
    public SlideMlMeasureResult? Find(string elementId);
}

public sealed class SlideMlMeasureResult
{
    public double MeasuredWidth { get; init; }
    public double MeasuredHeight { get; init; }
    public int? ActualLineCount { get; init; }  // 仅文本
}
```

## 6. 管道上下文与结果

### 6.1 `SlideMlPipelineContext`

文件：`Models/SlideMlPipelineContext.cs`

```csharp
public sealed class SlideMlPipelineContext
{
    public const int DefaultCanvasWidth = 1280;
    public const int DefaultCanvasHeight = 720;

    public int CanvasWidth { get; }    // 默认 1280
    public int CanvasHeight { get; }   // 默认 720
    public IReadOnlyList<string> Warnings { get; }
    public IReadOnlyList<string> Errors { get; }

    public void AddWarning(string message);
    public void AddWarnings(IEnumerable<string> messages);
    public void AddError(string message);
    public void AddErrors(IEnumerable<string> messages);
    public void Reset();  // 清空 warnings 和 errors
}
```

### 6.2 `SlideMlRenderResult`

文件：`Models/SlideMlRenderResult.cs`

```csharp
public sealed class SlideMlRenderResult
{
    public string InputXml { get; init; }
    public string OutputXml { get; init; }    // 回填后的 XML
    public IReadOnlyList<string> Warnings { get; init; }
    public IReadOnlyList<string> Errors { get; init; }
    public IPreviewImage? PreviewImage { get; init; }
}
```

## 7. 渲染管道 — `SlideMlRenderPipeline`

文件：`Rendering/SlideMlRenderPipeline.cs`

```csharp
public sealed class SlideMlRenderPipeline : ISlideMlRenderPipeline
{
    public SlideMlRenderPipeline(
        ISlideMlLayoutEngine layoutEngine,
        ISlideMlRenderEngine renderEngine,
        IMainThreadDispatcher dispatcher,
        SlideMlPipelineContext? context = null);

    public SlideMlPipelineContext Context { get; }

    public async Task<SlideMlRenderResult> RenderAsync(
        string slideXml, CancellationToken cancellationToken = default);
}
```

**RenderAsync 流程**：
1. 校验输入非空
2. `ExtractXml` + `NormalizeXml` 预处理
3. `context.Reset()` 清空诊断
4. `parser.Parse(xml, context)` → SlideMlPage
5. `layoutEngine.PreLayout(page, context)` — 使用声明尺寸
6. `dispatcher.InvokeAsync(() => { PreMeasure → FinalLayout → Render })` — 在 UI 线程
7. `FormatRenderedXml(xml, page, context)` — 回填 ActualWidth/ActualHeight/ActualLineCount
8. 返回 SlideMlRenderResult
9. 解析异常时：渲染错误预览图，返回包含 Warning 的结果

## 8. 工具类

### 8.1 `SlideMlXmlUtilities`

文件：`Models/SlideMlXmlUtilities.cs`

| 方法 | 说明 |
|------|------|
| `ExtractXml(string text)` | 从文本中提取 XML（支持 `<?xml` 或 `<Page` 开头） |
| `NormalizeXml(string xml)` | 规范化 XML 格式 |
| `FormatRenderedXml(string xml, SlideMlPage page, SlideMlPipelineContext context)` | 回填 ActualWidth/ActualHeight/ActualLineCount 到 XML |
| `FormatNumber(double value)` | 格式化数字（保留两位小数去零） |

### 8.2 `IMainThreadDispatcher`

文件：`../SemanticKernelLib/.../AgentLib/IMainThreadDispatcher.cs`（注意：此接口来自外部库 AgentLib）

```csharp
public interface IMainThreadDispatcher
{
    Task InvokeAsync(Func<Task> action);
    Task<T> InvokeAsync<T>(Func<Task<T>> action);
    bool CheckAccess();
}
```

### 8.3 `IPreviewImage`

```csharp
public interface IPreviewImage
{
    void Save(string filePath);
    void Save(Stream stream);
}
```

## 9. 异常类型

文件：`Models/SlideMlParseException.cs`

| 异常类 | 说明 |
|--------|------|
| `SlideMlParseException` (abstract) | 基类 |
| `SlideMlRootElementException` | 根元素不是 Page 或为空 |
| `SlideMlUnsupportedElementException` | 未知元素类型（含 TagName 属性） |
| `SlideMlRequiredAttributeMissingException` | 必填属性缺失（含 ElementId、AttributeName） |
| `SlideMlAttributeFormatException` | 属性值格式错误（含 ElementId、AttributeName、RawValue） |

## 10. 现有测试

### 测试文件

| 文件 | 说明 |
|------|------|
| `SlideLayoutEngineTests.cs` | 布局引擎直接测试（303 行） |
| `Evaluation/EvaluationModelsTests.cs` | 评估模型测试 |
| `Pipeline/IterationPipelineTests.cs` | 迭代管道测试 |
| `Prompt/SlideMlPromptProviderTests.cs` | Prompt 提供器测试 |

### 现有布局引擎测试覆盖

- `HorizontalLayout_ChildrenPlacedSequentially` — 水平排列 + Gap
- `VerticalLayout_ChildrenPlacedSequentially` — 垂直排列 + Gap
- `HorizontalLayout_MarginAffectsSpacing` — Margin 影响间距
- `HorizontalLayout_CrossAxisAlignment_Respected` — 跨轴对齐
- `FlowLayout_EmptyChildren_DoesNotCrash` — 空子元素
- `AbsoluteLayout_BehaviorUnchanged` — 绝对定位
- `HorizontalLayout_WithPadding_OffsetsContent` — Padding 偏移
- `NestedFlowPanels_LayoutCorrectly` — 嵌套流式布局
- `FlowLayout_Overflow_GeneratesWarning` — 溢出警告
- `FinalLayout_UsesMeasuredSizes` — FinalLayout 使用测量值

### Fake 实现状态

**目前项目中没有 Fake 渲染引擎或 Fake 主线程调度器的实现。** 集成测试需要自行创建这些 Fake 实现。

## 11. 关键接口与类型汇总表

| 类型 | 文件路径 | 说明 |
|------|----------|------|
| `SlideMlPage` | `Models/SlideDocuments/SlideMlPage.cs` | 页面根元素 |
| `SlideMlElement` | `Models/SlideDocuments/SlideMlElement.cs` | 元素基类 |
| `SlideMlPanelElement` | `Models/SlideDocuments/SlideMlPanelElement.cs` | Panel 容器 |
| `SlideMlRectElement` | `Models/SlideDocuments/SlideMlRectElement.cs` | Rect 矩形 |
| `SlideMlTextElement` | `Models/SlideDocuments/SlideMlTextElement.cs` | TextElement 文本 |
| `SlideMlImageElement` | `Models/SlideDocuments/SlideMlImageElement.cs` | Image 图片 |
| `SlideMlSpan` | `Models/SlideDocuments/SlideMlSpan.cs` | 富文本片段 |
| `SlideMlTextStyle` | `Models/SlideDocuments/SlideMlTextStyle.cs` | 文本样式 |
| `SlideMlCornerRadius` | `Models/SlideDocuments/SlideMlCornerRadius.cs` | 圆角（四角独立） |
| `SlideMlThickness` | `Models/SlideDocuments/SlideMlThickness.cs` | 间距（四边） |
| `SlideMlShadow` | `Models/SlideDocuments/SlideMlShadow.cs` | 阴影 |
| `SlideMlLinearGradientBrush` | `Models/SlideDocuments/SlideMlLinearGradientBrush.cs` | 线性渐变 |
| `SlideMlSolidColorBrush` | `Models/SlideDocuments/SlideMlSolidColorBrush.cs` | 纯色画刷 |
| `SlideMlGradientStop` | `Models/SlideDocuments/SlideMlGradientStop.cs` | 渐变停止点 |
| `ISlideMlBrush` | `Models/SlideDocuments/ISlideMlBrush.cs` | 画刷接口 |
| `SlideMlParser` | `Models/SlideMlParser.cs` | XML 解析器 |
| `SlideMlPipelineContext` | `Models/SlideMlPipelineContext.cs` | 管道上下文 |
| `SlideMlRenderResult` | `Models/SlideMlRenderResult.cs` | 渲染结果 |
| `SlideMlElementMeasurements` | `Models/SlideMlElementMeasurements.cs` | 测量结果集合 |
| `SlideMlMeasureResult` | `Models/SlideMlMeasureResult.cs` | 单个测量结果 |
| `SlideMlRenderedMetrics` | `Models/SlideXmlUtilities.cs` | 回填度量信息 |
| `SlideMlXmlUtilities` | `Models/SlideMlXmlUtilities.cs` | XML 工具类 |
| `SlideMlRect` | `Models/SlideMlRect.cs` | 纯数据矩形 |
| `SlideMlPoint` | `Models/SlideMlPoint.cs` | 纯数据点 |
| `SlideMlSize` | `Models/SlideMlSize.cs` | 纯数据尺寸 |
| `SlideMlParseException` | `Models/SlideMlParseException.cs` | 异常类型族 |
| `ISlideMlLayoutEngine` | `Rendering/ISlideMlLayoutEngine.cs` | 布局引擎接口 |
| `SlideMlLayoutEngine` | `Rendering/SlideMlLayoutEngine.cs` | 布局引擎实现 |
| `ISlideMlRenderEngine` | `Rendering/ISlideMlRenderEngine.cs` | 渲染引擎接口 |
| `ISlideMlRenderPipeline` | `Rendering/ISlideMlRenderPipeline.cs` | 渲染管道接口 |
| `SlideMlRenderPipeline` | `Rendering/SlideMlRenderPipeline.cs` | 渲染管道实现 |
| `IPreviewImage` | `Models/IPreviewImage.cs` | 预览图片接口 |
| `FilePreviewImage` | `Models/FilePreviewImage.cs` | 文件预览图片实现 |
| `IMainThreadDispatcher` | (外部库 AgentLib) | 主线程调度器接口 |
