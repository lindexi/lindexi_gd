# Markdown 代码块高亮对齐 ColorCode 并补充独立测试

## 背景与目标

本次任务聚焦两件事：

1. `OtherCodeDocumentHighlighter` 不再使用大量正则表达式对 ColorCode 的结果做二次修补；
2. 为 Markdown 中的代码块高亮补充独立测试文件，并以测试驱动方式验证各语言代码块的最终着色结果。

目标不是继续弥补 ColorCode 缺失的语义能力，而是让仓库内“独立代码高亮”和“Markdown 代码块高亮”保持一致，并修复 Markdown 代码块高亮中的真实实现错误。

## 关键文件与模块

- `LightTextEditorPlus.Highlighters/OtherCodeDocumentHighlighter.cs`
- `LightTextEditorPlus.Highlighters/MarkdownDocumentHighlighter.cs`
- `LightTextEditorPlus.Highlighters.Avalonia.UnitTests/MarkdownCodeBlockHighlightingTests.cs`
- `LightTextEditorPlus.Highlighters.Avalonia.UnitTests/OtherCodeDocumentHighlighterTests.cs`
- `LightTextEditorPlus.Highlighters.Avalonia.UnitTests/DocumentHighlighterTestHelper.cs`

## 主要决策与原因

1. 删除 `OtherCodeDocumentHighlighter` 中的大量正则修补逻辑
   - 用户明确要求不要为 ColorCode 缺失的能力做修补；
   - 因此保留纯 `ColorCodeCodeHighlighter` 路径，移除语言特定正则覆盖层。

2. Markdown 代码块测试不再手写各语言 scope 预期
   - ColorCode 对不同语言能稳定输出的 scope 粒度并不完全一致；
   - 如果直接在 Markdown 测试里手写“某 token 必须是什么 scope”，本质上又回到了人工定义跨语言语义；
   - 更合理的约束是：Markdown 代码块里的同一段代码，其最终字符颜色应与对应独立高亮器一致。

3. 修复 Markdown fenced code block 内部代码范围偏移错误
   - 新测试暴露出 Markdown 代码块高亮和独立高亮结果存在系统性错位；
   - 根因是 `MarkdownDocumentHighlighter` 计算 `innerCodeSpan` 时把换行边界处理错了；
   - 修复后，Markdown 中代码块的着色结果才能与独立高亮器对齐。

4. OtherCode 单测改为验证“与直接 ColorCode 结果一致”
   - 旧测试中不少断言依赖已删除的正则修补行为；
   - 调整后继续保持细粒度逐字符颜色断言，但基准改为直接 ColorCode 结果。

## 修改点摘要

- `OtherCodeDocumentHighlighter.cs`
  - 删除正则字段、辅助匹配方法及 `ApplyStableLanguageOverrides` 调用；
  - 保留 PlainText 基底 + `ColorCodeCodeHighlighter` 的实现路径。

- `MarkdownDocumentHighlighter.cs`
  - 修复 fenced code block 的内部代码起止范围计算；
  - 跳过开头和结尾的换行，保证传入代码高亮器的文本与应用偏移一致。

- `MarkdownCodeBlockHighlightingTests.cs`
  - 新增独立测试文件；
  - 覆盖 C# 别名、无语言代码块，以及 javascript/js、python/py、java、cpp/c、sql、xml、json、html、css、typescript/ts、fsharp、vb、php 等语言；
  - 逐字符比较 Markdown 代码块与独立高亮器的最终前景色。

- `DocumentHighlighterTestHelper.cs`
  - 新增 `AssertSameForegroundColors`，用于逐字符比较两个编辑器的最终前景色。

- `OtherCodeDocumentHighlighterTests.cs`
  - 调整为以直接 ColorCode 高亮结果为基准的细粒度一致性测试；
  - 保留构造、重复调用、切换文本、渲染调用等行为测试。

## 验证方式（构建/测试/手工验证）

- 定向测试
  - 运行 `MarkdownCodeBlockHighlightingTests`
  - 结果：22 通过，0 失败

- 定向测试
  - 运行 `MarkdownCodeBlockHighlightingTests` + `OtherCodeDocumentHighlighterTests`
  - 结果：43 通过，0 失败

- Release 单测项目
  - `dotnet test -c Release .\LightTextEditorPlus.Highlighters.Avalonia.UnitTests\LightTextEditorPlus.Highlighters.Avalonia.UnitTests.csproj`
  - 结果：322 通过，0 失败，0 跳过

- 整体构建
  - `run_build`
  - 结果：成功

## 后续建议

- 如果后续继续扩展 Markdown 代码块支持，优先复用“Markdown 结果应与独立高亮结果一致”的测试模式，而不是在 Markdown 测试里复制各语言的语义断言。
- 若未来要提升某语言高亮精度，应优先改进底层 `ColorCodeCodeHighlighter` 或替换对应语言高亮器，而不是再次在 `OtherCodeDocumentHighlighter` 里堆叠正则补丁。
- 当前 Release 测试通过，但项目仍存在 XML 文档注释类警告；若后续整理公共 API，可单独处理这些警告，避免与功能修改混在一次任务中。
