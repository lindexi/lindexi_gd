# SlideML 渲染管道集成测试用例

> 目标类：`SlideMlRenderPipeline`（`Rendering/SlideMlRenderPipeline.cs`）
> 测试框架：MSTest
> 前置条件：需要创建以下 Fake 实现
> - `FakeMainThreadDispatcher`：直接同步执行委托
> - `FakeRenderEngine`（实现 `ISlideMlRenderEngine`）：PreMeasure 为 TextElement 按文本长度计算模拟尺寸，Image 使用默认尺寸，其他元素使用声明尺寸；Render 返回 FakePreviewImage
> - `FakePreviewImage`（实现 `IPreviewImage`）：Save 空实现
> - 可以使用 `FakeRenderEngine.ReturnMeasurements` 的公共属性来预设测量结果

---

## 1. 基本 End-to-End 流程

### 1.1 简单 Page + 单个 Rect
- **用例名**: `RenderAsync_SimplePageWithRect_ReturnsCorrectResult`
- **输入 XML**: `<Page><Rect Id="r1" X="10" Y="20" Width="100" Height="50" Fill="#FF0000"/></Page>`
- **Fake 行为**: PreMeasure 返回空字典（Rect 不需要测量）
- **预期**:
  - result.OutputXml 包含 `ActualWidth="100"` `ActualHeight="50"` 在 Rect 上
  - Page 上回填 `ActualWidth="1280"` `ActualHeight="720"`
  - result.Warnings 为空
  - result.Errors 为空
  - result.PreviewImage 不为 null
- **验证点**: OutputXml 内容、Warnings/Errors 数量、PreviewImage

### 1.2 简单 Page + 单个 TextElement
- **用例名**: `RenderAsync_SimplePageWithTextElement_ReturnsMeasuredResult`
- **输入 XML**: `<Page><TextElement Id="t1" Text="Hello World" FontSize="16"/></Page>`
- **Fake 行为**: PreMeasure 返回 t1: MeasuredWidth=88, MeasuredHeight=19.2, ActualLineCount=1
- **预期**:
  - t1 回填 `ActualWidth="88"` `ActualHeight="19.2"` `ActualLineCount="1"`
  - LayoutBounds.X=0（页面原点左对齐）
  - Warnings 为空
- **验证点**: 测量值回填到 XML、坐标正确

### 1.3 简单 Page + 单个 Image
- **用例名**: `RenderAsync_SimplePageWithImage_ReturnsMeasuredSize`
- **输入 XML**: `<Page><Image Id="img1" Source="pic_001" Width="400" Height="300"/></Page>`
- **Fake 行为**: PreMeasure 返回 img1: MeasuredWidth=400, MeasuredHeight=300
- **预期**:
  - img1 回填 `ActualWidth="400"` `ActualHeight="300"`
  - Warnings 为空
- **验证点**: 图片尺寸回填

### 1.4 简单 Page + Panel(Absolute) + 子元素
- **用例名**: `RenderAsync_AbsolutePanel_ChildrenRendered`
- **输入 XML**: `<Page><Panel Id="p1" X="50" Y="50" Width="400" Height="300"><Rect Id="r1" X="20" Y="20" Width="100" Height="80" Fill="#00FF00"/></Panel></Page>`
- **Fake 行为**: PreMeasure 返回空
- **预期**:
  - OutputXml 中 p1 回填 ActualWidth/ActualHeight
  - r1 回填 ActualWidth/ActualHeight
  - Warnings 空
- **验证点**: 嵌套 Panel + Rect 回填正确

### 1.5 简单 Page + Panel(Horizontal) + 子元素
- **用例名**: `RenderAsync_HorizontalFlowPanel_ChildrenArranged`
- **输入 XML**: `<Page><Panel Id="row" Layout="Horizontal" Gap="12" X="40" Y="40" Width="1000"><Rect Id="c1" Width="300" Height="200"/><Rect Id="c2" Width="300" Height="200"/><Rect Id="c3" Width="300" Height="200"/></Panel></Page>`
- **Fake 行为**: PreMeasure 返回空
- **预期**:
  - c1 LayoutBounds.X=40（Panel X+无Padding），c2.X=352，c3.X=664
  - c1 LayoutBounds.Y=40
  - Warnings 空
- **验证点**: 流式布局坐标正确、回填正确

### 1.6 简单 Page + Panel(Vertical) + 子元素
- **用例名**: `RenderAsync_VerticalFlowPanel_ChildrenArranged`
- **输入 XML**: `<Page><Panel Id="col" Layout="Vertical" Gap="16" X="100" Y="100" Width="400"><Rect W="400" H="60" Fill="#FF0000"/><Rect W="400" H="60" Fill="#00FF00"/><Rect W="400" H="60" Fill="#0000FF"/></Panel></Page>`
- **Fake 行为**: PreMeasure 返回空
- **预期**:
  - Panel 子元素垂直排列，Gap=16
  - 每个 Rect 的 X=100（Panel 的 X）
  - Warnings 空
- **验证点**: 垂直排列坐标

---

## 2. XML 预处理

### 2.1 输入包含 XML 声明头
- **用例名**: `RenderAsync_WithXmlDeclaration_Normalized`
- **输入**: `<?xml version="1.0" encoding="UTF-8"?>\n<Page><Rect Width="100" Height="50"/></Page>`
- **预期**: ExtractXml 正常提取，输出包含规范化后的 XML
- **验证点**: 声明头存在

### 2.2 输入不包含声明头
- **用例名**: `RenderAsync_WithoutDeclaration_DeclarationAdded`
- **输入**: `<Page><Rect Width="100" Height="50"/></Page>`
- **预期**: ExtractXml 添加 `<?xml version="1.0" encoding="UTF-8"?>` 前缀
- **验证点**: 声明头被添加

### 2.3 输入包含 markdown 代码块
- **用例名**: `RenderAsync_WithMarkdownCodeBlock_XmlExtracted`
- **输入**: `` ```xml\n<Page><Rect Width="100" Height="50"/></Page>\n``` ``
- **预期**: ExtractXml 正确提取 `<Page>` 部分的 XML
- **验证点**: XML 被正确提取

### 2.4 输入前后有空白
- **用例名**: `RenderAsync_WithWhitespace_XmlTrimmed`
- **输入**: `\n  \n<Page><Rect Width="100" Height="50"/></Page>\n  `
- **预期**: 正常解析，无错误
- **验证点**: 空白被正确处理

### 2.5 输入纯文本无 XML
- **用例名**: `RenderAsync_NoXml_ExceptionCaught`
- **输入**: `这是一段纯文本`
- **预期**: 解析异常被捕获，result.Warnings 包含 `[Warning] parser: SlideML 解析失败`（或类似消息），result.PreviewImage 不为 null（错误预览图）
- **验证点**: 错误处理

---

## 3. 回填验证（FormatRenderedXml）

### 3.1 Page 回填 ActualWidth/ActualHeight
- **用例名**: `FormatRenderedXml_Page_BackfillsCanvasSize`
- **输入**: `<Page Background="#F5F5F5"><Rect Id="r1" X="0" Y="0" Width="100" Height="50"/></Page>`
- **预期**: OutputXml 中 Page 有 `ActualWidth="1280"` `ActualHeight="720"`
- **验证点**: 画布尺寸回填

### 3.2 嵌套元素全部回填
- **用例名**: `FormatRenderedXml_NestedElements_AllBackfilled`
- **输入**: `<Page><Panel Id="outer"><Panel Id="inner"><Rect Id="leaf" Width="50" Height="30"/></Panel></Panel></Page>`
- **预期**: outer、inner、leaf 均有 ActualWidth/ActualHeight
- **验证点**: 所有层级元素回填

### 3.3 ActualLineCount 仅在 TextElement 上出现
- **用例名**: `FormatRenderedXml_ActualLineCount_OnlyOnTextElement`
- **输入**: `<Page><TextElement Id="t1" Text="Hello" FontSize="16"/><Rect Id="r1" Width="100" Height="50"/></Page>`
- **Fake 行为**: PreMeasure 返回 t1 的 ActualLineCount=1
- **预期**:
  - t1 有 `ActualLineCount="1"`
  - r1 没有 ActualLineCount 属性
- **验证点**: 行数仅文本元素回填

### 3.4 无 Id 元素跳过回填
- **用例名**: `FormatRenderedXml_NoIdElement_Skipped`
- **输入**: `<Page><Rect Width="100" Height="50"/></Page>`
- **预期**: Rect 被自动分配 Id（elem_001），仍然回填 ActualWidth/ActualHeight（因为 FindMetrics 会查找它）
- **验证点**: 自动 Id 元素也被回填

### 3.5 自定义画布尺寸回填
- **用例名**: `FormatRenderedXml_CustomCanvas_Backfilled`
- **输入**: 使用 SlideMlPipelineContext(1920, 1080)
- **预期**: OutputXml 中 Page 有 `ActualWidth="1920"` `ActualHeight="1080"`
- **验证点**: 自定义画布尺寸回填

---

## 4. 上下文重置

### 4.1 多次调用时 Context.Reset 生效
- **用例名**: `RenderAsync_MultipleCalls_ContextReset`
- **步骤**: 第一次调用正常，第二次调用产生 Warning
- **预期**: 第二次调用的结果中不包含第一次的 Warning
- **验证点**: Reset 清空上下文

### 4.2 连续调用互不干扰
- **用例名**: `RenderAsync_ConsecutiveCalls_Independent`
- **步骤**: 调用两次 RenderAsync 不同的 XML
- **预期**: 两次结果各自独立，不互相影响
- **验证点**: 隔离性

---

## 5. 错误处理

### 5.1 空字符串输入
- **用例名**: `RenderAsync_EmptyString_ThrowsArgumentException`
- **输入**: `""`
- **预期**: 抛出 `ArgumentException`（因为 `ExtractXml("")` 返回 ""，然后 `XDocument.Parse("")` 会抛异常被捕获）
- **验证点**: 异常被适当处理

### 5.2 null 输入
- **用例名**: `RenderAsync_NullString_ThrowsArgumentNullException`
- **输入**: `null`
- **预期**: 抛出 `ArgumentNullException`
- **验证点**: null 检查

### 5.3 空白字符串输入
- **用例名**: `RenderAsync_WhitespaceString_ThrowsArgumentException`
- **输入**: `"   \n  "`
- **预期**: 解析失败被捕获，返回错误结果
- **验证点**: 空白串处理

### 5.4 根元素非 Page
- **用例名**: `RenderAsync_NonPageRoot_ErrorPreviewReturned`
- **输入**: `<NotPage></NotPage>`
- **预期**: SlideMlRootElementException 被捕获，result.Warnings 包含解析失败信息，result.PreviewImage 为错误预览图
- **验证点**: 根元素校验异常捕获

### 5.5 XML 格式错误
- **用例名**: `RenderAsync_MalformedXml_ErrorHandled`
- **输入**: `<Page><Rect></Page>`
- **预期**: XmlException 被捕获，返回错误结果
- **验证点**: XML 解析异常捕获

### 5.6 渲染引擎 PreMeasure 抛异常
- **用例名**: `RenderAsync_PreMeasureThrows_ErrorHandled`
- **Fake 行为**: FakeRenderEngine.PreMeasure 抛出异常
- **预期**: 异常被捕获，结果包含错误信息
- **验证点**: 渲染异常处理

### 5.7 渲染引擎 Render 抛异常
- **用例名**: `RenderAsync_RenderThrows_ErrorHandled`
- **Fake 行为**: FakeRenderEngine.Render 抛出异常
- **预期**: 异常被捕获，结果包含错误信息
- **验证点**: 渲染异常处理

---

## 6. Warning 收集

### 6.1 元素超出画布 → Warning
- **用例名**: `RenderAsync_ElementOutsideCanvas_WarningCollected`
- **输入 XML**: `<Page><Rect X="1200" Y="600" Width="200" Height="200"/></Page>`
- **预期**: result.Warnings 包含 `右边界 X=1400 超出画布宽度 1280` 和 `下边界 Y=800 超出画布高度 720`
- **验证点**: 边界警告收集

### 6.2 流式布局溢出 → Warning
- **用例名**: `RenderAsync_FlowLayoutOverflow_WarningCollected`
- **输入**: `<Page><Panel Layout="Horizontal" Width="150" Gap="8"><Rect Width="100" Height="50"/><Rect Width="100" Height="50"/></Panel></Page>`
- **预期**: result.Warnings 包含 `流式布局内容宽度...超出 Panel 宽度 150`
- **验证点**: 溢出警告

### 6.3 文本溢出容器 → Warning
- **用例名**: `RenderAsync_TextHeightOverflow_WarningCollected`
- **输入**: `<Page><TextElement Text="Long text..." Width="400" Height="30" FontSize="16"/></Page>`
- **Fake 行为**: PreMeasure 返回 MeasuredHeight=80, ActualLineCount=5
- **预期**: result.Warnings 包含 `超出容器高度`
- **验证点**: 文本溢出警告

### 6.4 未知属性 → Warning
- **用例名**: `RenderAsync_UnknownAttribute_WarningCollected`
- **输入**: `<Page><Rect Unknown="value" Width="100" Height="50"/></Page>`
- **预期**: result.Warnings 包含 `未知属性 "Unknown"`
- **验证点**: 未知属性警告

### 6.5 未知标签 → Warning
- **用例名**: `RenderAsync_UnknownTag_WarningCollected`
- **输入**: `<Page><UnknownTag/></Page>`
- **预期**: result.Warnings 包含 `未知标签 "UnknownTag"`
- **验证点**: 未知标签警告

### 6.6 多重 Warning 同时产生
- **用例名**: `RenderAsync_MultipleWarnings_AllCollected`
- **输入**: 同时含超出边界、溢出、未知属性等
- **预期**: 所有 Warning 按顺序收集
- **验证点**: 多重警告

---

## 7. Error 收集

### 7.1 无效枚举值 → Error
- **用例名**: `RenderAsync_InvalidEnumValue_ErrorCollected`
- **输入**: `<Page><Rect HorizontalAlignment="Diagonal" Width="100" Height="50"/></Page>`
- **预期**: result.Errors 包含 `HorizontalAlignment 值 "Diagonal" 无效`
- **验证点**: 枚举错误

### 7.2 无效数值 → Error
- **用例名**: `RenderAsync_InvalidNumericValue_ErrorCollected`
- **输入**: `<Page><Rect Width="abc" Height="50"/></Page>`
- **预期**: result.Errors 包含 `Width 值 "abc" 不是有效的数值`
- **验证点**: 数值错误

### 7.3 无效 Margin 格式 → Error
- **用例名**: `RenderAsync_InvalidMargin_ErrorCollected`
- **输入**: `<Page><Rect Margin="abc" Width="100" Height="50"/></Page>`
- **预期**: result.Errors 包含 `不是有效的间距格式`
- **验证点**: Margin 错误

### 7.4 同时有 Error 和 Warning
- **用例名**: `RenderAsync_ErrorAndWarning_CollectedSeparately`
- **输入**: 同时含无效枚举值（Error）和超出边界（Warning）
- **预期**: result.Errors 和 result.Warnings 都有内容，互不混淆
- **验证点**: 分类收集

---

## 8. CancellationToken

### 8.1 正常取消
- **用例名**: `RenderAsync_Cancelled_CancellationPropagated`
- **Fake 行为**: 解析后检查 CancellationToken
- **预期**: 如果在解析前取消，抛出 OperationCanceledException 或 TaskCanceledException
- **验证点**: 取消处理

### 8.2 取消后不取消
- **用例名**: `RenderAsync_NotCancelled_NormalExecution`
- **输入**: 正常 XML，传入 CancellationToken.None
- **预期**: 正常完成
- **验证点**: 无取消

---

## 9. Fake 渲染引擎验证

### 9.1 PreMeasure 被调用
- **用例名**: `RenderAsync_PreMeasureWasCalled`
- **Fake 行为**: FakeRenderEngine 记录 PreMeasure 被调用的 flag
- **预期**: PreMeasure 被调用一次
- **验证点**: 调用验证

### 9.2 Render 被调用
- **用例名**: `RenderAsync_RenderWasCalled`
- **Fake 行为**: FakeRenderEngine 记录 Render 被调用的 flag
- **预期**: Render 被调用一次
- **验证点**: 调用验证

### 9.3 PreMeasure → FinalLayout → Render 顺序
- **用例名**: `RenderAsync_ExecutionOrder_Correct`
- **Fake 行为**: 记录调用顺序
- **预期**: 顺序为 PreMeasure → FinalLayout → Render
- **验证点**: 执行顺序

### 9.4 测量值正确传递给 FinalLayout
- **用例名**: `RenderAsync_MeasurementsPassedToFinalLayout`
- **Fake 行为**: 自定义测量值
- **预期**: FinalLayout 后的元素尺寸与测量值一致
- **验证点**: 测量值传递

### 9.5 Render 返回的 PreviewImage 出现在结果中
- **用例名**: `RenderAsync_PreviewImageFromRender_Returned`
- **Fake 行为**: FakeRenderEngine.Render 返回特定 FakePreviewImage
- **预期**: result.PreviewImage 就是 Render 返回的那个实例
- **验证点**: PreviewImage 引用一致

---

## 10. 复杂场景

### 10.1 完整规范示例（渐变背景 + 流式布局卡片 + 富文本 Span）
- **用例名**: `RenderAsync_FullSpecimen_AllFeaturesCombined`
- **输入**: 包含渐变背景的 Panel、流式布局卡片 Row、Span 富文本、阴影 Rect 等
- **Fake 行为**: PreMeasure 为所有 TextElement 返回合理的模拟尺寸
- **预期**:
  - 所有元素正确回填 ActualWidth/ActualHeight
  - 流式布局卡片按顺序排列
  - Span 被正确解析（Text 拼接）
  - 渐变背景被解析
  - 无 Warning（假设布局合理）
- **验证点**: 完整端到端

### 10.2 多层嵌套 Panel（水平 + 垂直交替）
- **用例名**: `RenderAsync_DeeplyNestedPanels_AllResolved`
- **输入**: 3~4 层嵌套，水平/垂直交替
- **预期**: 所有 Panel 自动尺寸正确，子元素坐标正确
- **验证点**: 多层嵌套

### 10.3 混合元素类型含 Padding/Gap/Margin
- **用例名**: `RenderAsync_MixedLayout_WithPaddingGapMargin`
- **输入**: Panel(Padding=16, Gap=8) + 混合 Rect/TextElement/Image，含不同 Margin
- **Fake 行为**: 合理的测量值
- **预期**: 间距 = max(Gap, prevMargin + nextMargin)
- **验证点**: 间距计算

### 10.4 带对齐方式的布局
- **用例名**: `RenderAsync_Alignment_LayoutCorrect`
- **输入**: Panel(H, W=400, H=200) + Rect(W=100, H=50, HorizontalAlignment=Center, VerticalAlignment=Center)
- **预期**: Rect 在父容器中居中
- **验证点**: 对齐

### 10.5 自定义画布尺寸 + 元素布局
- **用例名**: `RenderAsync_CustomCanvasSize_LayoutRespected`
- **输入**: 使用 SlideMlPipelineContext(1920, 1080) 构建管道
- **预期**: Page 回填 ActualWidth="1920" ActualHeight="1080"，布局使用 1920x1080
- **验证点**: 自定义画布

---

## 11. XML 回填格式验证

### 11.1 OutputXml 是合法 XML
- **用例名**: `RenderAsync_OutputXml_IsValidXml`
- **步骤**: 对 OutputXml 调用 XDocument.Parse
- **预期**: 不抛出异常
- **验证点**: XML 合法性

### 11.2 数值格式正确（两位小数去零）
- **用例名**: `RenderAsync_NumericFormat_DisplayCorrect`
- **输入**: Rect(W=100.5, H=100.0)
- **预期**: ActualWidth="100.5" ActualHeight="100"
- **验证点**: 格式化

### 11.3 ActualLineCount 仅在 TextElement 上
- **用例名**: `RenderAsync_LineCount_OnlyOnText`
- **输入**: 混合元素
- **预期**: 只有 TextElement 有 ActualLineCount 属性，Rect/Panel/Image 没有
- **验证点**: 属性管理

### 11.4 原始属性保留
- **用例名**: `RenderAsync_OriginalAttributes_Preserved`
- **输入**: `<Rect Id="r1" X="10" Y="20" Width="100" Height="50" Fill="#FF0000" CornerRadius="8"/>`
- **预期**: OutputXml 中保留 X/Y/Width/Height/Fill/CornerRadius，新增 ActualWidth/ActualHeight
- **验证点**: 原属性不被移除

---

## 12. Fake 实现的设计规格

### 12.1 FakeMainThreadDispatcher
- `InvokeAsync<T>(Func<Task<T>> action)`：直接 `return await action();`
- `InvokeAsync(Func<Task> action)`：直接 `await action();`
- `CheckAccess()`：`return true;`

### 12.2 FakeRenderEngine
```csharp
public class FakeRenderEngine : ISlideMlRenderEngine
{
    public bool PreMeasureWasCalled { get; private set; }
    public bool RenderWasCalled { get; private set; }
    public SlideMlPage? PreMeasurePage { get; private set; }
    public SlideMlPage? RenderPage { get; private set; }

    // 预设测量结果，可由测试设置
    public Dictionary<string, (double Width, double Height, int? LineCount)> MeasureOverrides { get; set; } = new();

    public SlideMlElementMeasurements PreMeasure(SlideMlPage page, SlideMlPipelineContext context)
    {
        PreMeasureWasCalled = true;
        PreMeasurePage = page;
        var measurements = new Dictionary<string, SlideMlMeasureResult>();
        FillMeasurements(page.Children, measurements);
        return new SlideMlElementMeasurements(measurements);
    }

    public IPreviewImage Render(SlideMlPage page, SlideMlPipelineContext context)
    {
        RenderWasCalled = true;
        RenderPage = page;
        return new FakePreviewImage();
    }

    public IPreviewImage RenderErrorPreview(string message, SlideMlPipelineContext context)
        => new FakePreviewImage();

    private void FillMeasurements(List<SlideMlElement> children, Dictionary<string, SlideMlMeasureResult> dict)
    {
        foreach (var child in children)
        {
            // 如果设置了覆盖值，使用覆盖值；否则 TextElement 模拟计算，Image 默认 240x180，其他使用声明值
            if (MeasureOverrides.TryGetValue(child.Id, out var ov))
            {
                dict[child.Id] = new SlideMlMeasureResult
                {
                    MeasuredWidth = ov.Width,
                    MeasuredHeight = ov.Height,
                    ActualLineCount = ov.LineCount,
                };
            }
            else
            {
                dict[child.Id] = ComputeDefaultMeasure(child);
            }

            if (child is SlideMlPanelElement panel)
            {
                FillMeasurements(panel.Children, dict);
            }
        }
    }

    private static SlideMlMeasureResult ComputeDefaultMeasure(SlideMlElement element)
    {
        return element switch
        {
            SlideMlTextElement text => new()
            {
                MeasuredWidth = text.Width ?? text.Text.Length * text.FontSize * 0.6,
                MeasuredHeight = text.Height ?? text.FontSize,
                ActualLineCount = 1,
            },
            SlideMlImageElement img => new()
            {
                MeasuredWidth = img.Width ?? 240,
                MeasuredHeight = img.Height ?? 180,
            },
            _ => new()
            {
                MeasuredWidth = element.Width ?? 0,
                MeasuredHeight = element.Height ?? 0,
            },
        };
    }
}

public class FakePreviewImage : IPreviewImage
{
    public void Save(string filePath) { }
    public void Save(Stream stream) { }
}
```