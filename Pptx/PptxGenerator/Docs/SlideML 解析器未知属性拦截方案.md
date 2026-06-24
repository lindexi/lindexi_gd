# SlideML 解析器未知属性拦截方案

## 问题描述

当 LLM 在 SlideML 中输出某元素不支持的属性时（例如在 `TextElement` 上写 `VerticalTextAlignment="Center"`，或在 `Rect` 上写 `Text`），当前 `SlideMlParser` 静默忽略这些属性，不给 LLM 任何反馈。这导致 LLM 无法感知自己的错误，无法在下一轮迭代中修正。

### 典型案例

```xml
<TextElement X="20" Y="20" Width="36" Height="36" Text="️"
             FontSize="18" TextAlignment="Center" VerticalAlignment="Center" />
```

LLM 的意图是让文本在 36×36 的框内垂直居中，但 `VerticalAlignment` 在 `SlideElement` 基类中仅控制**元素在父容器内的定位**，无法实现文本内容在自身框内的垂直居中。LLM 收不到任何警告，误以为自己的写法正确。

更严重的情况：LLM 写出完全捏造的属性名（如 `TextVerticalAlignment="Center"`），解析器也静默丢弃。

---

## 根因

`SlideMlParser` 的各个 `Parse*` 方法只做"正向读取"——只读取自己关心的属性，不检查 `XElement` 上还有哪些属性未被消费。缺少"反向校验"环节。

---

## 修复方案

### 核心思路

引入 `SlideParseContext` 上下文类型，承载一个诊断收集器。`Parse` 方法接收 context，所有 `Parse*` 子方法通过 context 收集警告和错误。在每个 `Parse*` 方法中，收集 XML 元素上的所有属性名，与该元素实际支持的属性名做差集，将未识别的属性名作为警告产出。同时将现有的 `Enum.Parse` 替换为 `Enum.TryParse`，对非法枚举值也产出警告而非抛异常。

### 具体步骤

#### 步骤 1：新增 `SlideParseContext` 上下文类型

新建 `SlideParseContext` 类，包含：

- `Warnings: List<string>` — 警告收集器
- `Errors: List<string>` — 错误收集器（预留，当前可先不抛异常，而是收集后统一处理）

```csharp
internal sealed class SlideParseContext
{
    public List<string> Warnings { get; } = [];
    public List<string> Errors { get; } = [];
}
```

#### 步骤 2：修改 `SlideMlParser.Parse` 方法签名

将 `Parse(string xml)` 改为接收 `SlideParseContext`：

```csharp
public SlidePage Parse(string xml, SlideParseContext context)
```

context 由调用方（`SlideRenderer`）创建并传入，解析完成后调用方从 `context.Warnings` 和 `context.Errors` 读取诊断信息。

#### 步骤 3：为每种元素定义已知属性白名单

在 `SlideMlParser` 中为每种元素类型定义已知属性集合：

| 元素 | 已知属性 |
|------|----------|
| **Page** | `Background` |
| **Panel** | `Id`, `X`, `Y`, `Width`, `Height`, `HorizontalAlignment`, `VerticalAlignment`, `Opacity`, `Padding`, `Background` |
| **Rect** | `Id`, `X`, `Y`, `Width`, `Height`, `HorizontalAlignment`, `VerticalAlignment`, `Opacity`, `Fill`, `Stroke`, `StrokeThickness`, `CornerRadius` |
| **TextElement** | `Id`, `X`, `Y`, `Width`, `Height`, `HorizontalAlignment`, `VerticalAlignment`, `Opacity`, `Text`, `FontName`, `FontSize`, `Foreground`, `TextAlignment` |
| **Image** | `Id`, `X`, `Y`, `Width`, `Height`, `HorizontalAlignment`, `VerticalAlignment`, `Opacity`, `Source`, `Stretch` |

#### 步骤 4：在每个 Parse* 方法中校验未知属性

在每个 `Parse*` 方法末尾，获取 `XElement.Attributes()` 的所有属性名，与白名单做差集：

```csharp
private static readonly HashSet<string> TextElementKnownAttributes = new(StringComparer.OrdinalIgnoreCase)
{
    "Id", "X", "Y", "Width", "Height",
    "HorizontalAlignment", "VerticalAlignment", "Opacity",
    "Text", "FontName", "FontSize", "Foreground",
    "TextAlignment",
};

private static void ValidateAttributes(XElement element, string elementId,
    HashSet<string> knownAttributes, SlideParseContext context)
{
    foreach (var attr in element.Attributes())
    {
        if (!knownAttributes.Contains(attr.Name.LocalName))
        {
            context.Warnings.Add($"[Warning] {elementId}: 未知属性 \"{attr.Name.LocalName}\"，已忽略");
        }
    }
}
```

#### 步骤 5：将 Enum.Parse 替换为 Enum.TryParse，产出警告

当前 `GetOptionalHorizontalAlignment`、`GetOptionalVerticalAlignment`、`GetOptionalTextAlignment`、`GetOptionalImageStretch` 四个方法使用 `Enum.Parse`，遇到非法值直接抛异常。改为 `Enum.TryParse`，非法值时产出 warning 并返回 null：

```csharp
private static SlideHorizontalAlignment? GetOptionalHorizontalAlignment(XElement element, string elementId, SlideParseContext context)
{
    var text = GetOptionalString(element, "HorizontalAlignment");
    if (string.IsNullOrWhiteSpace(text))
    {
        return null;
    }

    if (Enum.TryParse<SlideHorizontalAlignment>(text, ignoreCase: true, out var result))
    {
        return result;
    }

    context.Warnings.Add($"[Warning] {elementId}: HorizontalAlignment 值 \"{text}\" 无效，已忽略（有效值：Left, Center, Right）");
    return null;
}
```

其他三个枚举解析方法同理改造。这需要 context 和 elementId 能传递到这些辅助方法中。

#### 步骤 6：更新 `SlideRenderer.RenderAsync`

将 parser 返回的 warnings 合并到渲染流程的 warnings 列表中：

```csharp
var context = new SlideParseContext();
var page = _parser.Parse(normalizedXml, context);
var warnings = new List<string>(context.Warnings);
```

---

## 影响范围

| 文件 | 变更类型 |
|------|----------|
| `Code/PptxGenerator/Core/SlideMl/SlideParseContext.cs` | 新增：上下文类型 |
| `Code/PptxGenerator/Core/SlideMl/SlideMlParser.cs` | 修改：添加白名单集合、校验方法、修改 Parse 签名、Enum.Parse → Enum.TryParse |
| `Code/PptxGenerator/Core/SlideRenderer.cs` | 修改：创建 context 并传入 parser，合并 warnings |

---

## 预期效果

- LLM 输出 `TextElement` 上的 `VerticalTextAlignment` 时，收到 `[Warning] elem_001: 未知属性 "VerticalTextAlignment"，已忽略`
- LLM 输出 `Rect` 上的 `Text` 时，收到 `[Warning] card1: 未知属性 "Text"，已忽略`
- LLM 输出 `TextAlignment="Middle"` 时，收到 `[Warning] title: TextAlignment 值 "Middle" 无效，已忽略（有效值：Left, Center, Right, Justify）`
- LLM 根据警告修正 SlideML，减少无效属性使用
- 不影响现有合法 SlideML 的解析和渲染
