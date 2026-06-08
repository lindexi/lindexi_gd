# Markdown 代码块 UTF-16 索引到文档偏移转换 Bug 分析

## 背景与目标

Markdown 文档中包含 emoji（如 `💡`）等代理对字符时，后续的 XML 代码块着色位置出现整体偏移，导致注释色 `#ff579a4c` 出现在错误位置，实际位置显示为纯文本黑色 `#ff000000`。

目标：定位根因，编写复现测试，规划修复方案。

## 根因分析

### 索引体系

项目中存在两套字符索引体系：

| 索引类型 | 说明 | `💡` 长度 | `\r\n` 长度 |
|---------|------|----------|------------|
| UTF-16 索引 | .NET `string` 中 `char` 的位置，Markdig `SourceSpan` 使用此索引 | 2 | 2 |
| 文档字符偏移 | TextEditor 内部使用的偏移，`DocumentOffset` 使用此索引 | 1 | 1 |

### 着色流程

```
Markdig 解析 Markdown 文本
  → 产生 FencedCodeBlock，Span 为 UTF-16 索引
  → 提取 innerCodeSpan（代码块内容的 UTF-16 范围）
  → ApplyHighlightSegment:
      new DocumentOffset(innerCodeSpan.Start)  ← 这里直接用 UTF-16 索引当作文档偏移！
      TextEditorColorCode 内部:
        TextRunPropertySetter.StartOffset = innerCodeSpan.Start  (UTF-16)
        SourceSpanToSelection 时:
          start = StartOffset + GetDocumentCharOffset(span.Start)
          GetDocumentCharOffset 将内部代码文本的 UTF-16 → 文档偏移（局部转换正确）
          但 StartOffset 仍是 UTF-16，不是文档偏移！
```

### 偏移量

当代码块前存在一个代理对字符 `💡` 时：
- Markdig 给的 `innerCodeSpan.Start` = N（UTF-16 索引）
- 正确的文档偏移应为 N-1（因为 `💡` 在 UTF-16 中多占 1 个 char）
- 实际传给 `DocumentOffset` 的是 N，比正确值大 1

结果是整个代码块的着色向右偏移 1 个字符。

### 为什么之前的测试没发现

之前的 `AssertMarkdownXmlHighlight` 辅助方法使用 `IndexOf`（UTF-16 索引）计算位置：
- `codeStart = markdown.IndexOf(code, ...)` → UTF-16 索引
- `tokenStart = GetOccurrenceStart(code, token, ...)` → UTF-16 索引
- `codeStart + tokenStart` → UTF-16 索引
- 传给 `AssertScopeColor(textEditor, start, ...)` → 被当作文档偏移

生产代码偏大 1，测试代码也偏大 1，两者抵消，测试通过。实际上两个偏移都是错的，但错误一致。

用 `AssertCodeBlockMatchesStandaloneHighlight`（逐字符比较独立编辑器 vs Markdown 编辑器）才能暴露真正的偏移。

## 关键文件与模块

- `LightTextEditorPlus.Highlighters/MarkdownDocumentHighlighter.cs` - `ApplyHighlightSegment` 方法，Bug 所在
- `LightTextEditorPlus.Highlighters/TextEditorColorCode.cs` - `FillCodeColor` 桥接 ColorCode 和 TextEditor
- `LightTextEditorPlus.Highlighters/TextRunPropertySetter.cs` - `GetDocumentCharOffset` 已有 UTF-16→文档偏移转换
- `LightTextEditorPlus.Highlighters.Avalonia.UnitTests/MarkdownCodeBlockHighlightingTests.cs` - 复现测试

## 现有复现测试

两个测试构成对照：

1. **`ApplyHighlight_HeadingWithEmojiBeforeXmlCodeBlock_ColorsMatchStandaloneXml`** ❌ 失败
   - Markdown 标题含 `💡`，后跟 XML 代码块
   - 用 `AssertCodeBlockMatchesStandaloneHighlight` 比较独立 XML 着色与 Markdown 内 XML 着色
   - 失败：期望 `#ff579a4c`（注释色），实际 `#ff000000`（纯文本黑色）

2. **`ApplyHighlight_HeadingWithoutEmojiBeforeXmlCodeBlock_ColorsMatchStandaloneXml`** ✅ 通过
   - 同上，但标题不含 `💡`
   - 验证了没有代理对字符时着色正确

## 修复方向

### 方案

在 `MarkdownDocumentHighlighter.ApplyHighlightSegment` 中，将 `innerCodeSpan.Start` 从 UTF-16 索引转换为文档字符偏移后再传入 `DocumentOffset`。

```csharp
// 修复前：
var colorCode = new TextEditorColorCode(_textEditor, 
    new DocumentOffset(codeBlockHighlightSnapshot.InnerCodeSpan.Start), 
    codeBlockHighlightSnapshot.InnerCodeText);

// 修复后：
var documentOffset = ConvertUtf16ToDocumentOffset(markdownText, codeBlockHighlightSnapshot.InnerCodeSpan.Start);
var colorCode = new TextEditorColorCode(_textEditor, 
    new DocumentOffset(documentOffset), 
    codeBlockHighlightSnapshot.InnerCodeText);
```

### 转换方法

`TextRunPropertySetter.GetDocumentCharOffset` 已经实现了 UTF-16 → 文档偏移转换，可以直接复用其逻辑。但需要以整个 `markdownText`（而非内部代码文本）作为输入。

需要在 `MarkdownDocumentHighlighter` 或 `TextRunPropertySetter` 中新增或暴露一个静态方法：

```csharp
/// <summary>
/// 将 UTF-16 字符串索引转换为文档字符偏移。
/// 文档字符偏移中，\r\n 和代理对字符各算 1 个字符。
/// </summary>
internal static int ConvertUtf16IndexToDocumentOffset(string text, int utf16Index)
{
    if (string.IsNullOrEmpty(text) || utf16Index <= 0)
        return utf16Index;

    if (utf16Index >= text.Length)
        utf16Index = text.Length;

    var documentOffset = 0;
    var currentUtf16Index = 0;
    var isLastCharCarriageReturn = false;

    foreach (Rune rune in text.EnumerateRunes())
    {
        if (currentUtf16Index >= utf16Index)
            break;

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
```

### 影响范围

此修复同样影响 `CodeBlockHighlightSnapshot.InnerCodeSpan`，不仅 XML 代码块，所有语言代码块（JSON、C# 等）在 Markdown 标题含 emoji 时都可能受影响。修复后应添加多语言的回归测试。

## 验证方式

1. 修复后运行 `ApplyHighlight_HeadingWithEmojiBeforeXmlCodeBlock_ColorsMatchStandaloneXml`，应通过
2. 运行全部 `MarkdownCodeBlockHighlightingTests`，确保无回归
3. 考虑补充 JSON、C# 代码块 + emoji 标题的测试

## 后续建议

- 统一项目中 UTF-16 索引和文档偏移的使用约定，避免隐式转换
- `TextRunPropertySetter.GetDocumentCharOffset` 当前是 `private`，考虑提取为 `internal static` 工具方法
