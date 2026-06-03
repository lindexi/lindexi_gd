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
    /// <param name="charData">当前内联元素字符数据</param>
    /// <param name="setter">用于将测量结果写入 CharData 的 setter</param>
    void MeasureInlineElement(CharData charData, ICharDataLayoutInfoSetter setter);
}
```

### 设计要点

- 接口放在 Core 层，不依赖任何平台（不依赖 SkiaSharp、WPF、Avalonia）
- 参数只传单个 `CharData`（内联元素每次只出现一个，不像文本需要批量 shaping）
- 通过 `ICharDataLayoutInfoSetter` 写入结果，与现有 `MeasureAndFillCharData` fallback 使用相同的写入路径
- 不传 `UpdateLayoutContext`：inline 测量不需要 shaping 上下文，只需要 `CharData` 自身的信息（`CharObject`、`RunProperty`）

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
            if (inlineMeasurer is not null)
            {
                inlineMeasurer.MeasureInlineElement(charData, updateLayoutContext);
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

| 平台 | `IInlineElementMeasurer` | `ICharInfoMeasurer` |
|------|--------------------------|---------------------|
| **Core（无平台）** | `null` → 方块占位 | `null` → 逐个方块 |
| **纯 Skia** | `NaturalSize` 直接填入；无 `NaturalSize` 则方块占位 | `SkiaCharInfoMeasurer` |
| **WPF** | `MeasureOverride` 中预测量 UIElement，`IInlineElementMeasurer` 作为兜底 | WPF `CharInfoMeasurer` |
| **Avalonia** | 同 WPF，控件 `MeasureOverride` 中预测量，`IInlineElementMeasurer` 兜底 | 继承/组合 `SkiaCharInfoMeasurer` |

### 时序策略（WPF / Avalonia）

```
能赶上 UI Measure → UI 层预先填入 CharDataInfo → IsInvalidCharDataInfo == false
                   → Step 1 跳过 → Step 2 走文本测量（inline 元素已填充，不参与）
赶不上           → CharDataInfo Invalid → IInlineElementMeasurer 填默认尺寸
                   → 打脏标记 → 下次 UI Measure 获得真实尺寸后重新填入 → 重新布局
```

### Avalonia 特别说明

Avalonia 的文本测量器继承自 `SkiaCharInfoMeasurer`，但 inline 元素测量完全不经过 Skia 文本管线。测量路径为：

1. Avalonia 控件层预先测量内联元素（图片 → `Image.Source.Size`，公式 → 子排版引擎）
2. 通过 `ICharDataLayoutInfoSetter` 直接写入 `CharDataInfo`
3. `SkiaCharInfoMeasurer` 永远看不到需要测量的 inline 元素字符

若 Avalonia 也实现 `IInlineElementMeasurer`，仅作为异步公式渲染等时序问题的兜底。

## 与 `ISkiaTextInlineElementCharObject` 的关系

`IInlineElementMeasurer` 和 `ISkiaTextInlineElementCharObject` 是正交的两个概念，服务于不同阶段：

| 接口 | 阶段 | 目的 |
|------|------|------|
| `IInlineElementMeasurer` | **测量** | 决定内联元素在布局中的尺寸 |
| `ISkiaTextInlineElementCharObject` | **渲染** | 给纯 Skia 路径提供自绘制能力 |

Avalonia 只需要 `IInlineElementMeasurer`（测量），不需要 `ISkiaTextInlineElementCharObject`（渲染走 Avalonia 控件树）。

## 修改点摘要

| 文件 | 变更 |
|------|------|
| `Core\Platform\Layout_\IInlineElementMeasurer.cs` | **新增**接口 |
| `Core\Platform\IPlatformProvider.cs` | 新增 `GetInlineElementMeasurer()` 默认方法 |
| `Core\Layout\ArrangingLayoutProvider.cs` | `EnsureMeasureAndFillSizeOfCharDataList` 中增加 Step 1 分流 |
| `Skia\Platform\SkiaCharInfoMeasurer.cs` | GSUB 快速路径增加 `IsInlineElementCharData` 守卫（3 行） |
| 各平台 `PlatformProvider.cs` | 可选 override `GetInlineElementMeasurer()` |

## 后续建议

- `IInlineElementMeasurer` 接口本身较轻量，未来公式等复杂内联元素的异步测量策略可在平台实现层扩展，不影响 Core 接口
- `MeasureAndFillCharData` fallback 的方块基线比例（`4/5`）是硬编码测试值，可考虑在未来内联元素场景下根据 `BaseLineRatio` 动态调整
- 若未来出现需要批量测量的内联元素场景（如连续多张图片），可以将 `MeasureInlineElement` 签名扩展为批量版本，但当前无此需求
