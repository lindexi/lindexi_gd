# V2 规划文档

## V2 目标

在 V1 验证了核心闭环（生成 → 渲染 → 反馈 → 迭代）可行之后，V2 重点解决两个问题：

1. **排版效率**：减少模型反复试错的轮次
2. **表现力**：覆盖更多真实 PPT 场景

---

## V2 功能清单

### 1. 单向流式布局 ⭐ 高优先级

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

### 2. measure_text 工具 ⭐ 高优先级

把文本测量能力暴露为模型可调用的函数，让模型在生成 XML **之前**就能精确知道文本尺寸。

```
工具名: measure_text
参数: text, fontName, fontSize, maxWidth, lineHeight
返回: { width, height, lineCount, overflow }
```

预期效果：关键文本（标题、卡片描述）一次写对，不需要反馈回路纠正。

### 3. 渐变填充 ⭐ 高优先级

突破纯色填充限制，支持线性渐变背景：

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

### 4. 阴影效果 ⭐ 高优先级

为元素添加投影，增强层次感：

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

### 5. FontWeight 字体粗细 ⭐ 高优先级

为 `TextElement` 增加 `FontWeight` 属性，让标题和正文有清晰的层级区分：

```xml
<TextElement Text="SlideML" FontSize="64" FontWeight="700"/>
<TextElement Text="副标题" FontSize="24" FontWeight="400"/>
```

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `FontWeight` | 数字/枚举 | `400` | 支持数值 100~900（步进 100）或枚举 `Thin`/`ExtraLight`/`Light`/`Normal`/`Medium`/`SemiBold`/`Bold`/`ExtraBold`/`Black` |

`Span` 标签同样支持 `FontWeight`（已在富文本中定义）。

### 6. 富文本 Span 标签

支持同一 TextElement 内多种样式混排：

```xml
<TextElement X="40" Y="200" Width="400">
  <Span FontSize="24" Foreground="#333" FontWeight="Bold">标题</Span>
  <Span FontSize="14" Foreground="#666"> — 副标题说明</Span>
</TextElement>
```

Span 属性子集：`FontSize`, `FontName`, `Foreground`, `FontWeight`（Bold/Normal）, `FontStyle`（Italic/Normal）, `TextDecoration`（Underline/None）。

### 7. 样式复用系统（TextStyle）

避免重复写属性：

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

### 8. Margin 属性

配合流式布局使用：

```xml
<Panel Layout="Vertical" Gap="12">
  <TextElement Text="标题" Margin="0,0,0,8" />  <!-- 底部额外 8px 间距 -->
  <TextElement Text="正文" />
</Panel>
```

实际间距 = `max(Gap, 前元素下Margin + 后元素上Margin)`。

### 9. 增强型 CornerRadius

支持四个角独立圆角：

```xml
<Rect CornerRadius="8,16,8,16" />  <!-- 左上,右上,右下,左下 -->
<!-- 或 -->
<Rect CornerRadius="8" />  <!-- 统一值，向后兼容 -->
```

### 10. StrokeDashArray

虚线描边：

```xml
<Rect Stroke="#CCC" StrokeThickness="2" StrokeDashArray="4,2" />
<!-- 4px 实线, 2px 空白 -->
```

### 11. 图片资源 SubAgent / RAG 集成

V1 中 Source 只是占位 ID。V2 实现：

- 模型调用 SubAgent：`search_image(query="科技感背景图", count=3)` → 返回 `["img_bg_001", "img_bg_002", "img_bg_003"]`
- 模型在 XML 中引用返回的 ID
- 或者预先通过 RAG 将用户素材库的图片做向量化，模型直接检索

### 12. 多模态截图反馈

对支持图片输入的模型，渲染引擎在每轮渲染后附加页面截图，模型可以从视觉层面评估：
- 颜色搭配是否协调
- 间距是否美观
- 元素是否对齐
- 整体设计风格是否统一

这可能是提升最终输出质量的「质变」功能。

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
- 导出 PPTX（V2 输出仍是位图或 SVG）

---

## 版本路线图

```
V1 (当前)           V2                    V3+
─────               ────                  ────
绝对定位             流式布局              动画/过渡
5 标签              measure_text 工具     Transform（旋转/缩放/倾斜）
反馈回填             渐变填充              Blur/毛玻璃
基础属性             阴影效果              图标字体/SVG 图标库
                    FontWeight 字体粗细    图表标签
                    富文本 Span           表格标签
                    样式复用              多画布尺寸
                    Margin                导出 PPTX
                    增强型 CornerRadius   协作功能
                    StrokeDashArray
                    SubAgent 图片检索
                    多模态截图反馈
                    模板/主题
```
