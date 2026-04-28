# Markdown URL 高亮与命中信息记录

## 场景

为 `SimpleWrite` 的 `MarkdownDocumentHighlighter` 增加正文 URL 高亮时，需要同时满足：

- URL 文本使用常见的蓝色下划线样式；
- 不影响代码块的既有高亮逻辑；
- 为后续点击命中打开链接保留原始 URL 与文本范围信息。

## 实现约定

1. 普通文本样式不要每次先对全文重置；应在遍历 Markdown block 时按 span 增量设置。
2. 如果当前 block 与上一个 block 的 `SourceSpan` 不连续，需要把中间缺口区间恢复为普通 `RunProperty`，这样空段落和被跳过的内容也能回到默认样式。
2. 链接样式使用 `SkiaTextRunProperty`：
   - `Foreground` 设为蓝色；
   - `DecorationCollection` 使用 `UnderlineTextEditorDecoration.Instance`。
3. URL 命中信息使用与 `_codeBlockList` 相同的思路单独缓存，建议记录：
   - 文本范围 `SourceSpan`
   - 原始链接字符串 `Url`
4. URL 扫描应限定在正文段落内，避免误伤代码块内容。
5. 扫描结果需要去掉常见尾随标点（如 `.`、`，`、`）`），避免把正文标点算进链接。
6. 对 Markdown block 的样式操作建议保留一份按顺序排列的快照；当新的 span、样式操作列表、代码块高亮输入与上次一致时，可直接跳过重复设置。
7. 代码块背景绘制不能只看“当前段落是否属于代码块”，还要看代码段落在渲染段落列表里是否连续；一旦遇到普通段落或不连续的代码段落索引，就应先结束上一块背景合并，避免把中间空白段也涂成代码背景。

## 适用场景

- Markdown 编辑器中的裸链接识别
- 后续补充 Ctrl+Click / 点击打开链接
- 需要在文本源文件里直接做链接命中测试的编辑器功能
