# SlideML 集成测试范例

## 概述

本文档设计一个可用的集成测试范例，用于测试从输入的 SlideML 文本内容，经过解析 → 布局 → Fake 渲染 → 回填，到最终反馈出的 SlideML 文本内容的完整流程。重点验证布局逻辑是否符合预期。

## Fake 实现设计

集成测试需要以下 Fake 实现（不含具体代码，仅描述行为）：

### FakeMainThreadDispatcher
- `InvokeAsync<T>(Func<Task<T>> action)`：直接同步执行传入的委托并返回结果
- `InvokeAsync(Func<Task> action)`：直接同步执行
- `CheckAccess()`：始终返回 `true`

### FakeRenderEngine（实现 ISlideMlRenderEngine）
- `PreMeasure`：遍历页面所有元素，为每个 TextElement 根据文本长度和字号计算模拟的 MeasuredWidth/MeasuredHeight/ActualLineCount；为 Image 使用声明的 Width/Height 或默认 240×180；其他元素使用声明的尺寸
- `Render`：返回一个 FakePreviewImage（空实现，不实际生成图片）
- `RenderErrorPreview`：返回 FakePreviewImage

### FakePreviewImage（实现 IPreviewImage）
- `Save(string filePath)` / `Save(Stream stream)`：空实现

## 集成测试用例

### 用例 1：基本水平流式布局端到端

**输入 XML**：
```xml
<Page Background="#FFFFFF">
  <Panel Id="row" Layout="Horizontal" Gap="12" X="80" Y="260" Width="1120">
    <Rect Id="card1" Width="340" Height="260" Fill="#FFFFFF" />
    <Rect Id="card2" Width="340" Height="260" Fill="#FFFFFF" />
    <Rect Id="card3" Width="340" Height="260" Fill="#FFFFFF" />
  </Panel>
</Page>
```

**预期输出 XML 关键断言**：
- `Page` 元素回填 `ActualWidth="1280"` `ActualHeight="720"`
- `Panel#row` 回填 `ActualWidth` = 340×3 + 12×2 = 1044（加上无 Padding），`ActualHeight` = 260
- `Rect#card1` 回填 `ActualWidth="340"` `ActualHeight="260"`，LayoutBounds.X = 80
- `Rect#card2` 回填 `ActualWidth="340"` `ActualHeight="260"`，LayoutBounds.X = 80 + 340 + 12 = 432
- `Rect#card3` 回填 `ActualWidth="340"` `ActualHeight="260"`，LayoutBounds.X = 432 + 340 + 12 = 784
- Warnings 为空
- Errors 为空

### 用例 2：带 Padding 的垂直流式布局端到端

**输入 XML**：
```xml
<Page>
  <Panel Id="col" Layout="Vertical" Gap="16" Padding="24" X="100" Y="100" Width="400">
    <TextElement Id="title" Text="标题" FontSize="24" IsBold="True" />
    <TextElement Id="desc" Text="正文内容" FontSize="15" Width="352" />
  </TextElement>
</Page>
```

**预期**：
- FakeRenderEngine.PreMeasure 为 `title` 返回模拟尺寸（如 MeasuredWidth=48, MeasuredHeight=28.8, ActualLineCount=1）
- FakeRenderEngine.PreMeasure 为 `desc` 返回模拟尺寸（如 MeasuredWidth=352, MeasuredHeight=18, ActualLineCount=1）
- `title` 的 LayoutBounds.X = 100 + 24 = 124，LayoutBounds.Y = 100 + 24 = 124
- `desc` 的 LayoutBounds.X = 124，LayoutBounds.Y = 124 + 28.8 + 16 = 168.8
- Panel 的 ActualHeight = 28.8 + 16 + 18 + 24×2 = 110.8
- OutputXml 中 `title` 回填 ActualLineCount="1"

### 用例 3：绝对定位 Panel 嵌套子元素

**输入 XML**：
```xml
<Page Background="#F5F5F5">
  <Panel Id="hero" X="0" Y="0" Width="1280" Height="360" Background="#1A1A2E">
    <TextElement Id="title" X="80" Y="120" Width="1120"
                 Text="SlideML V2" FontSize="56" IsBold="True" Foreground="#FFFFFF" />
    <TextElement Id="sub" X="80" Y="200" Width="1120"
                 Text="副标题" FontSize="24" Foreground="#CCCCDD" />
  </Panel>
</Page>
```

**预期**：
- `title` LayoutBounds = (80, 120, 1120, ~67.2)
- `sub` LayoutBounds = (80, 200, 1120, ~28.8)
- Panel#hero ActualWidth=1280, ActualHeight=360
- 无 Warning

### 用例 4：文本溢出容器高度产生 Warning

**输入 XML**：
```xml
<Page>
  <TextElement Id="long-text" X="40" Y="40" Width="400" Height="30"
               Text="这是一段很长的文本内容，会超出容器高度限制"
               FontSize="16" />
</Page>
```

**预期**：
- FakeRenderEngine.PreMeasure 返回 ActualLineCount > 1，MeasuredHeight > 30
- Warnings 中包含 `[Warning] long-text: ActualLineCount=...超出容器高度`
- OutputXml 回填 ActualLineCount

### 用例 5：流式布局溢出 Panel 宽度

**输入 XML**：
```xml
<Page>
  <Panel Id="row" Layout="Horizontal" Gap="8" X="0" Y="0" Width="200">
    <Rect Id="r1" Width="150" Height="50" />
    <Rect Id="r2" Width="150" Height="50" />
  </Panel>
</Page>
```

**预期**：
- Warnings 包含 `[Warning] row: 流式布局内容宽度 ... 超出 Panel 宽度 200`
- r1 和 r2 仍然被布局，只是超出部分产生警告

### 用例 6：元素超出画布边界

**输入 XML**：
```xml
<Page>
  <Rect Id="big" X="1200" Y="600" Width="200" Height="200" Fill="#FF0000" />
</Page>
```

**预期**：
- Warnings 包含 `[Warning] big: 元素右边界 X=1400 超出画布宽度 1280`
- Warnings 包含 `[Warning] big: 元素下边界 Y=800 超出画布高度 720`

### 用例 7：解析错误时返回错误预览

**输入 XML**：
```xml
<NotPage></NotPage>
```

**预期**：
- 抛出 `SlideMlRootElementException`，被管道捕获
- 返回的 SlideMlRenderResult 中 Warnings 包含 `[Warning] parser: SlideML 解析失败`
- PreviewImage 不为 null（为错误预览图）
- OutputXml 等于 InputXml（原样返回）

### 用例 8：完整规范示例端到端

**输入 XML**（使用规范文档中的完整示例）：
```xml
<Page Background="#F5F5F5">
  <Panel Id="hero" X="0" Y="0" Width="1280" Height="360">
    <Fill>
      <LinearGradient X1="0" Y1="0" X2="1" Y2="1">
        <Stop Offset="0" Color="#1A1A2E"/>
        <Stop Offset="1" Color="#4A4A6E"/>
      </LinearGradient>
    </Fill>
    <TextElement Id="hero-title" X="80" Y="120" Width="1120"
                 Text="SlideML V2" FontSize="56" IsBold="True"
                 Foreground="#FFFFFF" TextAlignment="Center" />
    <TextElement Id="hero-sub" X="80" Y="200" Width="1120"
                 Text="让大语言模型生成专业幻灯片"
                 FontSize="24" Foreground="#CCCCDD" TextAlignment="Center" />
  </Panel>
  <Panel Id="cards-row" Layout="Horizontal" Gap="24" X="80" Y="400" Width="1120" Height="280">
    <Rect Width="340" Height="260" Fill="#FFFFFF" CornerRadius="12"
          Shadow="0 4 12 #00000033" Stroke="#E8E8E8" StrokeThickness="1" />
    <TextElement Id="card1-title" X="24" Y="24" Width="292"
                 Text="流式布局" FontSize="22" IsBold="True" Foreground="#333" />
    <TextElement Id="card1-desc" X="24" Y="72" Width="292"
                 Text="支持 Panel Layout='Horizontal'/'Vertical'，自动排列子元素。"
                 FontSize="15" Foreground="#666" />
    <Rect Width="340" Height="260" Fill="#FFFFFF" CornerRadius="12"
          Shadow="0 4 12 #00000033" Stroke="#E8E8E8" StrokeThickness="1" />
    <TextElement Id="card2-title" X="24" Y="24" Width="292"
                 Text="渐变与阴影" FontSize="22" IsBold="True" Foreground="#333" />
    <TextElement Id="card2-desc" X="24" Y="72" Width="292"
                 Text="支持线性渐变填充/描边和元素阴影效果。"
                 FontSize="15" Foreground="#666" />
    <Rect Width="340" Height="260" Fill="#FFFFFF" CornerRadius="12"
          Shadow="0 4 12 #00000033" Stroke="#E8E8E8" StrokeThickness="1" />
    <TextElement Id="card3-title" X="24" Y="24" Width="292"
                 Text="富文本" FontSize="22" IsBold="True" Foreground="#333" />
    <TextElement Id="card3-desc" X="24" Y="72" Width="292">
      <Span Text="支持 Span 子元素" FontSize="15" Foreground="#666"/>
      <Span Text="在同一文本块内" FontSize="15" IsBold="True" Foreground="#333"/>
      <Span Text="混排多种样式。" FontSize="15" Foreground="#666"/>
    </TextElement>
  </Panel>
</Page>
```

**预期**：
- 无解析错误
- Panel#hero: ActualWidth=1280, ActualHeight=360
- Panel#cards-row: ActualWidth=1120, ActualHeight=280
- 所有元素回填 ActualWidth/ActualHeight
- 卡片在流式布局中按顺序排列（注意：Rect 和 TextElement 混合排列）
- 渐变背景被正确解析（LinearGradientBrush 对象）
- Span 子元素被正确解析
- 检查是否有元素超出 Panel 边界的 Warning（卡片内的 TextElement 使用绝对 X/Y 但在流式 Panel 内）

## 测试验证点汇总

| 验证维度 | 验证内容 |
|----------|----------|
| XML 解析 | 所有元素正确解析为模型对象 |
| 布局坐标 | LayoutBounds.X/Y/Width/Height 符合预期 |
| 自动尺寸 | Panel 未指定尺寸时自动包裹子元素 |
| 流式排列 | Horizontal/Vertical 子元素依次排列 |
| Gap 间距 | 子元素间距等于 Gap |
| Margin 间距 | 间距 = max(Gap, prevMargin + nextMargin) |
| Padding 偏移 | 子元素原点偏移 Padding |
| 对齐 | HorizontalAlignment/VerticalAlignment 正确解析 |
| 回填 | OutputXml 包含 ActualWidth/ActualHeight/ActualLineCount |
| Warning | 超出边界、溢出等场景产生正确 Warning |
| Error | 解析失败场景正确处理 |
| 测量值 | FinalLayout 使用 PreMeasure 的结果 |
