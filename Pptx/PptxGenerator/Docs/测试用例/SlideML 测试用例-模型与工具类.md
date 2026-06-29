# SlideML 模型与工具类单元测试用例

> 目标类型：`SlideMlCornerRadius`、`SlideMlThickness`、`SlideMlShadow`、`SlideMlXmlUtilities`、`SlideMlRect`、`SlideMlPipelineContext`、`SlideMlElementMeasurements`、`SlideMlRenderedMetrics`
> 测试框架：MSTest

---

## 1. SlideMlCornerRadius.Parse

### 1.1 单值四角统一
- **用例名**: `Parse_SingleValue_AllCornersEqual`
- **输入**: `"8"`
- **预期**: TopLeft=8, TopRight=8, BottomRight=8, BottomLeft=8
- **验证点**: 四个属性值

### 1.2 两值对角相同
- **用例名**: `Parse_TwoValues_DiagonalPair`
- **输入**: `"8,16"`
- **预期**: TopLeft=8, TopRight=16, BottomRight=8, BottomLeft=16
- **验证点**: 对角相同

### 1.3 三值展开
- **用例名**: `Parse_ThreeValues_Expanded`
- **输入**: `"8,16,8"`
- **预期**: TopLeft=8, TopRight=16, BottomRight=8, BottomLeft=16
- **验证点**: 三值展开规则

### 1.4 四值四角独立
- **用例名**: `Parse_FourValues_AllIndependent`
- **输入**: `"8,16,12,20"`
- **预期**: TopLeft=8, TopRight=16, BottomRight=12, BottomLeft=20
- **验证点**: 四值独立

### 1.5 null 输入
- **用例名**: `Parse_Null_ReturnsNull`
- **输入**: `null`
- **预期**: 返回 null
- **验证点**: null 处理

### 1.6 空字符串
- **用例名**: `Parse_EmptyString_ReturnsNull`
- **输入**: `""`
- **预期**: 返回 null
- **验证点**: 空串处理

### 1.7 纯空白
- **用例名**: `Parse_Whitespace_ReturnsNull`
- **输入**: `"  "`
- **预期**: 返回 null
- **验证点**: 空白处理

### 1.8 无效数值
- **用例名**: `Parse_InvalidNumber_ReturnsNull`
- **输入**: `"abc"`
- **预期**: 返回 null
- **验证点**: 无效值

### 1.9 混合有效无效值
- **用例名**: `Parse_MixedValidInvalid_ReturnsNull`
- **输入**: `"8,abc,8,16"`
- **预期**: 返回 null（第二个值解析失败）
- **验证点**: 部分无效

### 1.10 负数
- **用例名**: `Parse_NegativeValues_Parsed`
- **输入**: `"-8,16,-8,16"`
- **预期**: TopLeft=-8, TopRight=16, BottomRight=-8, BottomLeft=16
- **验证点**: 负值解析

### 1.11 零值
- **用例名**: `Parse_ZeroValue_Parsed`
- **输入**: `"0"`
- **预期**: 四角均为 0
- **验证点**: 零值

### 1.12 小数值
- **用例名**: `Parse_DecimalValues_Parsed`
- **输入**: `"8.5,16.3,12.7,20.1"`
- **预期**: 四角正确解析为小数
- **验证点**: 小数精度

### 1.13 超过 4 个值
- **用例名**: `Parse_MoreThanFourValues_FirstFourUsed`
- **输入**: `"1,2,3,4,5,6"`
- **预期**: 只取前 4 个，TopLeft=1, TopRight=2, BottomRight=3, BottomLeft=4
- **验证点**: 截断

### 1.14 隐式转换 double → SlideMlCornerRadius
- **用例名**: `ImplicitConversion_DoubleToCornerRadius`
- **输入**: `(SlideMlCornerRadius)12.5`
- **预期**: 四角均为 12.5
- **验证点**: 隐式转换

---

## 2. SlideMlThickness.Parse

### 2.1 单值四边相同
- **用例名**: `Parse_SingleValue_AllSidesEqual`
- **输入**: `"10"`
- **预期**: Left=10, Top=10, Right=10, Bottom=10
- **验证点**: 四边值

### 2.2 两值上下/左右
- **用例名**: `Parse_TwoValues_VerticalHorizontal`
- **输入**: `"10,20"`
- **预期**: Left=20, Top=10, Right=20, Bottom=10
- **验证点**: 两值展开（上下=第一个，左右=第二个）

### 2.3 三值展开
- **用例名**: `Parse_ThreeValues_Expanded`
- **输入**: `"10,20,30"`
- **预期**: Left=20, Top=10, Right=20, Bottom=30
- **验证点**: 三值展开

### 2.4 四值左/上/右/下
- **用例名**: `Parse_FourValues_LeftTopRightBottom`
- **输入**: `"10,20,30,40"`
- **预期**: Left=10, Top=20, Right=30, Bottom=40
- **验证点**: 四值独立

### 2.5 null 输入
- **用例名**: `Parse_Null_ReturnsNull`
- **输入**: `null`
- **预期**: 返回 null
- **验证点**: null 处理

### 2.6 空字符串
- **用例名**: `Parse_EmptyString_ReturnsNull`
- **输入**: `""`
- **预期**: 返回 null
- **验证点**: 空串

### 2.7 无效数值
- **用例名**: `Parse_InvalidNumber_ReturnsNull`
- **输入**: `"abc"`
- **预期**: 返回 null
- **验证点**: 无效值

### 2.8 负数
- **用例名**: `Parse_NegativeValues_Parsed`
- **输入**: `"-10,20,-30,40"`
- **预期**: Left=-10, Top=20, Right=-30, Bottom=40
- **验证点**: 负值

### 2.9 小数
- **用例名**: `Parse_DecimalValues_Parsed`
- **输入**: `"10.5,20.3"`
- **预期**: Left=20.3, Top=10.5, Right=20.3, Bottom=10.5
- **验证点**: 小数

### 2.10 超过 4 个值
- **用例名**: `Parse_MoreThanFourValues_FirstFourUsed`
- **输入**: `"1,2,3,4,5"`
- **预期**: 只取前 4 个，Left=1, Top=2, Right=3, Bottom=4
- **验证点**: 截断

---

## 3. SlideMlShadow.Parse

### 3.1 完整 4 部分
- **用例名**: `Parse_FourParts_AllProperties`
- **输入**: `"0 4 12 #00000033"`
- **预期**: OffsetX=0, OffsetY=4, Blur=12, Color="#00000033", Opacity=1（默认）
- **验证点**: 各属性

### 3.2 仅 2 个值
- **用例名**: `Parse_TwoParts_OffsetOnly`
- **输入**: `"0 4"`
- **预期**: OffsetX=0, OffsetY=4, Blur=12（默认）, Color="#00000033"（默认）, Opacity=1
- **验证点**: 部分值 + 默认值

### 3.3 仅 3 个值
- **用例名**: `Parse_ThreeParts_OffsetAndBlur`
- **输入**: `"0 4 24"`
- **预期**: OffsetX=0, OffsetY=4, Blur=24, Color="#00000033"（默认）
- **验证点**: 三值

### 3.4 仅 1 个值
- **用例名**: `Parse_OnePart_OffsetXOnly`
- **输入**: `"5"`
- **预期**: OffsetX=5, OffsetY=4（默认）, Blur=12（默认）, Color="#00000033"（默认）
- **验证点**: 单值

### 3.5 null 输入
- **用例名**: `Parse_Null_ReturnsNull`
- **输入**: `null`
- **预期**: 返回 null
- **验证点**: null

### 3.6 空字符串
- **用例名**: `Parse_EmptyString_ReturnsNull`
- **输入**: `""`
- **预期**: 返回 null
- **验证点**: 空串

### 3.7 颜色含 # 号
- **用例名**: `Parse_ColorWithHash_ColorParsed`
- **输入**: `"0 8 24 #FF0000"`
- **预期**: Color="#FF0000"
- **验证点**: # 号保留

### 3.8 颜色不含 # 号
- **用例名**: `Parse_ColorWithoutHash_ColorParsed`
- **输入**: `"0 8 24 FF0000"`
- **预期**: Color="FF0000"
- **验证点**: 无 # 号

### 3.9 负偏移
- **用例名**: `Parse_NegativeOffset_Parsed`
- **输入**: `"-2 4 12 #00000033"`
- **预期**: OffsetX=-2, OffsetY=4
- **验证点**: 负偏移

### 3.10 小数值
- **用例名**: `Parse_DecimalValues_Parsed`
- **输入**: `"0.5 4.5 12.8 #00000033"`
- **预期**: OffsetX=0.5, OffsetY=4.5, Blur=12.8
- **验证点**: 小数

### 3.11 多余空格
- **用例名**: `Parse_ExtraSpaces_Parsed`
- **输入**: `"  0   4   12   #00000033  "`
- **预期**: 正确解析
- **验证点**: 多余空格

---

## 4. SlideMlXmlUtilities.ExtractXml

### 4.1 包含 `<?xml` 声明
- **用例名**: `ExtractXml_WithXmlDeclaration_Extracted`
- **输入**: `"Some text\n<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<Page></Page>"`
- **预期**: 返回 `"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<Page></Page>"`
- **验证点**: 声明头被保留

### 4.2 以 `<Page` 开头
- **用例名**: `ExtractXml_StartsWithPage_DeclarationAdded`
- **输入**: `"<Page Background=\"#FFF\"></Page>"`
- **预期**: 返回 `"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<Page Background=\"#FFF\"></Page>"`
- **验证点**: 添加声明头

### 4.3 包含 markdown 代码块
- **用例名**: `ExtractXml_MarkdownCodeBlock_XmlExtracted`
- **输入**: `` "```xml\n<Page></Page>\n```" ``
- **预期**: 返回 `"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<Page></Page>"`
- **验证点**: Markdown 代码块被去除

### 4.4 前后多余文本
- **用例名**: `ExtractXml_LeadingTrailingText_Extracted`
- **输入**: `"Here is the slide:\n<Page><Rect Width=\"100\" Height=\"50\"/></Page>\n---END---"`
- **预期**: 提取 `<Page>` 部分
- **验证点**: 多余文本去除

### 4.5 纯 XML 无多余文本
- **用例名**: `ExtractXml_PureXml_Unchanged`
- **输入**: `"<Page></Page>"`
- **预期**: 添加声明头后返回
- **验证点**: 纯 XML

### 4.6 空字符串
- **用例名**: `ExtractXml_EmptyString_ReturnsEmpty`
- **输入**: `""`
- **预期**: 返回 `""`（trim 后为空）
- **验证点**: 空串

### 4.7 null 输入
- **用例名**: `ExtractXml_Null_ThrowsArgumentNullException`
- **输入**: `null`
- **预期**: 抛出 `ArgumentNullException`
- **验证点**: null 检查

### 4.8 无 XML 内容的纯文本
- **用例名**: `ExtractXml_PlainText_ReturnsTrimmed`
- **输入**: `"这是一段纯文本"`
- **预期**: 返回 `"这是一段纯文本"`（trim 后）
- **验证点**: 无 XML 内容

---

## 5. SlideMlXmlUtilities.NormalizeXml

### 5.1 基本规范化
- **用例名**: `NormalizeXml_BasicXml_Normalized`
- **输入**: `"<Page><Rect Width=\"100\"/></Page>"`
- **预期**: 格式化的 XML 字符串
- **验证点**: 输出为合法 XML

### 5.2 已规范化的 XML
- **用例名**: `NormalizeXml_AlreadyNormalized_Unchanged`
- **输入**: 已格式化的 XML
- **预期**: 与输入一致
- **验证点**: 幂等性

### 5.3 含注释
- **用例名**: `NormalizeXml_WithComment_Preserved`
- **输入**: `"<Page><!-- comment --><Rect Width=\"100\"/></Page>"`
- **预期**: 注释被保留
- **验证点**: 注释

### 5.4 null 输入
- **用例名**: `NormalizeXml_Null_ThrowsArgumentNullException`
- **输入**: `null`
- **预期**: 抛出 `ArgumentNullException`
- **验证点**: null 检查

### 5.5 格式错误 XML
- **用例名**: `NormalizeXml_MalformedXml_ThrowsXmlException`
- **输入**: `"<Page><Rect></Page>"`
- **预期**: 抛出 `XmlException`
- **验证点**: 格式错误

---

## 6. SlideMlXmlUtilities.FormatRenderedXml

### 6.1 Page 回填 ActualWidth/ActualHeight
- **用例名**: `FormatRenderedXml_Page_BackfillsCanvasSize`
- **输入**: `"<Page Background=\"#FFF\"><Rect Id=\"r1\" Width=\"100\" Height=\"50\"/></Page>"`
- **Page 模型**: 默认 SlideMlPage，Children 含 Rect(Id="r1", W=100, H=50)
- **预期**: 输出 XML 中 Page 有 `ActualWidth="1280"` `ActualHeight="720"`
- **验证点**: 画布尺寸回填

### 6.2 各元素类型回填
- **用例名**: `FormatRenderedXml_AllElementTypes_Backfilled`
- **输入**: 含 Panel、Rect、TextElement、Image 的 XML
- **预期**: 每个元素都有 ActualWidth/ActualHeight
- **验证点**: 全类型回填

### 6.3 ActualLineCount 仅 TextElement
- **用例名**: `FormatRenderedXml_ActualLineCount_OnlyOnText`
- **输入**: 含 TextElement 和 Rect 的 XML
- **Page 模型**: TextElement(Id="t1", ActualLineCount=3)
- **预期**: t1 有 `ActualLineCount="3"`，Rect 没有
- **验证点**: 行数回填

### 6.4 嵌套元素
- **用例名**: `FormatRenderedXml_NestedElements_AllBackfilled`
- **输入**: `<Page><Panel Id="outer"><Panel Id="inner"><Rect Id="leaf"/></Panel></Panel></Page>`
- **预期**: outer、inner、leaf 均有 ActualWidth/ActualHeight
- **验证点**: 嵌套回填

### 6.5 Id 在 page 中不存在（跳过）
- **用例名**: `FormatRenderedXml_IdNotFound_Skipped`
- **输入**: `<Page><Rect Id="r1"/></Page>`
- **Page 模型**: 无子元素（或 Children 为空）
- **预期**: 输出 XML 中 r1 不添加 ActualWidth/ActualHeight（因为 FindMetrics 找不到）
- **验证点**: 不存在的 Id 跳过

### 6.6 null 参数
- **用例名**: `FormatRenderedXml_NullXml_ThrowsArgumentNullException`
- **输入**: xml=null
- **预期**: 抛出 `ArgumentNullException`
- **验证点**: null 检查

### 6.7 null page 参数
- **用例名**: `FormatRenderedXml_NullPage_ThrowsArgumentNullException`
- **输入**: page=null
- **预期**: 抛出 `ArgumentNullException`
- **验证点**: null 检查

### 6.8 原始属性保留
- **用例名**: `FormatRenderedXml_OriginalAttributes_Preserved`
- **输入**: `<Rect Id="r1" X="10" Y="20" Width="100" Height="50" Fill="#FF0000" CornerRadius="8"/>`
- **预期**: 输出 XML 保留 X/Y/Width/Height/Fill/CornerRadius，新增 ActualWidth/ActualHeight
- **验证点**: 原属性不被移除

---

## 7. SlideMlXmlUtilities.FormatNumber

### 7.1 整数去零
- **用例名**: `FormatNumber_Integer_NoDecimal`
- **输入**: `100.0`
- **预期**: `"100"`
- **验证点**: 去零

### 7.2 两位小数
- **用例名**: `FormatNumber_TwoDecimals_Formatted`
- **输入**: `100.25`
- **预期**: `"100.25"`
- **验证点**: 保留两位

### 7.3 多位小数截断
- **用例名**: `FormatNumber_ManyDecimals_Rounded`
- **输入**: `100.2567`
- **预期**: `"100.26"`（Math.Round 到两位）
- **验证点**: 四舍五入

### 7.4 零
- **用例名**: `FormatNumber_Zero_Formatted`
- **输入**: `0`
- **预期**: `"0"`
- **验证点**: 零

### 7.5 负数
- **用例名**: `FormatNumber_Negative_Formatted`
- **输入**: `-50.5`
- **预期**: `"-50.5"`
- **验证点**: 负数

### 7.6 大数
- **用例名**: `FormatNumber_LargeNumber_Formatted`
- **输入**: `1280`
- **预期**: `"1280"`
- **验证点**: 大数

### 7.7 去除多余零
- **用例名**: `FormatNumber_TrailingZeros_Removed`
- **输入**: `100.50`
- **预期**: `"100.5"`（`0.##` 格式）
- **验证点**: 去尾零

---

## 8. SlideMlRect

### 8.1 构造函数
- **用例名**: `Constructor_SetsProperties`
- **输入**: `new SlideMlRect(10, 20, 100, 50)`
- **预期**: X=10, Y=20, Width=100, Height=50
- **验证点**: 四个属性

### 8.2 Right 属性
- **用例名**: `Right_CalculatedCorrectly`
- **输入**: `new SlideMlRect(10, 20, 100, 50)`
- **预期**: Right=110
- **验证点**: X + Width

### 8.3 Bottom 属性
- **用例名**: `Bottom_CalculatedCorrectly`
- **输入**: `new SlideMlRect(10, 20, 100, 50)`
- **预期**: Bottom=70
- **验证点**: Y + Height

### 8.4 CenterX
- **用例名**: `CenterX_CalculatedCorrectly`
- **输入**: `new SlideMlRect(10, 20, 100, 50)`
- **预期**: CenterX=60
- **验证点**: X + Width/2

### 8.5 CenterY
- **用例名**: `CenterY_CalculatedCorrectly`
- **输入**: `new SlideMlRect(10, 20, 100, 50)`
- **预期**: CenterY=45
- **验证点**: Y + Height/2

### 8.6 Equals 相同值
- **用例名**: `Equals_SameValues_ReturnsTrue`
- **输入**: `new SlideMlRect(10,20,100,50)` vs `new SlideMlRect(10,20,100,50)`
- **预期**: `true`
- **验证点**: 值相等

### 8.7 Equals 不同值
- **用例名**: `Equals_DifferentValues_ReturnsFalse`
- **输入**: `new SlideMlRect(10,20,100,50)` vs `new SlideMlRect(10,20,200,50)`
- **预期**: `false`
- **验证点**: 值不等

### 8.8 零尺寸
- **用例名**: `ZeroSize_PropertiesCorrect`
- **输入**: `new SlideMlRect(0, 0, 0, 0)`
- **预期**: 所有属性为 0
- **验证点**: 零尺寸

---

## 9. SlideMlPipelineContext

### 9.1 默认画布尺寸
- **用例名**: `DefaultConstructor_DefaultCanvasSize`
- **输入**: `new SlideMlPipelineContext()`
- **预期**: CanvasWidth=1280, CanvasHeight=720
- **验证点**: 默认值

### 9.2 自定义画布尺寸
- **用例名**: `CustomCanvasSize_Constructor`
- **输入**: `new SlideMlPipelineContext(1920, 1080)`
- **预期**: CanvasWidth=1920, CanvasHeight=1080
- **验证点**: 自定义值

### 9.3 AddWarning
- **用例名**: `AddWarning_WarningAdded`
- **输入**: `context.AddWarning("test warning")`
- **预期**: context.Warnings.Count==1，包含 "test warning"
- **验证点**: 警告添加

### 9.4 AddWarnings 批量
- **用例名**: `AddWarnings_Multiple_AllAdded`
- **输入**: `context.AddWarnings(["w1", "w2"])`
- **预期**: Warnings.Count==2
- **验证点**: 批量添加

### 9.5 AddError
- **用例名**: `AddError_ErrorAdded`
- **输入**: `context.AddError("test error")`
- **预期**: context.Errors.Count==1，包含 "test error"
- **验证点**: 错误添加

### 9.6 AddErrors 批量
- **用例名**: `AddErrors_Multiple_AllAdded`
- **输入**: `context.AddErrors(["e1", "e2"])`
- **预期**: Errors.Count==2
- **验证点**: 批量添加

### 9.7 Reset 清空
- **用例名**: `Reset_ClearsWarningsAndErrors`
- **步骤**: 添加 Warning 和 Error，调用 Reset
- **预期**: Warnings.Count==0, Errors.Count==0
- **验证点**: 清空

### 9.8 Warnings/Errors 只读
- **用例名**: `Warnings_ReadOnly_Throws`
- **预期**: 尝试修改 IReadOnlyList 会编译错误或运行时异常
- **验证点**: 只读性

### 9.9 MaterialResourceManager 非空
- **用例名**: `MaterialResourceManager_NotNull`
- **预期**: MaterialResourceManager 不为 null
- **验证点**: 默认初始化

---

## 10. SlideMlElementMeasurements

### 10.1 TryGetValue 存在的 Id
- **用例名**: `TryGetValue_ExistingId_ReturnsTrue`
- **输入**: 字典含 "t1" → (100, 50, 1)
- **预期**: `TryGetValue("t1", out result)` 返回 true，result.MeasuredWidth=100, MeasuredHeight=50, ActualLineCount=1
- **验证点**: 查找成功

### 10.2 TryGetValue 不存在的 Id
- **用例名**: `TryGetValue_NonExistingId_ReturnsFalse`
- **输入**: 空字典
- **预期**: `TryGetValue("nonexist", out _)` 返回 false
- **验证点**: 查找失败

### 10.3 Find 存在的 Id
- **用例名**: `Find_ExistingId_ReturnsResult`
- **输入**: 字典含 "t1" → (100, 50, 1)
- **预期**: `Find("t1")` 返回 SlideMlMeasureResult，Width=100, Height=50
- **验证点**: Find 成功

### 10.4 Find 不存在的 Id
- **用例名**: `Find_NonExistingId_ReturnsNull`
- **输入**: 空字典
- **预期**: `Find("nonexist")` 返回 null
- **验证点**: Find 失败

### 10.5 构造函数 null 参数
- **用例名**: `Constructor_NullDictionary_ThrowsArgumentNullException`
- **输入**: `null`
- **预期**: 抛出 `ArgumentNullException`
- **验证点**: null 检查

---

## 11. SlideMlMeasureResult

### 11.1 构造后属性正确
- **用例名**: `Constructor_PropertiesSet`
- **输入**: `new SlideMlMeasureResult { MeasuredWidth=100, MeasuredHeight=50, ActualLineCount=2 }`
- **预期**: MeasuredWidth=100, MeasuredHeight=50, ActualLineCount=2
- **验证点**: 属性值

### 11.2 ActualLineCount 可为 null
- **用例名**: `ActualLineCount_Nullable`
- **输入**: `new SlideMlMeasureResult { MeasuredWidth=100, MeasuredHeight=50 }`
- **预期**: ActualLineCount==null
- **验证点**: 可空

---

## 12. SlideMlRenderedMetrics

### 12.1 构造后属性正确
- **用例名**: `Constructor_PropertiesSet`
- **输入**: `new SlideMlRenderedMetrics { ActualWidth=200, ActualHeight=100, ActualLineCount=3 }`
- **预期**: ActualWidth=200, ActualHeight=100, ActualLineCount=3
- **验证点**: 属性值

### 12.2 ActualLineCount 可为 null
- **用例名**: `ActualLineCount_Nullable`
- **输入**: `new SlideMlRenderedMetrics { ActualWidth=200, ActualHeight=100 }`
- **预期**: ActualLineCount==null
- **验证点**: 可空

---

## 13. SlideMlLayoutEngine.SlideRectContains（内部方法）

### 13.1 完全包含
- **用例名**: `SlideRectContains_ContainerContainsInner_ReturnsTrue`
- **输入**: container=(0,0,200,200), inner=(10,10,100,100)
- **预期**: true
- **验证点**: 包含

### 13.2 超出右侧
- **用例名**: `SlideRectContains_InnerExceedsRight_ReturnsFalse`
- **输入**: container=(0,0,200,200), inner=(150,10,100,100)
- **预期**: false（Right=250 > 200）
- **验证点**: 右超

### 13.3 超出底部
- **用例名**: `SlideRectContains_InnerExceedsBottom_ReturnsFalse`
- **输入**: container=(0,0,200,200), inner=(10,150,100,100)
- **预期**: false（Bottom=250 > 200）
- **验证点**: 底超

### 13.4 完全在外部
- **用例名**: `SlideRectContains_InnerOutside_ReturnsFalse`
- **输入**: container=(0,0,200,200), inner=(300,300,100,100)
- **预期**: false
- **验证点**: 外部

### 13.5 边界对齐
- **用例名**: `SlideRectContains_ExactlyAtEdge_ReturnsTrue`
- **输入**: container=(0,0,200,200), inner=(0,0,200,200)
- **预期**: true（X>=container.X, Y>=container.Y, Right<=container.Right, Bottom<=container.Bottom 均满足）
- **验证点**: 边界

---

## 14. SlideMlLayoutEngine.ResolveOrigin（内部方法）

### 14.1 显式 X → parentOrigin + offset
- **用例名**: `ResolveOrigin_Horizontal_ExplicitOffset`
- **输入**: parentOrigin=0, parentSize=1280, elementSize=100, explicitOffset=50, alignment=Left
- **预期**: 50
- **验证点**: 显式偏移

### 14.2 HorizontalAlignment.Center
- **用例名**: `ResolveOrigin_Horizontal_Center`
- **输入**: parentOrigin=0, parentSize=1280, elementSize=200, explicitOffset=null, alignment=Center
- **预期**: 540
- **验证点**: 居中

### 14.3 HorizontalAlignment.Right
- **用例名**: `ResolveOrigin_Horizontal_Right`
- **输入**: parentOrigin=0, parentSize=1280, elementSize=200, explicitOffset=null, alignment=Right
- **预期**: 1080
- **验证点**: 右对齐

### 14.4 HorizontalAlignment.Left（默认）
- **用例名**: `ResolveOrigin_Horizontal_Left_Default`
- **输入**: parentOrigin=100, parentSize=1280, elementSize=200, explicitOffset=null, alignment=Left
- **预期**: 100（parentOrigin）
- **验证点**: 左对齐

### 14.5 元素比父容器大时 max(0, ...) 保护
- **用例名**: `ResolveOrigin_ElementLargerThanParent_Center_ReturnsZero`
- **输入**: parentOrigin=0, parentSize=200, elementSize=400, explicitOffset=null, alignment=Center
- **预期**: 0（Math.Max(0, (200-400)/2)=0）
- **验证点**: 不产生负坐标

### 14.6 VerticalAlignment.Center
- **用例名**: `ResolveOrigin_Vertical_Center`
- **输入**: parentOrigin=0, parentSize=600, elementSize=200, explicitOffset=null, alignment=Center
- **预期**: 200
- **验证点**: 垂直居中

### 14.7 VerticalAlignment.Bottom
- **用例名**: `ResolveOrigin_Vertical_Bottom`
- **输入**: parentOrigin=0, parentSize=600, elementSize=200, explicitOffset=null, alignment=Bottom
- **预期**: 400
- **验证点**: 底部对齐

### 14.8 VerticalAlignment.Top（默认）
- **用例名**: `ResolveOrigin_Vertical_Top_Default`
- **输入**: parentOrigin=50, parentSize=600, elementSize=200, explicitOffset=null, alignment=Top
- **预期**: 50
- **验证点**: 顶部对齐

### 14.9 null alignment 回退到默认
- **用例名**: `ResolveOrigin_NullAlignment_ReturnsParentOrigin`
- **输入**: parentOrigin=50, parentSize=600, elementSize=200, explicitOffset=null, alignment=null
- **预期**: 50
- **验证点**: null 对齐