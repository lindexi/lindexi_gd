# V2 规划文档

## V2 目标

在 V1 验证了核心闭环（生成 → 渲染 → 反馈 → 迭代）可行之后，V2 重点解决两个问题：

1. **排版效率**：减少模型反复试错的轮次
2. **表现力**：覆盖更多真实 PPT 场景

**实施进度**: 约 70%（阶段一~三已完成，阶段四~六待实施）

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
参数: text, fontName, fontSize, maxWidth, lineHeight
返回: { width, height, lineCount, overflow }
```

预期效果：关键文本（标题、卡片描述）一次写对，不需要反馈回路纠正。

### 3. 渐变填充 ✅ 已完成（阶段二+三）

突破纯色填充限制，支持线性渐变背景和描边。Panel 也通过 `<Fill>` 子元素支持渐变背景。

实现细节：`<Fill>`/`<Stroke>` 子元素优先于同名属性。

```xml
<!-- 渐变填充 -->
<Rect X="0" Y="0" Width="1280" Height="720">
  <Fill>
    <LinearGradient X1="0" Y1="0" X2="1" Y2="1">
      <Stop Offset="0%" Color="#4A7BF7"/>
      <Stop Offset="100%" Color="#F4F6FA"/>
    </LinearGradient>
  </Fill>
</Rect>

<!-- 渐变描边 -->
<Rect Width="200" Height="60" CornerRadius="8"
      StrokeThickness="2">
  <Stroke>
    <LinearGradient X1="0" Y1="0" X2="1" Y2="0">
      <Stop Offset="0%" Color="#4A7BF7"/>
      <Stop Offset="100%" Color="#6C5CE7"/>
    </LinearGradient>
  </Stroke>
</Rect>
```

规则：
- `LinearGradient` 仅支持两点线性渐变（X1,Y1 → X2,Y2，范围 0~1 表示相对元素尺寸的百分比）
- `Stop` 的 `Offset` 支持 `0%` ~ `100%`，至少两个 Stop
- `Fill` 子元素与 `Fill` 属性互斥；同时存在时子元素优先
- 渐变同样可用于 `Stroke` 的子元素
- 向后兼容：`Fill="#RRGGBB"` 属性仍然有效

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
子元素形式 `<Shadow>`：`OffsetX`（默认 0）、`OffsetY`（默认 4）、`Blur`（默认 8）、`Color`（默认 `#000000`）、`Opacity`（0~1，默认 0.15）。

### 5. FontWeight 字体粗细 ✅ 已完成（阶段二+三）

`TextElement` 和 `Span` 均支持 `FontWeight` 属性。

```xml
<TextElement Text="SlideML" FontSize="64" FontWeight="700"/>
<TextElement Text="副标题" FontSize="24" FontWeight="400"/>
```

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `FontWeight` | 数字/枚举 | `400` | 支持数值 100~900（步进 100）或枚举 `Thin`/`ExtraLight`/`Light`/`Normal`/`Medium`/`SemiBold`/`Bold`/`ExtraBold`/`Black` |

`Span` 标签同样支持 `FontWeight`（已在富文本中定义）。

### 6. 富文本 Span 标签 ✅ 已完成（阶段二+三）

支持同一 TextElement 内多种样式混排。Span 支持 FontSize、FontName、Foreground、FontWeight、FontStyle、TextDecoration。

```xml
<TextElement X="40" Y="200" Width="400">
  <Span FontSize="24" Foreground="#333" FontWeight="Bold">标题</Span>
  <Span FontSize="14" Foreground="#666"> — 副标题说明</Span>
</TextElement>
```

Span 属性子集：`FontSize`, `FontName`, `Foreground`, `FontWeight`（Bold/Normal）, `FontStyle`（Italic/Normal）, `TextDecoration`（Underline/None）。

### 7. 样式复用系统（TextStyle） ⚠️ 部分完成（阶段五未做）

数据模型和解析器已完成（阶段二），`<Page.Styles>` 和 `TextElement.Style` 属性已可解析。但 `TextStyleResolver` 样式解析器尚未实现，`Style` 属性不会生效。

```xml
<!-- 样式定义（放在 Page 开头） -->
<Page.Styles>
  <TextStyle Id="style.title" FontSize="36" FontWeight="Bold" Foreground="#1A1A2E" />
  <TextStyle Id="style.body" FontSize="15" Foreground="#666" LineHeight="1.6" />
</Page.Styles>

<!-- 使用 -->
<TextElement Style="style.title" Text="Hello" />
<TextElement Style="style.body" Text="正文内容..." />
```

Style 引用优先级：`Style` 属性中的默认值 < 元素自身的显式属性（后者覆盖前者）。

### 8. Margin 属性 ✅ 已完成（阶段一）

所有元素支持 `Margin` 属性。流式布局中实际间距 = `max(Gap, 前元素下Margin + 后元素上Margin)`。

```xml
<Panel Layout="Vertical" Gap="12">
  <TextElement Text="标题" Margin="0,0,0,8" />  <!-- 底部额外 8px 间距 -->
  <TextElement Text="正文" />
</Panel>
```

实际间距 = `max(Gap, 前元素下Margin + 后元素上Margin)`。

### 9. 增强型 CornerRadius ✅ 已完成（阶段二+三）

支持四角独立圆角：`"8,16,8,16"` 或统一值 `"8"`（向后兼容）。

```xml
<Rect CornerRadius="8,16,8,16" />  <!-- 左上,右上,右下,左下 -->
<!-- 或 -->
<Rect CornerRadius="8" />  <!-- 统一值，向后兼容 -->
```

### 10. StrokeDashArray ✅ 已完成（阶段二+三）

虚线描边：`StrokeDashArray="4,2"`。

### 11. 图片资源 SubAgent / RAG 集成 ❌ 未实现（阶段六）

V1 中 Source 只是占位 ID。计划实现搜索图片 SubAgent，模型可检索并引用图片 ID。

### 12. 多模态截图反馈 ✅ 已完成

`get_render_preview` 工具已存在，渲染引擎返回页面截图供视觉评估。

---

## 后续待实现

| 功能 | 阶段 | 优先级 | 说明 |
|------|------|--------|------|
| measure_text 工具 | 阶段四 | 高 | 暴露文本测量能力给模型，减少排版试错 |
| TextStyle 样式解析器 | 阶段五 | 中 | `<Page.Styles>` 数据模型和解析器已完成，仅差 ApplyStyle 逻辑 |
| 图片检索 SubAgent | 阶段六 | 中 | 模型通过 SubAgent 检索可用的图片资源 ID |
| 提示词更新 | 阶段四 | 高 | 在 LLM 提示词中加入 V2 新标签/属性说明 |

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
