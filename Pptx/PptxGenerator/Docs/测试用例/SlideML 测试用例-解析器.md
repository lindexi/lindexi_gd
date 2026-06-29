# SlideML 解析器单元测试用例

> 目标类：`SlideMlParser`（`Models/SlideMlParser.cs`）
> 测试框架：MSTest
> 命名规范：`方法名_场景_预期行为`

---

## 1. 根元素解析

### 1.1 正常 Page 根元素
- **用例名**: `Parse_PageRoot_ReturnsPage`
- **输入**: `<Page Background="#FFFFFF"></Page>`
- **预期**: 返回 SlideMlPage，Background="#FFFFFF"，Children.Count==0
- **验证点**: Background 值、Children 为空

### 1.2 非 Page 根元素
- **用例名**: `Parse_NonPageRoot_ThrowsRootElementException`
- **输入**: `<NotPage></NotPage>`
- **预期**: 抛出 `SlideMlRootElementException`
- **验证点**: 异常类型、消息包含"根元素"

### 1.3 空根元素
- **用例名**: `Parse_EmptyPage_ReturnsEmptyPage`
- **输入**: `<Page></Page>`
- **预期**: 返回 SlideMlPage，Background 默认 "#FFFFFF"，Children.Count==0
- **验证点**: Background 默认值、Children 为空

### 1.4 根元素带 Background 属性
- **用例名**: `Parse_PageWithBackground_BackgroundParsed`
- **输入**: `<Page Background="#1A1A2E"></Page>`
- **预期**: Background == "#1A1A2E"
- **验证点**: Background 值

### 1.5 XML 格式错误
- **用例名**: `Parse_MalformedXml_ThrowsXmlException`
- **输入**: `<Page><Rect></Page>`
- **预期**: 抛出 `System.Xml.XmlException`
- **验证点**: 异常类型

### 1.6 空字符串
- **用例名**: `Parse_EmptyString_ThrowsXmlException`
- **输入**: `""`
- **预期**: 抛出 `XmlException`
- **验证点**: 异常类型

### 1.7 Page 含未知属性
- **用例名**: `Parse_PageWithUnknownAttribute_GeneratesWarning`
- **输入**: `<Page Foo="bar"></Page>`
- **预期**: context.Warnings 包含 `[Warning] Page: 未知属性 "Foo"，已忽略`
- **验证点**: Warnings.Count==1，消息内容

---

## 2. Panel 元素解析

### 2.1 Panel 所有属性
- **用例名**: `Parse_Panel_AllAttributes_Parsed`
- **输入**: `<Panel Id="p1" X="10" Y="20" Width="100" Height="80" Padding="8" Background="#FF0000" Layout="Horizontal" Gap="12" Opacity="0.5" HorizontalAlignment="Center" VerticalAlignment="Top" Margin="0,0,0,8"></Panel>`
- **预期**: 所有属性正确映射到 SlideMlPanelElement
- **验证点**: Id, X, Y, Width, Height, Padding, Background(Color), Layout, Gap, Opacity, HorizontalAlignment, VerticalAlignment, Margin

### 2.2 Panel 默认值
- **用例名**: `Parse_Panel_DefaultValues`
- **输入**: `<Panel></Panel>`
- **预期**: Layout==Absolute, Gap==0, Padding==0, Opacity==1, Background==null
- **验证点**: 各默认值

### 2.3 Panel 含 Fill 子元素（渐变背景）
- **用例名**: `Parse_Panel_WithGradientFill_BackgroundIsGradient`
- **输入**: `<Panel><Fill><LinearGradient X1="0" Y1="0" X2="1" Y2="1"><Stop Offset="0" Color="#FF0000"/><Stop Offset="1" Color="#00FF00"/></LinearGradient></Fill></Panel>`
- **预期**: Background 为 SlideMlLinearGradientBrush 类型，Stops.Count==2
- **验证点**: Background 类型、Stops[0].Offset/Color、Stops[1].Offset/Color、X1/Y1/X2/Y2

### 2.4 Panel Fill 子元素优先于 Background 属性
- **用例名**: `Parse_Panel_GradientFillOverridesBackgroundAttribute`
- **输入**: `<Panel Background="#FF0000"><Fill><LinearGradient><Stop Offset="0" Color="#000000"/><Stop Offset="1" Color="#FFFFFF"/></LinearGradient></Fill></Panel>`
- **预期**: Background 为 SlideMlLinearGradientBrush，非 SolidColorBrush
- **验证点**: Background 类型

### 2.5 Panel 含未知属性
- **用例名**: `Parse_Panel_UnknownAttribute_GeneratesWarning`
- **输入**: `<Panel Foo="bar"></Panel>`
- **预期**: Warnings 包含 `未知属性 "Foo"`
- **验证点**: Warnings.Count==1

### 2.6 Panel 含未知子标签
- **用例名**: `Parse_Panel_UnknownChildTag_GeneratesWarning`
- **输入**: `<Panel><UnknownTag/></Panel>`
- **预期**: Warnings 包含 `未知标签 "UnknownTag"`
- **验证点**: Warnings 内容

### 2.7 Panel Layout 无效枚举值
- **用例名**: `Parse_Panel_InvalidLayout_GeneratesError`
- **输入**: `<Panel Layout="Diagonal"></Panel>`
- **预期**: Errors 包含 `Layout 值 "Diagonal" 无效`，Layout 使用默认值 Absolute
- **验证点**: Errors.Count==1，Layout==Absolute

### 2.8 Panel Padding 非数值
- **用例名**: `Parse_Panel_InvalidPadding_GeneratesError`
- **输入**: `<Panel Padding="abc"></Panel>`
- **预期**: Errors 包含 `Padding 值 "abc" 不是有效的数值`，Padding==0
- **验证点**: Errors.Count==1，Padding 默认值

---

## 3. Rect 元素解析

### 3.1 Rect 所有属性
- **用例名**: `Parse_Rect_AllAttributes_Parsed`
- **输入**: `<Rect Id="r1" X="10" Y="20" Width="100" Height="50" Fill="#FF0000" Stroke="#000000" StrokeThickness="2" CornerRadius="8" Shadow="0 4 12 #00000033" Opacity="0.8" StrokeDashArray="4,2" Margin="0,0,0,8"></Rect>`
- **预期**: 所有属性正确映射
- **验证点**: Id, X, Y, Width, Height, Fill(Color), Stroke(Color), StrokeThickness, CornerRadius(四角==8), Shadow, ShadowString, StrokeDashArray, Opacity, Margin

### 3.2 Rect 四角独立圆角
- **用例名**: `Parse_Rect_FourCornerRadii_Parsed`
- **输入**: `<Rect CornerRadius="8,16,8,16"></Rect>`
- **预期**: CornerRadius.TopLeft==8, TopRight==16, BottomRight==8, BottomLeft==16
- **验证点**: 四个角的值

### 3.3 Rect 两值圆角
- **用例名**: `Parse_Rect_TwoCornerRadii_Parsed`
- **输入**: `<Rect CornerRadius="8,16"></Rect>`
- **预期**: TopLeft==8, TopRight==16, BottomRight==8, BottomLeft==16
- **验证点**: 对角相同的展开规则

### 3.4 Rect 三值圆角
- **用例名**: `Parse_Rect_ThreeCornerRadii_Parsed`
- **输入**: `<Rect CornerRadius="8,16,8"></Rect>`
- **预期**: TopLeft==8, TopRight==16, BottomRight==8, BottomLeft==16
- **验证点**: 三值展开规则

### 3.5 Rect 含 Fill 渐变子元素
- **用例名**: `Parse_Rect_WithGradientFill_FillIsGradient`
- **输入**: `<Rect><Fill><LinearGradient X1="0" Y1="0" X2="1" Y2="0"><Stop Offset="0" Color="#4A7BF7"/><Stop Offset="1" Color="#F4F6FA"/></LinearGradient></Fill></Rect>`
- **预期**: Fill 为 SlideMlLinearGradientBrush
- **验证点**: Fill 类型、Stops.Count、X1/Y1/X2/Y2

### 3.6 Rect 含 Stroke 渐变子元素
- **用例名**: `Parse_Rect_WithGradientStroke_StrokeIsGradient`
- **输入**: `<Rect StrokeThickness="2"><Stroke><LinearGradient><Stop Offset="0" Color="#4A7BF7"/><Stop Offset="1" Color="#6C5CE7"/></LinearGradient></Stroke></Rect>`
- **预期**: Stroke 为 SlideMlLinearGradientBrush
- **验证点**: Stroke 类型

### 3.7 Rect 含 Shadow 子元素（精细阴影）
- **用例名**: `Parse_Rect_WithShadowElement_ShadowParsed`
- **输入**: `<Rect><Shadow OffsetX="0" OffsetY="8" Blur="24" Color="#000000" Opacity="0.12"/></Rect>`
- **预期**: Shadow 对象 OffsetX==0, OffsetY==8, Blur==24, Color=="#000000", Opacity==0.12
- **验证点**: Shadow 各属性值

### 3.8 Rect Shadow 子元素优先于 Shadow 属性
- **用例名**: `Parse_Rect_ShadowElementOverridesShadowAttribute`
- **输入**: `<Rect Shadow="0 4 12 #00000033"><Shadow OffsetX="0" OffsetY="8" Blur="24" Color="#000000" Opacity="0.12"/></Rect>`
- **预期**: Shadow.OffsetY==8（子元素值），ShadowString=="0 4 12 #00000033"（原始属性保留）
- **验证点**: Shadow.OffsetY、ShadowString

### 3.9 Rect StrokeDashArray 解析
- **用例名**: `Parse_Rect_StrokeDashArray_Parsed`
- **输入**: `<Rect StrokeDashArray="4,2,1,2"></Rect>`
- **预期**: StrokeDashArray.Count==4, 值为 [4,2,1,2]
- **验证点**: 列表长度和值

### 3.10 Rect StrokeDashArray 含无效值
- **用例名**: `Parse_Rect_StrokeDashArrayInvalidValue_GeneratesError`
- **输入**: `<Rect StrokeDashArray="4,abc,2"></Rect>`
- **预期**: Errors 包含 `包含无效数值`，StrokeDashArray==null
- **验证点**: Errors.Count==1，StrokeDashArray 为 null

### 3.11 Rect 未知子标签
- **用例名**: `Parse_Rect_UnknownChildTag_GeneratesWarning`
- **输入**: `<Rect><UnknownTag/></Rect>`
- **预期**: Warnings 包含 `Rect 下未知子标签`
- **验证点**: Warnings 内容

---

## 4. TextElement 解析

### 4.1 TextElement 所有属性
- **用例名**: `Parse_TextElement_AllAttributes_Parsed`
- **输入**: `<TextElement Id="t1" X="10" Y="20" Width="400" Height="30" Text="Hello" FontName="Arial" FontSize="24" Foreground="#333333" TextAlignment="Center" IsBold="True" IsItalic="True" Opacity="0.9" Margin="0,0,0,8"></TextElement>`
- **预期**: 所有属性正确映射
- **验证点**: 所有属性值

### 4.2 TextElement 默认值
- **用例名**: `Parse_TextElement_DefaultValues`
- **输入**: `<TextElement Text="Hi"></TextElement>`
- **预期**: FontName=="Microsoft YaHei", FontSize==16, Foreground=="#000000", TextAlignment==Left, IsBold==null, IsItalic==null, Spans==null
- **验证点**: 各默认值

### 4.3 TextElement 含 Span 子元素
- **用例名**: `Parse_TextElement_WithSpans_SpansParsed`
- **输入**: `<TextElement><Span Text="标题" FontSize="24" Foreground="#333" IsBold="True"/><Span Text=" — 副标题" FontSize="14" Foreground="#666"/></TextElement>`
- **预期**: Spans.Count==2，Text 自动拼接为 "标题 — 副标题"
- **验证点**: Spans[0]/Spans[1] 各属性、Text 值

### 4.4 TextElement 无 Text 且无 Span
- **用例名**: `Parse_TextElement_NoTextNoSpan_ThrowsException`
- **输入**: `<TextElement></TextElement>`
- **预期**: 抛出 `SlideMlRequiredAttributeMissingException`
- **验证点**: 异常类型、ElementId、AttributeName=="Text"

### 4.5 TextElement 有 Span 无 Text
- **用例名**: `Parse_TextElement_SpanOnly_TextConcatenated`
- **输入**: `<TextElement><Span Text="A"/><Span Text="B"/></TextElement>`
- **预期**: Text=="AB"，Spans.Count==2
- **验证点**: Text 值

### 4.6 Span 无 Text 属性
- **用例名**: `Parse_Span_NoText_ThrowsException`
- **输入**: `<TextElement><Span FontSize="14"/></TextElement>`
- **预期**: 抛出 `SlideMlRequiredAttributeMissingException`
- **验证点**: 异常类型

### 4.7 IsBold 各种值
- **用例名**: `Parse_TextElement_IsBold_VariousValues`
- **输入参数化**:
  - `"True"` → IsBold==true
  - `"true"` → IsBold==true
  - `"False"` → IsBold==false
  - `"false"` → IsBold==false
  - `"yes"` → IsBold==null（bool.TryParse 失败）
  - 不写 → IsBold==null
- **验证点**: IsBold 值

### 4.8 TextAlignment 无效值
- **用例名**: `Parse_TextElement_InvalidTextAlignment_GeneratesError`
- **输入**: `<TextElement Text="Hi" TextAlignment="Justified"></TextElement>`
- **预期**: Errors 包含 `TextAlignment 值 "Justified" 无效`，TextAlignment==Left（默认）
- **验证点**: Errors.Count==1，TextAlignment 默认值

---

## 5. Image 元素解析

### 5.1 Image 所有属性
- **用例名**: `Parse_Image_AllAttributes_Parsed`
- **输入**: `<Image Id="img1" X="10" Y="20" Width="400" Height="300" Source="img_001" Stretch="UniformToFill" Opacity="0.8" HorizontalAlignment="Center" VerticalAlignment="Bottom" Margin="0,0,0,8"></Image>`
- **预期**: 所有属性正确映射
- **验证点**: 所有属性值

### 5.2 Image 默认值
- **用例名**: `Parse_Image_DefaultValues`
- **输入**: `<Image Source="img_001"></Image>`
- **预期**: Stretch==Uniform, Opacity==1
- **验证点**: 默认值

### 5.3 Image 无 Source
- **用例名**: `Parse_Image_NoSource_ThrowsException`
- **输入**: `<Image></Image>`
- **预期**: 抛出 `SlideMlRequiredAttributeMissingException`
- **验证点**: 异常类型、AttributeName=="Source"

### 5.4 Image Stretch 无效值
- **用例名**: `Parse_Image_InvalidStretch_GeneratesError`
- **输入**: `<Image Source="img" Stretch="Cover"></Image>`
- **预期**: Errors 包含 `Stretch 值 "Cover" 无效`，Stretch==Uniform（默认）
- **验证点**: Errors.Count==1，Stretch 默认值

---

## 6. Id 自动生成

### 6.1 无 Id 自动分配
- **用例名**: `Parse_NoId_AutoAssigned`
- **输入**: `<Page><Rect Width="100" Height="50"/></Page>`
- **预期**: Rect.Id 匹配 `elem_001` 格式
- **验证点**: Id 值匹配模式 `elem_\d{3}`

### 6.2 多个无 Id 元素递增
- **用例名**: `Parse_MultipleNoId_IncrementingIds`
- **输入**: `<Page><Rect/><Rect/><Rect/></Page>`
- **预期**: 三个 Rect 的 Id 分别为 elem_001, elem_002, elem_003
- **验证点**: Id 值递增

### 6.3 有 Id 保留原值
- **用例名**: `Parse_WithId_IdPreserved`
- **输入**: `<Page><Rect Id="my-card"/></Page>`
- **预期**: Rect.Id == "my-card"
- **验证点**: Id 值

### 6.4 混合有 Id 和无 Id
- **用例名**: `Parse_MixedIdAndNoId_CorrectAssignment`
- **输入**: `<Page><Rect Id="named"/><Rect/><TextElement Text="hi"/></Page>`
- **预期**: 第一个 Id="named"，第二个 elem_001，第三个 elem_002
- **验证点**: 各元素 Id

---

## 7. 渐变解析

### 7.1 LinearGradient 基本解析
- **用例名**: `Parse_LinearGradient_BasicAttributes`
- **输入**: `<Rect><Fill><LinearGradient X1="0" Y1="0" X2="1" Y2="1"><Stop Offset="0" Color="#FF0000"/><Stop Offset="1" Color="#00FF00"/></LinearGradient></Fill></Rect>`
- **预期**: X1==0, Y1==0, X2==1, Y2==1, Stops.Count==2
- **验证点**: 渐变属性、Stops

### 7.2 LinearGradient 默认方向
- **用例名**: `Parse_LinearGradient_DefaultDirection`
- **输入**: `<Rect><Fill><LinearGradient><Stop Offset="0" Color="#000"/><Stop Offset="1" Color="#FFF"/></LinearGradient></Fill></Rect>`
- **预期**: X1==0, Y1==0, X2==1, Y2==0（默认水平方向）
- **验证点**: X1/Y1/X2/Y2

### 7.3 Stop 缺少 Offset
- **用例名**: `Parse_LinearGradient_StopMissingOffset_GeneratesWarning`
- **输入**: `<Rect><Fill><LinearGradient><Stop Color="#000"/><Stop Offset="1" Color="#FFF"/></LinearGradient></Fill></Rect>`
- **预期**: Warnings 包含 `Stop 缺少 Offset`，该 Stop 被跳过，Stops.Count==1
- **验证点**: Warnings、Stops.Count

### 7.4 Stop 缺少 Color
- **用例名**: `Parse_LinearGradient_StopMissingColor_GeneratesWarning`
- **输入**: `<Rect><Fill><LinearGradient><Stop Offset="0"/><Stop Offset="1" Color="#FFF"/></LinearGradient></Fill></Rect>`
- **预期**: Warnings 包含 `Stop 缺少 Offset 或 Color`，该 Stop 被跳过
- **验证点**: Warnings、Stops.Count

### 7.5 LinearGradient 无有效 Stop
- **用例名**: `Parse_LinearGradient_NoValidStops_GeneratesWarning_ReturnsNull`
- **输入**: `<Rect><Fill><LinearGradient></LinearGradient></Fill></Rect>`
- **预期**: Warnings 包含 `不包含有效 Stop`，渐变被忽略，Fill==null
- **验证点**: Warnings、Fill 为 null

### 7.6 Stop Offset 超出范围被 Clamp
- **用例名**: `Parse_LinearGradient_StopOffsetOutOfRange_Clamped`
- **输入**: `<Rect><Fill><LinearGradient><Stop Offset="-0.5" Color="#000"/><Stop Offset="1.5" Color="#FFF"/></LinearGradient></Fill></Rect>`
- **预期**: Stops[0].Offset==0, Stops[1].Offset==1
- **验证点**: Offset 被 Math.Clamp 到 0~1

---

## 8. Margin 解析

### 8.1 Margin 四值
- **用例名**: `Parse_Margin_FourValues_Parsed`
- **输入**: `<Rect Margin="10,20,30,40"></Rect>`
- **预期**: Margin.Left==10, Top==20, Right==30, Bottom==40
- **验证点**: 四边值

### 8.2 Margin 单值
- **用例名**: `Parse_Margin_SingleValue_AllSidesEqual`
- **输入**: `<Rect Margin="8"></Rect>`
- **预期**: Left==8, Top==8, Right==8, Bottom==8
- **验证点**: 四边值

### 8.3 Margin 两值
- **用例名**: `Parse_Margin_TwoValues_VerticalHorizontal`
- **输入**: `<Rect Margin="10,20"></Rect>`
- **预期**: Left==20, Top==10, Right==20, Bottom==10
- **验证点**: 两值展开规则（上下=第一个值，左右=第二个值）

### 8.4 Margin 三值
- **用例名**: `Parse_Margin_ThreeValues`
- **输入**: `<Rect Margin="10,20,30"></Rect>`
- **预期**: Left==20, Top==10, Right==20, Bottom==30
- **验证点**: 三值展开规则

### 8.5 Margin 无效格式
- **用例名**: `Parse_Margin_InvalidFormat_GeneratesError`
- **输入**: `<Rect Margin="abc"></Rect>`
- **预期**: Errors 包含 `不是有效的间距格式`，Margin==null
- **验证点**: Errors.Count==1，Margin 为 null

---

## 9. Page.Styles 解析

### 9.1 Page.Styles 含 TextStyle
- **用例名**: `Parse_PageStyles_TextStyle_Parsed`
- **输入**: `<Page><Page.Styles><TextStyle Id="title" FontSize="24" IsBold="True" Foreground="#333" FontName="Arial" TextAlignment="Center"/></Page.Styles></Page>`
- **预期**: page.Styles.Count==1，style.Id=="title"，FontSize==24，IsBold==true
- **验证点**: Styles 列表、各属性

### 9.2 Page.Styles 中 TextStyle 缺少 Id
- **用例名**: `Parse_PageStyles_TextStyleMissingId_GeneratesWarning`
- **输入**: `<Page><Page.Styles><TextStyle FontSize="24"/></Page.Styles></Page>`
- **预期**: Warnings 包含 `TextStyle 缺少 Id 属性，已忽略`，Styles 不含该样式
- **验证点**: Warnings、Styles.Count==0 或 null

---

## 10. 数值解析边界情况

### 10.1 小数坐标
- **用例名**: `Parse_DecimalCoordinates_Parsed`
- **输入**: `<Rect X="10.5" Y="20.3" Width="100.7" Height="50.2"></Rect>`
- **预期**: X==10.5, Y==20.3, Width==100.7, Height==50.2
- **验证点**: 小数值精度

### 10.2 负数坐标
- **用例名**: `Parse_NegativeCoordinates_Parsed`
- **输入**: `<Rect X="-10" Y="-20" Width="100" Height="50"></Rect>`
- **预期**: X==-10, Y==-20
- **验证点**: 负数值

### 10.3 零值
- **用例名**: `Parse_ZeroValues_Parsed`
- **输入**: `<Rect X="0" Y="0" Width="0" Height="0"></Rect>`
- **预期**: 所有值为 0
- **验证点**: 零值

### 10.4 数值带空格
- **用例名**: `Parse_ValuesWithSpaces_Parsed`
- **输入**: `<Rect X=" 10 " Y=" 20 " Width=" 100 " Height=" 50 "></Rect>`
- **预期**: 正确解析数值
- **验证点**: 数值正确

### 10.5 无效数值
- **用例名**: `Parse_InvalidNumber_GeneratesError`
- **输入**: `<Rect X="abc" Width="100"></Rect>`
- **预期**: Errors 包含 `X 值 "abc" 不是有效的数值`，X==null
- **验证点**: Errors.Count==1，X 为 null

### 10.6 CultureInvariant 解析
- **用例名**: `Parse_DecimalWithInvariantCulture`
- **输入**: `<Rect X="10.5" Width="100.5"></Rect>`（在德语环境下小数点为逗号）
- **预期**: 使用 InvariantCulture，X==10.5
- **验证点**: 小数点解析正确

---

## 11. 嵌套结构

### 11.1 Panel 嵌套 Panel
- **用例名**: `Parse_NestedPanels_StructureCorrect`
- **输入**: `<Page><Panel Id="outer"><Panel Id="inner"><Rect Id="leaf"/></Panel></Panel></Page>`
- **预期**: outer.Children[0] 是 inner，inner.Children[0] 是 leaf
- **验证点**: 层级结构

### 11.2 Panel 混合子元素
- **用例名**: `Parse_Panel_MixedChildren_AllParsed`
- **输入**: `<Page><Panel><Rect/><TextElement Text="hi"/><Image Source="img"/><Panel/></Panel></Page>`
- **预期**: Children.Count==4，类型分别为 Rect/TextElement/Image/Panel
- **验证点**: 子元素类型和数量

### 11.3 深层嵌套
- **用例名**: `Parse_DeepNesting_AllParsed`
- **输入**: 5 层嵌套 Panel
- **预期**: 最内层 Rect 正确解析
- **验证点**: 最内层元素存在且属性正确

---

## 12. SlideMlParseException 族

### 12.1 SlideMlRootElementException
- **用例名**: `Parse_RootElementEmpty_ThrowsRootElementException`
- **输入**: 空文档
- **预期**: 抛出 `SlideMlRootElementException`
- **验证点**: 异常类型

### 12.2 SlideMlRequiredAttributeMissingException 含 ElementId/AttributeName
- **用例名**: `Parse_MissingRequiredAttribute_ExceptionContainsContext`
- **输入**: `<Image></Image>`
- **预期**: 异常 ElementId 不为空，AttributeName=="Source"
- **验证点**: ElementId、AttributeName

### 12.3 SlideMlUnsupportedElementException
- **用例名**: `LayoutEngine_UnknownElementType_ThrowsUnsupportedElementException`
- **说明**: 此异常由布局引擎抛出（非解析器），但属于异常族测试
- **验证点**: 异常类型、TagName 属性
