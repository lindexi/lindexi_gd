# SlideRenderer 重构计划 v2

> 基于 v1 重构结果（布局/渲染分离 + 四阶段流水线）的进一步优化。
> 目标：消除 `List<string> warnings` 参数散落、重命名上下文、引入 `SlideElementMeasurements`、移除 `FindMetrics`、删除 `SlideRenderer` 兼容层。

---

## 1. 重命名 `SlideRenderContext` → `SlidePipelineContext`，内置 Warnings

**文件**：`Core\SlideRenderContext.cs` → 重命名为 `Core\SlidePipelineContext.cs`

**变更**：
- 类名 `SlideRenderContext` → `SlidePipelineContext`
- 常量 `DefaultCanvasWidth` / `DefaultCanvasHeight` 保留
- 新增 `List<string> Warnings { get; }` 属性，初始化 `new List<string>()`
- 构造函数不变

**影响范围**：所有引用 `SlideRenderContext` 的文件（约 12 处）。

---

## 2. 新建 `SlideElementMeasurements` 包装类型

**文件**：`Core\SlideElementMeasurements.cs`（新建）

**设计**：
```csharp
public sealed class SlideElementMeasurements
{
    private readonly Dictionary<string, SlideMeasureResult> _measurements;

    public SlideElementMeasurements(Dictionary<string, SlideMeasureResult> measurements)
    {
        _measurements = measurements;
    }

    public bool TryGetValue(string elementId, out SlideMeasureResult result)
        => _measurements.TryGetValue(elementId, out result);

    public SlideMeasureResult? Find(string elementId)
        => _measurements.TryGetValue(elementId, out var r) ? r : null;
}
```

**目的**：替代 `IReadOnlyDictionary<string, SlideMeasureResult>`，消除 key 无语义的问题。

---

## 3. 更新 `ISlideLayoutEngine` 接口

**文件**：`Core\ISlideLayoutEngine.cs`

**变更**：
- 移除 `FindMetrics(SlidePage, string)` 方法
- `PreLayout` 签名：`void PreLayout(SlidePage page, SlidePipelineContext context)`
  - 移除 `List<string> warnings` 参数
- `FinalLayout` 签名：`void FinalLayout(SlidePage page, SlidePipelineContext context, SlideElementMeasurements measurements)`
  - 移除 `List<string> warnings` 参数
  - `IReadOnlyDictionary<string, SlideMeasureResult>` → `SlideElementMeasurements`

---

## 4. 更新 `SlideLayoutEngine` 实现

**文件**：`Core\SlideLayoutEngine.cs`

**变更**：
- 适配新接口签名（同步骤 3）
- 移除 `FindMetrics` 方法（含 private 重载）
- 所有内部 `Layout*` 方法：移除 `List<string> warnings` 参数，改为从 `context.Warnings` 读写
- 内部 `Layout*` 方法：`IReadOnlyDictionary<string, SlideMeasureResult>?` → `SlideElementMeasurements?`
- `ValidateBounds` 方法：`warnings.Add(...)` → `context.Warnings.Add(...)`

---

## 5. 更新 `ISlideRenderEngine` 接口

**文件**：`Core\ISlideRenderEngine.cs`

**变更**：
- `PreMeasure` 签名：`SlideElementMeasurements PreMeasure(SlidePage page, SlidePipelineContext context)`
  - 移除 `List<string> warnings` 参数
  - 返回类型 `Dictionary<string, SlideMeasureResult>` → `SlideElementMeasurements`
- `Render` 签名：`void Render(SlidePage page, DrawingContext dc, SlidePipelineContext context)`
  - 移除 `List<string> warnings` 参数
- `RenderErrorPreview` 签名：`RenderTargetBitmap RenderErrorPreview(string message, SlidePipelineContext context)`
  - `SlideRenderContext` → `SlidePipelineContext`

---

## 6. 更新 `SlideRenderEngine` 实现

**文件**：`Core\SlideRenderEngine.cs`

**变更**：
- 适配新接口签名（同步骤 5）
- 所有内部 `PreMeasure*` / `Draw*` 方法：移除 `List<string> warnings` 参数
- 警告写入改为通过 `context.Warnings`（需要在 PreMeasure/Render 入口传入 context）
- `ClampOpacity` 方法：替换为 `Math.Clamp(opacity, 0, 1)`，删除方法定义
- 所有调用 `ClampOpacity(...)` 处改为 `Math.Clamp(element.Opacity, 0, 1)`

---

## 7. 更新 `SlideRenderPipeline`

**文件**：`Core\SlideRenderPipeline.cs`

**变更**：
- 字段 `SlideRenderContext _context` → `SlidePipelineContext _context`
- 构造函数参数类型同步更新
- `RenderAsync` 方法：
  - 创建 `SlidePipelineContext` 时 warnings 自动初始化
  - 移除局部 `var warnings = new List<string>(...)`
  - 所有引擎调用移除 `warnings` 参数
  - `FindMetrics` 调用改为直接传 `page` 给 `SlideXmlUtilities`
- 返回 `SlideRenderResult` 时 `Warnings = _context.Warnings`

---

## 8. 更新 `SlideXmlUtilities`

**文件**：`Core\SlideMl\SlideXmlUtilities.cs`

**变更**：
- `FormatRenderedXml` 重载：
  - 旧：`FormatRenderedXml(string xml, Func<string, SlideRenderedMetrics?> metricsProvider)`
  - 新：`FormatRenderedXml(string xml, SlidePage page, SlidePipelineContext context)`
  - 内部直接遍历 `page.Children` 收集指标（原 `FindMetrics` 逻辑移入此处）
  - 移除 `Func` 委托版本

---

## 9. 更新 `SlideDocument.cs`

**文件**：`Core\SlideMl\SlideDocument.cs`

**变更**：
- `SlidePage.LayoutBounds` 默认值中 `SlideRenderContext.DefaultCanvasWidth` → `SlidePipelineContext.DefaultCanvasWidth`

---

## 10. 更新 `App.xaml.cs`

**文件**：`App.xaml.cs`

**变更**：
- `new SlideRenderPipeline()` 不变（内部自动创建 `SlidePipelineContext`）

---

## 11. 删除 `SlideRenderer.cs`

**文件**：`Core\SlideRenderer.cs`

**操作**：直接删除文件。Avalonia 项目有自己的 `SlideRenderer`，不受影响。

---

## 12. 编译验证

运行 `dotnet build` 确保零错误。

---

## 变更文件清单

| 操作 | 文件 |
|------|------|
| 重命名 + 修改 | `Core\SlideRenderContext.cs` → `Core\SlidePipelineContext.cs` |
| 新建 | `Core\SlideElementMeasurements.cs` |
| 修改 | `Core\ISlideLayoutEngine.cs` |
| 修改 | `Core\SlideLayoutEngine.cs` |
| 修改 | `Core\ISlideRenderEngine.cs` |
| 修改 | `Core\SlideRenderEngine.cs` |
| 修改 | `Core\SlideRenderPipeline.cs` |
| 修改 | `Core\SlideMl\SlideXmlUtilities.cs` |
| 修改 | `Core\SlideMl\SlideDocument.cs` |
| 修改 | `App.xaml.cs` |
| 删除 | `Core\SlideRenderer.cs` |

---

## 接口签名对比

### ISlideLayoutEngine

| | 旧 | 新 |
|---|-----|-----|
| PreLayout | `(SlidePage, SlideRenderContext, List<string>)` | `(SlidePage, SlidePipelineContext)` |
| FinalLayout | `(SlidePage, SlideRenderContext, IReadOnlyDictionary<string,SlideMeasureResult>, List<string>)` | `(SlidePage, SlidePipelineContext, SlideElementMeasurements)` |
| FindMetrics | `(SlidePage, string) → SlideRenderedMetrics?` | **移除** |

### ISlideRenderEngine

| | 旧 | 新 |
|---|-----|-----|
| PreMeasure | `(SlidePage, SlideRenderContext, List<string>) → Dictionary<string,SlideMeasureResult>` | `(SlidePage, SlidePipelineContext) → SlideElementMeasurements` |
| Render | `(SlidePage, DrawingContext, SlideRenderContext, List<string>)` | `(SlidePage, DrawingContext, SlidePipelineContext)` |
| RenderErrorPreview | `(string, SlideRenderContext) → RenderTargetBitmap` | `(string, SlidePipelineContext) → RenderTargetBitmap` |

### SlideXmlUtilities

| | 旧 | 新 |
|---|-----|-----|
| FormatRenderedXml | `(string, Func<string,SlideRenderedMetrics?>)` | **移除** |
| FormatRenderedXml | `(string, Func<string,SlideRenderedMetrics?>, SlideRenderContext)` | `(string, SlidePage, SlidePipelineContext)` |
