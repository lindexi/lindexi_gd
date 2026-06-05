# 补充 XML 与 Markdown 代码块高亮测试

## 背景与目标

本次任务是对 `LightTextEditorPlus.Highlighters.Avalonia.UnitTests` 中 XML 相关单元测试进行补强。

用户指出当前 XML 的单元测试覆盖不足，希望参考 JSON 测试的组织方式，补充：

- 单行 XML 场景；
- 多行、多层嵌套 XML 场景；
- 带属性、中文内容、注释、自闭合标签等场景；
- Markdown 中 XML 代码块的高亮场景；
- 断言不能只验证“未抛异常”或“文本没变”，而要明确验证着色结果；
- 采用测试驱动开发模式推进，即先补测试，再依据失败结果收敛断言。

## 关键文件与模块

- `LightTextEditorPlus.Highlighters.Avalonia.UnitTests/XmlCodeDocumentHighlighterTests.cs`
- `LightTextEditorPlus.Highlighters.Avalonia.UnitTests/MarkdownCodeBlockHighlightingTests.cs`
- `LightTextEditorPlus.Highlighters.Avalonia.UnitTests/DocumentHighlighterTestHelper.cs`
- `LightTextEditorPlus.Highlighters/OtherCodeDocumentHighlighter.cs`
- `LightTextEditorPlus.Highlighters/MarkdownDocumentHighlighter.cs`

## 主要决策与原因

1. 新增独立 `XmlCodeDocumentHighlighterTests`
   - 参考 `JsonCodeDocumentHighlighterTests` 的组织方式单独建立 XML 测试文件；
   - 让 XML 场景不再只散落在 `OtherCodeDocumentHighlighterTests` 中的一两个基础用例里。

2. XML 着色断言分为“两层”
   - 第一层：对稳定 token 做明确断言；
   - 第二层：对整段文本或关键 token 与独立 XML 高亮结果做逐字符前景色对比。

3. 不把 ColorCode 对 XML 的不稳定语义硬编码进测试
   - 在 TDD 过程中，最初直接把部分 XML 值、注释、文本内容绑定到固定 `ScopeType`，出现了失败；
   - 失败说明这部分在当前实现/底层库中的语义并不稳定；
   - 因此对这些 token 改为“必须与独立 XML 高亮器结果一致”，既能明确验证着色，也不会把底层偶然行为误写成仓库契约。

4. Markdown 中 XML 代码块继续沿用“与独立高亮一致”的测试模式
   - 这是当前仓库已经采用并验证有效的模式；
   - 对 Markdown 场景，重点约束代码块内部着色结果与独立 XML 高亮保持一致，并额外挑选稳定 token 做显式检查。

## 修改点摘要

- 新增 `XmlCodeDocumentHighlighterTests.cs`
  - 覆盖单行标签与数字内容；
  - 覆盖属性、中文属性值、中文文本；
  - 覆盖多行、多层嵌套、XML 声明；
  - 覆盖注释与自闭合标签；
  - 覆盖重复高亮与切换文本后的更新行为；
  - 保留前后台渲染不抛异常测试。

- 扩展 `MarkdownCodeBlockHighlightingTests.cs`
  - 新增多行 XML 代码块测试；
  - 新增多个 XML 代码块夹杂普通 Markdown 文本的测试；
  - 对代码块内部关键 token 做独立高亮结果对齐验证。

## 验证方式（构建/测试/手工验证）

- 定向测试
  - 运行 `XmlCodeDocumentHighlighterTests` 与 `MarkdownCodeBlockHighlightingTests`
  - 结果：52 通过，0 失败

- Release 单测项目
  - 运行：`dotnet test -c Release .\LightTextEditorPlus.Highlighters.Avalonia.UnitTests\LightTextEditorPlus.Highlighters.Avalonia.UnitTests.csproj`
  - 结果：360 通过，0 失败，0 跳过

- 整体构建
  - 使用工作区构建校验
  - 结果：成功

## 后续建议

- 如果后续继续提升 XML 高亮语义，优先先补独立 XML 高亮测试，再补 Markdown 代码块映射测试；
- 对于底层高亮器在 XML 文本节点、注释、属性值上的语义差异，不建议直接假设固定 `ScopeType`，除非仓库明确将其定义为行为契约；
- 可以考虑后续再为 HTML/XML/XAML 这类同族语言补一层共享测试辅助方法，减少重复的“独立高亮对齐”断言代码。
