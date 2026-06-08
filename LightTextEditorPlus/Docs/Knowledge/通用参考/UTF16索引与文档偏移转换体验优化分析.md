# UTF-16 索引与文档偏移转换体验优化分析

## 问题本质

从 commit e67fdd5262564229d0b0d22c72917e4a6ee8285b 可以看到，TextEditor 内部使用了两套索引体系：

| 索引体系 | 说明 | `💡` 长度 | `\r\n` 长度 |
|---------|------|----------|------------|
| **UTF-16 索引** | .NET `string` 的 `char` 位置，外部库（Markdig、ColorCode 等）使用 | 2 | 2 |
| **文档字符偏移** | TextEditor 内部 `ICharObject` 计数，`DocumentOffset`/`Selection` 使用 | 1 | 1 |

**核心矛盾**：`ICharObject`（Rune 粒度）≠ C# `char`（UTF-16 code unit），但 TextEditor 对外暴露的 API（`GetText`、`SetRunProperty`、`Selection`、`DocumentOffset`）使用的都是文档字符偏移。外部高亮器/解析器拿到的却是 UTF-16 索引。**两者之间没有公开、统一、容易发现的转换方法**。

## 当前现状：转换逻辑散落在 4 个地方

```
1. MarkdownDocumentHighlighter.ConvertUtf16IndexToDocumentOffset  → internal static
2. TextRunPropertySetter.GetDocumentCharOffset                    → private (record struct)
3. DocumentHighlighterTestHelper.GetDocumentOffsetFromUtf16Index  → internal (测试项目)
4. TextCharObjectCreator.TextToCharObjectList                     → internal (Core，构建列表而非单纯的计数)
```

所有实现逻辑几乎相同（Rune 枚举 + `\r\n` 折叠），但**没有一处是 public 的**。上层开发者如果自己写高亮器、写插件、写扩展，**无法复用这些转换**，只能再抄一遍（就像这次 commit 做的事情）。

## 转换算法本质

文档字符偏移的计数规则来自 `TextCharObjectCreator.TextToCharObjectList`：

| 原始文本（UTF-16） | 产出的 `ICharObject` | 文档字符偏移计数 |
|---|---|---|
| 单个 BMP 字符（如 `a`） | 1 个 `SingleCharObject` | 1 |
| 代理对字符（如 `💡`） | 1 个 `RuneCharObject` | 1 |
| `\r` | 1 个 `LineBreakCharObject` | 1 |
| `\n`（前无 `\r`） | 1 个 `LineBreakCharObject` | 1 |
| `\r\n` | 1 个 `LineBreakCharObject`（在 `\r` 时产生，`\n` 被跳过） | 1 |
| `\uFFFC`（内联元素占位符） | 1 个 `IInlineElementCharObject` | 1 |

其中 `\uFFFC` 是 BMP 单码点字符，映射到 `IInlineElementCharObject` 时为 1:1，对转换逻辑无额外影响。

因此 UTF-16 → 文档偏移的通用转换方法只需按 Rune 枚举输入字符串，并处理 `\r\n` 折叠即可覆盖所有现有字符类型（包括未来 `IInlineElementCharObject`）。

## 推荐方案：三层 API 协同

文本库设计上会提供冗余 API 来满足不同层级开发者的需求，这里采用三层递进式设计：

### 第一层：Core 公开工具类 `TextIndexConverter`

在 `LightTextEditorPlus.Core` 中新增 `public static` 工具类，作为**唯一权威**的转换实现：

```csharp
// LightTextEditorPlus.Core/Utils/TextIndexConverter.cs
namespace LightTextEditorPlus.Core;

/// <summary>
/// 提供 UTF-16 字符串索引与文档字符偏移之间的转换。
/// 文档字符偏移中，代理对字符（如 emoji）算 1 个字符，\r\n 折叠为 1 个字符。
/// </summary>
public static class TextIndexConverter
{
    /// <summary>
    /// 将 UTF-16 字符串索引转换为文档字符偏移。
    /// </summary>
    /// <param name="text">原始文本（使用 UTF-16 编码）。</param>
    /// <param name="utf16Index">UTF-16 索引，即 string[index] 的 index。</param>
    /// <returns>对应的文档字符偏移。</returns>
    public static int ConvertUtf16IndexToDocumentOffset(string text, int utf16Index)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (utf16Index <= 0) return utf16Index;
        if (utf16Index >= text.Length) utf16Index = text.Length;

        var documentOffset = 0;
        var currentUtf16Index = 0;
        var isLastCharCarriageReturn = false;

        foreach (Rune rune in text.EnumerateRunes())
        {
            if (currentUtf16Index >= utf16Index) break;

            if (rune.Value is '\r')
            {
                isLastCharCarriageReturn = true;
                documentOffset++;
                currentUtf16Index += rune.Utf16SequenceLength;
                continue;
            }

            if (rune.Value is '\n')
            {
                currentUtf16Index += rune.Utf16SequenceLength;
                if (isLastCharCarriageReturn)
                {
                    isLastCharCarriageReturn = false;
                    continue;
                }
                documentOffset++;
                continue;
            }

            isLastCharCarriageReturn = false;
            documentOffset++;
            currentUtf16Index += rune.Utf16SequenceLength;
        }

        return documentOffset;
    }

    /// <summary>
    /// 将文档字符偏移转换为 UTF-16 字符串索引（即 string 中的下标）。
    /// </summary>
    /// <param name="text">原始文本（使用 UTF-16 编码）。</param>
    /// <param name="documentOffset">文档字符偏移。</param>
    /// <returns>对应的 UTF-16 索引（string 下标）。</returns>
    public static int ConvertDocumentOffsetToUtf16Index(string text, DocumentOffset documentOffset)
    {
        int offset = documentOffset.Offset;
        ArgumentNullException.ThrowIfNull(text);
        if (offset <= 0) return offset;
        if (offset >= text.Length) return text.Length;

        var currentDocumentOffset = 0;
        var currentUtf16Index = 0;
        var isLastCharCarriageReturn = false;

        foreach (Rune rune in text.EnumerateRunes())
        {
            if (currentDocumentOffset >= offset) return currentUtf16Index;

            if (rune.Value is '\r')
            {
                isLastCharCarriageReturn = true;
                currentDocumentOffset++;
                currentUtf16Index += rune.Utf16SequenceLength;
                continue;
            }

            if (rune.Value is '\n')
            {
                currentUtf16Index += rune.Utf16SequenceLength;
                if (isLastCharCarriageReturn)
                {
                    isLastCharCarriageReturn = false;
                    continue;
                }
                currentDocumentOffset++;
                continue;
            }

            isLastCharCarriageReturn = false;
            currentDocumentOffset++;
            currentUtf16Index += rune.Utf16SequenceLength;
        }

        return currentUtf16Index;
    }

    /// <summary>
    /// 计算 UTF-16 范围内的文档字符长度。
    /// </summary>
    /// <param name="text">原始文本。</param>
    /// <param name="utf16Start">UTF-16 起始索引（含）。</param>
    /// <param name="utf16Length">UTF-16 长度（char 数）。</param>
    /// <returns>对应的文档字符长度。</returns>
    public static int GetDocumentLength(string text, int utf16Start, int utf16Length)
    {
        var utf16EndExclusive = utf16Start + utf16Length;
        return ConvertUtf16IndexToDocumentOffset(text, utf16EndExclusive)
             - ConvertUtf16IndexToDocumentOffset(text, utf16Start);
    }
}
```

**关键设计决策**：
- `ConvertDocumentOffsetToUtf16Index` 的 `documentOffset` 参数使用 `DocumentOffset` 类型而非 `int`，确保类型明确
- 三个方法的实现直接取自现有已验证逻辑（`TextRunPropertySetter.GetDocumentCharOffset` + `DocumentHighlighterTestHelper`），向前兼容
- 放在 Core 的 `Utils` 命名空间，与 `TextCharObjectCreator` 同级，方便发现

### 第二层：`DocumentOffset` 添加工厂方法

在 `DocumentOffset` 上添加静态工厂方法，让创建时有更自然的意图表达：

```csharp
// 在 DocumentOffset 上添加
/// <summary>
/// 根据原始文本中的 UTF-16 索引创建文档偏移量。
/// 自动处理代理对字符（如 emoji）和 \r\n 折叠。
/// </summary>
/// <param name="text">原始文本（使用 UTF-16 编码）。</param>
/// <param name="utf16Index">UTF-16 索引。</param>
public static DocumentOffset FromUtf16Index(string text, int utf16Index)
    => new(TextIndexConverter.ConvertUtf16IndexToDocumentOffset(text, utf16Index));
```

使用对比：

```csharp
// 之前（容易出错）：
new DocumentOffset(innerCodeSpan.Start)  // innerCodeSpan.Start 是 UTF-16 偏移，不是文档偏移

// 之后（意图清晰）：
DocumentOffset.FromUtf16Index(markdownText, innerCodeSpan.Start)
```

**为什么不在构造函数上做**：`DocumentOffset(int)` 已被广泛使用且语义固定（即传入的是文档偏移值），不改动它避免破坏现有调用。新增静态工厂是 `TimeSpan.FromSeconds` 式设计，清晰表达"这是一个带转换的构造"。

### 第三层：`TextEditorCore` 扩展方法作为便捷入口

对于手上已有 `TextEditorCore` 实例的上层开发者，最自然的调用方式是扩展方法。这与现有的 `TextEditorCoreTextExtensions` 模式一致：

```csharp
// 在 LightTextEditorPlus.Core/API/Extensions_/TextEditorCoreTextExtensions.cs 中添加
/// <summary>
/// 根据原始文本中的 UTF-16 索引创建文档偏移量。
/// 自动处理代理对字符（如 emoji）和 \r\n 折叠。
/// </summary>
/// <param name="textEditorCore">文本编辑器核心。</param>
/// <param name="text">原始文本（使用 UTF-16 编码）。</param>
/// <param name="utf16Index">UTF-16 索引，即 string[index] 的 index。</param>
/// <returns>对应的文档字符偏移。</returns>
public static DocumentOffset CreateDocumentOffsetFromUtf16Index(
    this TextEditorCore textEditorCore, string text, int utf16Index)
    => DocumentOffset.FromUtf16Index(text, utf16Index);
```

**为什么只保留一个方法**：`CreateDocumentOffsetFromUtf16Index` 返回 `DocumentOffset`，调用方可以直接用于 `Selection`、`CaretOffset` 等 API。如果只需要 `int` 值，通过 `.Offset` 属性即可获取。不需要额外提供一个只返回 `int` 的 `ConvertUtf16IndexToDocumentOffset` 重载——两个方法本质是同一个转换，只是返回值包装不同。

**为什么用扩展方法而非 `partial class TextEditor`**：
- `TextEditorCoreTextExtensions` 已有先例（`GetText`、`GetRunList` 等）
- 扩展方法放在 Core 项目中，所有平台（WPF/Avalonia/Skia）无需额外引用即可使用
- 不需要在 `Build/Shared/API/` 中新增 partial 方法，减少跨平台共享代码的维护面

对于外部高亮器作者来说：

```csharp
// 之前（容易出错）：
var offset = new DocumentOffset(innerCodeSpan.Start); // 错了！innerCodeSpan.Start 是 UTF-16 偏移

// 之后（意图清晰）：
var offset = textEditorCore.CreateDocumentOffsetFromUtf16Index(markdownText, innerCodeSpan.Start);
```

### 方案对比总结

| 方案 | 角色 | 目标用户 |
|---|---|---|
| `TextIndexConverter`（第一层） | 权威实现，所有转换逻辑唯一出口 | 所有项目（Core 内 + Highlighters + 测试 + 外部扩展） |
| `DocumentOffset.FromUtf16Index`（第二层） | 语义化工厂方法，防呆 | 任何创建 `DocumentOffset` 的代码 |
| `TextEditorCore` 扩展方法（第三层） | 就近入口，降低发现成本 | 持有 `TextEditorCore` 实例的开发者 |

---

## 附：类型安全包装 `Utf16StringIndex` 深度探索

用户希望在编译器层面防止 UTF-16 索引和文档偏移混淆。以下是对 `Utf16StringIndex` 类型安全包装的详细分析。

### 设计目标

```csharp
// 理想效果：
new DocumentOffset(utf16Index)  // ← 编译器报错！utf16Index 是 Utf16StringIndex
DocumentOffset.FromUtf16Index(text, utf16Index)  // ← 编译器允许，转换清晰
```

### 方案 A：`Utf16StringIndex` 仅作为文档标签

```csharp
/// <summary>
/// 表示 UTF-16 字符串中的一个索引位置。
/// 仅作为语义标记，不能隐式转换为 int 或 DocumentOffset。
/// </summary>
public readonly struct Utf16StringIndex
{
    public int Value { get; }
    public Utf16StringIndex(int value) => Value = value;
    public override string ToString() => Value.ToString();
}
```

然后 `DocumentOffset.FromUtf16Index` 接收它：

```csharp
public static DocumentOffset FromUtf16Index(string text, Utf16StringIndex utf16Index)
    => new(TextIndexConverter.ConvertUtf16IndexToDocumentOffset(text, utf16Index.Value));
```

**实际效果**：

```csharp
int utf16Pos = span.Start;                          // 来自 Markdig
DocumentOffset.FromUtf16Index(text, utf16Pos);      // ✅ int 重载，仍可用
DocumentOffset.FromUtf16Index(text, new Utf16StringIndex(utf16Pos)); // ✅ 类型包装版

new DocumentOffset(utf16Pos);                       // ❓ 编译通过！DocumentOffset 有 implicit operator int→DocumentOffset
```

### 核心障碍：`DocumentOffset` 的隐式转换

`DocumentOffset` 目前有：

```csharp
public static implicit operator DocumentOffset(int offset)  // int → DocumentOffset
public static implicit operator int(DocumentOffset offset)  // DocumentOffset → int
```

这意味着 `new DocumentOffset(anyInt)` **永远能编译通过**，`Utf16StringIndex` 无法阻止这一点。除非移除 `implicit operator int → DocumentOffset`，但这是**大规模破坏性变更**。

### 结论：不引入 `Utf16StringIndex`

UTF-16 索引与文档偏移的混淆是一个**少数场景**才会遇到的问题——主要出现在高亮器作者对接外部解析库（Markdig、ColorCode）时。绝大多数上层开发者操作的是文档内部的 `Selection`/`CaretOffset`/`DocumentOffset`，不会接触到 UTF-16 索引。

引入 `Utf16StringIndex` 作为一等类型会：
- 污染 `DocumentOffset` 的 API 表面（新增构造函数重载、工厂方法重载）
- 增加所有开发者的认知负担（又多了一个"索引类型"需要理解）
- 无法真正阻止错误（隐式转换绕过了类型检查）

**替代方案已足够**：`DocumentOffset.FromUtf16Index(string, int)` 的命名本身就是最好的"类型标记"——它明确告诉读者"这是从 UTF-16 索引构造的"。配合 `TextIndexConverter` 作为权威实现，已能覆盖所有实际场景。分析器方案虽然技术上可行，但维护成本与收益不成比例，不推荐。

---

## 实施计划

### 1. 新增 `TextIndexConverter` 公开工具类

- **文件**：`LightTextEditorPlus.Core/Utils/TextIndexConverter.cs`
- **内容**：三个公开方法（`ConvertUtf16IndexToDocumentOffset`、`ConvertDocumentOffsetToUtf16Index`、`GetDocumentLength`）
- **单元测试**：在 `LightTextEditorPlus.Core.Tests` 中新增 `TextIndexConverterTests`，覆盖：
  - 纯 ASCII 文本（1:1）
  - 含单个 emoji（💡）的文本
  - 含多个 emoji 的文本
  - `\r\n` 折叠
  - 混合 emoji + `\r\n`
  - `\uFFFC`（内联元素占位符）
  - 边界条件（索引为 0、索引为 text.Length、空字符串）

### 2. `DocumentOffset` 新增 `FromUtf16Index` 工厂方法

- **文件**：`LightTextEditorPlus.Core/Document/Segments/DocumentOffset.cs`

### 3. `TextEditorCore` 新增扩展方法

- **文件**：`LightTextEditorPlus.Core/API/Extensions_/TextEditorCoreTextExtensions.cs`
- **内容**：`CreateDocumentOffsetFromUtf16Index(this TextEditorCore, string, int)` 扩展方法

### 4. 替换现有内部调用

| 文件 | 原调用 | 替换为 |
|---|---|---|
| `MarkdownDocumentHighlighter.cs` | `ConvertUtf16IndexToDocumentOffset`（私有 static） | `TextIndexConverter.ConvertUtf16IndexToDocumentOffset` |
| `TextRunPropertySetter.cs` | `GetDocumentCharOffset`（私有） | `TextIndexConverter.ConvertUtf16IndexToDocumentOffset` |
| `DocumentHighlighterTestHelper.cs` | `GetDocumentOffsetFromUtf16Index`（internal） | `TextIndexConverter.ConvertUtf16IndexToDocumentOffset` |
| `MarkdownDocumentHighlighter.cs` | `new DocumentOffset(innerCodeSpan.Start)` | `DocumentOffset.FromUtf16Index(text, index)` |

### 5. 更新知识库文档

- **`Docs/Knowledge/领域与技术参考.md`** 3.4 节：补充 `TextIndexConverter` 的使用说明
- **`Docs/Knowledge/README.md`**：新增本分析文档的索引条目

### 6. 验证

- `dotnet test -c Release` 全量通过
- 现有 `MarkdownCodeBlockHighlightingTests` 全部通过（确保重构不引入回归）
