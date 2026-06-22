# SlideML V2 规范文档

## 概述

SlideML 是一套基于 XML 的幻灯片内容描述语言，专为大语言模型生成而设计。渲染引擎读取 SlideML 文档，生成可视化页面，并将测量结果回填到原文档中，形成「生成 → 渲染 → 反馈」闭环。

V2 在 V1 基础上新增了流式布局、渐变填充、阴影、IsBold/IsItalic 字体控制、富文本 Span、四角独立圆角、虚线描边等能力。

### 基本约定

- 画布尺寸：**1280 × 720** 像素
- 坐标原点：**左上角**，X 轴向右，Y 轴向下
- 所有数值单位默认为 **px**，不写单位
- 颜色格式：**`#RRGGBB`** 或 **`#AARRGGBB`**（八位含透明度）
- 标签名和属性名采用 **PascalCase**

---

## 标签定义

### 1. Page — 根元素

每个文档有且仅有一个 Page 元素，作为根容器。

```xml
<Page Background="#FFFFFF">
  <!-- 所有子元素放在这里 -->
</Page>
```

| 属性 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `Background` | 颜色 | 否 | 画布背景色，默认 `#FFFFFF` |

### 2. Panel — 容器

用于组织子元素的逻辑分组，支持嵌套。支持绝对定位和流式布局两种模式。

#### 绝对定位模式（默认）

```xml
<Panel Id="header" X="0" Y="0" Width="1280" Height="120"
       Padding="24" Background="#1A1A2E">
  <TextElement X="24" Y="30" Text="标题文字" ... />
</Panel>
```

#### 流式布局模式

```xml
<!-- 水平排列 -->
<Panel Layout="Horizontal" Gap="12" X="80" Y="260" Width="1120">
  <Rect Width="340" Height="320" Fill="#FFFFFF" CornerRadius="12" />
  <Rect Width="340" Height="320" Fill="#FFFFFF" CornerRadius="12" />
</Panel>

<!-- 垂直排列 -->
<Panel Layout="Vertical" Gap="16" Padding="24">
  <TextElement Text="标题" FontSize="24" IsBold="True" />
  <TextElement Text="正文内容..." FontSize="15" />
</Panel>
```

| 属性 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `Id` | 字符串 | 否 | 元素标识符，未指定时引擎自动分配 |
| `X`, `Y` | 数字 | 否 | 绝对定位模式下相对于父容器的坐标，默认 `0, 0`。流式布局中排列轴方向的坐标被忽略 |
| `Width`, `Height` | 数字 | 否 | 固定尺寸。不写则自动撑开到包裹所有子元素 |
| `Padding` | 数字 | 否 | 内边距，四个方向统一值，默认 `0` |
| `Background` | 颜色 | 否 | 背景色，默认透明 |
| `Layout` | 枚举 | 否 | 子元素排列方向：`Absolute`（默认）/ `Horizontal` / `Vertical` |
| `Gap` | 数字 | 否 | 流式布局下子元素之间的默认间距（px），默认 `0` |
| `HorizontalAlignment` | 枚举 | 否 | 水平对齐：`Left` / `Center` / `Right`，仅当不写 X 时生效 |
| `VerticalAlignment` | 枚举 | 否 | 垂直对齐：`Top` / `Center` / `Bottom`，仅当不写 Y 时生效 |
| `Opacity` | 数字 | 否 | 透明度 `0.0 ~ 1.0`，默认 `1.0` |
| `Margin` | 字符串 | 否 | 外边距，逗号分隔 1~4 个值，如 `"0,0,0,8"`。流式布局中实际间距 = `max(Gap, 前元素下Margin + 后元素上Margin)` |

**Panel 子元素：**

Panel 支持 `<Fill>` 子元素定义渐变背景（优先于 `Background` 属性）：

```xml
<Panel X="0" Y="0" Width="1280" Height="720">
  <Fill>
    <LinearGradient X1="0" Y1="0" X2="1" Y2="1">
      <Stop Offset="0" Color="#4A7BF7"/>
      <Stop Offset="1" Color="#F4F6FA"/>
    </LinearGradient>
  </Fill>
  <!-- 子元素 -->
</Panel>
```

**流式布局规则：**

- 仅支持单向（水平或垂直），不支持 Wrap
- 子元素在排列轴方向上的 X 或 Y 被忽略，由引擎自动计算
- 跨轴方向仍使用声明的坐标或 `VerticalAlignment`/`HorizontalAlignment`
- 子元素超出 Panel 尺寸时产生 Warning，不崩溃

### 3. Rect — 矩形

```xml
<Rect Id="card" X="40" Y="160" Width="380" Height="280"
      Fill="#FFFFFF" Stroke="#E0E0E0" StrokeThickness="1"
      CornerRadius="8" Opacity="1.0" />
```

| 属性 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `Id` | 字符串 | 否 | 元素标识符 |
| `X`, `Y` | 数字 | 否 | 默认 `0, 0` |
| `Width`, `Height` | 数字 | 否 | 固定尺寸 |
| `Fill` | 颜色 | 否 | 填充色，默认透明 |
| `Stroke` | 颜色 | 否 | 描边色，默认无描边 |
| `StrokeThickness` | 数字 | 否 | 描边粗细，默认 `0` |
| `CornerRadius` | 数字/字符串 | 否 | 圆角半径。单值如 `"8"` 表示四角统一；逗号分隔如 `"8,16,8,16"` 表示左上/右上/右下/左下独立值。默认 `0` |
| `StrokeDashArray` | 字符串 | 否 | 虚线描边模式，逗号分隔数值，如 `"4,2"` 表示 4px 实线 + 2px 空白 |
| `Shadow` | 字符串 | 否 | 阴影属性形式：`"OffsetX OffsetY Blur Color"`，如 `"0 4 12 #00000033"` |
| `HorizontalAlignment` | 枚举 | 否 | 水平对齐：`Left` / `Center` / `Right`，仅当不写 X 时生效 |
| `VerticalAlignment` | 枚举 | 否 | 垂直对齐：`Top` / `Center` / `Bottom`，仅当不写 Y 时生效 |
| `Opacity` | 数字 | 否 | 透明度 `0.0 ~ 1.0`，默认 `1.0` |
| `Margin` | 字符串 | 否 | 外边距，逗号分隔 |

**Rect 子元素：**

Rect 支持以下子元素，可用于定义渐变填充/描边和精细阴影控制。子元素属性优先于同名 XML 属性。

#### `<Fill>` — 渐变填充

```xml
<Rect X="0" Y="0" Width="1280" Height="720">
  <Fill>
    <LinearGradient X1="0" Y1="0" X2="1" Y2="1">
      <Stop Offset="0" Color="#4A7BF7"/>
      <Stop Offset="1" Color="#F4F6FA"/>
    </LinearGradient>
  </Fill>
</Rect>
```

| 元素 | 说明 |
|------|------|
| `LinearGradient` | 线性渐变定义。`X1, Y1, X2, Y2` 范围 0~1，表示相对元素尺寸的比例。默认 `0,0 → 1,0` |
| `Stop` | 渐变停止点。`Offset` 范围 0~1；`Color` 为颜色字符串。至少需要两个 Stop |

#### `<Stroke>` — 渐变描边

```xml
<Rect Width="200" Height="60" CornerRadius="8" StrokeThickness="2">
  <Stroke>
    <LinearGradient X1="0" Y1="0" X2="1" Y2="0">
      <Stop Offset="0" Color="#4A7BF7"/>
      <Stop Offset="1" Color="#6C5CE7"/>
    </LinearGradient>
  </Stroke>
</Rect>
```

#### `<Shadow>` — 精细阴影

```xml
<Rect Fill="#FFFFFF" CornerRadius="8">
  <Shadow OffsetX="0" OffsetY="8" Blur="24" Color="#000000" Opacity="0.12"/>
</Rect>
```

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `OffsetX` | 数字 | `0` | 水平偏移 |
| `OffsetY` | 数字 | `4` | 垂直偏移 |
| `Blur` | 数字 | `12` | 模糊半径 |
| `Color` | 颜色 | `#00000033` | 阴影颜色 |
| `Opacity` | 数字 | `1` | 阴影透明度 `0~1` |

### 4. TextElement — 文本

```xml
<TextElement Id="title" X="60" Y="180" Width="340"
             Text="这是一段可能会自动换行的文本内容"
             FontName="Microsoft YaHei" FontSize="28" IsBold="True"
             Foreground="#1A1A2E"
             TextAlignment="Left" LineHeight="1.4"
             Opacity="1.0" />
```

| 属性 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `Id` | 字符串 | 否 | 元素标识符 |
| `X`, `Y` | 数字 | 否 | 默认 `0, 0` |
| `Width`, `Height` | 数字 | 否 | 固定尺寸。`Width` 不写则单行无限宽；`Width` 写了则文本在约束宽度内自动换行 |
| `Text` | 字符串 | 是（若无 Span） | 文本内容。若有 Span 子元素则可省略 |
| `FontName` | 字符串 | 否 | 字体名称，默认 `Microsoft YaHei` |
| `FontSize` | 数字 | 否 | 字号（px），默认 `16` |
| `IsBold` | 布尔 | 否 | 是否为粗体。`True` 为粗体，`False` 或不写为正常粗细。默认 `False` |
| `IsItalic` | 布尔 | 否 | 是否为斜体。`True` 为斜体，`False` 或不写为正常。默认 `False` |
| `Foreground` | 颜色 | 否 | 文字颜色，默认 `#000000` |
| `TextAlignment` | 枚举 | 否 | 文本水平对齐：`Left` / `Center` / `Right` / `Justify`，默认 `Left` |
| `LineHeight` | 数字 | 否 | 行高倍数，如 `1.5` 表示 1.5 倍行高，默认 `1.2` |
| `HorizontalAlignment` | 枚举 | 否 | 元素在父容器内的水平对齐：`Left` / `Center` / `Right` |
| `VerticalAlignment` | 枚举 | 否 | 元素在父容器内的垂直对齐：`Top` / `Center` / `Bottom` |
| `Opacity` | 数字 | 否 | 透明度，默认 `1.0` |
| `Margin` | 字符串 | 否 | 外边距，逗号分隔 |

**TextElement 子元素：`<Span>` — 富文本片段**

支持同一 TextElement 内多种样式混排：

```xml
<TextElement X="40" Y="200" Width="400">
  <Span Text="标题" FontSize="24" Foreground="#333" IsBold="True"/>
  <Span Text=" — 副标题说明" FontSize="14" Foreground="#666"/>
</TextElement>
```

| 属性 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `Text` | 字符串 | **是** | 片段文本内容 |
| `FontSize` | 数字 | 否 | 继承 TextElement 的值 |
| `FontName` | 字符串 | 否 | 继承 TextElement 的值 |
| `Foreground` | 颜色 | 否 | 继承 TextElement 的值 |
| `IsBold` | 布尔 | 否 | 继承 TextElement 的值 |
| `IsItalic` | 布尔 | 否 | 继承 TextElement 的值 |
| `TextDecoration` | 枚举 | 否 | `None` / `Underline`，默认 `None` |

### 5. Image — 图片

```xml
<Image Id="hero" X="800" Y="160" Width="400" Height="400"
       Source="img_hero_001" Stretch="Uniform" Opacity="1.0" />
```

| 属性 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `Id` | 字符串 | 否 | 元素标识符 |
| `X`, `Y` | 数字 | 否 | 默认 `0, 0` |
| `Width`, `Height` | 数字 | 否 | 固定尺寸 |
| `Source` | 字符串 | **是** | 图片资源 ID，非 URL，由上游 RAG 或资源系统解析 |
| `Stretch` | 枚举 | 否 | 缩放模式：`None` / `Fill` / `Uniform` / `UniformToFill`，默认 `Uniform` |
| `HorizontalAlignment` | 枚举 | 否 | 同上 |
| `VerticalAlignment` | 枚举 | 否 | 同上 |
| `Opacity` | 数字 | 否 | 透明度，默认 `1.0` |
| `Margin` | 字符串 | 否 | 外边距，逗号分隔 |

---

## 引擎回填属性

以下属性由渲染引擎在渲染完成后自动填写，**模型不应输出这些属性**，写了也会被覆盖。

| 属性 | 适用标签 | 说明 |
|------|----------|------|
| `ActualWidth` | 全部 | 渲染后的实际宽度（px） |
| `ActualHeight` | 全部 | 渲染后的实际高度（px） |
| `ActualLineCount` | TextElement | 文本换行后的实际行数 |

---

## 渲染反馈

渲染完成后，引擎返回四部分内容：

### 第一部分：回填后的 XML

原始文档加上 `ActualWidth`、`ActualHeight`、`ActualLineCount`，模型可以直接对比预期与实际的差异。

### 第二部分：Warning 文本

纯文本，一行一条警告，格式：

```
[Warning] <ElementId>: <描述>
```

警告类型：

| 场景 | 示例 |
|------|------|
| 文本溢出容器 | `[Warning] desc: ActualLineCount=5，超出容器高度（当前高度仅容纳 3 行），末尾 42 字符被截断` |
| 图片资源缺失 | `[Warning] hero: 图片资源 img_hero_001 未找到，已使用占位图` |
| 字体回退 | `[Warning] title: 字体 "PingFang SC" 不可用，已回退至 "Microsoft YaHei"` |
| 元素超出画布 | `[Warning] card3: 元素右边界 X=1340 超出画布宽度 1280` |
| 未知属性/标签 | `[Warning] elem_001: 未知属性 "Foo"，已忽略` |
| 流式布局溢出 | `[Warning] row: 流式布局子元素超出容器，末尾 2 个元素被裁剪` |

### 第三部分：Error 文本

纯文本，一行一条错误，格式：

```
[Error] <ElementId>: <描述>
```

### 第四部分：截图

渲染引擎返回当前页面的 PNG 截图，模型可从视觉层面评估颜色、间距、对齐等。

---

## Id 自动生成规则

- 若元素未指定 `Id`，引擎在渲染时自动分配，格式为 `elem_NNN`（如 `elem_001`）
- 自动分配的 Id 会回填到 XML 中

---

## 渲染与排版规则

1. **默认布局为绝对定位**：子元素的位置由 `X`、`Y` 决定，相对于直接父容器左上角
2. **流式布局**：Panel 设置 `Layout="Horizontal"` 或 `Layout="Vertical"` 时，子元素沿排列轴依次排列，排列轴上的 `X`/`Y` 被忽略
3. **Z 序按文档顺序**：同一父容器内，后出现的元素渲染在上层
4. **阴影在 Opacity 之前绘制**：元素阴影不受元素自身 `Opacity` 影响
5. **渐变填充优先于纯色填充**：`<Fill>`/`<Stroke>` 子元素优先于 `Fill`/`Stroke` 属性
6. **Panel 自动尺寸**：未指定 `Width`/`Height` 时，Panel 尺寸自动扩展以包裹所有子元素（加上 Padding）
7. **TextElement 自动换行**：指定了 `Width` 时，文本在宽度约束内自动换行；未指定 `Width` 时单行不换行
8. **裁剪行为**：子元素超出父容器边界的部分被裁剪（Clip）

---

## 完整示例

```xml
<Page Background="#F5F5F5">

  <!-- 渐变背景 Panel -->
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

  <!-- 流式布局：卡片行 -->
  <Panel Id="cards-row" Layout="Horizontal" Gap="24" X="80" Y="400" Width="1120" Height="280">
    <!-- 卡片 1 -->
    <Rect Width="340" Height="260" Fill="#FFFFFF" CornerRadius="12"
          Shadow="0 4 12 #00000033" Stroke="#E8E8E8" StrokeThickness="1" />
    <TextElement Id="card1-title" X="24" Y="24" Width="292"
                 Text="流式布局" FontSize="22" IsBold="True" Foreground="#333" />
    <TextElement Id="card1-desc" X="24" Y="72" Width="292"
                 Text="支持 Panel Layout='Horizontal'/'Vertical'，自动排列子元素，减少手动坐标计算。"
                 FontSize="15" Foreground="#666" LineHeight="1.5" />

    <!-- 卡片 2 -->
    <Rect Width="340" Height="260" Fill="#FFFFFF" CornerRadius="12"
          Shadow="0 4 12 #00000033" Stroke="#E8E8E8" StrokeThickness="1" />
    <TextElement Id="card2-title" X="24" Y="24" Width="292"
                 Text="渐变与阴影" FontSize="22" IsBold="True" Foreground="#333" />
    <TextElement Id="card2-desc" X="24" Y="72" Width="292"
                 Text="支持线性渐变填充/描边和元素阴影效果，提升视觉层次感。"
                 FontSize="15" Foreground="#666" LineHeight="1.5" />

    <!-- 卡片 3 -->
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
