# ICharObject 扩展类型命名决策文档

## 背景

文本库 LightTextEditorPlus 的文档树中，所有字符都通过 `ICharObject` 接口表示。现有实现包括 `RuneCharObject`（Unicode 符文字符）、`SingleCharObject`（单 char 字符）、`LineBreakCharObject`（换行符）、`TextSpanCharObject`（字符串片段引用字符）。这些类型的共同特征是它们都表示文本字符，都拥有 Unicode 代码点，都能通过 `ToText()` 方法转换为文本。

现需要新增一种字符类型，用于在文本流中插入图片、图表、自定义控件等非文本元素，实现图文混排功能。该类型同样需要加入元素列表并参与布局测量（Core 层负责纯数值布局，UI 层负责实际渲染测量）。本文档记录该新类型的命名决策过程。

决策采用 `InlineElementCharObject` 命名

## 命名约束

新类型必须继承或实现 `ICharObject` 接口，命名应遵循项目现有的 `{Descriptor}CharObject` 模式。同时，新类型表示的不是文本字符，而是嵌入文本流中的非文本元素，这一语义差异需要在命名或接口设计层面有所体现。

## 候选命名方案

以下列出讨论过程中出现的所有命名方案及其分析。

### InlineObjectCharObject

`InlineObject` 是排版和文档领域的标准术语，Word 的 OpenXML 规范中使用 `InlineShape`，WPF 和 Avalonia 框架中使用 `InlineUIContainer`。该命名精确描述了元素的特征：`Inline` 表示在行内参与文本流布局，`Object` 表示它是非文本的对象。

不采用原因：`Object` 与后缀 `CharObject` 中的 `Object` 重复，构成 `XxxObjectCharObject`，显得有些冗余。

### EmbeddedCharObject

`Embedded` 意为「嵌入的」，天然隐含了「非原生文本内容」的语义，与 `RuneCharObject` 等原生文本字符形成直观对比。该命名简洁（仅 18 个字符），且派生类命名自然，例如 `ImageEmbeddedCharObject`。

不采用原因：与各主流框架的术语对齐度不如 `Inline` 方向。WPF、Avalonia、Word 均使用 `Inline` 描述行内非文本元素，`Embedded` 更接近 OLE 嵌入的概念，语义稍偏。

### AtomicCharObject

`Atomic` 来自 CSS 排版中「atomic inline」的概念，指在行内布局中被视为不可分割的原子单元。该术语在排版引擎领域精准。

不采用原因：术语门槛较高，不熟悉 CSS 排版术语的开发者难以直观理解。且 `Atomic` 强调的是不可分割性，而非「非文本」的身份。

### ForeignCharObject

类比 SVG 的 `foreignObject` 元素，强调这是文本流中的「外来者」。

不采用原因：概念过于隐晦，需要事先了解 SVG 规范才能理解其意图，不利于团队协作和新人上手。

### CustomInlineCharObject

`Custom` 强调了使用方可自定义内容。

不采用原因：`Custom` 暗示只有一种实现方式，且前缀较长。类型命名的重点应放在元素本质特征上，而非「可自定义」这一属性。

### ExtensionCharObject

项目最初候选 `ExtensionalCharObject` 作为该类型的名称。`Extension` 表达了该类型是对 `ICharObject` 的扩展突破。经过评审，该命名存在以下问题：

第一，命名模式不一致。现有的 `RuneCharObject`、`LineBreakCharObject`、`TextSpanCharObject` 均以前缀描述自身是什么，例如 `Rune` 表示用符文表示、`LineBreak` 表示换行。而 `Extensional` 描述的是来源（通过扩展而来），而非自身的性质或位置，不符合项目的命名惯例。

第二，语义偏业务化。`Extension` 一词偏向表达「在文本外部扩展字符」的业务概念，而非文本库底层应有的通用抽象。文本库作为基础库，其类型命名应当描述元素在文档树中的本质特征（位置、行为、身份），而非描述使用场景。

第三，与字符集扩展概念混淆。在字符编码领域，存在扩展字符集（Extended Character Set）的概念，例如 ASCII 扩展字符。使用 `Extensional` 容易引发歧义，让阅读者误以为该类型与字符集扩展相关，而实际含义是在文本行内嵌入非文本对象。

且在 .NET 生态中 `Extension` 与扩展方法强关联，容易产生歧义。且与前述 `ExtensionalCharObject` 存在相似的「描述来源而非本质」的问题。

## 最终选择：InlineElementCharObject

经过综合评审，最终选定 **`InlineElementCharObject`** 作为新类型的名称。

选择理由如下：

第一，术语对齐主流框架。WPF 和 Avalonia 的 `InlineUIContainer`、Word 的 `InlineShape` 均使用 `Inline` 描述行内非文本元素。使用 `Inline` 前缀使熟悉上述框架的开发者能够快速理解该类型的定位。

第二，避免 Object 重复。相对于 `InlineObjectCharObject`，使用 `Element` 替换 `Object`，既保持了 `Inline` 方向的优势，又避免了 `Object` 一词在类型名中出现两次的冗余感。

第三，符合项目命名惯例。`InlineElement` 作为前缀描述该类型是什么（行内元素），与 `Rune`、`LineBreak`、`TextSpan` 等前缀的模式一致，命名读起来自然连贯。

第四，派生扩展性好。未来若需要细分不同类型的行内元素，可以自然地派生出 `ImageInlineElementCharObject`、`ChartInlineElementCharObject`、`FormulaInlineElementCharObject` 等子类型，命名均保持一致风格。

## 接口设计

为在接口层面明确该类型与普通文本字符的差异，引入 `IInlineElementCharObject : ICharObject` 接口。该接口在 XML 文档注释中明确说明：

> 此接口代表文档树中非文本的内联元素（如图片、图表、自定义控件等）。与 RuneCharObject 等文本字符类型不同，它不表示 Unicode 字符，而是在文本流中作为一个不透明布局单元参与混排。其 CodePoint 和 ToText() 方法仅返回占位值。

这样设计的好处是：Core 层的布局引擎可以通过 `is IInlineElementCharObject` 统一判断「非文本字符」，对它们进行特殊的布局测量逻辑；UI 层则根据具体的 `ICharObject` 实现类型来决定渲染方式。