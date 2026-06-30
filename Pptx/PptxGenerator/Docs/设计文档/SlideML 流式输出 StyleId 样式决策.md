# SlideML 流式输出 — StyleId 样式资源决策记录

本文档记录 SlideML 流式输出中关于“悬挂元素是否允许作为样式资源”以及“如何避免 LLM 将样式资源误认为页面内容”的决策思考。

该文档是设计决策记录，不直接替代正式规范。后续如果要落地到规范文档中，可以将本文的“最终决策”和“推荐规则”整理进《SlideML 流式输出规范》。

---

## 背景

在 SlideML 流式输出中，解析器允许模型连续输出 XML 片段。片段可以通过 `Id` 匹配已有元素，并进行属性 Merge、子元素 Merge 或删除操作。

在这个模型下，存在一种“悬挂元素”的使用方式：某个元素不是 `Page` 树中的真实页面元素，而是作为可复用样式来源存在。

例如，模型可能希望定义一组卡片样式，然后在多个真实页面元素中复用：

```xml
<Panel Background="#FFFFFF"
       CornerRadius="12"
       Shadow="0 4 12 #00000033"/>
```

这种元素本身并不应该出现在页面中，它只是提供样式属性。

问题在于：LLM 很容易忘记自己输出的这个元素并不在 `Page` 树里面。它可能误以为这个元素已经成为页面内容，或者误以为只要前面输出过这个元素，后续渲染时它就一定会被保留和使用。

最终会导致两个风险：

1. **内容丢失**：模型以为某个元素已经加入页面，但实际上它只是悬挂元素，没有进入 `Page` 树。
2. **样式关系不明确**：模型输出了样式来源，但真实页面元素没有明确引用它，导致样式不会生效。

因此需要给“悬挂元素作为样式资源”增加明确标识，让模型、解析器和后续文档都能清晰地区分：

- 哪些元素是页面内容
- 哪些元素是样式资源
- 页面内容如何引用样式资源

---

## 讨论过的方案

### 方案一：引入类似 XAML ResourceKey / ResourceId 的属性

一种思路是为样式资源引入明确的资源键，例如：

```xml
<Panel ResourceKey="card-panel"
       Background="#FFFFFF"
       CornerRadius="12"/>
```

真实页面元素再通过某个属性引用它。

该方案的优点是资源语义明确，但缺点是会引入新的资源概念，且需要额外解释 `ResourceKey`、`ResourceId` 与普通 `Id` 的关系。

对于 SlideML 面向 LLM 流式生成的场景来说，概念越多，模型越容易混淆。

### 方案二：引入 `Page.Styles` 集合

另一种方案是学习 XAML，在 `Page` 下增加一个样式集合：

```xml
<Page>
  <Styles>
    ...
  </Styles>

  ...
</Page>
```

该方案的优点是所有最终有效资源都归属于 `Page`，不再依赖悬挂元素。

但是本次最终不选择该方案，原因是：

1. 会增加新的 `Styles` 容器结构。
2. 会让流式片段多一层资源管理心智。
3. 当前目标是保持 SlideML 结构尽量简单，不额外引入 `Styles` 集合。

### 方案三：引入 `<Style>` 标签

也曾讨论过类似如下写法：

```xml
<Style Id="card-style">
  ...
</Style>
```

但该方案不符合当前决策方向。

当前不希望新增 `<Style>` 标签，也不希望引入 `Setter`、`TargetType` 之类的新样式语法。样式资源仍然应该复用已有元素本身的属性系统，例如 `Panel` 的样式就用 `Panel` 元素来描述，`TextElement` 的样式就用 `TextElement` 元素来描述。

因此，**不采用 `<Style>` 标签写法**。

---

## 最终决策

采用 **`StyleId` 标记悬挂样式元素** 的方案。

不引入：

- `<Style>` 标签
- `<Styles>` 集合
- `ResourceKey` / `ResourceId` 等额外资源键概念

而是在现有元素上使用 `StyleId` 表达样式身份或样式引用。

### 核心规则

1. **悬挂元素如果用于样式资源，必须声明 `StyleId`。**
2. **真实页面元素仍然使用 `Id` 表示页面身份。**
3. **页面元素可以通过 `StyleId` 引用对应样式资源。**
4. **`Id` 和 `StyleId` 的职责不同：**
   - `Id`：页面元素身份，用于流式 Merge、删除、定位等。
   - `StyleId`：样式身份或样式引用，用于复用属性。
5. **不允许使用 `<Style>` 标签来定义样式。**
6. **不引入 `Page.Styles`。**

---

## 推荐写法

### 定义样式资源

样式资源使用现有元素类型表达，并通过 `StyleId` 声明样式身份。

例如卡片面板样式：

```xml
<Panel StyleId="card-panel"
       Background="#FFFFFF"
       CornerRadius="12"
       Shadow="0 4 12 #00000033"/>
```

例如卡片标题样式：

```xml
<TextElement StyleId="card-title"
             FontSize="20"
             IsBold="True"
             Foreground="#333333"/>
```

这两个元素不是页面内容。它们不在 `Page` 树中，也不应该被渲染为页面上的实际元素。它们的作用是提供可复用属性。

### 引用样式资源

真实页面元素通过 `StyleId` 引用样式资源，同时用 `Id` 表示自己的页面身份：

```xml
<Page Background="#F5F5F5">
  <Panel Id="card1"
         StyleId="card-panel"
         X="80"
         Y="120"
         Width="340"
         Height="160">
    <TextElement Id="card1-title"
                 StyleId="card-title"
                 Text="流式输出"
                 X="24"
                 Y="20"
                 Width="292"/>
  </Panel>
</Page>
```

这里：

- `card-panel` 是样式资源的 `StyleId`
- `card-title` 是样式资源的 `StyleId`
- `card1` 是真实页面元素的 `Id`
- `card1-title` 是真实页面元素的 `Id`

---

## 为什么不使用 `<Style>` 写法

需要特别说明：当前决策并不允许如下写法：

```xml
<Style Id="card-style">
  ...
</Style>
```

原因如下：

1. SlideML 当前不希望为样式系统新增独立标签体系。
2. 如果引入 `<Style>`，还需要继续设计样式内部如何表达属性，例如是否需要 `Setter`。
3. 现有元素已经拥有完整属性集合，直接使用现有元素作为样式资源更简单。
4. 对 LLM 来说，`<Panel StyleId="card-panel" .../>` 比 `<Style TargetType="Panel">...</Style>` 更贴近已有 SlideML 元素结构。

因此样式定义应写成：

```xml
<Panel StyleId="card-panel"
       Background="#FFFFFF"
       CornerRadius="12"/>
```

而不是：

```xml
<Style Id="card-panel" TargetType="Panel">
  ...
</Style>
```

---

## 决策原因

### 1. `StyleId` 能明确告诉 LLM：这是样式资源，不是页面内容

原来的风险是模型输出一个悬挂元素后，可能误以为这个元素已经进入了 `Page` 树。

例如，如果模型输出：

```xml
<Panel Id="card-style"
       Background="#FFFFFF"
       CornerRadius="12"/>
```

它容易把这个元素理解成“一个已经存在的页面元素”。但实际上，如果这个元素不在 `Page` 树中，它不会作为页面内容渲染。

改成：

```xml
<Panel StyleId="card-panel"
       Background="#FFFFFF"
       CornerRadius="12"/>
```

语义就更清楚：

> 这是一个样式资源，不是一个页面元素。

因为它没有 `Id`，也不在 `Page` 树里，所以不会被当作页面内容。

### 2. `Id` 和 `StyleId` 分工明确

`Id` 用于真实页面元素：

```xml
<Panel Id="card1"/>
```

`StyleId` 用于样式资源或样式引用：

```xml
<Panel StyleId="card-panel"/>
<Panel Id="card1" StyleId="card-panel"/>
```

这让解析器和 LLM 都能区分两个概念：

| 属性 | 作用 |
|------|------|
| `Id` | 页面元素身份，用于 Merge、Remove、定位 |
| `StyleId` | 样式身份或样式引用，用于复用属性 |

### 3. 不引入 `Styles` 集合，保持结构简单

`Page.Styles` 的方式更严格，但会增加一个新的资源容器。

当前更希望 SlideML 流式输出保持轻量：

```xml
<Panel StyleId="card-panel" .../>
<Page>...</Page>
```

而不是：

```xml
<Page>
  <Styles>
    ...
  </Styles>
  ...
</Page>
```

对于 LLM 来说，少一层结构就少一类忘记闭合、放错位置、合并错误的风险。

### 4. 继续复用现有元素属性系统

样式资源本质上是一组属性。既然 `Panel`、`Rect`、`TextElement` 等元素已经定义了属性，就可以直接复用这些元素作为样式属性载体。

例如：

```xml
<TextElement StyleId="title-text"
             FontSize="36"
             IsBold="True"
             Foreground="#1A1A2E"/>
```

这比新建一套样式语法更直接。

### 5. 适合流式输出

流式场景下，样式可以先输出，也可以后输出。

先输出样式：

```xml
<Panel StyleId="card-panel"
       Background="#FFFFFF"
       CornerRadius="12"/>
```

后续再输出页面元素：

```xml
<Panel Id="card1"
       StyleId="card-panel"
       X="80"
       Y="120"
       Width="340"
       Height="160"/>
```

也可以先输出页面元素，再补充样式资源。最终渲染前，解析器可以统一解析 `StyleId` 关系。

---

## 建议解析器规则

### 1. 悬挂样式资源

如果一个顶层片段不是 `Page`，且元素：

- 有 `StyleId`
- 没有 `Id`

则可以视为样式资源定义。

例如：

```xml
<Panel StyleId="card-panel"
       Background="#FFFFFF"
       CornerRadius="12"/>
```

### 2. Page 树中的真实元素

`Page` 树中的真实元素应当使用 `Id` 表示页面身份。

例如：

```xml
<Panel Id="card1" StyleId="card-panel"/>
```

其中 `StyleId="card-panel"` 表示引用样式资源。

### 3. 悬挂普通元素应当报错或警告

如果一个顶层片段不是 `Page`，且元素：

- 有 `Id`
- 没有 `StyleId`
- 又不能匹配到已有页面元素

则它很可能是 LLM 误以为会加入页面的内容。

建议解析器给出 Warning 或 Error，提示该元素没有进入 `Page` 树，不会成为页面内容。

例如：

```xml
<Rect Id="decoration" Fill="#FF0000"/>
```

如果它既不在 `Page` 树中，也不是对已有 `Id` 的更新，就不应静默保留为页面内容。

### 4. 页面元素引用不存在的样式

如果页面元素声明了 `StyleId`，但找不到对应样式资源，建议给出 Warning 或 Error。

例如：

```xml
<Panel Id="card1" StyleId="missing-card-style"/>
```

建议提示：

```text
[Warning] StyleId "missing-card-style" not found.
```

### 5. 样式资源类型应与引用元素类型一致

建议同类型引用。

例如：

```xml
<Panel StyleId="card-panel" Background="#FFFFFF"/>
<Panel Id="card1" StyleId="card-panel"/>
```

这是合理的。

但如果：

```xml
<TextElement StyleId="card-panel" Text="标题"/>
```

引用到了一个 `Panel` 样式，则应给出 Warning 或 Error。

### 6. 本地属性优先级高于样式属性

建议规定属性优先级为：

```text
元素本地属性 > StyleId 引用的样式属性 > 默认值
```

例如：

```xml
<TextElement StyleId="title-style"
             FontSize="36"
             Foreground="#333333"/>

<TextElement Id="title"
             StyleId="title-style"
             Text="标题"
             FontSize="42"/>
```

最终 `title` 的 `FontSize` 应为 `42`，因为页面元素自身显式声明的属性优先于样式属性。

### 7. 重复 StyleId 的处理

如果多个样式资源使用相同 `StyleId`，可以有两种处理方式：

1. 按流式属性 Merge 规则合并。
2. 视为重复定义并报错。

从流式输出的连续修正能力考虑，建议倾向于 **按 `StyleId` 合并**：

```xml
<Panel StyleId="card-panel" Background="#FFFFFF"/>
<Panel StyleId="card-panel" CornerRadius="12"/>
```

最终等价于：

```xml
<Panel StyleId="card-panel"
       Background="#FFFFFF"
       CornerRadius="12"/>
```

但如果同一个流片段内出现两个相同 `StyleId`，建议仍可视为错误，以避免片段内部歧义。

---

## 推荐规范表述

后续可以将以下内容整理进正式规范：

> 为了避免 LLM 将悬挂元素误认为 `Page` 树内容，所有作为样式资源存在的悬挂元素必须显式声明 `StyleId`。
>
> 悬挂样式元素不使用 `Id` 表示页面身份；`Id` 只用于 `Page` 树中的真实元素。
>
> `Page` 树中的元素可以通过 `StyleId` 引用对应样式资源。
>
> 不引入 `<Style>` 标签，也不引入 `<Styles>` 集合。

示例：

```xml
<!-- 样式资源：不在 Page 树中 -->
<Panel StyleId="card-panel"
       Background="#FFFFFF"
       CornerRadius="12"
       Shadow="0 4 12 #00000033"/>

<TextElement StyleId="card-title"
             FontSize="20"
             IsBold="True"
             Foreground="#333333"/>

<!-- 页面内容 -->
<Page Background="#F5F5F5">
  <Panel Id="card1"
         StyleId="card-panel"
         X="80"
         Y="120"
         Width="340"
         Height="160">
    <TextElement Id="card1-title"
                 StyleId="card-title"
                 Text="流式输出"
                 X="24"
                 Y="20"
                 Width="292"/>
  </Panel>
</Page>
```

---

## 最终结论

本次决策采用 **`StyleId` 标记悬挂样式元素** 的方案。

该方案的核心价值是：

1. 保留悬挂元素作为样式资源的能力。
2. 通过 `StyleId` 明确区分样式资源和页面内容。
3. 不新增 `<Style>` 标签。
4. 不新增 `<Styles>` 集合。
5. 继续复用现有元素属性系统。
6. 降低 LLM 把样式资源误认为页面元素的概率。
7. 让解析器可以明确校验样式是否被定义、是否被引用、类型是否匹配。

最终推荐心智模型是：

```text
Id      = 页面元素身份
StyleId = 样式身份或样式引用
```

页面内容必须进入 `Page` 树；悬挂元素只有在声明 `StyleId` 时，才被认为是样式资源。
