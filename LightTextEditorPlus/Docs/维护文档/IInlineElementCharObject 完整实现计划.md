# IInlineElementCharObject 完整实现计划

## 背景与目标

为 LightTextEditorPlus 引入图文混排、公式混排能力。核心是在 `ICharObject` 基础上新增 `IInlineElementCharObject` 接口，并在 Core 布局引擎、文档系统、渲染管线全链路加入对它的支持。

---

## 任务进度

| 步骤 | 内容 | 状态 |
|---|---|---|
| 步骤 1 | `TextContext` 添加 `ObjectReplacementChar` | ✅ 已完成 |
| 步骤 2 | 确定 `IInlineElementCharObject` 接口 | ⬜ 待开始 |
| 步骤 3 | `CharData` 添加 `IsInlineElementCharData` | ⬜ 待开始 |
| 步骤 4 | Core 布局层处理 inline 元素分行 | ⬜ 待开始 |
| 步骤 5 | `ICharInfoMeasurer` 平台适配 | ⬜ 待开始 |
| 步骤 6 | 渲染管线适配 | ⬜ 待开始 |
| 步骤 7 | 光标系统 | ⬜ 待开始 |
| 步骤 8 | 测试 | ⬜ 待开始 |

> **状态说明**：✅ 已完成 / 🔄 进行中 / ⬜ 待开始 / ⏸️ 暂缓

---

## 1. 架构分析总结

### 1.1 现有字符体系

- `ICharObject` 是所有文档字符的基接口（`ToText()` + `CodePoint`），继承 `IEquatable<string>` + `IDeepCloneable<ICharObject>`
- 现有实现：`RuneCharObject`、`SingleCharObject`、`LineBreakCharObject`（单例）、`TextSpanCharObject`，都是不可变对象
- `CharData` 包装 `ICharObject` + `IReadOnlyRunProperty`，承载 `CharDataInfo`（尺寸/基线）和 `CharLayoutData`（布局位置）
- `LineBreakCharObject` 通过 `CharData.IsLineBreakCharData` 属性做类型判断（`ReferenceEquals` 检查）

### 1.2 现有布局管线（三条路径，需全部接入）

- **水平排版**：`HorizontalArrangingLayoutProvider`
  - 关键方法：`UpdateSingleCharInLineLayout`（字符分行/宽度测试）、`UpdateTextLineStartPoint`（行内坐标设定）、`UpdateWholeLineLayout`（行排版）
- **垂直排版**：`VerticalArrangingLayoutProvider`，与水平排版对称
- **平台注入路径**：`ISingleRunInLineLayouter.MeasureSingleRunLayout`、`IWholeLineCharsLayouter`、`IWholeLineLayouter`——平台层可能完全替换默认布局逻辑，因此 inline 元素处理不能仅放在默认实现中

### 1.3 现有渲染管线（需适配）

现有渲染架构如下：

```
TextEditor (FrameworkElement)
  └── TextView (UIElement) — 唯一的 VisualChild
        ├── DrawingVisual (文本渲染，通过 DrawingContext 绘制)
        └── SelectionAndCaretLayer (选择/光标叠加层)
```

**Skia 渲染**：`BaseSkiaTextRenderer.RenderTextLine` 逐 Run 遍历 `CharList`，调用 `RenderCharList` 绘制——当前假设所有 `CharData` 都是文本字符。

**WPF 渲染**：`HorizontalTextRenderer.Render` 逐段落实体化 `DrawingVisual`，通过 `DrawingContext` 绘制文本，同样假设所有 `CharData` 是文本字符。

**渲染缓存**：`LineDrawnResult` → `LineDrawingArgument.LineAssociatedRenderData`

#### 核心设计决策：文本渲染器镂空，UI 框架层负责实际渲染

**不**在现有文本渲染逻辑（`RenderCharList`、`HorizontalTextRenderer`）里尝试绘制内联元素。理由是：

1. 文本渲染器无法绘制图片/公式——它们不是字形（glyph），`SKFont` / `GlyphTypeface` 无能为力
2. 渲染器可能根本没有足够的信息来渲染（公式需要子排版引擎）

正确做法是经典的 "embedded foreign objects in text layout" 模式：

```
┌──────────────────────────────────────────────────────┐
│ Core 布局层                                           │
│ • 排版时识别 IInlineElementCharObject，占位镂空        │
│ • 产出内联元素最终坐标 (CharData.GetStartPoint + Size) │
│ • 文本渲染器跳过 IsInlineElementCharData（镂空不绘制） │
└──────────────────────────────────────────────────────┘
                          │
          ┌───────────────┼───────────────┐
          ▼               ▼               ▼
┌──────────────┐  ┌──────────────┐  ┌───────────────────┐
│ WPF 平台      │  │ Avalonia     │  │ 纯 Skia            │
│              │  │ (有 UI 框架) │  │ (无 UI 框架)       │
│ 在视觉树中    │  │              │  │                   │
│ 添加真正的    │  │ 走 IInline   │  │ 若实现 ISkiaText  │
│ UIElement     │  │ ElementHost  │  │ InlineElement     │
│ 子元素        │  │ 管理控件树   │  │ CharObject → 回调 │
│ Arrange       │  │              │  │ 绘制              │
│ Override 定位 │  │              │  │ 否则：镂空跳过     │
└──────────────┘  └──────────────┘  └───────────────────┘
```

**WPF 平台**：将 `TextEditor` 视为"带特殊布局逻辑的 Panel"，内联元素对应的 `UIElement` 直接加入 `TextEditor` 的视觉树，在 `ArrangeOverride` 中根据 `CharData` 坐标定位。这直接对标 WPF 原生 `InlineUIContainer` 的设计——`InlineUIContainer` 正是将任意 `UIElement` 嵌入到 `TextBlock`/`FlowDocument` 的流内容中的机制。

**Avalonia 平台（有 UI 框架）**：同 WPF，走 `IInlineElementHost` 管理控件树。不使用 Skia 层的 `ISkiaTextInlineElementCharObject` 接口——Avalonia 的输入系统（命中测试、焦点）和控件体系需要真正的 Avalonia 控件，在 Skia 画布上绘制无法满足交互需求。

**纯 Skia（无 UI 框架）**：仅求不崩溃。若内联元素额外实现了 `ISkiaTextInlineElementCharObject`（定义在 Skia 项目中），Skia 文本渲染器通过此接口回调 `Render(SKCanvas, TextRect)` 完成绘制；若未实现，则镂空跳过（调试模式下可绘制占位矩形）。此接口专为纯 Skia 路径设计，UI 框架不应依赖。

**Z 序**：内联元素应在文本 `DrawingVisual` 之上、`SelectionAndCaretLayer` 之下。

### 1.4 文档与光标系统

- 文本插入最终走 `DocumentManager.EditAndReplaceRun`，创建 `CharData(ICharObject, IReadOnlyRunProperty)`
- 光标系统通过 `CharData` 定位，内联元素应作为一个原子光标位

---

## 2. 核心设计决策

### 2.1 尺寸存储：`CharData.CharDataInfo` 是唯一权威

| 层 | 谁存放 | 角色 |
|---|---|---|
| `IInlineElementCharObject` | 可声明**只读自然尺寸**（如 `NaturalSize`） | 给 `ICharInfoMeasurer` 的**输入参照**，不做 final |
| `CharData.CharDataInfo` | 测量/协商后的**最终尺寸** | 通过 `ICharDataLayoutInfoSetter.SetCharDataInfo` 写入，布局全线只读这个 |
| Core 布局层 | 只读 `CharData.Size` / `CharData.Baseline` | 完全不管 `CharObject` 的类型，和普通文本字符走的路径一样 |

**设计理由**：

- `ICharObject` 的现有实现全都是不可变的，保持这一约定
- 布局链路中已有大量的 `charData.Size` 调用（`UpdateSingleCharInLineLayout`、`UpdateTextLineStartPoint`、`HorizontalUnion`），Core 层只关心 `CharData`
- 同一 inline 元素的尺寸可能因不同 `RunProperty`（如字号）而变化，存放在 `ICharObject` 层面语义不正确

### 2.2 约束尺寸来源

**重要纠正**：`DocumentLayoutBounds` 是 `UpdateLayout()` 末尾赋值的输出属性，不是测量时的输入。

正确的约束链路：

```
UpdateLayout() 开始前
  → UI 层已设置 DocumentManager.DocumentWidth（输入）

UpdateLayout() 执行中
  → GetLineMaxWidth()
      查看 HorizontalArrangingLayoutProvider.GetLineMaxWidth() 的实现：
        TextSizeToContent.Manual  → TextEditor.DocumentManager.DocumentWidth
        TextSizeToContent.Height  → TextEditor.DocumentManager.DocumentWidth
        TextSizeToContent.Width   → double.PositiveInfinity
        TextSizeToContent.WidthAndHeight → double.PositiveInfinity
  → EnsureMeasureAndFillSizeOfCharDataList(charDataList)
      → ICharInfoMeasurer.FillCharDataInfoList(argument)
          → 对 IInlineElementCharObject：
              width constraint  = GetLineMaxWidth() 的结果
              height constraint = +∞（内联元素高度不限制）
          → 写入 CharDataInfo
```

**时序**：`MeasureOverride` → 设 `DocumentWidth`（输入） → `UpdateLayout()` → `GetLineMaxWidth()`（读输入） → `FillCharDataInfoList`（用输入算约束） → 布局完成 → `DocumentLayoutBounds`（输出）。

### 2.3 UI 框架测量时机策略

```
能赶上 UI Measure → UI 层预先填入 CharDataInfo → 布局走缓存分支（IsInvalidCharDataInfo == false 跳过）
赶不上          → CharDataInfo Invalid → ICharInfoMeasurer 填默认尺寸（如行高方框）
                  → 布局先跑通 → 打脏标记
                  → 下次 UI Measure 获得真实尺寸后重新填入 → 触发重新布局
```

关键代码已有的缓存命中（`ArrangingLayoutProvider.EnsureMeasureAndFillSizeOfCharDataList` 第 723-730 行）：

```csharp
for (var i = 0; i < toMeasureCharDataList.Count; i++)
{
    var charData = toMeasureCharDataList[i];
    if (charData.IsInvalidCharDataInfo)
    {
        // 没有尺寸才需要测量
        measure(charData);
    }
    // 有尺寸则直接使用
}
```

---

## 3. 分步实施计划

### 步骤 1：`TextContext` 添加 `ObjectReplacementChar`

**文件**：`LightTextEditorPlus.Core\Utils\TextContext.cs`

**内容**：

```csharp
// 已有的
public const char UnknownChar = '\uFFFD';

// 新增
/// <summary>
/// Object Replacement Character (U+FFFC)，用于表示内联对象（图片、公式等）的占位字符。
/// 区别于 <see cref="UnknownChar"/>（\uFFFD）——后者表示"无法识别的字符"，语义不同。
/// </summary>
public const char ObjectReplacementChar = '\uFFFC';
```

**验证**：确保 `\uFFFC` 不会被现有逻辑（如文本扫描、分词器、回退测量）误处理。

### 步骤 2：确定 `IInlineElementCharObject` 接口

**文件**：`LightTextEditorPlus.Core\Document\Context_\Char_\IInlineElementCharObject.cs`

**内容**：

```csharp
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Utils;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 内联元素字符，用于表示图片、公式等非文本内联对象。
/// </summary>
/// 参阅 `ICharObject 扩展类型命名决策文档.md` 文档
public interface IInlineElementCharObject : ICharObject
{
    Utf32CodePoint ICharObject.CodePoint => new(TextContext.ObjectReplacementChar);

    string ICharObject.ToText() => new(TextContext.ObjectReplacementChar, 1);

    /// <summary>
    /// 自然尺寸（如来自图片文件的像素尺寸），只读参照。
    /// 返回 null 表示需要平台 UI 层参与测量才能确定尺寸（如公式渲染）。
    /// </summary>
    TextSize? NaturalSize { get; }

    /// <summary>
    /// 是否允许跨行。图片返回 false，行内公式可能返回 true。
    /// </summary>
    bool CanBreakAcrossLines { get; }

    /// <summary>
    /// 基线所在的比例（0~1）。例如高度为 10，基线比例为 0.7，则按 7:3 划分。
    /// 0 表示顶部对齐，1 表示底部对齐，0.5 表示居中对齐。
    /// </summary>
    double BaseLineRatio { get; }
}
```

**设计说明**：

- `CodePoint` 和 `ToText()` 使用 default interface method 实现，返回 `\uFFFC`
- `Measure()` / `Layout()` 方法移除——测量职责交给 `ICharInfoMeasurer`，布局计算由现有 `HorizontalArrangingLayoutProvider` 完成
- `BaseLineRatio` 保留，在 `UpdateTextLineStartPoint` 中用于计算 Y 偏移

### 步骤 3：`CharData` 添加 `IsInlineElementCharData`

**文件**：`LightTextEditorPlus.Core\Document\DocumentManagers_\ParagraphManagers_\Paragraphs_\Chars_\CharData.cs`

**内容**：

```csharp
/// <summary>
/// 是否是一个内联元素字符（如图片、公式）
/// </summary>
public bool IsInlineElementCharData => CharObject is IInlineElementCharObject;
```

**位置**：放在 `IsLineBreakCharData` 属性旁边。

### 步骤 4：Core 布局层处理 inline 元素分行

**文件**：`LightTextEditorPlus.Core\Layout\HorizontalArrangingLayoutProvider.cs`

**关键方法**：`UpdateSingleCharInLineLayout`（第 880-965 行）

当前逻辑：
1. `wordDivider.DivideWord()` 分词
2. 测量 `takeCount` 字符的总宽度
3. 判断是否放入当前行 → 放不下时降级为逐字符放入

**Inline 元素干预点**：

1. `DivideWord` 调用前：检查 `currentCharData.IsInlineElementCharData`
   - 如果是：`takeCount = 1`（inline 元素作为一个不可拆分的原子）
   - 除非 `CanBreakAcrossLines = true`
2. 宽度测试：取 `charData.Size.Width`（已由 `FillCharDataInfoList` 预先填入）
3. 行宽不足时：
   - `CanBreakAcrossLines = false` 且不在行首 → 整行换行，元素挪到下一行
   - 在行首仍放不下 → 强行放入（降级，至少显示一个）

**同理需处理**：`VerticalArrangingLayoutProvider` 对称逻辑。

### 步骤 5：`ICharInfoMeasurer` 平台适配

**详细方案见**：[内联元素测量器 `IInlineElementMeasurer` 平台分离方案](./内联元素测量器_IInlineElementMeasurer_平台分离方案.md)

核心决策：内联元素测量不应放入 `ICharInfoMeasurer`（如 `SkiaCharInfoMeasurer`），而是新增独立接口 `IInlineElementMeasurer`，在 `ArrangingLayoutProvider.EnsureMeasureAndFillSizeOfCharDataList` 中于 `ICharInfoMeasurer` 调用前做分流。

概要：

- 新增 `IInlineElementMeasurer` 接口（Core 层），由 `IPlatformProvider.GetInlineElementMeasurer()` 获取
- `EnsureMeasureAndFillSizeOfCharDataList` 中 Step 1 处理 inline 元素，Step 2 走原有文本测量
- `SkiaCharInfoMeasurer` 仅需 3 行守卫（`IsInlineElementCharData` 跳过），不承载 inline 测量职责
- 各平台（WPF/Avalonia/纯 Skia）按自身策略实现 `IInlineElementMeasurer`，UI 框架层优先在 `MeasureOverride` 中预测量

### 步骤 6：渲染管线适配

#### 6.1 文本渲染器——镂空跳过

两个文本渲染路径均需跳过 `IsInlineElementCharData`，不尝试绘制。

**Skia 渲染**（`BaseSkiaTextRenderer.RenderTextLine` + `HorizontalSkiaTextRenderer.RenderCharList`）：

在 `RenderTextLine` 中遍历 `argument.CharList` 时，检查每个 `CharData.IsInlineElementCharData`：
- 如果 `CharObject` 实现了 `ISkiaTextInlineElementCharObject` → 回调 `Render(SKCanvas, TextRect)` 完成 Skia 自绘制
- 如果未实现 → 跳过，不调用 `RenderCharList`（镂空，不崩溃）
- 如果处于调试模式 → 可选绘制占位矩形（浅灰背景 + 边框），帮助开发者识别内联元素位置

> **设计理由**：纯 Skia（无 UI 框架）路径不追求完整实现 inline 元素渲染。`ISkiaTextInlineElementCharObject` 是可选接口，放在 `LightTextEditorPlus.Skia` 项目中。如果 inline 元素需要在此路径下正确渲染，实现该接口即可；否则镂空跳过，不崩溃。

**WPF 渲染**（`HorizontalTextRenderer` / `VerticalTextRenderer`）：

在 `GetCharSpanContinuous` 或等效的 Run 遍历逻辑中，遇到 `IsInlineElementCharData` 时跳过，不在 `DrawingVisual` 中绘制。

> **`GetCharSpanContinuous` 分组决策**：一个 inline 元素只占一个 `CharData`，`GetCharSpanContinuous` 按 RunProperty 连续性分组时，inline 元素的 RunProperty 与相邻文本字符不同，会被自然单独分组为一个 span，渲染器处理起来足够干净。唯一需要注意：若 inline 元素与相邻文本的 RunProperty 恰好相同，`GetCharSpanContinuous` 会将它们合并到一个 span。渲染器需在遍历 span 时额外检查其中是否混入 `IsInlineElementCharData`，若混入则拆分处理（inline 部分镂空/回调，文本部分正常渲染）。

#### 6.2 Core 层新增 `IInlineElementHost` 接口

**文件**：`LightTextEditorPlus.Core\Rendering\IInlineElementHost.cs`

```csharp
namespace LightTextEditorPlus.Core.Rendering;

/// <summary>
/// 内联元素宿主接口。由平台 UI 层实现，负责在视觉树中管理内联元素的 UI 控件。
/// </summary>
public interface IInlineElementHost
{
    /// <summary>
    /// 添加内联元素到视觉树。
    /// </summary>
    /// <param name="renderInfo">内联元素渲染信息（含坐标、尺寸、元素引用）</param>
    void AddInlineElement(InlineElementRenderInfo renderInfo);

    /// <summary>
    /// 从视觉树移除内联元素。
    /// </summary>
    /// <param name="element">要移除的内联元素</param>
    void RemoveInlineElement(IInlineElementCharObject element);

    /// <summary>
    /// 更新内联元素在视觉树中的位置和尺寸。
    /// </summary>
    void UpdateInlineElementPosition(InlineElementRenderInfo renderInfo);

    /// <summary>
    /// 清空所有内联元素。
    /// </summary>
    void ClearInlineElements();
}

/// <summary>
/// 内联元素渲染信息，由布局完成后产出，传递给 <see cref="IInlineElementHost"/>。
/// </summary>
public readonly struct InlineElementRenderInfo
{
    /// <summary>
    /// 关联的内联元素
    /// </summary>
    public IInlineElementCharObject Element { get; init; }

    /// <summary>
    /// 内联元素在文档中的布局位置（Canvas 坐标）
    /// </summary>
    public TextRect Bounds { get; init; }
}
```

#### 6.3 WPF 平台适配

**`TextEditor` 实现 `IInlineElementHost`**：

当前 `TextEditor` 的视觉树结构（`TextEditor` → `TextView` → `DrawingVisual` + `SelectionAndCaretLayer`）不变。内联元素直接作为 `TextEditor` 的额外 `VisualChild`，Z 序在 `TextView` 之上。

```csharp
// TextEditor 新增
partial class TextEditor : IInlineElementHost
{
    // 内联元素视图管理器（可抽取为 InlineElementLayer，类似 SelectionAndCaretLayer）
    private InlineElementLayer _inlineElementLayer;

    void IInlineElementHost.AddInlineElement(InlineElementRenderInfo renderInfo) { ... }
    void IInlineElementHost.RemoveInlineElement(IInlineElementCharObject element) { ... }
    void IInlineElementHost.UpdateInlineElementPosition(InlineElementRenderInfo renderInfo) { ... }
    void IInlineElementHost.ClearInlineElements() { ... }
}
```

**触发时机**：`LayoutCompleted` 事件后，获取 `RenderInfoProvider` 中所有内联元素的坐标，调用 `IInlineElementHost` 方法同步视觉树。

**`ArrangeOverride`**：`TextEditor.ArrangeOverride` 中，对内联元素子 `UIElement` 调用 `Arrange` 定位到正确坐标。

#### 6.4 `ISkiaTextInlineElementCharObject` 接口（Skia 项目，可选）

**文件**：`LightTextEditorPlus.Skia\Rendering\ISkiaTextInlineElementCharObject.cs`

```csharp
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;
using SkiaSharp;

namespace LightTextEditorPlus.Rendering;

/// <summary>
/// 让内联元素在纯 Skia 渲染路径下自绘制的可选接口。
/// 仅当 <see cref="IInlineElementCharObject"/> 同时实现此接口时，
/// Skia 文本渲染器才会回调此方法进行绘制。
/// UI 框架（WPF/Avalonia）不应依赖此接口。
/// </summary>
public interface ISkiaTextInlineElementCharObject
{
    /// <summary>
    /// 在指定区域绘制内联元素。
    /// </summary>
    /// <param name="canvas">Skia 画布</param>
    /// <param name="bounds">内联元素在文档中的布局区域</param>
    void Render(SKCanvas canvas, TextRect bounds);
}
```

**Skia 渲染器中的处理逻辑**（`BaseSkiaTextRenderer.RenderTextLine`）：

```csharp
// 在 GetCharSpanContinuous 遍历中
foreach (TextReadOnlyListSpan<CharData> charList in argument.CharList.GetCharSpanContinuous())
{
    if (charList[0].IsInlineElementCharData)
    {
        RenderInlineElementIfSupported(in charList, in lineRenderInfo);
        continue;
    }

    RenderCharList(charList, lineRenderInfo);
    // ...
}

private void RenderInlineElementIfSupported(
    in TextReadOnlyListSpan<CharData> charList,
    in ParagraphLineRenderInfo lineInfo)
{
    var charData = charList[0];
    if (charData.CharObject is ISkiaTextInlineElementCharObject skiaElement)
    {
        var bounds = charData.GetBounds();
        skiaElement.Render(Canvas, bounds);
    }
    // 否则：什么都不做（镂空），不崩溃
}
```

> **设计原则**：纯 Skia（无 UI 框架）路径下不追求完整实现。`ISkiaTextInlineElementCharObject` 是 Skia 项目中的可选接口，inline 元素有需要时可实现它；不实现则镂空跳过、不崩溃。

> **接口位置决策**：`ISkiaTextInlineElementCharObject` 放在 `LightTextEditorPlus.Skia` 项目中，不创建额外的 Skia 抽象项目。理由：
> - 该接口依赖 `SKCanvas`（SkiaSharp），只有 Skia 项目引用了 SkiaSharp，Core 项目无法定义含 `SKCanvas` 参数的接口
> - 当前 Skia 项目已经是 Core 之上的薄层，再拆一层会过度设计
> - Avalonia 虽然引用 Skia 项目能"看到"此接口，但 Avalonia 走 `IInlineElementHost` 路径，不使用此接口，没有冲突

#### 6.5 Avalonia 平台适配

**Avalonia 走 `IInlineElementHost` 路径**（同 WPF），不用 `ISkiaTextInlineElementCharObject`。理由：

- Avalonia 的输入系统（命中测试、焦点管理）依赖控件树中的真实控件
- 在 Skia 画布上直接绘制内联元素会导致鼠标点击、无障碍（UIA）等交互无法正常工作
- `IInlineElementHost` 在 Avalonia 控件树中管理内联元素对应的 `Control`，由其自有渲染管线绘制

**Avalonia 中的 `SkiaTextEditor`** 作为渲染底层引擎时，其 `BaseSkiaTextRenderer` 镂空跳过 inline 元素（同 6.1），实际渲染由 Avalonia 控件层完成。

> **Avalonia 实现方式决策**：走 `IInlineElementHost` + `ArrangeOverride` 定位，与 WPF 方案一致。理由：
> - Avalonia 没有 `InlineUIContainer` 等价物——Avalonia 的 `TextBlock`/`SelectableTextBlock` 不支持嵌入任意控件
> - Avalonia 的输入系统（命中测试、焦点、无障碍 UIA）依赖控件树中的真实 `Control`，不能只在 Skia 画布上绘制
> - 内联元素对应的 `Control` 作为 `TextEditor` 的逻辑/视觉子元素加入控件树，在 `ArrangeOverride` 中根据 `CharData` 坐标调用 `Arrange` 定位
> - 需在 `MeasureOverride` 中测量内联元素子控件，Z 序通过 Avalonia 的 ZIndex 或控件顺序控制（文本层之上、选择/光标层之下）

#### 6.5 布局完成后的同步流程

```
UpdateLayout() 完成
  → LayoutCompleted 事件触发
  → TextEditor 获取 RenderInfoProvider
  → 遍历所有 ParagraphLineRenderInfo
      → 查找其中 IsInlineElementCharData 的 CharData
      → 生成 InlineElementRenderInfo（坐标 = CharData.GetStartPoint()，尺寸 = CharData.Size）
      → IInlineElementHost.AddInlineElement / UpdateInlineElementPosition
  → InvalidateArrange() 确保 UI 层 Arrange 内联元素
```

### 步骤 7：光标系统

#### 7.1 默认行为：原子光标位

**未实现 `IInlineElementCursorInteractable` 的内联元素**（如图片）使用此默认行为：

- 内联元素 = **一个原子的光标位置**（`CaretOffset` 前后各 1 位）
- 方向键移动：`←` 到元素前，`→` 到元素后，不进入元素内部
- `Delete`/`Backspace`：删除整个 inline 元素（1 个字符原子）
- 选择范围：包含 inline 元素时视为整体

**判定逻辑**（在 `KeyboardCaretNavigationHelper` 方向键处理中）：

```csharp
// 光标在 inline 元素左侧，按 →：
//   1. 检查 charData.IsInlineElementCharData
//   2. 检查 charData.CharObject is IInlineElementCursorInteractable interactable
//   3. 否 → 光标跳到元素右侧（+1 偏移）
//   4. 是 → 尝试 interactable.EnterCursor(Direction.Right, ...)
```

#### 7.2 可选增强：`IInlineElementCursorInteractable`（光标可进入）

**实现了 `IInlineElementCursorInteractable` 的内联元素**（如行内公式）允许光标进入其内部导航。

**接口定义**（见 `LightTextEditorPlus.Core\Rendering\IInlineElementCursorInteractable.cs`）：

```csharp
public interface IInlineElementCursorInteractable
{
    bool CanCursorEnter { get; }
    InlineElementCursorState? EnterCursor(Direction direction, InlineElementLayoutInfo layoutInfo);
    InlineElementCursorResult MoveCursor(Direction direction, InlineElementCursorState currentState);
    InlineElementCursorResult HandleMouseClick(TextPoint localPoint, InlineElementCursorState? currentState);
    TextRect GetCaretRect(InlineElementCursorState currentState);
}
```

**关键类型**：

| 类型 | 角色 |
|---|---|
| `InlineElementCursorState` (abstract record) | Core 层透明传递的不透明状态，由实现方定义具体子类型 |
| `InlineElementCursorResult` (readonly struct) | 光标移动/点击结果：`Stay(NewState)` 仍在内部 或 `Exit(Direction)` 脱出 |
| `InlineElementLayoutInfo` (readonly record struct) | 布局信息，仅含 `Bounds`（`TextRect`） |

**光标系统状态机变化**：

现有光标状态只有一种模式（`InText`）。引入此接口后，`CaretManager` 需新增一个光标模式：

```
enum CursorMode
{
    InText,               // 原有：在正常文本中
    InsideInlineElement,  // 新增：在 IInlineElementCursorInteractable 内部
}
```

状态转换：

```
                   方向键碰到 inline 元素
                   且实现了 IInlineElementCursorInteractable
  InText ────────────────────────────────────────→ InsideInlineElement
    ↑                                                    │
    │                                                    │ MoveCursor 返回 Exit
    │                                                    │
    └────────────────────────────────────────────────────┘
                       恢复为 InText
```

**`InsideInlineElement` 模式下的行为**：

- 方向键 → 委托给 `IInlineElementCursorInteractable.MoveCursor`
  - 返回 `Stay(newState)` → 保持 `InsideInlineElement`，更新内部状态
  - 返回 `Exit(direction)` → 恢复 `InText`，光标移到元素前/后对应 `CaretOffset`
- 鼠标点击 → 委托给 `HandleMouseClick`
- `Delete`/`Backspace` → 由光标系统决定：若光标在 `InsideInlineElement` 模式，由实现方定义语义（可能删除内部一个符号，或整体删除元素）；可后续扩展接口增加 `HandleDelete` 方法
- Caret 渲染 → 调用 `GetCaretRect(currentState)` 获取相对于元素左上角的 Caret 矩形，再由平台层转换为文档坐标

**进入方向决定初始内部光标位置**：

| 进入方向 | 含义 | 典型的 `EnterCursor` 行为 |
|---|---|---|
| `Direction.Right` | 从左向右（从元素左侧文本按 `→` 进入） | 光标置于元素内部第一个位置 |
| `Direction.Left` | 从右向左（从元素右侧文本按 `←` 进入） | 光标置于元素内部最后一个位置 |
| `Direction.Up` | 从上方行移入 | 按 X 坐标做命中测试确定内部位置 |
| `Direction.Down` | 从下方行移入 | 按 X 坐标做命中测试确定内部位置 |

**光标脱出后内部状态不保留**：

光标从 `InsideInlineElement` 脱出回到 `InText` 后，`InlineElementCursorState` 立即丢弃。当光标再次进入该元素时，由 `EnterCursor` 根据当前进入方向重新计算初始内部光标位置。这避免了内部状态序列化/持留的复杂度，同时大多数使用场景下（从左侧进入通常在首个位置，从右侧进入通常在末尾位置）不会造成明显的用户体验损失。

**`IInlineElementCursorInteractable` 与 `IInlineElementCharObject` 的关系**：

两个接口正交：

```
IInlineElementCharObject  IInlineElementCursorInteractable
  ↑                        ↑
  │                        │
  ├── 图片：只实现此接口      ├── 行内公式：两个都实现
  │   (原子光标位)           │   (光标可进入内部)
  ├── 徽章：只实现此接口      │
  │   (原子光标位)           │
```

### 步骤 8：测试

1. **`CodePoint` = U+FFFC** — 插入 inline 元素后 `ToText()` 输出验证
2. **`CharData.IsInlineElementCharData`** — 正确识别
3. **分行**：inline 元素在行宽足够时放入一行，超出时换行
4. **`CanBreakAcrossLines = false`** — 不被分割
5. **垂直布局对称测试**
6. **光标**：前后正确移动、删除
7. **渲染不崩溃**（至少占位输出）
8. **`ICharObject` 不可变性**：`IInlineElementCharObject` 实现不包含 mutable 状态

---

## 4. UI 框架层约束尺寸细节

约束由框架层 `TextEditor.Measure(availableSize)` 调用链计算，**不在 Core 层**：

```csharp
// WPF / Avalonia MeasureOverride 中
protected override Size MeasureOverride(Size availableSize)
{
    // 1. 根据 SizeToContent 决定 DocumentManager.DocumentWidth
    SizeToContent sizeToContent = TextEditorCore.SizeToContent;
    if (sizeToContent == TextSizeToContent.Manual || sizeToContent == TextSizeToContent.Height)
    {
        DocumentWidth = Math.Min(availableSize.Width, this.Width);  // 固定宽度
    }
    else
    {
        DocumentWidth = double.PositiveInfinity;  // 宽度自适应
    }

    // 2. 触发布局（内部 GetLineMaxWidth 会读 SizeToContent + DocumentWidth）
    var result = ForceLayout();

    // 3. 返回尺寸
    return MeasureTextEditorCore(availableSize);
}
```

---

## 5. 验证清单

- [ ] `dotnet test -c Release` 全部通过
- [ ] 内联元素在 `HorizontalArrangingLayoutProvider` 正确分行
- [ ] 内联元素在 `VerticalArrangingLayoutProvider` 正确分行
- [ ] `IInlineElementCharObject` 不可变（无 mutable 状态）
- [ ] 平台层 WPF/Avalonia 的 `CharInfoMeasurer` 能识别 inline 元素
- [ ] 文本渲染器（WPF `HorizontalTextRenderer`、Skia `BaseSkiaTextRenderer`）正确跳过 inline 元素（镂空）
- [ ] WPF `IInlineElementHost` 正确管理内联元素视觉树（添加/移除/更新位置）
- [ ] WPF `ArrangeOverride` 正确将内联元素 `UIElement` 定位到 `CharData` 坐标
- [ ] 内联元素 Z 序正确：文本之上、选择/光标层之下
- [ ] 纯 Skia 路径：`ISkiaTextInlineElementCharObject` 实现时正确回调绘制；未实现时镂空跳过不崩溃
- [ ] Skia 文本渲染器调试模式下内联元素占位矩形正确绘制
- [ ] 未实现 `IInlineElementCursorInteractable` 的内联元素：方向键跳过（原子光标位）
- [ ] 实现 `IInlineElementCursorInteractable` 的内联元素：`CanCursorEnter = false` 时降级为原子光标位
- [ ] 实现 `IInlineElementCursorInteractable` 的内联元素：光标正确进入/内部移动/脱出
- [ ] 进入方向对应的初始内部光标位置正确（`Left`→末尾，`Right`→开头）
- [ ] 光标脱出后内部状态丢弃，再次进入时重新计算
- [ ] 鼠标点击内联元素内部命中测试正确
- [ ] `InsideInlineElement` 模式下 Caret 渲染正确（坐标转换）
- [ ] 知识库文件写入 `Docs/Knowledge/`

---

## 6. 关键文件清单

| 文件 | 角色 |
|---|---|
| `LightTextEditorPlus.Core\Utils\TextContext.cs` | 添加 `ObjectReplacementChar` |
| `LightTextEditorPlus.Core\Document\Context_\Char_\IInlineElementCharObject.cs` | 接口定义 |
| `LightTextEditorPlus.Core\Document\...\CharData.cs` | 添加 `IsInlineElementCharData` |
| `LightTextEditorPlus.Core\Layout\HorizontalArrangingLayoutProvider.cs` | 分行逻辑适配 |
| `LightTextEditorPlus.Core\Layout\VerticalArrangingLayoutProvider.cs` | 垂直布局适配 |
| `LightTextEditorPlus.Core\Rendering\IInlineElementHost.cs` | 内联元素宿主接口 + `InlineElementRenderInfo` |
| `LightTextEditorPlus.Core\Rendering\IInlineElementCursorInteractable.cs` | 光标可进入内联元素接口 + `InlineElementCursorState` + `InlineElementCursorResult` + `InlineElementLayoutInfo` |
| `LightTextEditorPlus.Skia\Rendering\ISkiaTextInlineElementCharObject.cs` | Skia 项目可选接口：纯 Skia 路径下内联元素自绘制回调 |
| `LightTextEditorPlus.Core\Carets\KeyboardCaretNavigationHelper.cs` | 光标方向键导航，增加 `IInlineElementCursorInteractable` 进入/脱出逻辑 |
| `LightTextEditorPlus.Core\Carets\CaretManager.cs` | 光标管理，增加 `InsideInlineElement` 模式 |
| `LightTextEditorPlus.Wpf\Platform\CharInfoMeasurer.cs` | WPF 测量适配 |
| `LightTextEditorPlus.Wpf\Layers\InlineElementLayer.cs` | WPF 内联元素视觉树管理层 |
| `LightTextEditorPlus.Wpf\Platform\TextEditor.Platform.cs` | WPF `IInlineElementHost` 实现 + `ArrangeOverride` 适配 |
| `LightTextEditorPlus.Wpf\Rendering\HorizontalTextRenderer.cs` | WPF 渲染镂空（跳过 inline 元素） |
| `LightTextEditorPlus.Wpf\Rendering\VerticalTextRenderer.cs` | WPF 垂直渲染镂空 |
| `LightTextEditorPlus.Skia\Rendering\Core\BaseSkiaTextRenderer.cs` | Skia 渲染镂空（`RenderTextLine` 跳过 inline 元素） |
| `LightTextEditorPlus.Skia\Rendering\Core\HorizontalSkiaTextRenderer.cs` | Skia 渲染镂空 |
| `LightTextEditorPlus.Avalonia\...` | Avalonia 走 `IInlineElementHost` 路径管理内联元素控件树 |
| `Tests\...\FixedCharSizeCharInfoMeasurer.cs` | 测试测量适配 |

---

## 7. 参考资料

### 7.1 业界 "embedded foreign objects in text layout" 模式

"embedded foreign objects" 是富文本排版中的经典设计模式，核心思想是：

- **排版引擎**将嵌入对象视为一个不可拆分的"黑盒"（black box），为其分配布局空间（宽度 × 高度 × 基线偏移），文本围绕它流动
- **渲染引擎**不在字形绘制管线中处理嵌入对象，而是通过回调或视觉树注入，由外部组件完成实际绘制
- 嵌入对象在文本流中占据一个"对象替换字符"（U+FFFC, Object Replacement Character）作为占位

主流实现参考：

| 平台/引擎 | 实现方式 |
|---|---|
| WPF `InlineUIContainer` | 将任意 `UIElement` 嵌入 `TextBlock`/`FlowDocument` 流内容，WPF 内部排版时为其预留空间，渲染时走正常 WPF 视觉树 |
| WinUI `InlineUIContainer` | 同上，用于 `RichTextBlock` |
| WPF `TextFormatter` + `TextEmbeddedObject` | 低级 API：`TextSource.GetTextRun()` 返回 `TextEmbeddedObject`，由 `TextFormatter` 调用其 `ComputeBoundingBox`/`Draw` 方法 |
| DirectWrite `IDWriteInlineObject` | 回调接口：`Draw`、`GetMetrics`、`GetOverhangMetrics`、`GetBreakConditions` |
| Word/Office 对象模型 | 内联图片/公式作为文档流中的特殊 run，有自己的布局和渲染回调 |

### 7.2 Microsoft 官方文档

- [WPF InlineUIContainer (Windows Desktop)](https://learn.microsoft.com/dotnet/api/system.windows.documents.inlineuicontainer) — 将 `UIElement` 嵌入流内容的 WPF 原生机制
- [WinUI InlineUIContainer](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.documents.inlineuicontainer) — WinUI 3 等价实现
- [WPF Advanced Text Formatting](https://learn.microsoft.com/dotnet/desktop/wpf/advanced/advanced-text-formatting) — `TextFormatter` + `TextEmbeddedObject` 低级文本格式化引擎
- [WPF Flow Document Overview](https://learn.microsoft.com/dotnet/desktop/wpf/advanced/flow-document-overview) — Flow Document 中的嵌入对象体系
- [UI Automation: How UI Automation Exposes Embedded Objects](https://learn.microsoft.com/windows/win32/winauto/uiauto-textpattern-and-embedded-objects-overview) — 嵌入对象在无障碍（UIA）中的表示：U+FFFC 作为占位字符，作为一个字符/单词单元

### 7.3 本方案与 WPF `InlineUIContainer` 的对比

| 方面 | WPF `InlineUIContainer` | 本方案 |
|---|---|---|
| 嵌入对象表示 | `InlineUIContainer` 是 `Inline` 子类，在 XAML 树中 | `IInlineElementCharObject` 是 `ICharObject` 实现，在文档字符流中 |
| 布局 | WPF 内部 `TextFormatter` 处理 | Core 层 `HorizontalArrangingLayoutProvider` 处理，与文本字符走相同路径 |
| 渲染 | WPF 视觉树自动渲染 | WPF/Avalonia 通过 `IInlineElementHost` 加入视觉树；纯 Skia 通过 `ISkiaTextInlineElementCharObject` 可选回调 |
| 测量 | WPF 内部测量 | `ICharInfoMeasurer` 平台适配，支持预填入和延迟测量两种策略 |
| 可扩展性 | 仅 WPF | 跨平台（WPF / Avalonia / Skia / MAUI） |
