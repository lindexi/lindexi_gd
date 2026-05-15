# SlideML V1 规范文档

## 概述

SlideML 是一套基于 XML 的幻灯片内容描述语言，专为大语言模型生成而设计。渲染引擎读取 SlideML 文档，生成可视化页面，并将测量结果回填到原文档中，形成「生成 → 渲染 → 反馈」闭环。

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

用于组织子元素的逻辑分组，支持嵌套。**V1 仅支持绝对定位**（子元素靠 X/Y 确定位置）。

```xml
<Panel Id="header" X="0" Y="0" Width="1280" Height="120"
       Padding="24" Background="#1A1A2E">
  <TextElement X="24" Y="30" Text="标题文字" ... />
</Panel>
```

| 属性 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `X`, `Y` | 数字 | 否 | 相对于父容器的坐标，默认 `0, 0` |
| `Width`, `Height` | 数字 | 否 | 固定尺寸。不写则自动撑开到包裹所有子元素 |
| `Padding` | 数字 | 否 | 内边距，四个方向统一值，默认 `0` |
| `Background` | 颜色 | 否 | 背景色，默认透明 |

### 3. Rect — 矩形

```xml
<Rect Id="card" X="40" Y="160" Width="380" Height="280"
      Fill="#FFFFFF" Stroke="#E0E0E0" StrokeThickness="1"
      CornerRadius="8" Opacity="1.0" />
```

| 属性 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `X`, `Y` | 数字 | 否 | 默认 `0, 0` |
| `Width`, `Height` | 数字 | 否 | 固定尺寸 |
| `Fill` | 颜色 | 否 | 填充色，默认透明 |
| `Stroke` | 颜色 | 否 | 描边色，默认无描边 |
| `StrokeThickness` | 数字 | 否 | 描边粗细，默认 `0` |
| `CornerRadius` | 数字 | 否 | 圆角半径，默认 `0` |
| `HorizontalAlignment` | 枚举 | 否 | 水平对齐：`Left` / `Center` / `Right`，仅当不写 X 时生效 |
| `VerticalAlignment` | 枚举 | 否 | 垂直对齐：`Top` / `Center` / `Bottom`，仅当不写 Y 时生效 |
| `Opacity` | 数字 | 否 | 透明度 `0.0 ~ 1.0`，默认 `1.0` |

### 4. TextElement — 文本

```xml
<TextElement Id="title" X="60" Y="180" Width="340"
             Text="这是一段可能会自动换行的文本内容"
             FontName="Microsoft YaHei" FontSize="28"
             Foreground="#1A1A2E"
             TextAlignment="Left" LineHeight="1.4"
             Opacity="1.0" />
```

| 属性 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `X`, `Y` | 数字 | 否 | 默认 `0, 0` |
| `Width`, `Height` | 数字 | 否 | 固定尺寸。`Width` 不写则单行无限宽；`Width` 写了则文本在约束宽度内自动换行 |
| `Text` | 字符串 | **是** | 文本内容 |
| `FontName` | 字符串 | 否 | 字体名称，默认 `Microsoft YaHei` |
| `FontSize` | 数字 | 否 | 字号（px），默认 `16` |
| `Foreground` | 颜色 | 否 | 文字颜色，默认 `#000000` |
| `TextAlignment` | 枚举 | 否 | 文本水平对齐：`Left` / `Center` / `Right` / `Justify`，默认 `Left` |
| `LineHeight` | 数字 | 否 | 行高倍数，如 `1.5` 表示 1.5 倍行高，默认 `1.2` |
| `HorizontalAlignment` | 枚举 | 否 | 元素在父容器内的水平对齐：`Left` / `Center` / `Right` |
| `VerticalAlignment` | 枚举 | 否 | 元素在父容器内的垂直对齐：`Top` / `Center` / `Bottom` |
| `Opacity` | 数字 | 否 | 透明度，默认 `1.0` |

### 5. Image — 图片

```xml
<Image Id="hero" X="800" Y="160" Width="400" Height="400"
       Source="img_hero_001" Stretch="Uniform" Opacity="1.0" />
```

| 属性 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `X`, `Y` | 数字 | 否 | 默认 `0, 0` |
| `Width`, `Height` | 数字 | 否 | 固定尺寸 |
| `Source` | 字符串 | **是** | 图片资源 ID，非 URL，由上游 RAG 或资源系统解析 |
| `Stretch` | 枚举 | 否 | 缩放模式：`None` / `Fill` / `Uniform` / `UniformToFill`，默认 `Uniform` |
| `HorizontalAlignment` | 枚举 | 否 | 同上 |
| `VerticalAlignment` | 枚举 | 否 | 同上 |
| `Opacity` | 数字 | 否 | 透明度，默认 `1.0` |

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

渲染完成后，引擎返回两部分内容：

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

### 第三部分：截图（多模态模式可选）

如果模型支持图片输入，渲染引擎可额外返回当前页面的 PNG 截图。

---

## Id 自动生成规则

- 若元素未指定 `Id`，引擎在渲染时自动分配，格式为 `elem_NNN`（如 `elem_001`）
- 自动分配的 Id 会回填到 XML 中

---

## 渲染与排版规则

1. **默认布局为绝对定位**：子元素的位置由 `X`、`Y` 决定，相对于直接父容器左上角
2. **Z 序按文档顺序**：同一父容器内，后出现的元素渲染在上层
3. **Panel 自动尺寸**：未指定 `Width`/`Height` 时，Panel 尺寸自动扩展以包裹所有子元素（加上 Padding）
4. **TextElement 自动换行**：指定了 `Width` 时，文本在宽度约束内自动换行；未指定 `Width` 时单行不换行
5. **裁剪行为**：子元素超出父容器边界的部分被裁剪（Clip）

---

## 完整示例

```xml
<Page Background="#F5F5F5">

  <!-- 顶部导航栏 -->
  <Panel Id="top-bar" X="0" Y="0" Width="1280" Height="80"
         Background="#1A1A2E" Padding="32">
    <TextElement Id="logo" X="0" Y="20"
                 Text="SlideML" FontName="Arial" FontSize="24"
                 Foreground="#FFFFFF" />
  </Panel>

  <!-- 主标题 -->
  <TextElement Id="main-title" X="80" Y="140" Width="1120"
               Text="让大语言模型生成幻灯片"
               FontSize="48" Foreground="#1A1A2E"
               TextAlignment="Center" />

  <!-- 卡片容器 -->
  <Panel Id="cards-row" X="80" Y="260" Width="1120" Height="320">
    <!-- 卡片 1 -->
    <Rect Id="card1" X="0" Y="0" Width="340" Height="320"
          Fill="#FFFFFF" CornerRadius="12"
          Stroke="#E8E8E8" StrokeThickness="1" />
    <TextElement Id="card1-title" X="24" Y="24" Width="292"
                 Text="定义标签" FontSize="22" Foreground="#333" />
    <TextElement Id="card1-desc" X="24" Y="72" Width="292"
                 Text="使用简洁的 XML 标签描述页面结构，Panel、Rect、Text、Image 四种元素覆盖常见场景。"
                 FontSize="15" Foreground="#666" LineHeight="1.5" />

    <!-- 卡片 2 -->
    <Rect Id="card2" X="380" Y="0" Width="340" Height="320"
          Fill="#FFFFFF" CornerRadius="12"
          Stroke="#E8E8E8" StrokeThickness="1" />
    <TextElement Id="card2-title" X="24" Y="24" Width="292"
                 Text="模型生成" FontSize="22" Foreground="#333" />
    <TextElement Id="card2-desc" X="24" Y="72" Width="292"
                 Text="大语言模型根据用户需求生成 SlideML 文档，无需学习复杂排版软件。"
                 FontSize="15" Foreground="#666" LineHeight="1.5" />

    <!-- 卡片 3 -->
    <Rect Id="card3" X="760" Y="0" Width="340" Height="320"
          Fill="#FFFFFF" CornerRadius="12"
          Stroke="#E8E8E8" StrokeThickness="1" />
    <TextElement Id="card3-title" X="24" Y="24" Width="292"
                 Text="渲染反馈" FontSize="22" Foreground="#333" />
    <TextElement Id="card3-desc" X="24" Y="72" Width="292"
                 Text="引擎渲染后回填实际尺寸和换行信息，模型据此迭代优化排版。"
                 FontSize="15" Foreground="#666" LineHeight="1.5" />
  </Panel>

</Page>
```
