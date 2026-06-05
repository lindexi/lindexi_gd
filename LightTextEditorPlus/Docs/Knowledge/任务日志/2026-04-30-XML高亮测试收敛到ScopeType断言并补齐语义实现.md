# XML 高亮测试收敛到 ScopeType 断言并补齐语义实现

## 背景与目标

现有 XML 相关测试主要通过“与独立 ColorCode 结果颜色一致”的方式验证高亮效果：

- `LightTextEditorPlus.Highlighters.Avalonia.UnitTests/XmlCodeDocumentHighlighterTests.cs`
- `LightTextEditorPlus.Highlighters.Avalonia.UnitTests/MarkdownCodeBlockHighlightingTests.cs`

这类测试只能说明颜色结果一致，无法直接约束具体 token 应该对应哪个 `ScopeType`。用户要求参照 Json 测试的方式，把 XML 测试改成直接断言语义 scope，并采用测试驱动开发流程：先写预期断言，再根据失败结果调查并修复实现。

本次任务目标：

1. 将两个 XML 测试文件中的核心 XML 断言改为直接验证 `ScopeType`；
2. 在 Markdown XML 代码块场景中同样验证具体 scope；
3. 若新断言失败，则修复 XML 高亮实现，使其满足这些语义断言；
4. 保持修改面尽量小，并补充必要验证。

## 关键文件与模块

- `LightTextEditorPlus.Highlighters.Avalonia.UnitTests/XmlCodeDocumentHighlighterTests.cs`
- `LightTextEditorPlus.Highlighters.Avalonia.UnitTests/MarkdownCodeBlockHighlightingTests.cs`
- `LightTextEditorPlus.Highlighters.Avalonia.UnitTests/OtherCodeDocumentHighlighterTests.cs`
- `LightTextEditorPlus.Highlighters/OtherCodeDocumentHighlighter.cs`
- `LightTextEditorPlus.Highlighters/MarkdownDocumentHighlighter.cs`
- `LightTextEditorPlus.Highlighters/CodeHighlighters/XmlCodeHighlighter.cs`

## 主要决策与原因

1. 先把 XML 测试改成 `ScopeType` 断言
   - 直接约束标签名、属性名为 `ScopeType.ClassMember`；
   - 属性值为 `ScopeType.String`；
   - 普通文本内容为 `ScopeType.PlainText`；
   - 注释为 `ScopeType.Comment`。
   - 这样测试表达的就是“语义正确”，而不是“颜色刚好一样”。

2. 测试失败后，不回退测试，而是补 XML 语义高亮实现
   - 首轮定向测试全部失败，说明现有 XML 高亮更多依赖通用 ColorCode 结果；
   - 该结果不足以稳定满足当前仓库想要的 XML 语义断言；
   - 因此新增独立 `XmlCodeHighlighter`，仿照 Json 的做法，在文档层先按 XML 结构直接标注 scope，再必要时回退通用 ColorCode。

3. 只做最小 XML 语义识别
   - 支持开始标签、结束标签、处理指令、属性名、带引号属性值、注释、CDATA；
   - 文本节点保留纯文本；
   - 不引入新的公共抽象，也不改现有 `ICodeHighlighter` 设计。

4. 顺手收敛残留的 XML 弱测试
   - 在按仓库要求执行 `dotnet test -c Release` 时，额外暴露出 `OtherCodeDocumentHighlighterTests.ApplyHighlight_XmlAttributesAndContent_HighlightsDetailedScopes` 仍旧依赖旧的颜色对比断言；
   - 因为它验证的是同一条 XML 高亮链路，所以一并改为具体 `ScopeType` 断言，避免 Release 测试回归。

## 修改点摘要

- `XmlCodeDocumentHighlighterTests.cs`
  - 删除 XML 测试里依赖独立 ColorCode 编辑器做逐 token 颜色对比的断言；
  - 改为通过 `AssertXmlHighlight(...)` 直接断言：
    - 标签名/属性名 -> `ScopeType.ClassMember`
    - 属性值 -> `ScopeType.String`
    - 文本节点 -> `ScopeType.PlainText`
    - 注释 -> `ScopeType.Comment`

- `MarkdownCodeBlockHighlightingTests.cs`
  - 将两个 XML 代码块测试改为直接验证 Markdown 中代码块内部 token 的 `ScopeType`；
  - 新增 `AssertMarkdownXmlHighlight(...)` 辅助方法，按代码块偏移定位并断言具体 scope。

- `OtherCodeDocumentHighlighterTests.cs`
  - 将残留 XML 用例改为直接验证 `ScopeType`，去掉旧的颜色对比方式。

- `CodeHighlighters/XmlCodeHighlighter.cs`
  - 新增轻量 XML 语义高亮器；
  - 识别并标记：
    - 标签名、属性名 -> `ClassMember`
    - 属性值 -> `String`
    - 注释 -> `Comment`
    - CDATA -> `DeclarationTypeSyntax`
  - 未命中的正文文本继续保留 PlainText。

- `OtherCodeDocumentHighlighter.cs`
  - 在 JSON 特判之后加入 XML 特判；
  - 当语言为 `LanguageId.Xml` 时优先使用 `XmlCodeHighlighter`。

- `MarkdownDocumentHighlighter.cs`
  - 为 Markdown fenced code block 增加 XML 语言识别后的语义高亮分支；
  - 覆盖 `xml/xaml/axaml/svg` 这些现有已映射到 XML 的语言别名。

## 验证方式（构建/测试/手工验证）

### 定向测试驱动验证

先运行以下 6 个被修改的 XML 测试：

- `XmlCodeDocumentHighlighterTests.ApplyHighlight_SingleLineElementWithNumberContent_HighlightsXmlScopes`
- `XmlCodeDocumentHighlighterTests.ApplyHighlight_XmlAttributesAndChineseText_HighlightsDetailedScopes`
- `XmlCodeDocumentHighlighterTests.ApplyHighlight_MultiLineNestedXml_HighlightsTagsAttributesStringsAndTextContent`
- `XmlCodeDocumentHighlighterTests.ApplyHighlight_XmlWithCommentAndSelfClosingElement_HighlightsCommentAndXmlScopes`
- `MarkdownCodeBlockHighlightingTests.ApplyHighlight_CodeBlockWithMultiLineXml_HighlightsXmlScopes`
- `MarkdownCodeBlockHighlightingTests.ApplyHighlight_MultipleXmlCodeBlocksWithTextBetween_UsesMatchingHighlightForEachBlock`

过程：

1. 先改测试后运行；
2. 首次运行 6 个测试全部失败；
3. 调查后补 `XmlCodeHighlighter` 并接入调用链；
4. 再次运行，6 个测试全部通过。

### Release 测试

按仓库要求执行：

- `dotnet test -c Release C:\lindexi\Code\lindexi_gd\LightTextEditorPlus\LightTextEditorPlus\LightTextEditorPlus.Highlighters.Avalonia.UnitTests\LightTextEditorPlus.Highlighters.Avalonia.UnitTests.csproj`

结果：

- 首次运行发现 1 个残留 XML 测试仍使用旧断言方式并失败；
- 修正 `OtherCodeDocumentHighlighterTests` 后再次运行；
- 最终结果：总计 360，失败 0，成功 360，跳过 0。

### 构建验证

- `run_build`
- 结果：生成成功。

## 后续建议

1. 对结构化语言高亮，优先写“token -> scope”的测试，不再依赖“与另一份颜色结果一致”的间接断言。
2. 如果后续还要增强 XML 高亮，可继续补：
   - 实体引用更细粒度分类；
   - DOCTYPE/声明节点专门 scope；
   - 更严格的错误容忍和不完整标签处理。
3. `JsonCodeHighlighter` 与 `XmlCodeHighlighter` 已经形成同类模式；若后续还要支持 YAML/TOML 等结构化文本，可继续沿用“解析优先、ColorCode 回退”的策略。
