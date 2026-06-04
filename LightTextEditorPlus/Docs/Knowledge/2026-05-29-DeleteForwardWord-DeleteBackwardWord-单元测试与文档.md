# DeleteForwardWord / DeleteBackwardWord 单元测试与文档补充

## 背景与目标

在上一轮对话中完成了 `DeleteForwardWord`（Ctrl+Backspace）和 `DeleteBackwardWord`（Ctrl+Delete）两个 API 的实现。本轮任务是为这两个方法补充单元测试和使用说明文档。

## 关键文件与模块

- `LightTextEditorPlus.Core/API/TextEditorCore.Edit.cs` - 实现代码，包含 `DeleteForwardWord` 和 `DeleteBackwardWord` 方法
- `LightTextEditorPlus.Core/API/Extensions_/TextEditorSelectionExtension.cs` - 包含 `GetCurrentCaretOffsetWord` 扩展方法，用于获取当前光标所在的单词范围
- `LightTextEditorPlus.Core/Layout/LayoutUtils/WordDividers/GetCaretWordHelper.cs` - 分词核心逻辑，先尝试匹配"单词"（非标点连续字符），若为空则匹配"同类标点符号连续"
- `Tests/LightTextEditorPlus.Core.Tests/API/TextEditorEditTest.cs` - 新增 15 个测试用例
- `Docs/使用说明文档.md` - 新增 Backspace、Delete、DeleteForwardWord、DeleteBackwardWord 四个删除相关 API 的文档

## 主要决策与原因

1. **测试放在 `TextEditorEditTest.cs`**：该文件已包含 `Backspace`、`Delete`、`Remove` 等删除相关测试，按词删除作为删除类 API 的扩展，放在同一文件中保持一致。

2. **测试用例覆盖**：
   - 有选中内容时删除选中
   - 光标在边界（文档开头/结尾）的无操作行为
   - 光标在单词中间：删除从光标到单词边界的内容
   - 光标在单词边界：退回到单字符删除（Backspace/Delete）
   - 中英文混合场景
   - 标点符号连续场景
   - 空文本场景

3. **文档放在「文本字符处理」小节**：在 `EditAndReplaceRun` 之后、`### 字符属性设置` 之前，按 API 使用频率排列文档条目。

## 修改点摘要

### 测试文件
- `Tests/LightTextEditorPlus.Core.Tests/API/TextEditorEditTest.cs`：新增 `DeleteForwardWordTest` 方法（7 个测试用例）和 `DeleteBackwardWordTest` 方法（8 个测试用例）

### 文档文件
- `Docs/使用说明文档.md`：在 `### 文本字符处理` 小节结尾新增 Backspace、Delete、DeleteForwardWord、DeleteBackwardWord 四个 API 的使用文档

## 验证方式

- 构建：`dotnet build` 通过
- 测试：`dotnet test` 运行 15 个新增测试用例，全部通过
- 文档：手动查看 `Docs/使用说明文档.md` 确认排版和代码示例正确

## 后续建议

- 目前尚未添加 `TextFeatures` 功能开关控制这两个 API，如需统一管理可后续补充
- 分词逻辑目前是无语言文化的（`GetCaretWordHelper.GetCaretCurrentWord`），后续可考虑接入语言文化分词提升准确性
