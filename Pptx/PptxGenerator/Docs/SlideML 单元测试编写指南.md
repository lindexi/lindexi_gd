# SlideML 单元测试编写指南

## 概述

本文档指导开发者如何为 SlideML 项目编写单元测试和集成测试。内容涵盖测试项目结构、命名规范、Fake 实现的使用、各模块测试编写模式，以及在编写测试过程中积累的注意事项。

### 测试框架

- **框架**：MSTest 4.0.2
- **目标框架**：net10.0-windows
- **断言库**：`Microsoft.VisualStudio.TestTools.UnitTesting`（通过 csproj 全局 Using 引入）

### 测试项目位置

```
PptxGenerator/
└── Code/
    └── PptxGenerator.Tests/
        ├── PptxGenerator.Tests.csproj
        ├── Models/              # 模型与工具类测试
        ├── Rendering/           # 渲染管道、布局引擎、Fake 实现、集成测试
        ├── Evaluation/          # 评估模块测试
        ├── Pipeline/            # 迭代管道测试
        └── Prompt/              # 提示词测试
```

### 项目引用关系

测试项目通过 `ProjectReference` 引用 `PptxGenerator.Core`，后者已配置 `InternalsVisibleTo`：

```xml
<!-- PptxGenerator.Core.csproj -->
<ItemGroup>
  <InternalsVisibleTo Include="PptxGenerator.Tests" />
</ItemGroup>
```

这意味着 `internal` 方法可以在测试中直接调用，无需反射。

---

## 命名规范

### 测试方法命名

统一采用 `方法名_场景_预期行为` 三段式：

```csharp
[TestMethod]
public void Parse_SingleValue_AllCornersEqual()

[TestMethod]
public async Task RenderAsync_SimplePageWithRect_ReturnsCorrectResult()

[TestMethod]
public void PreLayout_AbsolutePanel_AutoSize_WrapsContent()
```

### 测试文件命名

按被测类型拆分，一个被测类型对应一个测试文件：

| 模式 | 示例 |
|------|------|
| 模型/工具类 | `SlideMlCornerRadiusTests.cs` |
| 解析器 | `SlideMlParserRootTests.cs`、`SlideMlParserPanelTests.cs` |
| 布局引擎 | `SlideMlLayoutEngineAbsoluteLayoutTests.cs` |
| 渲染管道 | `SlideMlRenderPipelineBasicTests.cs` |
| 集成测试 | `SlideMlIntegrationTests.cs` |
| Fake 实现 | `FakeRenderEngine.cs`（不带 Tests 后缀） |

### 命名空间约定

```csharp
namespace PptxGenerator.Tests.Models;      // Models/ 目录下的测试
namespace PptxGenerator.Tests.Rendering;   // Rendering/ 目录下的测试
```

---

## 测试类结构模板

### 基本模板

```csharp
using PptxGenerator.Models.SlideDocuments;
using PptxGenerator.Rendering;

namespace PptxGenerator.Tests.Models;

[TestClass]
public sealed class SlideMlCornerRadiusTests
{
    [TestMethod]
    public void Parse_SingleValue_AllCornersEqual()
    {
        // Arrange
        var input = "8";

        // Act
        var result = SlideMlCornerRadius.Parse(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(8, result!.Value.TopLeft);
        Assert.AreEqual(8, result.Value.TopRight);
    }
}
```

### 带 TestInitialize 的模板

布局引擎等需要共享 Setup 逻辑的测试类：

```csharp
[TestClass]
public sealed class SlideMlLayoutEngineAbsoluteLayoutTests
{
    private SlideMlLayoutEngine _engine = null!;
    private SlideMlPipelineContext _context = null!;

    [TestInitialize]
    public void Setup()
    {
        _engine = new SlideMlLayoutEngine();
        _context = new SlideMlPipelineContext();
    }

    [TestMethod]
    public void PreLayout_AbsolutePanel_AutoSize_WrapsContent()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Absolute,
            Children =
            {
                new SlideMlRectElement { Id = "r1", X = 10, Y = 10, Width = 100, Height = 50 },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        Assert.AreEqual(0, _context.Errors.Count, "不应有错误");
        Assert.AreEqual("100x50", panel.RenderSize, "panel RenderSize 应为 100x50");
    }

    private static SlideMlPage CreatePage(params SlideMlElement[] children)
    {
        var page = new SlideMlPage();
        foreach (var child in children)
            page.Children.Add(child);
        return page;
    }
}
```

---

## MSTest 4.x 注意事项

### 异常断言 API 变更

MSTest 4.x 移除了 `Assert.ThrowsException<T>`，替换为两个方法：

| 旧 API（不可用） | 新 API | 说明 |
|------------------|--------|------|
| `Assert.ThrowsException<T>` | `Assert.ThrowsExactly<T>` | 严格匹配异常类型（不含子类） |
| — | `Assert.Throws<T>` | 非严格匹配（含继承层次） |

```csharp
// 正确写法
Assert.ThrowsExactly<SlideMlRootElementException>(() => SlideMlParser.Parse("<NotPage/>", context));

// 错误写法（编译报错）
// Assert.ThrowsException<SlideMlRootElementException>(...)
```

### 参数化测试

使用 `[DataTestMethod]` + `[DataRow]`：

```csharp
[DataTestMethod]
[DataRow("true", true)]
[DataRow("True", true)]
[DataRow("false", false)]
[DataRow("False", false)]
public void Parse_TextElement_IsBold_ValidBoolValues_Parsed(string input, bool expected)
{
    var xml = $"""<Page><TextElement Id="t1" Text="x" IsBold="{input}"/></Page>""";
    // ...
}
```

### 全局 Using

`PptxGenerator.Tests.csproj` 已配置全局 Using，无需在每个文件中手动引入：

```xml
<ItemGroup>
  <Using Include="Microsoft.VisualStudio.TestTools.UnitTesting" />
</ItemGroup>
```

---

## 模型对象构造

SlideML 模型类普遍使用 `init` 属性，通过对象初始化器构造：

```csharp
var panel = new SlideMlPanelElement
{
    Id = "panel1",
    X = 80,
    Y = 260,
    Width = 1120,
    Layout = SlideMlLayoutDirection.Horizontal,
    Gap = 12,
    Padding = 24,
    Children =
    {
        new SlideMlRectElement { Id = "card1", Width = 340, Height = 260, Fill = new SlideMlSolidColorBrush("#FFFFFF") },
    },
};
```

### 关键属性类型

| 属性 | 类型 | 说明 |
|------|------|------|
| `X`, `Y`, `Width`, `Height` | `double?` | null 表示未指定/自动 |
| `Margin` | `SlideMlThickness?` | 通过 `SlideMlThickness.Parse` 或直接构造 |
| `Padding` | `double` | Panel 专用，默认 0 |
| `LayoutBounds` | `SlideMlRect` | 布局引擎设置，测试中读取验证 |
| `RenderSize` | `string?` | 布局引擎设置，格式 "宽x高" |
| `RenderLocation` | `string?` | 布局引擎设置，格式 "XxY" |

### SlideMlThickness 展开规则

`SlideMlThickness.Parse` 的值展开规则需特别注意（上下=第一个值，左右=第二个值）：

| 输入 | Left | Top | Right | Bottom |
|------|------|-----|-------|--------|
| `"10"` | 10 | 10 | 10 | 10 |
| `"10,20"` | 20 | 10 | 20 | 10 |
| `"10,20,30"` | 20 | 10 | 30 | 20 |
| `"10,20,30,40"` | 40 | 10 | 30 | 20 |

---

## Fake 实现的使用

渲染管道依赖 `ISlideMlRenderEngine`、`IPreviewImage`、`IMainThreadDispatcher` 三个接口。测试项目在 `Rendering/` 目录下提供了 Fake 实现，使集成测试无需真实 UI 框架。

### 三个 Fake 实现

| 文件 | 实现接口 | 用途 |
|------|---------|------|
| `FakeRenderEngine.cs` | `ISlideMlRenderEngine` | 记录调用状态，支持预设测量结果 |
| `FakePreviewImage.cs` | `IPreviewImage` | 空实现的预览图 |
| `FakeMainThreadDispatcher.cs` | `IMainThreadDispatcher` | 直接同步执行，`CheckAccess()` 返回 true |

### 构建测试管道

```csharp
private static (SlideMlRenderPipeline Pipeline, FakeRenderEngine RenderEngine) CreatePipeline(
    SlideMlPipelineContext? context = null)
{
    context ??= new SlideMlPipelineContext();
    var layoutEngine = new SlideMlLayoutEngine();
    var renderEngine = new FakeRenderEngine();
    var dispatcher = new FakeMainThreadDispatcher();
    var pipeline = new SlideMlRenderPipeline(layoutEngine, renderEngine, dispatcher, context);
    return (pipeline, renderEngine);
}
```

### FakeRenderEngine 的 MeasureOverrides

通过 `MeasureOverrides` 字典预设元素的测量结果，模拟真实渲染引擎的测量行为：

```csharp
var (pipeline, renderEngine) = CreatePipeline();
renderEngine.MeasureOverrides["t1"] = (88, 19.2, 1);  // (Width, Height, LineCount)

var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

StringAssert.Contains(result.OutputXml, "RenderSize=\"88x19.2\"");
StringAssert.Contains(result.OutputXml, "ActualLineCount=\"1\"");
```

如果不设置 `MeasureOverrides`，`FakeRenderEngine` 会按默认逻辑计算测量值（如 TextElement 按字符数 × 字号估算宽度）。

### 自定义 Fake 子类模拟异常

需要测试渲染引擎抛异常的场景时，继承 `FakeRenderEngine` 并重写方法：

```csharp
private sealed class ThrowingRenderEngine : FakeRenderEngine
{
    public override SlideMlElementMeasurements PreMeasure(SlideMlPage page, SlideMlPipelineContext context)
        => throw new InvalidOperationException("模拟测量失败");
}
```

---

## 各模块测试编写模式

### 模型与工具类测试

纯函数测试，直接调用静态方法，验证返回值：

```csharp
[TestMethod]
public void Parse_Null_ReturnsNull()
{
    var result = SlideMlShadow.Parse(null);
    Assert.IsNull(result);
}
```

适用类型：`SlideMlCornerRadius`、`SlideMlThickness`、`SlideMlShadow`、`SlideMlXmlUtilities`、`SlideMlRect`、`SlideMlPipelineContext`、`SlideMlElementMeasurements`。

### 解析器测试

通过 `SlideMlParser.Parse(xml, context)` 将 XML 字符串解析为 `SlideMlPage`，然后验证模型属性：

```csharp
[TestMethod]
public void Parse_PageRoot_ReturnsPage()
{
    var xml = """<Page Background="#FF0000"></Page>""";
    var context = new SlideMlPipelineContext();

    var page = SlideMlParser.Parse(xml, context);

    Assert.IsNotNull(page);
    Assert.AreEqual("#FF0000", page.Background);
}
```

注意：`SlideMlParser.Parse` 需要传入 `SlideMlPipelineContext`，解析过程中的警告和错误会收集到 context 中。

### 布局引擎测试

直接构造模型对象（不经过解析器），调用 `PreLayout` 或 `FinalLayout`，验证 `LayoutBounds`、`RenderSize`、`RenderLocation` 等属性：

```csharp
[TestMethod]
public void PreLayout_HorizontalFlow_NoGap_ChildrenTouching()
{
    var panel = new SlideMlPanelElement
    {
        Id = "panel1",
        Layout = SlideMlLayoutDirection.Horizontal,
        Children =
        {
            new SlideMlRectElement { Id = "r1", Width = 100, Height = 50 },
            new SlideMlRectElement { Id = "r2", Width = 200, Height = 50 },
        },
    };
    var page = CreatePage(panel);

    _engine.PreLayout(page, _context);

    Assert.AreEqual(100, panel.Children[0].LayoutBounds.X, 0.01, "r1 X=0");
    Assert.AreEqual(100, panel.Children[1].LayoutBounds.X, 0.01, "r2 X=100（无 Gap，紧邻）");
}
```

### 渲染管道集成测试

从 XML 字符串输入到 `OutputXml`/`Warnings`/`Errors`/`PreviewImage` 的全链路验证：

```csharp
[TestMethod]
public async Task RenderAsync_SimplePageWithRect_ReturnsCorrectResult()
{
    var xml = """<Page><Rect Id="r1" X="10" Y="20" Width="100" Height="50" Fill="#FF0000"/></Page>""";
    var (pipeline, _) = CreatePipeline();

    var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

    Assert.IsNotNull(result.PreviewImage);
    Assert.AreEqual(0, result.Warnings.Count);
    StringAssert.Contains(result.OutputXml, "RenderSize=\"100x50\"");
    }
    ```

    ### 端到端集成测试

完整的从 XML 到回填输出 XML 的验证，使用辅助方法提取局部 XML 片段进行断言：

```csharp
[TestMethod]
public async Task HorizontalFlow_ThreeRects_ChildrenArrangedAndBackfilled()
{
    var xml = """
        <Page Background="#FFFFFF">
          <Panel Id="row" Layout="Horizontal" Gap="12" X="80" Y="260" Width="1120">
            <Rect Id="card1" Width="340" Height="260" Fill="#FFFFFF" />
            <Rect Id="card2" Width="340" Height="260" Fill="#FFFFFF" />
            <Rect Id="card3" Width="340" Height="260" Fill="#FFFFFF" />
          </Panel>
        </Page>
        """;
    var (pipeline, _) = CreatePipeline();

    var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);

    // 验证回填值
    var card1Segment = ExtractElementSegment(result.OutputXml, "card1");
    StringAssert.Contains(card1Segment, "RenderSize=\"340x260\"");
    }

private static string ExtractElementSegment(string xml, string id)
{
    var index = xml.IndexOf($"Id=\"{id}\"", StringComparison.Ordinal);
    Assert.IsTrue(index >= 0, $"元素 Id=\"{id}\" 应存在于输出 XML 中");
    return xml.Substring(index, Math.Min(300, xml.Length - index));
}
```

---

## 渲染管道的四步流程

`SlideMlRenderPipeline.RenderAsync` 内部执行以下四步流程，测试时可针对各步骤验证：

| 步骤 | 方法 | 说明 |
|------|------|------|
| 1. 解析 + 预处理 | `SlideMlParser.Parse` + `SlideMlXmlUtilities.NormalizeXml` | XML → 模型对象 |
| 2. 预布局 | `SlideMlLayoutEngine.PreLayout` | 计算元素初始坐标和尺寸 |
| 3. 测量 + 终布局 | `ISlideMlRenderEngine.PreMeasure` + `SlideMlLayoutEngine.FinalLayout` | 测量文本/图片实际尺寸，应用最终布局 |
| 4. 渲染 + 回填 | `ISlideMlRenderEngine.Render` + `SlideMlXmlUtilities.FormatRenderedXml` | 生成预览图，回填 RenderSize/RenderLocation/ActualLineCount |

---

## internal 方法测试

`PptxGenerator.Core` 已配置 `InternalsVisibleTo("PptxGenerator.Tests")`，因此 `internal` 方法可以直接在测试中调用：

```csharp
// SlideMlLayoutEngine.SlideRectContains 是 internal static 方法
[TestMethod]
public void SlideRectContains_ContainerContainsInner_ReturnsTrue()
{
    var outer = new SlideMlRect(0, 0, 100, 100);
    var inner = new SlideMlRect(10, 10, 50, 50);

    var result = SlideMlLayoutEngine.SlideRectContains(outer, inner);

    Assert.IsTrue(result);
}
```

对于 `private` 方法（如 `SlideMlLayoutEngine.GetChildSize`），通过公共 API 间接测试——构造特定输入，调用 `PreLayout`/`FinalLayout`，验证输出的 `RenderSize`/`RenderLocation` 间接验证私有方法的逻辑。

---

## 编写测试的注意事项

### 实际行为可能与文档预期不一致

编写测试时要以**实际代码行为为准**，而非测试用例文档的预期。以下是已发现的差异：

| 模块 | 文档预期 | 实际行为 |
|------|---------|---------|
| `SlideMlShadow.Parse("5")` 单值 | 解析成功，返回 OffsetX=5 | 返回 null（需要至少 2 个值） |
| `RenderAsync(null)` | 抛 `ArgumentNullException` | 抛 `ArgumentException`（`string.IsNullOrWhiteSpace` 拦截） |
| `RenderAsync("")` | 被捕获返回错误结果 | 抛 `ArgumentException`（在 try-catch 之前被拦截） |
| 渲染引擎抛异常 | 异常被捕获，返回错误结果 | 异常向上传播（catch 只捕获 `SlideMlParseException` 和 `XmlException`） |
| Panel RenderSize | 使用计算的内容宽度 x 高度 | 声明的 Width 值优先于计算值 |
| 流式布局中 ActualLineCount | 从测量结果回填 | 不回填（保持默认 0），只有绝对定位路径的 `LayoutText` 才会回填 |
| 声明 Height vs 测量值 | 使用测量值 | 声明 Height 优先于测量值 |

### 浮点数比较

使用 `Assert.AreEqual(expected, actual, delta)` 进行浮点数比较，delta 通常取 `0.01`：

```csharp
Assert.AreEqual("100x50", panel.RenderSize, "panel RenderSize 应为 100x50");
```

### 异步测试

渲染管道的 `RenderAsync` 返回 `Task<SlideMlRenderResult>`，测试方法需标记为 `async Task` 并使用 `await`：

```csharp
[TestMethod]
public async Task RenderAsync_SimplePageWithRect_ReturnsCorrectResult()
{
    var result = await pipeline.RenderAsync(xml).ConfigureAwait(false);
    // ...
}
```

### XML 字符串使用原始字符串

C# 11 的原始字符串字面量（`"""..."""`）非常适合编写多行 SlideML XML：

```csharp
var xml = """
    <Page Background="#FFFFFF">
      <Panel Id="row" Layout="Horizontal" Gap="12">
        <Rect Id="card1" Width="340" Height="260" />
      </Panel>
    </Page>
    """;
```

### 输出 XML 的局部断言

验证 `OutputXml` 中的回填值时，使用 `StringAssert.Contains` 或先提取元素片段再断言：

```csharp
// 简单断言
StringAssert.Contains(result.OutputXml, "RenderSize=\"1280x720\"");

// 提取局部片段后断言（避免误匹配其他元素）
var segment = ExtractElementSegment(result.OutputXml, "card1");
StringAssert.Contains(segment, "RenderSize=\"340x260\"");
```

### Warning/Error 集合验证

```csharp
// 验证无 Warning
Assert.AreEqual(0, result.Warnings.Count, "Warnings 应为空");

// 验证包含特定关键词的 Warning
Assert.IsTrue(result.Warnings.Any(w => w.Contains("溢出")), "应包含溢出警告");

// 验证 Warning 数量
Assert.AreEqual(2, result.Warnings.Count, "应有 2 条警告");
```

### 自定义画布尺寸

默认画布为 1280×720，如需测试自定义画布：

```csharp
var context = new SlideMlPipelineContext(1920, 1080);
```

---

## 测试文件组织清单

当前测试项目完整的文件组织如下：

| 目录 | 文件 | 覆盖内容 |
|------|------|---------|
| `Models/` | `SlideMlCornerRadiusTests.cs` | 圆角解析 |
| | `SlideMlThicknessTests.cs` | 边距解析 |
| | `SlideMlShadowTests.cs` | 阴影解析 |
| | `SlideMlXmlUtilitiesExtractXmlTests.cs` | XML 提取 |
| | `SlideMlXmlUtilitiesNormalizeXmlTests.cs` | XML 规范化 |
| | `SlideMlXmlUtilitiesFormatRenderedXmlTests.cs` | XML 回填格式化 |
| | `SlideMlXmlUtilitiesFormatNumberTests.cs` | 数字格式化 |
| | `SlideMlRectTests.cs` | 矩形结构 |
| | `SlideMlPipelineContextTests.cs` | 管道上下文 |
| | `SlideMlElementMeasurementsTests.cs` | 测量结果集合 |
| | `SlideMlMeasureResultTests.cs` | 单个测量结果 |
| | `SlideMlRenderedMetricsTests.cs` | 渲染指标 |
| | `SlideMlLayoutEngineInternalTests.cs` | 布局引擎内部方法 |
| | `SlideMlParser*.cs`（12 个文件） | 解析器各模块 |
| `Rendering/` | `FakeRenderEngine.cs` | Fake 渲染引擎 |
| | `FakePreviewImage.cs` | Fake 预览图 |
| | `FakeMainThreadDispatcher.cs` | Fake 线程调度器 |
| | `SlideMlLayoutEngine*.cs`（10 个文件） | 布局引擎各章节 |
| | `SlideMlRenderPipeline*.cs`（6 个文件） | 渲染管道各章节 |
| | `SlideMlIntegrationTests.cs` | 端到端集成测试 |
| `Evaluation/` | `EvaluationModelsTests.cs` | 评估模型 |
| `Pipeline/` | `IterationPipelineTests.cs` | 迭代管道 |
| `Prompt/` | `SlideMlPromptProviderTests.cs` | 提示词 |

---

## 新增测试的检查清单

编写新测试时，请逐项确认：

- [ ] 测试方法命名为 `方法名_场景_预期行为` 格式
- [ ] 测试类标记 `[TestClass]`，方法标记 `[TestMethod]`
- [ ] 每个测试方法只验证一个行为（AAA 模式：Arrange-Act-Assert）
- [ ] 浮点数比较使用 `Assert.AreEqual(expected, actual, 0.01)`
- [ ] 异常测试使用 `Assert.ThrowsExactly<T>`（非 `ThrowsException`）
- [ ] 异步测试方法签名为 `async Task`
- [ ] 文件命名空间与目录结构一致
- [ ] 编译通过且测试运行通过
