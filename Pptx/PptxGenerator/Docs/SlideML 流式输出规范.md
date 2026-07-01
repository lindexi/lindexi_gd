# SlideML 流式输出规范

## 概述

本文档定义 SlideML 的流式输出（Streaming）工程实践。流式输出面向 LLM 以 Flash 模式无思考连续生成的场景：模型持续输出 XML 片段序列，解析器逐片段接收并累积构建页面。

流式输出**不是新的标签体系**——所有元素和属性沿用 [SlideML V2 规范文档](./SlideML%20V2%20规范文档.md) 的定义。本规范仅定义流式场景下的解析行为、合并规则和错误处理。

---

## 与 V2 规范的关键差异

| 项目 | V2 | 流式 |
|------|-----|------|
| `Id` | 可选，引擎自动分配 | **必填**，全局唯一 |
| 输出方式 | 一次性完整文档 | 连续片段序列 |

---

## 核心概念

### 片段序列

LLM 的输出是一个连续的 XML 片段序列。每个片段是一个顶层 XML 元素。一个典型的输出顺序：

1. 先输出 `<Page>...</Page>` 定义初始布局（元素带 `Id`、位置、尺寸，内容可空）
2. 后续持续输出片段，通过 `Id` 匹配已有元素进行属性合并、子元素调整或删除

`<Page>` 本身也可以作为片段在后续再次出现，更新页面属性或调整顶层结构。

### 流结束

解析器以 **EOF** 作为流结束信号，不引入额外的结束标记。

---

## 合并规则

### 1. 属性级 Merge

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

### 2. 子元素 Merge

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
   若 P > |L| → 追加到末尾：如果 P 比当前 L 的长度还大，则追加到 L 末尾。
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

### 3. 容器无子元素

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

### 4. 未提及 = 保留

流片段只影响显式声明的元素及其子树。**未被任何片段提及的元素保持原样**。

---

## 删除元素

### `<Remove>` 标签

用于显式删除已有元素及其子树。

| 属性 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `TargetId` | 字符串 | **是** | 要删除的元素 `Id`（全局唯一） |

```xml
<!-- 删除 card2 -->
<Remove TargetId="card2"/>
```

**边界情况：**

| 场景 | 行为 |
|------|------|
| `TargetId` 不存在 | 报 `[Warning]`，忽略 |

---

## 错误处理

由于流式输出不可回溯，解析器采用**容错续行**策略：

| 错误类型 | 处理方式 |
|----------|----------|
| XML 格式错误（标签未闭合、属性语法错误等） | 报 `[Error]` 并附位置信息，**跳过该片段**，继续解析后续内容 |
| 片段不是合法 XML | 报 `[Error]`，输出原始文本片段供调试 |
| 元素缺少 `Id` | 报 `[Error]`，跳过该元素 |
| 同一个 `Id` 出现在文档树的两个不同父容器下 | 报 `[Error]` |
| 同一个片段内出现两个相同 `Id` | 报 `[Error]` |
| `<Remove>` 的 `TargetId` 不存在 | 报 `[Warning]`，忽略该操作 |
| 悬空元素（不在 Page 子树内的顶层元素）缺少 `StyleId` | 报 `[Error]`，中断流 |
| `StyleFrom` 引用的 `StyleId` 不存在 | 报 `[Error]`，该元素恢复为无 `StyleFrom` 处理 |
| `StyleId` 重复（两个元素声明了相同的 `StyleId`） | 报 `[Error]` |

### 错误说明示例

**同一个 `Id` 出现在文档树的两个不同父容器下**：

如 `card1` 同时是 `PanelA` 和 `PanelB` 的子元素

**片段中同一个 `Id` 出现两次**：

如：`<Panel Id="x"><Rect Id="dup"/><Rect Id="dup"/></Panel>`

---

## 解析器行为时序

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
  │      仅供 StyleFrom 引用（必须带 StyleId）
  │
  └─ EOF → 流结束，最终渲染
```

---

## 完整示例

### 初始输出

```xml
<Page Background="#F5F5F5">
  <Panel Id="header" X="0" Y="0" Width="1280" Height="120"/>
  <Panel Id="hero" X="0" Y="120" Width="1280" Height="360"/>
  <Panel Id="cards" Layout="Horizontal" Gap="24" X="80" Y="520" Width="1120" Height="180"/>
  <Panel Id="footer" X="0" Y="700" Width="1280" Height="20"/>
</Page>
```

### 后续片段

```xml
<!-- 填充 header -->
<Panel Id="header" Background="#1A1A2E">
  <TextElement Id="header-title" X="80" Y="30" Width="1120"
               Text="SlideML 流式版式" FontSize="36" IsBold="True"
               Foreground="#FFFFFF" TextAlignment="Center"/>
</Panel>

<!-- 填充 hero -->
<Panel Id="hero">
  <Fill>
    <LinearGradient X1="0" Y1="0" X2="1" Y2="1">
      <Stop Offset="0" Color="#1A1A2E"/>
      <Stop Offset="1" Color="#4A4A6E"/>
    </LinearGradient>
  </Fill>
  <TextElement Id="hero-text" X="80" Y="140" Width="1120"
               Text="让大语言模型流式生成专业幻灯片"
               FontSize="24" Foreground="#CCCCDD" TextAlignment="Center"/>
</Panel>

<!-- 追加第一张卡片 -->
<Panel Id="cards">
  <Rect Id="card1-bg" StyleId="card-style" Width="340" Height="160" Fill="#FFFFFF" CornerRadius="12"
        Shadow="0 4 12 #00000033" Stroke="#E8E8E8" StrokeThickness="1"/>
  <TextElement Id="card1-title" X="24" Y="20" Width="292"
               Text="流式输出" FontSize="20" IsBold="True" Foreground="#333"/>
  <TextElement Id="card1-desc" X="24" Y="60" Width="292"
               Text="模型持续输出片段，解析器逐片段累积构建页面。"
               FontSize="14" Foreground="#666"/>
</Panel>

<!-- 追加第二张卡片，引用 card-style 样式模板 -->
<Panel Id="cards">
  <Rect Id="card2-bg" StyleFrom="card-style" Stroke="#4A7BF7" StrokeThickness="2"/>
  <TextElement Id="card2-title" X="24" Y="20" Width="292"
               Text="Id 合并" FontSize="20" IsBold="True" Foreground="#333"/>
  <TextElement Id="card2-desc" X="24" Y="60" Width="292"
               Text="通过 Id 匹配已有元素，属性级 Merge，零冗余。"
               FontSize="14" Foreground="#666"/>
</Panel>

<!-- 删除 footer -->
<Remove TargetId="footer"/>
```

---

## 与 V2 规范的兼容性

- 所有 V2 标签（Page、Panel、Rect、TextElement、Image、Span、Fill、Stroke、Shadow、LinearGradient、Stop）在流式输出中**完全可用**
- 所有 V2 属性（Layout、Gap、Padding、CornerRadius、Shadow、IsBold、IsItalic 等）在流式输出中**完全可用**
- 引擎回填属性（`RenderSize`、`RenderLocation`、`ActualLineCount`）在流式输出中同样适用，渲染后回填到最终合并的 XML 中
- 渲染反馈（Warning、Error、截图）与 V2 保持一致
- **唯一差异**：`Id` 从可选变为必填，且要求全局唯一
- **新增**：`StyleId` 属性用于标记样式模板源，悬空元素必须声明 `StyleId`；`StyleFrom` 引用 `StyleId` 而非 `Id`
