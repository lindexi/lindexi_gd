# 内联元素测量器 `IInlineElementMeasurer` 平台分离方案

## 背景

在 `IInlineElementCharObject` 完整实现计划的步骤 5（`ICharInfoMeasurer` 平台适配）中，核心问题是：当文本流中出现内联元素（图片、公式等）字符时，由谁来测量其尺寸并填入 `CharData.CharDataInfo`。

现有 `ICharInfoMeasurer` 的 Skia 实现 `SkiaCharInfoMeasurer` 已经非常复杂，承载了以下职责：

- HarfBuzz Shape（连字 `liga`、RTL、字距 `kern`、竖排字距、大写间距）
- Skia Glyph 尺寸获取（`GetGlyphWidths` + `GetGlyphBounds`）
- `CharRenderInfoSetter`：GlyphCluster → UTF-16 偏移 → `CharData` 索引的映射（处理连写、RTL 排序、emoji 拆分、去重）
- 竖排文本的宽高倒换、基线计算、加粗补偿
- GSUB 快速路径检测

将内联元素测量也塞入 `SkiaCharInfoMeasurer` 会导致其变为「上帝类」，职责混乱、难以测试。

## 核心决策

**在 `ArrangingLayoutProvider.EnsureMeasureAndFillSizeOfCharDataList` 调用点做分流，而非在 `SkiaCharInfoMeasurer` 内部做分支。**

即：内联元素测量和文本字符测量是两个不同的领域，应由两个独立的接口负责。

```
EnsureMeasureAndFillSizeOfCharDataList(toMeasureCharDataList, context)
  │
  ├─ Step 1: 内联元素字符 → IInlineElementMeasurer.MeasureInlineElement(...)
  │           无平台则走 MeasureAndFillCharData fallback（方块占位）
  │
  └─ Step 2: 文本字符 → ICharInfoMeasurer.FillCharDataInfoList(...)
              无平台则逐个走 MeasureAndFillCharData fallback
```

分流后，`SkiaCharInfoMeasurer` 完全不需要感知 inline 元素。当它遍历 `charDataList` 时，inline 元素的 `IsInvalidCharDataInfo` 已为 `false`（Step 1 已填充），自然进入快速跳过分支。

## 新接口 `IInlineElementMeasurer`

**文件**：`LightTextEditorPlus.Core\Platform\Layout_\IInlineElementMeasurer.cs`

```csharp
namespace LightTextEditorPlus.Core.Platform;

/// <summary>
/// 内联元素（图片、公式等）的尺寸测量器。由平台层实现。
/// 与 <see cref="ICharInfoMeasurer"/> 分离，保持文本测量逻辑的单一职责。
/// </summary>
public interface IInlineElementMeasurer
{
    /// <summary>
    /// 测量内联元素的尺寸并填入 <paramref name="charData"/>。
    /// 实现方保证调用后 <paramref name="charData"/> 的
    /// <see cref="CharData.IsInvalidCharDataInfo"/> 为 false。
    /// </summary>
    /// <param name="inlineElement">内联元素。调用方已通过 <see cref="CharData.IsInlineElementCharData"/> 判断，
    /// 实现方无需再次转型。</param>
    /// <param name="charData">当前内联元素字符数据，用于写回测量结果、读取 <see cref="CharData.RunProperty"/> 等</param>
    /// <param name="setter">用于将测量结果写入 CharData 的 setter</param>
    void MeasureInlineElement(
        IInlineElementCharObject inlineElement,
        CharData charData,
        ICharDataLayoutInfoSetter setter);
}
```

### 设计要点

- 接口放在 Core 层，不依赖任何平台（不依赖 SkiaSharp、WPF、Avalonia）
- `inlineElement` 参数：调用方在 `EnsureMeasureAndFillSizeOfCharDataList` 中已确认 `charData.IsInlineElementCharData`，顺手转型传入，实现方无需重复转型，签名即文档
- `charData` 参数：仍需要，用于写回 `CharDataInfo`（通过 `setter`）以及读取 `RunProperty.FontSize` 等上下文
- 参数只传单个元素（内联元素每次只出现一个，不像文本需要批量 shaping）
- 通过 `ICharDataLayoutInfoSetter` 写入结果，与现有 `MeasureAndFillCharData` fallback 使用相同的写入路径

### 与 `PlatformProvider` 的集成

在 `IPlatformProvider` 中新增可选方法：

```csharp
/// <summary>
/// 获取内联元素测量器。返回 null 时使用方块占位 fallback。
/// </summary>
IInlineElementMeasurer? GetInlineElementMeasurer() => null;
```

默认返回 `null`，保证不破坏现有平台实现。WPF / Avalonia 平台在各自 `PlatformProvider` 中 override 提供对应实现。

## `EnsureMeasureAndFillSizeOfCharDataList` 修改

```csharp
protected void EnsureMeasureAndFillSizeOfCharDataList(
    in TextReadOnlyListSpan<CharData> toMeasureCharDataList,
    UpdateLayoutContext updateLayoutContext)
{
    // Step 1: 内联元素单独处理
    IInlineElementMeasurer? inlineMeasurer = TextEditor.PlatformProvider.GetInlineElementMeasurer();

    for (int i = 0; i < toMeasureCharDataList.Count; i++)
    {
        CharData charData = toMeasureCharDataList[i];
        if (charData.IsInlineElementCharData && charData.IsInvalidCharDataInfo)
        {
            var inlineElement = (IInlineElementCharObject) charData.CharObject;

            if (inlineMeasurer is not null)
            {
                inlineMeasurer.MeasureInlineElement(inlineElement, charData, updateLayoutContext);
            }
            else
            {
                // 无平台实现 → 回退到方块占位
                MeasureAndFillCharData(charData, updateLayoutContext);
            }
        }
    }

    // Step 2: 文本字符走现有路径（逻辑不变）
    ICharInfoMeasurer? charInfoMeasurer = TextEditor.PlatformProvider.GetCharInfoMeasurer();
    if (charInfoMeasurer is not null)
    {
        charInfoMeasurer.FillCharDataInfoList(
            new FillCharDataInfoListArgument(toMeasureCharDataList, updateLayoutContext));

        for (var i = 0; i < toMeasureCharDataList.Count; i++)
        {
            var charData = toMeasureCharDataList[i];
            if (charData.IsInvalidCharDataInfo)
            {
                throw new TextEditorMeasurerInvalidCharDataInfoException(toMeasureCharDataList, i);
            }
        }

        return;
    }
    else
    {
        for (int i = 0; i < toMeasureCharDataList.Count; i++)
        {
            CharData charData = toMeasureCharDataList[i];
            if (charData.IsInvalidCharDataInfo)
            {
                MeasureAndFillCharData(charData, updateLayoutContext);
            }
        }
    }
}
```

## `SkiaCharInfoMeasurer` 所需变更

仅需在 GSUB 快速路径中增加一行守卫，避免 inline 元素进入文本 shaping 流程：

```csharp
// SkiaCharInfoMeasurer.FillCharDataInfoList 中
bool allMeasured = textReadOnlyListSpan.All(static t => !t.IsInvalidCharDataInfo);
if (allMeasured)
{
    CharData charData = textReadOnlyListSpan[0];

    // 新增：inline 元素已在 Step 1 填充，不需要文本 shaping
    if (charData.IsInlineElementCharData)
    {
        continue;
    }

    SkiaTextRunProperty skiaTextRunProperty = charData.RunProperty.AsSkiaRunProperty();
    // ... 现有 GSUB 检查逻辑 ...
}
```

总计新增 **3 行代码**，不增加 `SkiaCharInfoMeasurer` 的任何认知负担。

## 各平台实现策略

### 测量时机总览

内联元素的尺寸测量有两个可能的时机：

| 时机 | 说明 | 适用平台 |
|------|------|----------|
| **UI 层预测量**（最优） | 在 `MeasureOverride` 中，布局前先测量内联元素对应的 UI 控件，直接写入 `CharDataInfo` | WPF、Avalonia |
| **布局中测量**（兜底） | 在 `UpdateLayout` → `EnsureMeasureAndFillSizeOfCharDataList` 中，由 `IInlineElementMeasurer` 处理 | 所有平台 |

两者的关系是：UI 层预测量优先，`IInlineElementMeasurer` 兜底。

### WPF 平台测量时序

```
MeasureOverride(availableSize)                   ← WPF 布局入口
  │
  ├─ ① SyncControlSize()
  │    同步 Width/Height 到 DocumentManager
  │
  ├─ ② 【预测量时机】遍历内联元素
  │    foreach inline element:
  │      子 UIElement.Measure(constraint)
  │      → ICharDataLayoutInfoSetter.SetCharDataInfo(charData, result)
  │    ↓ 此时 inline 元素的 IsInvalidCharDataInfo 已变为 false
  │
  ├─ ③ MeasureTextEditorCore(availableSize)
  │    → ForceLayout()
  │      → TextEditorPlatformProvider.EnsureLayoutUpdated()
  │        → TextEditorCore.UpdateLayout()
  │          → ArrangingLayoutProvider.EnsureMeasureAndFillSizeOfCharDataList(...)
  │            │
  │            ├─ Step 1: IInlineElementMeasurer.MeasureInlineElement(...)
  │            │    ↑ 兜底：②中未测量的才会进入这里
  │            │
  │            └─ Step 2: ICharInfoMeasurer.FillCharDataInfoList(...)
  │                 ↑ 文本字符。inline 元素已有尺寸，被跳过
  │
  └─ ④ TextView.Measure(result)
```

**关键**：② 在 ③ 之前执行，因此 inline 元素在进入 `UpdateLayout` 之前已经被测量好了。`IInlineElementMeasurer` 仅在以下情况触发：

- 内联元素没有自然尺寸（`NaturalSize == null`）且 UI 控件尚未完成异步渲染（如公式）
- 纯 Skia 路径，不存在 UI 预测量阶段

### Avalonia 平台测量时序

```
MeasureOverride(availableSize)                   ← Avalonia 布局入口
  │
  ├─ ① 【预测量时机】遍历内联元素
  │    foreach inline element:
  │      子 Control.Measure(constraint)
  │      → ICharDataLayoutInfoSetter.SetCharDataInfo(charData, result)
  │
  ├─ ② MeasureTextEditorCore(availableSize)
  │    → ForceLayout()
  │      → (同 WPF 路径)
  │        → EnsureMeasureAndFillSizeOfCharDataList(...)
  │          ├─ Step 1: IInlineElementMeasurer（兜底）
  │          └─ Step 2: ICharInfoMeasurer
  │
  └─ ③ 渲染时 Render() 调用 ForceLayout() 再次触发布局
```

Avalonia 额外多一个路径：渲染时可能在 `Render()` 方法中再次调用 `ForceLayout()`，此时若 inline 元素尺寸有变化，再次走 `EnsureMeasureAndFillSizeOfCharDataList`，但 inline 元素已有尺寸（Step 1 跳过），`ICharInfoMeasurer` Step 2 中 GSUB 守卫会 `continue` 跳过。

### 纯 Skia（无 UI 框架）测量时序

```
用户 API 调用（如 GetRenderInfo、ExactLayout）
  → RequireDispatchUpdateLayout / InvokeDispatchUpdateLayout
    → TextEditorCore.UpdateLayout()
      → ArrangingLayoutProvider.EnsureMeasureAndFillSizeOfCharDataList(...)
        │
        ├─ Step 1: IInlineElementMeasurer.MeasureInlineElement(...)
        │    ↑ 唯一的测量路径！无 UI 预测量阶段
        │
        └─ Step 2: ICharInfoMeasurer.FillCharDataInfoList(...)
             ↑ GSUB 守卫 continue 跳过 inline 元素
```

纯 Skia 没有 `MeasureOverride`，`IInlineElementMeasurer` 是 inline 元素测量的**唯一入口**。Skia 的 `IInlineElementMeasurer` 实现逻辑：

- 有 `NaturalSize` → 直接以 `NaturalSize` 填入 `CharDataInfo`（基线按 `BaseLineRatio` 计算）
- 无 `NaturalSize` → 调用 `MeasureAndFillCharData` fallback（字号方块占位）

### 各平台实现策略总结

| 平台 | `IInlineElementMeasurer` | `ICharInfoMeasurer` | 预测量 |
|------|--------------------------|---------------------|--------|
| **Core（无平台）** | `null` → 方块占位 | `null` → 逐个方块 | 无 |
| **纯 Skia** | 读 `NaturalSize` 直接填入；无 `NaturalSize` 则方块占位 | `SkiaCharInfoMeasurer` | 无（`IInlineElementMeasurer` 是唯一入口） |
| **WPF** | 兜底：异步公式等时序问题 | WPF `CharInfoMeasurer` | 在 `MeasureOverride` 中，先于 `MeasureTextEditorCore` 执行 |
| **Avalonia** | 兜底：异步公式等时序问题 | 继承/组合 `SkiaCharInfoMeasurer` | 在 `MeasureOverride` 中，先于 `MeasureTextEditorCore` 执行 |

## 与 `ISkiaTextInlineElementCharObject` 的关系

`IInlineElementMeasurer` 和 `ISkiaTextInlineElementCharObject` 是正交的两个概念，服务于不同阶段：

| 接口 | 阶段 | 目的 |
|------|------|------|
| `IInlineElementMeasurer` | **测量** | 决定内联元素在布局中的尺寸 |
| `ISkiaTextInlineElementCharObject` | **渲染** | 给纯 Skia 路径提供自绘制能力 |

Avalonia 只需要 `IInlineElementMeasurer`（测量），不需要 `ISkiaTextInlineElementCharObject`（渲染走 Avalonia 控件树）。

## `ISkiaTextInlineElementCharObject` 的测量策略

`ISkiaTextInlineElementCharObject` 是纯 Skia 渲染路径下的**渲染接口**（`Render(SKCanvas, TextRect)`），不参与、也不应该参与测量。

### 职责分离

| 接口 | 阶段 | 是否涉及尺寸 |
|------|------|-------------|
| `IInlineElementCharObject.NaturalSize` | 测量输入 | 是（可选只读参照） |
| `IInlineElementMeasurer` | 测量执行 | 是（决定最终尺寸） |
| `ISkiaTextInlineElementCharObject` | 渲染 | 否（只在给定区域内绘制） |

### 纯 Skia 路径下 inline 元素的完整流程

```
测量阶段（IInlineElementMeasurer）:
  inlineElement.NaturalSize != null
    → 直接以 NaturalSize + BaseLineRatio 填入 CharDataInfo
    → IsInvalidCharDataInfo = false

  inlineElement.NaturalSize == null
    → 回退到方块占位（字号 × 字号）
    → IsInvalidCharDataInfo = false（只是可能不准确）

布局阶段:
  Core 布局层读 CharData.Size / CharData.Baseline
  → 不考虑 CharObject 具体类型

渲染阶段（BaseSkiaTextRenderer.RenderTextLine）:
  foreach run:
    if charData.IsInlineElementCharData:
      if charData.CharObject is ISkiaTextInlineElementCharObject skiaElement:
        skiaElement.Render(canvas, bounds)  ← 只在给定的 bounds 内绘制
      else:
        镂空跳过（不崩溃）
    else:
      正常文本渲染
```

**结论**：`ISkiaTextInlineElementCharObject` 不需要做任何测量。如果实现方想要控制自己的渲染尺寸，通过 `IInlineElementCharObject.NaturalSize` 即可。测量统一走 `IInlineElementMeasurer`。

## `IInlineElementCharObject` 生命周期通知

### RunProperty 变更

**不需要通知**。理由：

- `RunProperty` 变更 → 文本标记为脏 → `UpdateLayout` → `EnsureMeasureAndFillSizeOfCharDataList`
- 此时若 `IsInvalidCharDataInfo` 为 true（尺寸已失效），`IInlineElementMeasurer` 会重新测量
- 如果实在需要感知 `RunProperty.FontSize` 变化，inline 元素实现方可以在 `MeasureInlineElement` 被调用时通过 `charData.RunProperty` 读取到最新值

不放在 `IInlineElementCharObject` 上的理由：`ICharObject` 的实现都是不可变的（`RuneCharObject`、`LineBreakCharObject` 等），加变更通知破坏这一约定。

### 加入 / 删除通知

这是有意义的。例如图片元素被插入文档时可以开始加载图片，被删除时可以取消未完成的加载。但**应该放在独立的可选接口上**，而非 `IInlineElementCharObject`。

```csharp
// LightTextEditorPlus.Core\Document\Context_\Char_\IInlineElementLifecycle.cs

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 内联元素生命周期通知（可选接口）。
/// 仅当内联元素需要在加入/移出文档时执行初始化/清理时才实现此接口。
/// </summary>
public interface IInlineElementLifecycle
{
    /// <summary>
    /// 内联元素被加入到文档时调用。每次加入都会调用一次，与 <see cref="OnDetached"/> 成对。
    /// 实现方可在此启动异步加载（如图片下载、公式排版）。
    /// </summary>
    /// <param name="context">
    /// 附着上下文。提供当前文本编辑器引用和 <see cref="IInlineElementAttachedContext.InvalidateMeasure"/> 方法。
    /// 实现方应在异步加载完成或内容变更后调用 <see cref="IInlineElementAttachedContext.InvalidateMeasure"/>，
    /// 以清除测量缓存并触发重新布局。
    /// </param>
    void OnAttached(IInlineElementAttachedContext context);

    /// <summary>
    /// 内联元素从文档中移除时调用。实现方可在此取消加载、释放资源。
    /// </summary>
    /// <param name="textEditor">当前文本编辑器上下文</param>
    void OnDetached(TextEditorCore textEditor);
}
```

```csharp
// LightTextEditorPlus.Core\Document\Context_\Char_\IInlineElementAttachedContext.cs

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 内联元素附着到文档时的上下文。由 Core 层在 <see cref="IInlineElementLifecycle.OnAttached"/> 时传入。
/// </summary>
/// <remarks>
/// 内部持有 <see cref="CharData"/> 和 <see cref="ParagraphData"/> 的引用，
/// <see cref="InvalidateMeasure"/> 是 O(1) 操作——直接清除已捕获 CharData 的测量缓存并标记段落为脏。
/// 不暴露内部 CharData/ParagraphData 引用，保持封装。
/// </remarks>
public interface IInlineElementAttachedContext
{
    /// <summary>
    /// 当前文本编辑器。
    /// </summary>
    TextEditorCore TextEditor { get; }

    /// <summary>
    /// 使当前内联元素的测量缓存（<see cref="CharData.CharDataInfo"/>）失效，
    /// 标记所在段落为脏，并调度重新布局。
    /// </summary>
    /// <remarks>
    /// 对标 WPF <c>FrameworkElement.InvalidateMeasure()</c> 的语义：
    /// 清除的是测量结果，触发的是重新测量和布局。
    /// 当内联元素的 NaturalSize 发生变化（如图片下载完成、公式重新排版）时调用。
    /// </remarks>
    void InvalidateMeasure();
}
```

**`OnAttached` / `OnDetached` 成对调用的隐含能力：复用检测**

由于 `OnAttached` 和 `OnDetached` 保证成对调用（加入一次 → `OnAttached` 一次；移除一次 → `OnDetached` 一次），实现方可以自然地利用这个特性来控制复用行为：

- **允许重复加入**（如图片）：不维护计数，每次 `OnAttached` 启动新加载
- **禁止重复加入**（如公式）：维护 `_attachedCount`，`OnAttached` 时若 `_attachedCount > 0` 则抛出 `InvalidOperationException`
- **共享实例**（如徽章图标）：维护引用计数，`OnAttached` 时 +1，`OnDetached` 时 -1，到 0 时释放资源

```csharp
// 禁止重复加入的公式元素
class FormulaInlineElement : IInlineElementCharObject, IInlineElementLifecycle
{
    private int _attachedCount;

    void IInlineElementLifecycle.OnAttached(IInlineElementAttachedContext context)
    {
        if (_attachedCount > 0)
            throw new InvalidOperationException("公式已存在于另一文档中");
        _attachedCount++;
        _context = context;
        StartRendering();
    }

    void IInlineElementLifecycle.OnDetached(TextEditorCore textEditor)
    {
        _attachedCount--;
        _context = null;
        StopRendering();
    }
}
```

### 内联元素尺寸变化的无效化机制

有两个典型场景会触发 inline 元素尺寸变化：

| 场景 | 触发源 | 时机 |
|------|--------|------|
| **异步加载** | 图片下载完成、公式异步排版完成 | `OnAttached` 之后某个时刻 |
| **用户编辑** | 公式内容被编辑，渲染结果尺寸变化 | 运行中，在 `DocumentChanged` 流程之外 |

两者的共同点：inline 元素尺寸变化时，文档本身不一定脏，但 `CharDataInfo` 中缓存的尺寸已过时。而 `EnsureMeasureAndFillSizeOfCharDataList` 只测量 `IsInvalidCharDataInfo == true` 的 `CharData`——不清除缓存就不会触发重新测量。

**设计**：`IInlineElementAttachedContext` 由 Core 层在 `OnAttached` 时创建并传入，内部持有 `CharData` + `ParagraphData` + 分发逻辑的引用，但对外只暴露 `TextEditor` 属性和 `InvalidateMeasure()` 方法——内部状态完全封装。

```csharp
// Core 层内部实现（internal sealed class）
internal sealed class InlineElementAttachedContext : IInlineElementAttachedContext
{
    private readonly CharData _charData;
    private readonly ParagraphData _paragraph;
    private readonly TextEditorCore _textEditor;

    public TextEditorCore TextEditor => _textEditor;

    internal InlineElementAttachedContext(
        CharData charData, ParagraphData paragraph, TextEditorCore textEditor)
    {
        _charData = charData;
        _paragraph = paragraph;
        _textEditor = textEditor;
    }

    public void InvalidateMeasure()
    {
        _charData.ClearCharDataInfo();
        _paragraph.SetDirty();
        _textEditor.RequireDispatchUpdateLayout("Inline element size changed");
    }
}

// DocumentManager.EditAndReplaceRun 中
if (charObject is IInlineElementLifecycle lifecycle)
{
    var context = new InlineElementAttachedContext(charData, paragraphData, textEditorCore);
    lifecycle.OnAttached(context);
}
```

**为什么是上下文类型而非 `Action` 委托？**

- O(1)：内部持有 `CharData` + `ParagraphData` 引用，`InvalidateMeasure()` 无需遍历
- 封装：`CharData` 和 `ParagraphData` 是 `internal` 类型，通过接口隐藏实现细节
- 语义清晰：`context.InvalidateMeasure()` 比 `invalidateRenderInfo()` 更能表达"使测量失效"的意图
- 可扩展：未来如有需要可在 `IInlineElementAttachedContext` 上增加属性或方法，而 `Action` 委托无法扩展

**调用链**：

```
实现方调用 context.InvalidateMeasure()
  → charData.ClearCharDataInfo()        ← IsInvalidCharDataInfo 变为 true
  → paragraph.SetDirty()                ← 段落版本递增
  → RequireDispatchUpdateLayout(...)    ← 调度 UpdateLayout

UpdateLayout()
  → 遍历脏段落
    → EnsureMeasureAndFillSizeOfCharDataList(charDataList)
      → Step 1: if charData.IsInlineElementCharData && IsInvalidCharDataInfo
          → IInlineElementMeasurer.MeasureInlineElement(...)
          → 此时 NaturalSize 已更新，填入真实尺寸
      → Step 2: ICharInfoMeasurer 跳过已有尺寸的 inline 元素
```

**典型使用示例**：

```csharp
// 场景 1：图片异步加载完成
class ImageInlineElement : IInlineElementCharObject, IInlineElementLifecycle
{
    private TextSize? _naturalSize;
    private IInlineElementAttachedContext? _context;

    public TextSize? NaturalSize => _naturalSize;

    void IInlineElementLifecycle.OnAttached(IInlineElementAttachedContext context)
    {
        _context = context;
        _ = LoadImageAsync();
    }

    void IInlineElementLifecycle.OnDetached(TextEditorCore textEditor)
        => _context = null;

    private async Task LoadImageAsync()
    {
        var size = await DownloadAndDecodeImageAsync();
        _naturalSize = new TextSize(size.Width, size.Height);
        _context?.InvalidateMeasure();
    }
}

// 场景 2：公式内容被编辑
class FormulaInlineElement : IInlineElementCharObject, IInlineElementLifecycle
{
    private IInlineElementAttachedContext? _context;

    void IInlineElementLifecycle.OnAttached(IInlineElementAttachedContext context)
        => _context = context;

    void IInlineElementLifecycle.OnDetached(TextEditorCore textEditor)
        => _context = null;

    internal void OnFormulaContentChanged()
    {
        RecalculateSize();
        _context?.InvalidateMeasure();
    }
}
```

**`OnAttached` 与 `InvalidateMeasure` 的关系**：
- `OnAttached`：告诉你"你被加入到文档了，这是你的附着上下文"
- `context.InvalidateMeasure()`：O(1) 清除测量缓存并触发重排，无需查段落、无需 `ReferenceEquals`

## 修改点摘要

| 文件 | 变更 |
|------|------|
| `Core\Platform\Layout_\IInlineElementMeasurer.cs` | **新增**接口 |
| `Core\Document\Context_\Char_\IInlineElementLifecycle.cs` | **新增**接口（`OnAttached(IInlineElementAttachedContext)`, `OnDetached(TextEditorCore)`） |
| `Core\Document\Context_\Char_\IInlineElementAttachedContext.cs` | **新增**接口（`TextEditor` + `InvalidateMeasure()`），内部实现类 `InlineElementAttachedContext` |
| `Core\Platform\PlatformProvider.cs` | 新增 `GetInlineElementMeasurer()` 默认方法 |
| `Core\Layout\ArrangingLayoutProvider.cs` | `EnsureMeasureAndFillSizeOfCharDataList` 中增加 Step 1 分流 |
| `Core\Document\DocumentManagers_\...\DocumentManager.cs` | `EditAndReplaceRun` 中构建 `InlineElementAttachedContext` → 调用 `OnAttached`；删除/替换时调用 `OnDetached` |
| `Skia\Platform\SkiaCharInfoMeasurer.cs` | GSUB 快速路径增加 `IsInlineElementCharData` 守卫（3 行） |
| `Skia\Platform\SkiaTextEditorPlatformProvider.cs` | override `GetInlineElementMeasurer()` — 读 `NaturalSize` 直接填入 |
| `Wpf\Platform\TextEditor.Platform.cs` | `MeasureOverride` 中增加预测量逻辑 |
| `Wpf\Platform\TextEditorPlatformProvider.cs` | override `GetInlineElementMeasurer()` — 兜底测量 |
| `Avalonia\Platform\TextEditor.Platform.ava.cs` | `MeasureOverride` 中增加预测量逻辑 |
| `Avalonia\Platform\AvaloniaSkiaTextEditorPlatformProvider.cs` | override `GetInlineElementMeasurer()` — 兜底测量 |

## 后续建议

- `IInlineElementMeasurer` 接口保持单个元素测量签名（`MeasureInlineElement`）即可，无需扩展批量版本。理由：内联元素之间不存在文本字符的相互依赖（连字、字距、RTL 重排），每个 inline 元素的尺寸完全独立，批量测量不会带来正确性或性能上的收益
- `MeasureAndFillCharData` fallback 的方块基线比例（`4/5`）是硬编码测试值，可考虑在未来内联元素场景下根据 `BaseLineRatio` 动态调整

