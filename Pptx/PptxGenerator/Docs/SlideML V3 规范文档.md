# SlideML V3 规范文档

## 概述

SlideML 是一套基于 XML 的幻灯片内容描述语言，专为大语言模型生成而设计。渲染引擎读取 SlideML 文档，生成可视化页面，并将测量结果回填到原文档中，形成「生成 → 渲染 → 反馈」闭环。

V3 在 V2 基础上进行了架构统一和能力提升：

- **Id 升级为必填且全局唯一**，为流式输出和动画系统提供一致的标识体系
- **`StyleFrom` 提升为通用能力**，所有元素均可引用其他元素作为样式基础，减少重复声明
- **字号等级机制**：`FontSize` 支持 `L1`~`L5`，用于提升多页面课件的文字层级一致性
- **动画系统**：`Storyboard` + `Appear` + `OnClick`，描述元素的出现顺序与节奏

---

## 第一部分：标签定义

### §1 基本约定

- 画布尺寸：默认 **1280 × 720** 像素，由业务层通过 `SlideMlPipelineContext` 传入实际页面尺寸
- 坐标原点：**左上角**，X 轴向右，Y 轴向下
- 所有数值单位默认为 **px**，不写单位
- 颜色格式：**`#RRGGBB`** 或 **`#AARRGGBB`**（八位含透明度）
- 字号值：`FontSize` 支持绝对 px 数字，如 `36`；也支持字号等级：`L1` / `L2` / `L3` / `L4` / `L5`
- 标签名和属性名采用 **PascalCase**
- **`Id` 属性为必填**，且在文档内**全局唯一**。流式输出场景下，`Id` 是跨片段匹配元素的唯一凭据
- **`StyleFrom` 为通用属性**，所有可视元素（Panel、Rect、TextElement、Image）均可使用，值为源元素 `Id`。解析时先复制源元素的全部属性作为默认值，再用当前元素显式声明的属性覆盖。不复制子元素

#### 字号等级

为提升多页面课件在多人或多 AI 实例并行制作时的文字层级一致性，SlideML 支持在 `FontSize` 中使用字号等级。

`FontSize` 支持两种形式：

| 形式 | 示例 | 说明 |
|------|------|------|
| 绝对 px 数字 | `FontSize="36"` | 兼容旧文档，表示固定字号 px |
| 字号等级 | `FontSize="L3"` | 推荐写法，由渲染引擎按当前画布换算为实际 px |

默认字号等级基于 **1280 × 720** 画布定义，`L3` 为基准正文：

| 等级 | 倍数 | 默认用途 |
|------|------:|----------|
| `L1` | `1.67` | 封面主标题、章节大标题 |
| `L2` | `1.17` | 页面标题、重点标题 |
| `L3` | `1.00` | 正文、对话、句型 |
| `L4` | `0.83` | 辅助文字、词汇、注释 |
| `L5` | `0.67` | 微文字、页码、角标 |

默认基准值：

```text
BaseCanvas = 1280 × 720
BaseLevel = L3
BaseFontSize = 48px
```

当 `FontSize` 使用 `L1` ~ `L5` 时，渲染引擎按以下公式换算实际字号：

```text
ScaleX = CurrentCanvasWidth / BaseCanvasWidth
ScaleY = CurrentCanvasHeight / BaseCanvasHeight
FontScale = min(ScaleX, ScaleY)

ActualFontSize = BaseFontSize × LevelScale × FontScale
```

在默认 1280×720 画布下，`L3 = 48px`。若画布缩放到 960×540，则 `FontScale = 0.75`，此时 `L3 = 36px`。

若 `FontSize` 是数字，则表示绝对 px，不参与字号等级换算。

#### 通用属性

以下属性适用于所有可视元素（Panel、Rect、TextElement、Image），各元素定义中不再重复列出：

| 属性 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `Id` | 字符串 | **是** | 元素标识符，全局唯一。流式输出场景下，`Id` 是跨片段匹配元素的唯一凭据 |
| `StyleFrom` | 字符串 | 否 | 引用源元素 `Id`，复制其全部属性作为默认值。不复制子元素 |
| `X`, `Y` | 数字 | 否 | 相对于父容器左上角的坐标，默认 `0, 0` |
| `Width`, `Height` | 数字 | 否 | 固定尺寸。各元素对不写尺寸时的行为有各自说明 |
| `HorizontalAlignment` | 枚举 | 否 | 水平对齐：`Left` / `Center` / `Right`，仅当不写 X 时生效 |
| `VerticalAlignment` | 枚举 | 否 | 垂直对齐：`Top` / `Center` / `Bottom`，仅当不写 Y 时生效 |
| `Opacity` | 数字 | 否 | 透明度 `0.0 ~ 1.0`，默认 `1.0` |
| `Margin` | 字符串 | 否 | 外边距，逗号分隔 1~4 个值，如 `"0,0,0,8"` |

---

### §2 Page — 根元素

每个文档有且仅有一个 Page 元素，作为根容器。

```xml
<Page Background="#FFFFFF">
  <!-- 所有子元素放在这里 -->
</Page>
```

| 属性 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `Background` | 颜色 | 否 | 画布背景色，默认 `#FFFFFF` |

---

### §3 Panel — 容器

用于组织子元素的逻辑分组，支持嵌套。支持绝对定位和流式布局两种模式。

#### 绝对定位模式（默认）

```xml
<Panel Id="header" X="0" Y="0" Width="1280" Height="120"
       Padding="24" Background="#1A1A2E">
  <TextElement Id="header-title" X="24" Y="30" Text="标题文字" ... />
</Panel>
```

#### 流式布局模式

```xml
<!-- 水平排列 -->
<Panel Id="cards-row" Layout="Horizontal" Gap="12" X="80" Y="260" Width="1120">
  <Rect Id="card1" Width="340" Height="320" Fill="#FFFFFF" CornerRadius="12" />
  <Rect Id="card2" Width="340" Height="320" Fill="#FFFFFF" CornerRadius="12" />
</Panel>

<!-- 垂直排列 -->
<Panel Id="content-col" Layout="Vertical" Gap="16" Padding="24">
  <TextElement Id="title" Text="标题" FontSize="L2" IsBold="True" />
  <TextElement Id="body" Text="正文内容..." FontSize="L3" />
</Panel>
```

| 属性 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `Padding` | 数字 | 否 | 内边距，四个方向统一值，默认 `0` |
| `Background` | 颜色 | 否 | 背景色，默认透明 |
| `Layout` | 枚举 | 否 | 子元素排列方向：`Absolute`（默认）/ `Horizontal` / `Vertical` |
| `Gap` | 数字 | 否 | 流式布局下子元素之间的默认间距（px），默认 `0` |

> **通用属性**：`Id`、`StyleFrom`、`X`、`Y`、`Width`、`Height`、`HorizontalAlignment`、`VerticalAlignment`、`Opacity`、`Margin` 见 [§1 通用属性](#1-基本约定)。`Width`/`Height` 不写则自动撑开到包裹所有子元素。流式布局中排列轴方向的 `X`/`Y` 被忽略。流式布局中实际间距 = `max(Gap, 前元素下Margin + 后元素上Margin)`。

**Panel 子元素：**

Panel 支持 `<Fill>` 子元素定义渐变背景（优先于 `Background` 属性）：

```xml
<Panel Id="hero" X="0" Y="0" Width="1280" Height="720">
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

---

### §4 Rect — 矩形

```xml
<Rect Id="card1" X="40" Y="160" Width="380" Height="280"
      Fill="#FFFFFF" Stroke="#E0E0E0" StrokeThickness="1"
      CornerRadius="8" Opacity="1.0" />
```

| 属性 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `Fill` | 颜色 | 否 | 填充色，默认透明 |
| `Stroke` | 颜色 | 否 | 描边色，默认无描边 |
| `StrokeThickness` | 数字 | 否 | 描边粗细，默认 `0` |
| `CornerRadius` | 数字 | 否 | 圆角半径（px），默认 `0` |
| `StrokeDashArray` | 字符串 | 否 | 虚线描边模式，逗号分隔数值，如 `"4,2"` 表示 4px 实线 + 2px 空白 |
| `Shadow` | 字符串 | 否 | 阴影属性形式：`"OffsetX OffsetY Blur Color"`，如 `"0 4 12 #00000033"` |

> **通用属性**：`Id`、`StyleFrom`、`X`、`Y`、`Width`、`Height`、`HorizontalAlignment`、`VerticalAlignment`、`Opacity`、`Margin` 见 [§1 通用属性](#1-基本约定)。

**Rect 子元素：**

Rect 支持 `<Fill>`、`<Stroke>`、`<Shadow>` 子元素，用于定义渐变填充/描边和精细阴影控制。子元素属性优先于同名 XML 属性。详见 §8。

---

### §5 TextElement — 文本

```xml
<TextElement Id="title" X="60" Y="180" Width="340"
             Text="这是一段可能会自动换行的文本内容"
             FontName="Microsoft YaHei" FontSize="L2" IsBold="True"
             Foreground="#1A1A2E"
             TextAlignment="Left" LineHeight="1.4"
             Opacity="1.0" />
```

| 属性 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `Text` | 字符串 | 是（若无 Span） | 文本内容。若有 Span 子元素则可省略 |
| `FontName` | 字符串 | 否 | 字体名称，默认 `Microsoft YaHei` |
| `FontSize` | 字号值 | 否 | 字号。可为绝对 px 数字，如 `36`；也可为字号等级 `L1`~`L5`。默认 `16` |
| `IsBold` | 布尔 | 否 | 是否为粗体，`True` 为粗体，`False` 或不写为正常粗细 |
| `IsItalic` | 布尔 | 否 | 是否为斜体，`True` 为斜体，`False` 或不写为正常 |
| `Foreground` | 颜色 | 否 | 文字颜色，默认 `#000000` |
| `TextAlignment` | 枚举 | 否 | 文本水平对齐：`Left` / `Center` / `Right` / `Justify`，默认 `Left` |
| `LineHeight` | 数字 | 否 | 行高倍数，如 `1.5` 表示 1.5 倍行高，默认 `1.2` |

> **通用属性**：`Id`、`StyleFrom`、`X`、`Y`、`Width`、`Height`、`HorizontalAlignment`、`VerticalAlignment`、`Opacity`、`Margin` 见 [§1 通用属性](#1-基本约定)。`Width` 不写则单行无限宽；`Width` 写了则文本在约束宽度内自动换行。

**TextElement 子元素：`<Span>` — 富文本片段**，详见 §7。

---

### §6 Image — 图片

```xml
<Image Id="hero" X="800" Y="160" Width="400" Height="400"
       Source="img_hero_001" Stretch="Uniform" Opacity="1.0" />
```

| 属性 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `Source` | 字符串 | **是** | 图片资源 ID，非 URL，由上游 RAG 或资源系统解析 |
| `Stretch` | 枚举 | 否 | 缩放模式：`None` / `Fill` / `Uniform` / `UniformToFill`，默认 `Uniform` |

> **通用属性**：`Id`、`StyleFrom`、`X`、`Y`、`Width`、`Height`、`HorizontalAlignment`、`VerticalAlignment`、`Opacity`、`Margin` 见 [§1 通用属性](#1-基本约定)。

---

### §7 Span — 富文本片段

支持同一 TextElement 内多种样式混排：

```xml
<TextElement Id="desc" X="40" Y="200" Width="400">
  <Span Text="标题" FontSize="L3" Foreground="#333" IsBold="True"/>
  <Span Text=" — 副标题说明" FontSize="L4" Foreground="#666"/>
</TextElement>
```

| 属性 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `Text` | 字符串 | **是** | 片段文本内容 |
| `FontSize` | 字号值 | 否 | 继承 TextElement 的值。可为绝对 px 数字或字号等级 `L1`~`L5` |
| `FontName` | 字符串 | 否 | 继承 TextElement 的值 |
| `Foreground` | 颜色 | 否 | 继承 TextElement 的值 |
| `IsBold` | 布尔 | 否 | 继承 TextElement 的值 |
| `IsItalic` | 布尔 | 否 | 继承 TextElement 的值 |
| `TextDecoration` | 枚举 | 否 | `None` / `Underline`，默认 `None` |

---

### §8 Fill / Stroke / Shadow / LinearGradient / Stop — 渐变与阴影子元素

#### `<Fill>` — 渐变填充

用于 Panel、Rect 的渐变背景，优先于 `Fill` 属性（纯色）：

```xml
<Rect Id="bg" X="0" Y="0" Width="1280" Height="720">
  <Fill>
    <LinearGradient X1="0" Y1="0" X2="1" Y2="1">
      <Stop Offset="0" Color="#4A7BF7"/>
      <Stop Offset="1" Color="#F4F6FA"/>
    </LinearGradient>
  </Fill>
</Rect>
```

#### `<Stroke>` — 渐变描边

用于 Rect 的渐变描边，优先于 `Stroke` 属性（纯色）。需配合 `StrokeThickness` 属性：

```xml
<Rect Id="btn" Width="200" Height="60" CornerRadius="8" StrokeThickness="2">
  <Stroke>
    <LinearGradient X1="0" Y1="0" X2="1" Y2="0">
      <Stop Offset="0" Color="#4A7BF7"/>
      <Stop Offset="1" Color="#6C5CE7"/>
    </LinearGradient>
  </Stroke>
</Rect>
```

#### `<Shadow>` — 精细阴影

用于 Rect 的阴影效果，属性比字符串形式更精细：

```xml
<Rect Id="card" Fill="#FFFFFF" CornerRadius="8">
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

#### `<LinearGradient>` — 线性渐变

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `X1`, `Y1` | 数字 | `0, 0` | 渐变起点，0~1 表示相对元素尺寸的比例 |
| `X2`, `Y2` | 数字 | `1, 0` | 渐变终点，0~1 表示相对元素尺寸的比例 |

#### `<Stop>` — 渐变停止点

| 属性 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `Offset` | 数字 | **是** | 停止点位置，范围 0~1 |
| `Color` | 颜色 | **是** | 停止点颜色 |

---

## 第二部分：流式输出模式

流式输出模式下，文档通过连续的 XML 片段序列逐步构建。每个片段是一个顶层 XML 元素，解析器逐片段接收并累积构建完整页面。

流式输出**不是新的标签体系**——所有可视元素和属性沿用第一部分的定义，仅新增 `<Remove>` 作为流式场景下的删除操作标签。本部分仅定义流式场景下的解析行为、合并规则和错误处理。

---

### §9 Remove — 删除元素

用于显式删除已有元素及其子树。仅适用于流式输出场景（一次性输出无需删除——直接不输出即可）。

```xml
<Remove TargetId="card2"/>
```

| 属性 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `TargetId` | 字符串 | **是** | 要删除的元素 `Id`（全局唯一） |

**边界情况：**

| 场景 | 行为 |
|------|------|
| `TargetId` 不存在 | 报 `[Warning]`，忽略 |

---

### §10 片段序列与流结束

#### 片段序列

LLM 的输出是一个连续的 XML 片段序列。每个片段是一个顶层 XML 元素。一个典型的输出顺序：

1. 先输出 `<Page>...</Page>` 定义初始布局（元素带 `Id`、位置、尺寸，内容可空）
2. 后续持续输出片段，通过 `Id` 匹配已有元素进行属性合并、子元素调整或删除

`<Page>` 本身也可以作为片段在后续再次出现，更新页面属性或调整顶层结构。

#### 流结束

解析器以 **EOF** 作为流结束信号，不引入额外的结束标记。

---

### §11 属性级 Merge

当流片段中的元素通过 `Id` 匹配到已有元素时：

- 片段中**显式声明的属性** → 覆盖已有值
- 片段中**未声明的属性** → 保留原有值
- 片段中**未声明的子元素** → 保留原有子元素

```xml
<!-- 初始 -->
<Panel Id="header" X="0" Y="0" Width="1280" Height="120"/>

<!-- 片段：只写 Background，其余属性保留 -->
<Panel Id="header" Background="#1A1A2E"/>

<!-- 结果 -->
<Panel Id="header" X="0" Y="0" Width="1280" Height="120" Background="#1A1A2E"/>
```

---

### §12 StyleFrom 详解

`StyleFrom` 是通用属性（§1 已定义），在流式场景下尤为重要——模型一次定义模板元素，后续所有同类元素通过 `StyleFrom` 引用，只写差异属性。

```xml
<!-- 悬空模板（见 §13） -->
<Rect Id="card-template" Width="340" Height="160" Fill="#FFFFFF"
      CornerRadius="12" Shadow="0 4 12 #00000033"
      Stroke="#E8E8E8" StrokeThickness="1"/>

<!-- card2-bg 以 card1-bg 为样式基础，只声明差异属性 -->
<Rect Id="card2-bg" StyleFrom="card1-bg" Stroke="#4A7BF7" StrokeThickness="2"/>
<!-- 结果：Width/Height/Fill/CornerRadius/Shadow 照抄 card1-bg，
     Stroke/StrokeThickness 被显式声明覆盖 -->
```

**优先级链**：`StyleFrom` 源属性 < 元素显式声明的属性 < 后续片段 Merge 的属性。

若源元素属性值为字号等级，例如 `FontSize="L2"`，`StyleFrom` 复制的是等级值原文，而不是复制解析后的实际 px。字号等级会在最终渲染阶段统一解析。

如果源元素 `Id` 不存在，报 `[Error]`。

---

### §13 悬空元素

不在 `<Page>` 子树内的顶层元素为**悬空元素**。悬空元素不参与渲染，仅供 `StyleFrom` 引用。

```xml
<!-- 悬空模板：不渲染，只作为样式源 -->
<Rect Id="card-template" Width="340" Height="160" Fill="#FFFFFF" CornerRadius="12"/>

<Page Id="main" Background="#F5F5F5">
  <Panel Id="cards" Layout="Horizontal" Gap="24" X="80" Y="100"/>
</Page>

<!-- card1-bg 引用悬空模板 -->
<Panel Id="cards">
  <Rect Id="card1-bg" StyleFrom="card-template"/>
</Panel>
```

悬空元素一旦创建即为纯模板，**不可**在后续片段中纳入容器子树。`Id` 全局唯一规则已覆盖此约束——同一个 `Id` 不能出现在两个不同父容器下，而悬空元素无父容器，纳入容器子树将导致 `Id` 冲突。

---

### §14 子元素 Merge 算法

当容器元素（Panel 等）包含子元素时，按以下算法合并：

#### 排序算法

```
给定：
  当前子元素列表 L = [e₁, e₂, e₃, ...]
  片段子元素列表 F = [f₁, f₂, f₃, ...]

步骤：

① 定位锚点
   从 f₁ 开始依次检查 F 中每个元素：
     如果该元素的 Id 在 L 中也存在 → 记这个 Id 在 L 中的位置为 P，停止检查。
   如果 F 中所有元素的 Id 在 L 中都不存在 → P = |L|（L 的末尾位置）。

② 移除冲突
   遍历 L，对于 L 中的每个元素：
     如果它的 Id 在 F 中也出现了 → 从 L 中删掉它。
     否则保留。

③ 插入
   把 F 整个列表插入到 L 的位置 P 处。
   若 P > |L| → 追加到末尾。
```

#### 示例

```
原有 L = [card1, card2]

例 ① F = [card3]
   ① card3 在 L 中不存在 → P = 2（末尾）
   ② L 中没有与 F 重复的 Id → L 不变，仍是 [card1, card2]
   ③ 位置 2 插入 [card3]
   → 结果: [card1, card2, card3]

例 ② L = [card1, card2, card3], F = [card4, card2]
   ① card4 在 L 中不存在，card2 在 L 中存在 → P = 1（card2 在 L 中的位置）
   ② card2 在 F 中出现 → 从 L 中删掉 → L = [card1, card3]
   ③ 位置 1 插入 [card4, card2]
   → 结果: [card1, card4, card2, card3]

例 ③ L = [card1, card4, card2, card3], F = [card3, card2]
   ① card3 在 L 中存在 → P = 3（card3 在 L 中的位置）
   ② card2 和 card3 都在 F 中出现 → 从 L 中删掉 → L = [card1, card4]
   ③ P=3 > |L|=2 → 追加到末尾 [card3, card2]
   → 结果: [card1, card4, card3, card2]
```

---

### §15 容器无子元素 / 未提及=保留

#### 容器无子元素

片段中出现的容器元素**不含子元素**时，仅执行属性 Merge，现有子元素**保持不动**。

`<Panel Id="x"/>` 和 `<Panel Id="x"></Panel>` 在 XML 语义上等价，解析器**不区分**两者。

```xml
<!-- 初始 -->
<Panel Id="outer">
  <Panel Id="inner" X="80" Y="100" Width="1120" Height="520">
    <Rect Id="card1" Width="340" Height="260" Fill="#FFFFFF"/>
  </Panel>
</Panel>

<!-- 片段：inner 不含子元素 → inner 及其子树完全不动 -->
<Panel Id="outer">
  <Panel Id="inner"/>
  <Panel Id="inner2" X="80" Y="650" Width="1120" Height="70"/>
</Panel>
```

#### 未提及 = 保留

流片段只影响显式声明的元素及其子树。**未被任何片段提及的元素保持原样**。这一规则在所有层级上一致：

| 层级 | 未提及 = 保留的体现 |
|------|---------------------|
| 属性 | 片段中未声明的属性 → 保留原有值 |
| 容器自身（无子元素） | 片段中容器不含子元素 → 现有子元素保持不动 |
| 容器内子元素 | 片段中未出现的子元素 → 保留不动 |
| 整棵树 | 未被任何片段提及的元素 → 保持原样 |

这意味着模型在任何层级都只需要写**变化的部分**。

---

### §16 流式错误处理

由于流式输出不可回溯，解析器采用**容错续行**策略：

| 错误类型 | 处理方式 |
|----------|----------|
| XML 格式错误（标签未闭合、属性语法错误等） | 报 `[Error]` 并附位置信息，**跳过该片段**，继续解析后续内容 |
| 片段不是合法 XML | 报 `[Error]`，输出原始文本片段供调试 |
| 元素缺少 `Id` | 报 `[Error]`，跳过该元素 |
| 同一个 `Id` 出现在文档树的两个不同父容器下 | 报 `[Error]` |
| 同一个片段内出现两个相同 `Id` | 报 `[Error]` |
| `<Remove>` 的 `TargetId` 不存在 | 报 `[Warning]`，忽略该操作 |
| `StyleFrom` 的源元素 `Id` 不存在 | 报 `[Error]`，该元素恢复为无 `StyleFrom` 处理 |
| 未知字号等级，如 `FontSize="L6"` | 报 `[Error]`，该属性回退到默认字号 |

**错误说明示例：**

- 同一个 `Id` 出现在文档树的两个不同父容器下：如 `card1` 同时是 `PanelA` 和 `PanelB` 的子元素
- 片段中同一个 `Id` 出现两次：如 `<Panel Id="x"><Rect Id="dup"/><Rect Id="dup"/></Panel>`

**解析器行为时序：**

```
流开始
  │
  ├─ 接收到顶层元素
  │     ├─ <Page>...</Page>          → 设置/更新页面属性，合并子元素
  │     ├─ <Panel Id="x">...</Panel>  → 按 Id 查找，属性 Merge，子元素 Merge
  │     ├─ <Rect Id="x">...</Rect>    → 同上
  │     ├─ <TextElement Id="x">...</TextElement> → 同上
  │     ├─ <Image Id="x">...</Image>  → 同上
  │     └─ <Remove TargetId="x"/>    → 删除元素及其子树
  │
  │  注：不在 <Page> 子树内的顶层元素为悬空元素，不参与渲染，
  │      仅供 StyleFrom 引用
  │
  └─ EOF → 流结束，最终渲染
```

---

## 第三部分：动画系统

动画系统的核心职责是描述**元素的出现顺序与节奏**——哪些元素在什么时候以什么节奏显现。视觉呈现（动画类型、缓动函数等）由引擎的默认主题决定，模型不参与。

### 设计原则

- **叙事即动画**。模型只需回答「内容分几步出现、每一步出现什么」，引擎负责「怎么出现」
- **概念最少**。动画系统新增 XML 概念不超过 5 个，未尽需求交给引擎主题
- **与布局正交**。动画层和布局层独立，同一个元素可同时出现在布局定义和动画定义中
- **Merge 兼容**。主序列步骤沿用子元素 Merge 算法（§14），流式输出友好

---

### §17 架构总览

```
Page
  ├─ 布局层：TextElement, Rect, Panel, Image...
  └─ 动画层（当文档包含 Storyboard 或 OnClick 时生效）
       ├─ Storyboard（主时间线，由全局点击驱动）
       │    └─ Appear Id="..."（一个步骤 = 一次点击）
       │         └─ 多行文本
       └─ 元素.OnClick（分支时间线，由点击该元素驱动）
            └─ Appear
                 └─ 多行文本
```

### 可见性约定

- **未被任何 Appear 或 OnClick 引用的元素**：始终可见
- **被 Appear 或 OnClick 引用的元素**：初始不可见，按时间线节奏自动显现。一旦显现即**持久可见**，后续步骤再次引用同一元素无效果（对齐 PowerPoint 行为）
- **可见性沿树向下继承**：父元素不可见时，子元素无论是否被引用都不可见
- 引擎在渲染前自动处理可见性，模型不需要设置 `Opacity`

---

### §18 Storyboard — 主时间线

主序列容器，放在 Page 内部。包含若干 Appear 步骤，由用户全局点击驱动。一次点击推进一个 Appear。

```xml
<Storyboard Duration="0.5s">
  <Appear Id="step-intro">
    title ; subtitle
  </Appear>
  <Appear Id="step-cards">
    card1
    card2 +0.15s
    card3 +0.3s
  </Appear>
</Storyboard>
```

| 属性 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `Duration` | 时间 | 否 | 步骤内元素默认动画时长，如 `0.5s`。不写则由引擎主题决定 |

---

### §19 Appear — 一个叙事步骤

代表一次全局点击。与 Storyboard 配合使用，必须在 Storyboard 内。

```xml
<Appear Id="step-intro">
  title /0.6s
  subtitle +0.15s /0.4s
</Appear>
```

| 属性 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `Id` | 字符串 | 见说明 | 步骤标识。在 Storyboard 内**必填**且全局唯一，用于流式 Merge 排序。在 OnClick 内**省略**（不需要 Id） |
| `Duration` | 时间 | 否 | 该步骤内元素默认动画时长。优先级：元素 `/Ns` > Appear Duration > Storyboard Duration > 引擎默认 |

**Appear 的内容是多行文本**，每行可以写一个或多个元素。语法详见 §21。

---

### §20 OnClick — 元素上的分支触发

附着在任意元素上的分支时间线。点击该元素后触发，内部包含一个 Appear（不需要 Id，不需要 Duration）。

```xml
<Rect Id="card1" Fill="#1A1A3E" CornerRadius="12">
  <OnClick>
    <Appear>
      detail1
      detail2 +0.15s
    </Appear>
  </OnClick>
</Rect>
```

OnClick 只能包含一个 Appear。Appear 内部使用与 Storyboard 中相同的多行文本语法。

---

### §21 多行文本语法

Appear 的内容使用多行文本语法。每行若干元素 Id，可选后缀，按行从上往下编排。空行会被忽略。

#### 基本规则

```
一行一个元素 → 该元素在步骤内出现
一行多个元素，分号分隔 → 同时出现
后缀可选，不写就用引擎默认
```

#### 后缀

| 后缀 | 示例 | 含义 |
|------|------|------|
| `/Ns` | `card1 /0.6s` | 该元素动画持续 N 秒 |
| `+Ns` | `card2 +0.15s` | 该元素在**步骤开始后 N 秒**启动（绝对延迟，不依赖上一行） |

两个后缀可同时使用，顺序无关：`card1 /0.6s +0.15s` 等价于 `card1 +0.15s /0.6s`。

#### 同行分号

```
card1 ; card2 /0.5s
```

分号分隔的元素同时启动。后缀紧邻绑定到各自的元素：`/0.5s` 只作用于 `card2`，`card1` 使用默认时长。可以各自写后缀：`card1 /0.5s ; card2 /0.8s`。

#### 换行

```
card1
card2 +0.15s
card3 +0.3s
```

换行分隔的元素各自独立，启动时间由 `+Ns` 决定。不写 `+Ns` 则默认 `+0s`（步骤开始即刻启动）。

#### 完整示例

```
title ; subtitle
card1 /0.5s
card2 +0.15s /0.5s
card3 +0.3s /0.5s
```

| 行 | 行为 |
|------|------|
| `title ; subtitle` | 步骤开始时，title 和 subtitle 同时出现，使用引擎默认时长 |
| `card1 /0.5s` | 步骤开始时，card1 出现，持续 0.5s |
| `card2 +0.15s /0.5s` | 步骤开始后 0.15s，card2 出现，持续 0.5s |
| `card3 +0.3s /0.5s` | 步骤开始后 0.3s，card3 出现，持续 0.5s |

card1、card2、card3 依次错开 0.15s，视觉上交叠。

---

### §22 Storyboard Merge 排序

Storyboard 的子元素（Appear）沿用 §14 的子元素 Merge 算法。每个 Appear 通过 `Id` 参与排序。

#### 示例

```
初始：
  Storyboard
    Appear Id="step-intro"
    Appear Id="step-cards"

片段插入：
  Storyboard
    Appear Id="step-subtitle"
    Appear Id="step-cards"      ← 锚点

结果：
  Storyboard
    Appear Id="step-intro"
    Appear Id="step-subtitle"   ← 插入
    Appear Id="step-cards"
```

模型只需要知道「这个新步骤排在 cards 前面」，通过 `Id` 锚定即可，不需要维护序号。

---

### §23 边界与逃生舱

#### 引擎职责

以下参数由引擎默认主题决定，不在 SlideML 中暴露：

- 动画类型（淡入/飞入/缩放）
- 缓动函数
- 方向（从哪个方向飞入）

如需全局调整风格，通过引擎主题切换（如「商务」「活泼」「极简」），不通过 SlideML 语法。

#### 不支持的能力

以下能力不在 SlideML 动画范围内：

- **强调动画**（脉冲、抖动、高亮）：静态幻灯片极少需要，可在 PowerPoint 中手工添加
- **退出动画**：被引用的元素初始即隐藏，不存在「出现后再退出」的叙事需求
- **路径动画**：复杂的运动轨迹属于后期手工精调范畴
- **逐字出现**：属于文本级动画，暂不支持

如有此类需求，SlideML 保证静态布局正确，动画细节交给 PowerPoint 后期编辑。

---

## 第四部分：引擎回填与渲染反馈

### §24 引擎回填属性

以下属性由渲染引擎在渲染完成后自动填写，**模型不应输出这些属性**，写了也会被覆盖。

| 属性 | 适用标签 | 说明 |
|------|----------|------|
| `ActualWidth` | 全部 | 渲染后的实际宽度（px） |
| `ActualHeight` | 全部 | 渲染后的实际高度（px） |
| `ActualLineCount` | TextElement | 文本换行后的实际行数 |

---

### §25 渲染反馈

渲染完成后，引擎返回四部分内容：

#### 第一部分：回填后的 XML

原始文档加上 `ActualWidth`、`ActualHeight`、`ActualLineCount`，模型可以直接对比预期与实际的差异。

#### 第二部分：Warning 文本

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

#### 第三部分：Error 文本

纯文本，一行一条错误，格式：

```
[Error] <ElementId>: <描述>
```

#### 第四部分：截图

渲染引擎返回当前页面的 PNG 截图，模型可从视觉层面评估颜色、间距、对齐等。

---

## 第五部分：渲染与排版规则

1. **默认布局为绝对定位**：子元素的位置由 `X`、`Y` 决定，相对于直接父容器左上角
2. **流式布局**：Panel 设置 `Layout="Horizontal"` 或 `Layout="Vertical"` 时，子元素沿排列轴依次排列，排列轴上的 `X`/`Y` 被忽略
3. **Z 序按文档顺序**：同一父容器内，后出现的元素渲染在上层
4. **阴影在 Opacity 之前绘制**：元素阴影不受元素自身 `Opacity` 影响
5. **渐变填充优先于纯色填充**：`<Fill>`/`<Stroke>` 子元素优先于 `Fill`/`Stroke` 属性
6. **Panel 自动尺寸**：未指定 `Width`/`Height` 时，Panel 尺寸自动扩展以包裹所有子元素（加上 Padding）
7. **TextElement 自动换行**：指定了 `Width` 时，文本在宽度约束内自动换行；未指定 `Width` 时单行不换行
8. **裁剪行为**：子元素超出父容器边界的部分被裁剪（Clip）

---

## 第六部分：完整示例

以下示例覆盖布局层（渐变 Panel + 流式布局卡片行 + Span 富文本）+ 动画层（Storyboard + Appear + OnClick）：

```xml
<Page Background="#0F0F23">
  <!-- ===== 布局层 ===== -->

  <!-- 渐变 Hero 区域 -->
  <Panel Id="hero" X="0" Y="0" Width="1280" Height="360">
    <Fill>
      <LinearGradient X1="0" Y1="0" X2="1" Y2="1">
        <Stop Offset="0" Color="#1A1A2E"/>
        <Stop Offset="1" Color="#4A4A6E"/>
      </LinearGradient>
    </Fill>
    <TextElement Id="hero-title" X="80" Y="120" Width="1120"
                 Text="SlideML V3" FontSize="L1" IsBold="True"
                 Foreground="#FFFFFF" TextAlignment="Center" />
    <TextElement Id="hero-sub" X="80" Y="200" Width="1120"
                 Text="统一规范 · 流式生成 · 动画叙事"
                 FontSize="L3" Foreground="#CCCCDD" TextAlignment="Center" />
  </Panel>

  <!-- 流式布局：卡片行 -->
  <Panel Id="cards-row" Layout="Horizontal" Gap="24" X="80" Y="400" Width="1120" Height="280">
    <!-- 卡片 1 -->
    <Rect Id="card1" Width="340" Height="260" Fill="#1A1A3E" CornerRadius="12"
          Shadow="0 4 12 #00000033" Stroke="#2A2A5E" StrokeThickness="1">
      <OnClick>
        <Appear>
          detail1
        </Appear>
      </OnClick>
    </Rect>
    <TextElement Id="card1-title" X="24" Y="24" Width="292"
                 Text="流式布局" FontSize="L4" IsBold="True" Foreground="#FFFFFF" />
    <TextElement Id="card1-desc" X="24" Y="72" Width="292"
                 Text="支持 Panel Layout='Horizontal'/'Vertical'，自动排列子元素，减少手动坐标计算。"
                 FontSize="L5" Foreground="#8888AA" LineHeight="1.5" />

    <!-- 卡片 2 -->
    <Rect Id="card2" Width="340" Height="260" Fill="#1A1A3E" CornerRadius="12"
          Shadow="0 4 12 #00000033" Stroke="#2A2A5E" StrokeThickness="1">
      <OnClick>
        <Appear>
          detail2
        </Appear>
      </OnClick>
    </Rect>
    <TextElement Id="card2-title" X="24" Y="24" Width="292"
                 Text="渐变与阴影" FontSize="L4" IsBold="True" Foreground="#FFFFFF" />
    <TextElement Id="card2-desc" X="24" Y="72" Width="292"
                 Text="支持线性渐变填充/描边和元素阴影效果，提升视觉层次感。"
                 FontSize="L5" Foreground="#8888AA" LineHeight="1.5" />

    <!-- 卡片 3：富文本 Span -->
    <Rect Id="card3" Width="340" Height="260" Fill="#1A1A3E" CornerRadius="12"
          Shadow="0 4 12 #00000033" Stroke="#2A2A5E" StrokeThickness="1">
      <OnClick>
        <Appear>
          detail3
        </Appear>
      </OnClick>
    </Rect>
    <TextElement Id="card3-title" X="24" Y="24" Width="292"
                 Text="富文本与动画" FontSize="L4" IsBold="True" Foreground="#FFFFFF" />
    <TextElement Id="card3-desc" X="24" Y="72" Width="292">
      <Span Text="支持 Span 子元素" FontSize="L5" Foreground="#8888AA"/>
      <Span Text="在同一文本块内" FontSize="L5" IsBold="True" Foreground="#CCCCDD"/>
      <Span Text="混排多种样式，" FontSize="L5" Foreground="#8888AA"/>
      <Span Text="以及 OnClick 点击展开。" FontSize="L5" Foreground="#8888AA"/>
    </TextElement>
  </Panel>

  <!-- 点击展开的详情区域（初始隐藏） -->
  <Rect Id="detail1" X="80" Y="700" Width="340" Height="10"
        Fill="#2A2A5E" CornerRadius="8"/>
  <Rect Id="detail2" X="444" Y="700" Width="340" Height="10"
        Fill="#2A2A5E" CornerRadius="8"/>
  <Rect Id="detail3" X="808" Y="700" Width="340" Height="10"
        Fill="#2A2A5E" CornerRadius="8"/>

  <!-- ===== 动画层 ===== -->
  <Storyboard Duration="0.5s">
    <Appear Id="step-intro">
      hero-title ; hero-sub
    </Appear>

    <Appear Id="step-cards">
      card1
      card2 +0.15s
      card3 +0.3s
    </Appear>
  </Storyboard>
</Page>
```

### 播放流程

```
幻灯片打开         → 背景可见，所有被引用元素隐藏（hero-title, hero-sub, card1, card2, card3, detail1, detail2, detail3）
全局点击           → hero-title 和 hero-sub 同时出现（step-intro）
全局点击           → card1 出现，0.15s 后 card2，再 0.15s 后 card3（step-cards）
点击 card1         → detail1 出现（card1 的 OnClick）
点击 card2         → detail2 出现（card2 的 OnClick）
点击 card3         → detail3 出现（card3 的 OnClick）
```

---

## 附录：概念清单

### 标签

| 标签 | 说明 |
|------|------|
| `<Page>` | 根元素，每个文档一个 |
| `<Panel>` | 容器，支持绝对定位和流式布局 |
| `<Rect>` | 矩形，支持填充、描边、圆角、阴影 |
| `<TextElement>` | 文本，支持富文本 Span |
| `<Image>` | 图片 |
| `<Span>` | 富文本片段（TextElement 子元素） |
| `<Fill>` | 渐变填充定义（Panel/Rect 子元素） |
| `<Stroke>` | 渐变描边定义（Rect 子元素） |
| `<Shadow>` | 精细阴影定义（Rect 子元素） |
| `<LinearGradient>` | 线性渐变（Fill/Stroke 子元素） |
| `<Stop>` | 渐变停止点（LinearGradient 子元素） |
| `<Remove>` | 删除元素 |
| `<Storyboard>` | 主时间线容器（Page 子元素） |
| `<Appear>` | 一个叙事步骤（Storyboard/OnClick 子元素） |
| `<OnClick>` | 元素上的分支触发 |

### 字号概念

| 概念 | 说明 |
|------|------|
| 字号值 | `FontSize` 可使用绝对 px 数字，也可使用 `L1`~`L5` 字号等级 |
| 字号等级 | `L1`~`L5`，用于统一多页面课件的文字层级，并随当前画布尺寸换算为实际 px |

### 通用属性

| 属性 | 适用范围 | 说明 |
|------|----------|------|
| `Id` | 全部 | 必填，全局唯一标识符 |
| `StyleFrom` | Panel, Rect, TextElement, Image | 引用源元素 `Id`，复制属性作为默认值 |
| `X`, `Y` | Panel, Rect, TextElement, Image | 位置坐标 |
| `Width`, `Height` | Panel, Rect, TextElement, Image | 尺寸 |
| `HorizontalAlignment` | Panel, Rect, TextElement, Image | 水平对齐 |
| `VerticalAlignment` | Panel, Rect, TextElement, Image | 垂直对齐 |
| `Opacity` | Panel, Rect, TextElement, Image | 透明度 |
| `Margin` | Panel, Rect, TextElement, Image | 外边距 |
