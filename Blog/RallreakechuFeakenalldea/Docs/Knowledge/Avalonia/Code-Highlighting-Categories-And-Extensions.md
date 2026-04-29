# 代码着色分类与扩展名路由

## 场景

`SimpleWrite` 现在不再只针对 Markdown 代码块里的 C# 做着色，而是把文档高亮策略拆成三类：

- Markdown：保留 Markdown block、标题、链接与代码块背景等专用逻辑；
- C#：整篇 `.cs` 文档直接走现有 Roslyn 风格高亮；
- 其他：整篇代码文档和 Markdown fenced code 内的其他语言，统一走 `ColorCode.Core`。

同时，新建无文件关联文档或未知扩展名文档，默认仍按 Markdown 处理。

## 实现约定

1. 文档级高亮分类只保留 `Markdown`、`CSharp`、`Other` 三类，不继续向上暴露更多分类。
2. 文件扩展名到高亮分类的决策统一放在 `DocumentHighlighterSelector`，避免在 `ViewModel` 和 `View` 中散落判断。
3. `SimpleWriteTextEditor` 只接收 `DocumentHighlightDefinition`，由其内部创建具体 `IDocumentHighlighter`。
4. `EditorViewModel` 在两处切换文档高亮：
   - 创建 `TextEditor` 时，按当前 `EditorModel.FileInfo` 设置；
   - 另存为成功后，按新扩展名重新设置。
5. 未知扩展名、空扩展名和无文件关联文档，一律回退到 Markdown 高亮，不走纯文本兜底。
6. Markdown fenced code 内：
   - `csharp` / `cs` / `C#` / `dotnet` 继续走现有 C# 高亮；
   - 其他支持语言转成 `ColorCode` 的 language id 后做着色；
   - 未识别语言标记保持普通样式。
7. 其他分类的语言能力以 `ColorCode.Core` 实际支持为准；当前已经接入的语言包括：
   - ASAX、ASHX、ASPX、ASPX(C#)、ASPX(VB.NET)
   - C/C++
   - CSS
   - F#
   - Fortran
   - Haskell
   - HTML
   - Java
   - JavaScript
   - JSON
   - Koka
   - Markdown
   - MATLAB
   - PHP
   - PowerShell
   - Python
   - SQL
   - TypeScript
   - VB.NET
   - XML

## 迁移补充

- 高亮实现现在归属 `LightTextEditorPlus.Highlighters`，不再放在 `SimpleWrite.Business.TextEditors.Highlighters`。
- 迁移时不要继续依赖 `SimpleWriteTextEditor`，高亮器统一只依赖库内公开的 `TextEditor` API。
- 与高亮直接相关的辅助类型也需要一起迁移，例如：
  - `TextRunPropertySetter`
  - `LineReader`
  - `MarkdownUrlInfo` 等高亮器内部快照类型
- `SimpleWrite` 侧只保留业务接入：
  - `SimpleWriteTextEditor` 负责创建和切换 `IDocumentHighlighter`
  - `SimpleWriteTextEditorHandler` 继续基于 `MarkdownDocumentHighlighter.UrlInfoList` 处理 Ctrl+点击链接
- 迁移后优先检查两类残留：
  - 命名空间是否还停留在 `SimpleWrite.Business.*`
  - 辅助类型是否还散落在应用工程，导致库内编译缺少依赖

## 适用场景

- 新增文件类型与代码着色支持
- 调整扩展名到高亮分类的映射
- 排查 Markdown 代码块或整篇代码文档为何没有着色
