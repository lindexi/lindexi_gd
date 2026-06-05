# ColorCodeCodeHighlighter XML 着色修复

## 背景与目标

用户报告 XML 着色器对某些 XML 着色效果错误（如带渐变背景的 SVG/XAML 风格 XML）。按照 TDD 思想，先编写单元测试复现问题，再修复代码。

## 关键文件与模块

- `LightTextEditorPlus.Highlighters\CodeHighlighters\ColorCodeCodeHighlighter.cs` — 修复目标：通用 ColorCode 高亮器
- `LightTextEditorPlus.Highlighters\CodeHighlighters\XmlCodeHighlighter.cs` — 已有的专用 XML 高亮器
- `LightTextEditorPlus.Highlighters\CodeHighlighters\JsonCodeHighlighter.cs` — 已有的专用 JSON 高亮器
- `LightTextEditorPlus.Highlighters\OtherCodeDocumentHighlighter.cs` — 已正确路由 XML/JSON 到专用高亮器
- `LightTextEditorPlus.Highlighters.Avalonia.UnitTests\XmlCodeDocumentHighlighterTests.cs` — 新增测试

## 主要决策与原因

**根因分析：** `ColorCodeCodeHighlighter` 是通用高亮器，对所有语言（包括 XML）直接使用 ColorCode.Core 库解析。但 ColorCode.Core 对 XML 的高亮效果很差（产生全黑/无高亮）。而 `OtherCodeDocumentHighlighter` 已经在 `ApplyHighlight` 中正确地将 XML/JSON 路由到 `XmlCodeHighlighter`/`JsonCodeHighlighter`。

**修复方案：** 在 `ColorCodeCodeHighlighter.ApplyHighlight` 中增加与 `OtherCodeDocumentHighlighter` 相同的路由逻辑：当 `LanguageId` 为 XML 或 JSON 时，优先使用专用高亮器。

**为什么不在调用方修复：** `ColorCodeCodeHighlighter` 被多处使用（如 `MarkdownDocumentHighlighter` 的代码块高亮），在调用方逐一修复容易遗漏。在 `ColorCodeCodeHighlighter` 内部路由可以一劳永逸。

## 修改点摘要

1. **`ColorCodeCodeHighlighter.cs`**：在 `ApplyHighlight` 方法开头添加 XML/JSON 路由逻辑
   - 添加 `_xmlCodeHighlighter` 和 `_jsonCodeHighlighter` 实例字段
   - 在 ColorCode.Core 解析之前，先尝试专用高亮器

2. **`XmlCodeDocumentHighlighterTests.cs`**：新增 3 个测试
   - `ApplyHighlight_XmlWithGradientBackground_HighlightsAllTagsAttributesAndStrings` — 用户提供的 XML 示例（XmlCodeHighlighter 路径）
   - `ApplyHighlight_XmlWithGradientBackground_ColorCodePath_HighlightsAllTagsAttributesAndStrings` — 同上（ColorCodeCodeHighlighter 路径）
   - `ApplyHighlight_SimpleXml_ColorCodePath_ProducesConsistentHighlight` — 简单 XML（ColorCodeCodeHighlighter 路径）

## 验证方式

- 全部 366 个高亮器单元测试通过（`dotnet test -c Release`）
- Core 测试中的 6 个失败是已有的无关失败（`GetSelectionBoundsListTest`、`ReadWordCharCount`）

## 后续建议

- 考虑将 `ColorCodeCodeHighlighter` 和 `OtherCodeDocumentHighlighter` 的 XML/JSON 路由逻辑统一提取，避免重复
- 其他语言（如 HTML）如果 ColorCode.Core 效果不佳，也可以类似地添加专用高亮器路由
