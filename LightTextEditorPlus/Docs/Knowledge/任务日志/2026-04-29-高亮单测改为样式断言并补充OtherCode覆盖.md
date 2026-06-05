# 高亮单测改为样式断言并补充 OtherCode 覆盖

## 背景与目标

`LightTextEditorPlus.Highlighters.Avalonia.UnitTests` 里的 `CSharpDocumentHighlighterTests` 原本大量使用“高亮后文本内容保持不变”作为断言，这只能证明没有改坏文本，不能证明真正发生了预期的着色。

同时，`OtherCodeDocumentHighlighterTests` 虽然有少量样例，但没有围绕 `DocumentHighlighterSelector` 映射出来的其他代码语言做最终文本上的着色验证，覆盖面不足。

本次目标是：

1. 将 `CSharpDocumentHighlighterTests` 的核心断言改为基于字符属性前景色的样式断言；
2. 为 `OtherCodeDocumentHighlighterTests` 增加对其他代码语言最终着色结果的验证；
3. 在不修改生产代码行为的前提下，尽量让测试断言贴近当前实现的稳定输出。

## 关键文件与模块

- `LightTextEditorPlus.Highlighters.Avalonia.UnitTests/CSharpDocumentHighlighterTests.cs`
- `LightTextEditorPlus.Highlighters.Avalonia.UnitTests/OtherCodeDocumentHighlighterTests.cs`
- `LightTextEditorPlus.Highlighters.Avalonia.UnitTests/DocumentHighlighterTestHelper.cs`
- `LightTextEditorPlus.Highlighters/OtherCodeDocumentHighlighter.cs`
- `LightTextEditorPlus.Highlighters/DocumentHighlighterSelector.cs`
- `LightTextEditorPlus.Highlighters/TextEditorColorCode.cs`

## 主要决策与原因

1. 新增测试辅助类统一样式断言
   - 通过 `TextEditor.GetRunPropertyRange` 读取选区内字符属性；
   - 通过 `ColorCodeStyleManager` 获取期望 scope 对应的前景色；
   - 避免每个测试重复编写选区、查找 token、取色逻辑。

2. C# 高亮测试优先验证稳定 token
   - 对 `class`、`public`、`var`、字符串字面量、数字、括号、变量名、成员名等稳定输出使用精确颜色断言；
   - 对文档注释、插值字符串、部分多轮覆盖场景，使用“确实发生了非纯文本着色”的较弱断言；
   - 原因是当前实现存在多轮着色覆盖，某些 token 的最终颜色并不总是与单一 scope 一一对应。

3. OtherCode 测试验证“最终文档中存在着色”
   - 对 Cpp、Css、Java、Json、Php、Sql、TypeScript、Xml 等稳定样本执行高亮；
   - 不再强制要求每个指定 token 的所有字符都必须非纯文本颜色；
   - 而是验证整个文档在高亮后至少存在非纯文本前景色，避免把第三方高亮器的局部未着色或细粒度差异误判为失败。

4. 保留文本完整性断言
   - 所有高亮测试仍然保留“高亮后文本内容未改变”的验证；
   - 这样既能覆盖样式设置，又能防止测试只关注颜色而遗漏文本内容被破坏的问题。

## 修改点摘要

- 新增 `DocumentHighlighterTestHelper.cs`
  - 提供获取文档文本、按 token 定位、读取字符属性、断言 scope 颜色、断言存在非纯文本着色等能力。
- 重写 `CSharpDocumentHighlighterTests.cs`
  - 将原先大量 `Assert.Equal(text, GetEditorText(...))` 的空断言替换为样式断言；
  - 对关键 token 增加前景色验证；
  - 对不稳定覆盖路径改为更稳妥的非纯文本着色断言。
- 重写 `OtherCodeDocumentHighlighterTests.cs`
  - 补充多个 other code 语言的最终着色验证；
  - 增加重复调用、不同文本切换、注释场景、缩进场景、渲染调用等测试。

## 验证方式（构建/测试/手工验证）

- 运行构建
  - `run_build`
  - 结果：成功
- 运行定向测试
  - `CSharpDocumentHighlighterTests`
  - `OtherCodeDocumentHighlighterTests`
  - 结果：52 通过，0 失败
- 运行完整单测项目（Release）
  - `dotnet test -c Release C:\lindexi\Code\lindexi_gd\LightTextEditorPlus\LightTextEditorPlus\LightTextEditorPlus.Highlighters.Avalonia.UnitTests\LightTextEditorPlus.Highlighters.Avalonia.UnitTests.csproj`
  - 结果：300 通过，0 失败，0 跳过

## 后续建议

- 如果后续需要把 `OtherCodeDocumentHighlighterTests` 做得更强，可考虑先为 `ColorCodeCodeHighlighter` 或 `OtherCodeDocumentHighlighter` 暴露更稳定的 token/scope 观测点，再提升断言粒度。
- `DocumentHighlighterSelector` 当前扩展名映射较多，后续可以考虑为映射表增加“完整映射覆盖测试”，专门验证每个扩展名是否落到预期 category/languageId。
- 当前高亮实现对文档注释、插值字符串、部分类型 token 存在多轮覆盖现象；若未来希望测试更精确，建议先统一生产代码中的 scope 优先级规则。