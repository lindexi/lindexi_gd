# Markdown URL 高亮与命中信息记录

## 场景

为 `SimpleWrite` 的 `MarkdownDocumentHighlighter` 增加正文 URL 高亮时，需要同时满足：

- URL 文本使用常见的蓝色下划线样式；
- 不影响代码块的既有高亮逻辑；
- 为后续点击命中打开链接保留原始 URL 与文本范围信息。

## 实现约定

1. 先对全文恢复普通 `RunProperty`，再对段落中的 URL 追加链接样式。
2. 链接样式使用 `SkiaTextRunProperty`：
   - `Foreground` 设为蓝色；
   - `DecorationCollection` 使用 `UnderlineTextEditorDecoration.Instance`。
3. URL 命中信息使用与 `_codeBlockList` 相同的思路单独缓存，建议记录：
   - 文本范围 `SourceSpan`
   - 原始链接字符串 `Url`
4. URL 扫描应限定在正文段落内，避免误伤代码块内容。
5. 扫描结果需要去掉常见尾随标点（如 `.`、`，`、`）`），避免把正文标点算进链接。

## 适用场景

- Markdown 编辑器中的裸链接识别
- 后续补充 Ctrl+Click / 点击打开链接
- 需要在文本源文件里直接做链接命中测试的编辑器功能
