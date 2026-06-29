# V2 规划文档

## V2 目标

在 V1 验证了核心闭环（生成 → 渲染 → 反馈 → 迭代）可行之后，V2 重点解决两个问题：

1. **排版效率**：减少模型反复试错的轮次
2. **表现力**：覆盖更多真实 PPT 场景

**实施进度**: 阶段一~三（WPF 版）已完成，Avalonia 版部分完成，阶段四~五未实现

---

## V2 功能清单

### 1. 单向流式布局 ✅ 已完成（阶段一）

V1 纯绝对定位下，模型需要手动计算每个元素的坐标。引入有限流式布局：

```xml
<!-- 水平排列 -->
<Panel Layout="Horizontal" Gap="12" X="..." Y="..." Width="..." Height="...">
  <Rect Width="80" Height="32" />
  <Rect Width="100" Height="32" />
</Panel>

<!-- 垂直排列 -->
<Panel Layout="Vertical" Gap="16" Padding="20">
  <TextElement Text="标题" />
  <TextElement Text="正文" />
</Panel>
```

规则：
- 仅支持单向（水平或垂直），不支持 Wrap
- 子元素在排列轴方向上的 X/Y 被忽略，由引擎自动计算
- 溢出时产生 Warning，不崩溃

### 2. measure_text 工具 ❌ 未实现（阶段四）

把文本测量能力暴露为模型可调用的函数，让模型在生成 XML **之前**就能精确知道文本尺寸。

```
工具名: measure_text
参数: text, fontName, fontSize, maxWidth
返回: { width, height, lineCount, overflow }
```

预期效果：关键文本（标题、卡片描述）一次写对，不需要反馈回路纠正。

### 3. 统一画刷模型 ✅ 已完成（阶段二+三 + 后期重构）

突破纯色填充限制，通过 `ISlideMlBrush` 接口统一纯色（`SlideMlSolidColorBrush`）和渐变（`SlideMlLinearGradientBrush`）。

`Rect` 的 `Fill`/`Stroke` 和 `Panel` 的 `Background` 都接受统一画刷类型。属性形式（纯色字符串）自动包装为 `SlideMlSolidColorBrush`，子元素形式（`<Fill>/<Stroke>` 含渐变）解析为 `SlideMlLinearGradientBrush`。渲染引擎通过一次模式匹配分派，不再需要 if-else 优先级判断。

```xml
<!-- 纯色填充（属性形式） -->
<Rect X="0" Y="0" Width="1280" Height="720" Fill="#4A7BF7"/>

<!-- 渐变填充（子元素形式） -->
<Rect X="0" Y="0" Width="1280" Height="720">
  <Fill>
    <LinearGradient X1="0" Y1="0" X2="1" Y2="1">
      <Stop Offset="0" Color="#4A7BF7"/>
      <Stop Offset="1" Color="#F4F6FA"/>
    </LinearGradient>
  </Fill>
</Rect>

<!-- 渐变描边 -->
<Rect Width="200" Height="60" CornerRadius="8"
      StrokeThickness="2">
  <Stroke>
    <LinearGradient X1="0" Y1="0" X2="1" Y2="0">
      <Stop Offset="0" Color="#4A7BF7"/>
      <Stop Offset="1" Color="#6C5CE7"/>
    </LinearGradient>
  </Stroke>
</Rect>
```

规则：
- `LinearGradient` 仅支持两点线性渐变（X1,Y1 → X2,Y2，范围 0~1 表示相对元素尺寸的比例）
- `Stop` 的 `Offset` 范围 0~1，至少两个 Stop
- `Fill` 子元素与 `Fill` 属性统一为 `ISlideMlBrush`，不再需要两套属性
- 渐变同样可用于 `Stroke` 的子元素
- 内部类型：`ISlideMlBrush`（接口）、`SlideMlSolidColorBrush`（纯色）、`SlideMlLinearGradientBrush`（渐变）、`SlideMlGradientStop`（停止点）

### 4. 阴影效果 ✅ 已完成（阶段二+三）

为元素添加投影，增强层次感。支持属性形式和子元素形式。阴影在元素 Opacity 之前绘制。

```xml
<!-- 卡片阴影 -->
<Rect X="640" Y="80" Width="560" Height="520"
      Fill="#FFFFFF" CornerRadius="12"
      Shadow="0 4 12 rgba(0,0,0,0.1)"/>

<!-- 更细腻的阴影控制 -->
<Rect Fill="#FFFFFF" CornerRadius="8">
  <Shadow OffsetX="0" OffsetY="8" Blur="24" Color="#000000" Opacity="0.12"/>
</Rect>
```

属性形式 `Shadow`：`"OffsetX OffsetY Blur Color"`，如 `"0 4 12 #00000033"`。
子元素形式 `<Shadow>`：`OffsetX`（默认 0）、`OffsetY`（默认 4）、`Blur`（默认 12）、`Color`（默认 `#00000033`）、`Opacity`（0~1，默认 1）。

### 5. 字体粗细与斜体 ✅ 已完成（阶段二+三）

`TextElement` 和 `Span` 均支持 `IsBold` 和 `IsItalic` 属性。

```xml
<TextElement Text="SlideML" FontSize="64" IsBold="True"/>
<TextElement Text="副标题" FontSize="24" IsBold="False" IsItalic="True"/>
```

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `IsBold` | 布尔 | `False` | `True` 为粗体，`False` 或不写为正常粗细 |
| `IsItalic` | 布尔 | `False` | `True` 为斜体，`False` 或不写为正常 |

`Span` 标签同样支持 `IsBold` 和 `IsItalic`（已在富文本中定义）。

### 6. 富文本 Span 标签 ✅ 已完成（阶段二+三）

支持同一 TextElement 内多种样式混排。Span 支持 FontSize、FontName、Foreground、IsBold、IsItalic、TextDecoration。

```xml
<TextElement X="40" Y="200" Width="400">
  <Span Text="标题" FontSize="24" Foreground="#333" IsBold="True"/>
  <Span Text=" — 副标题说明" FontSize="14" Foreground="#666"/>
</TextElement>
```

Span 属性子集：`FontSize`, `FontName`, `Foreground`, `IsBold`（True/False）, `IsItalic`（True/False）, `TextDecoration`（Underline/None）。

### 7. Margin 属性 ✅ 已完成（阶段一）

所有元素支持 `Margin` 属性。流式布局中实际间距 = `max(Gap, 前元素下Margin + 后元素上Margin)`。

```xml
<Panel Layout="Vertical" Gap="12">
  <TextElement Text="标题" Margin="0,0,0,8" />  <!-- 底部额外 8px 间距 -->
  <TextElement Text="正文" />
</Panel>
```

实际间距 = `max(Gap, 前元素下Margin + 后元素上Margin)`。

### 8. CornerRadius ✅ 已完成（阶段二+三）

支持圆角半径，四角统一值。

```xml
<Rect CornerRadius="8" />
```

> 注：解析器代码保留了对逗号分隔四值（如 `"8,16,8,16"`）的解析能力以备向后兼容，但由于渲染引擎不支持四角独立圆角且存在平台差异（WPF vs Avalonia），规范文档和提示词均只对外声明单值。

### 9. StrokeDashArray ✅ 已完成（阶段二+三）

虚线描边：`StrokeDashArray="4,2"`。

### 10. 图片资源 SubAgent / RAG 集成 ❌ 未实现（阶段五）

V1 中 Source 只是占位 ID。计划实现搜索图片 SubAgent，模型可检索并引用图片 ID。

### 11. 多模态截图反馈 ✅ 已完成

`get_render_preview` 工具已存在，渲染引擎返回页面截图供视觉评估。

### 12. Avalonia 渲染引擎 ✅ 已完成

Avalonia 版渲染引擎（`AvaloniaSlideRenderEngine.cs`）已追平 WPF 版功能：

| 功能 | 状态 |
|------|------|
| 渐变画刷 | ✅ 已实现 |
| IsBold / IsItalic | ✅ 已实现 |
| CornerRadius | ✅ DrawRect 已实现（单值统一圆角） |
| Shadow 阴影 | ✅ DrawRect 已实现（含模糊效果） |
| StrokeDashArray 虚线 | ✅ DrawRect 已实现 |
| Span 富文本 | ✅ DrawText 已实现 |

---

## 后续待实现

| 功能 | 阶段 | 优先级 | 说明 |
|------|------|--------|------|
| 提示词更新 | 阶段四 | 高 | 当前系统提示词仍是 V1 版本，完全缺失 V2 新功能说明（Layout/Gap/Margin、渐变、阴影、Span、CornerRadius、StrokeDashArray） |
| measure_text 工具 | 阶段四 | 高 | 暴露文本测量能力给模型，减少排版试错 |
| ~~Avalonia 渲染引擎补全~~ | ~~阶段三~~ | ~~中~~ | ✅ 已完成：CornerRadius/Shadow/StrokeDashArray/Span 富文本均已实现 |
| 图片检索 SubAgent | 阶段五 | 中 | 模型通过 SubAgent 检索可用的图片资源 ID |

---

## V2 不做的事情（留到 V3 或更远）

- 动画 / 过渡效果
- Transform（旋转、缩放、倾斜）
- Blur / BackdropBlur（毛玻璃/磨砂效果）
- 图标字体 / SVG 图标库
- 图表（BarChart、PieChart 等专用标签）
- 表格标签
- 响应式 / 多尺寸画布
- 协作 / 多人编辑
- 导出 PPTX（V2 输出仍是位图）
