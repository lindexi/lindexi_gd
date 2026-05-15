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

### 3. 富文本 Span 标签

支持同一 TextElement 内多种样式混排：

```xml
<TextElement X="40" Y="200" Width="400">
  <Span FontSize="24" Foreground="#333" FontWeight="Bold">标题</Span>
  <Span FontSize="14" Foreground="#666"> — 副标题说明</Span>
</TextElement>
```

Span 属性子集：`FontSize`, `FontName`, `Foreground`, `FontWeight`（Bold/Normal）, `FontStyle`（Italic/Normal）, `TextDecoration`（Underline/None）。

### 4. 样式复用系统（TextStyle）

避免重复写属性：

```xml
<!-- 样式定义（放在 Page 开头） -->
<TextStyle Id="style.title" FontSize="36" FontWeight="Bold" Foreground="#1A1A2E" />
<TextStyle Id="style.body" FontSize="15" Foreground="#666" LineHeight="1.6" />

<!-- 使用 -->
<TextElement Style="style.title" Text="Hello" />
<TextElement Style="style.body" Text="正文内容..." />
```

Style 引用优先级：`Style` 属性中的默认值 < 元素自身的显式属性（后者覆盖前者）。

### 5. Margin 属性

配合流式布局使用：

```xml
<Panel Layout="Vertical" Gap="12">
  <TextElement Text="标题" Margin="0,0,0,8" />  <!-- 底部额外 8px 间距 -->
  <TextElement Text="正文" />
</Panel>
```

实际间距 = `max(Gap, 前元素下Margin + 后元素上Margin)`。

### 6. 增强型 CornerRadius

支持四个角独立圆角：

```xml
<Rect CornerRadius="8,16,8,16" />  <!-- 左上,右上,右下,左下 -->
<!-- 或 -->
<Rect CornerRadius="8" />  <!-- 统一值，向后兼容 -->
```

### 7. StrokeDashArray

虚线描边：

```xml
<Rect Stroke="#CCC" StrokeThickness="2" StrokeDashArray="4,2" />
<!-- 4px 实线, 2px 空白 -->
```

### 8. 图片资源 SubAgent / RAG 集成

V1 中 Source 只是占位 ID。V2 实现：

- 模型调用 SubAgent：`search_image(query="科技感背景图", count=3)` → 返回 `["img_bg_001", "img_bg_002", "img_bg_003"]`
- 模型在 XML 中引用返回的 ID
- 或者预先通过 RAG 将用户素材库的图片做向量化，模型直接检索

### 9. 多模态截图反馈

对支持图片输入的模型，渲染引擎在每轮渲染后附加页面截图，模型可以从视觉层面评估：
- 颜色搭配是否协调
- 间距是否美观
- 元素是否对齐
- 整体设计风格是否统一

这可能是提升最终输出质量的「质变」功能。

### 10. 模板/主题系统

```xml
<Page Theme="dark-tech">
  <!-- 主题预定义了背景色、字体、默认配色等 -->
</Page>
```

主题作为预设的 CSS 变量集合，模型可以不写属性直接继承主题默认值。

---

## V2 不做的事情（留到 V3 或更远）

- 动画 / 过渡效果
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
5 标签              富文本 Span           图表标签
反馈回填             measure_text 工具     表格标签
基础属性             样式复用              多画布尺寸
                    Margin                导出 PPTX
                    SubAgent 图片检索      协作功能
                    多模态截图反馈
                    模板/主题
```
