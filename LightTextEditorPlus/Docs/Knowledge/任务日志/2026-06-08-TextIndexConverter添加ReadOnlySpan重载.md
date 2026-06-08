# TextIndexConverter 添加 ReadOnlySpan&lt;char&gt; 重载

## 背景与目标

`TextIndexConverter`（`LightTextEditorPlus.Core/Utils/TextIndexConverter.cs`）的三个公共方法原先只接受 `string` 参数。作为基础库，应提供 `ReadOnlySpan<char>` 重载，避免调用方已有 `char[]` 或 `ReadOnlySpan<char>` 时强制分配 `string`。

## 关键文件与模块

| 文件 | 改动 |
|---|---|
| `LightTextEditorPlus.Core/Utils/TextIndexConverter.cs` | 重构：`string` 重载通过 `text.AsSpan()` 转发到 Span 版本，核心逻辑仅在 Span 版本中存在 |
| `LightTextEditorPlus.Core/Document/Segments/DocumentOffset.cs` | 新增 `FromUtf16Index(ReadOnlySpan<char>, int)` 重载 |
| `LightTextEditorPlus.Core/API/Extensions_/TextEditorCoreTextExtensions.cs` | 新增 `CreateDocumentOffsetFromUtf16Index(ReadOnlySpan<char>, int)` 重载 |
| `Tests/LightTextEditorPlus.Core.Tests/Utils/TextIndexConverterTest.cs` | 新增 Span 版本的测试用例（`_ReadOnlySpan` 后缀方法） |

## 主要决策

1. **保持 `string` 重载**：现有调用方（如 `TextRunPropertySetter`）始终传入 `string`，不破坏兼容性
2. **内部逻辑下沉到 Span**：`string` 重载通过 `text.AsSpan()` 转发，消除重复代码
3. **三层入口全部加 Span**：`TextIndexConverter` → `DocumentOffset.FromUtf16Index` → `TextEditorCore` 扩展方法，三层完整覆盖
4. **Span 版本无 null 检查**：`ReadOnlySpan<char>` 是 ref struct，不可能为 null

## 修改点摘要

- `TextIndexConverter.cs`：`string` 重载变为转发调用（3 行），核心逻辑迁移到 `ReadOnlySpan<char>` 版本
- `DocumentOffset.cs`：新增 Span 重载 `FromUtf16Index(ReadOnlySpan<char>, int)`
- `TextEditorCoreTextExtensions.cs`：新增 Span 重载，补充 `using System;`
- 测试：新增 `ConvertUtf16IndexToDocumentOffset_ReadOnlySpan`、`ConvertDocumentOffsetToUtf16Index_ReadOnlySpan`、`GetDocumentLength_ReadOnlySpan`，覆盖 ASCII、emoji、`\r\n` 折叠、边界条件、char[] 一致性

## 验证方式

- `dotnet test -c Release Tests/LightTextEditorPlus.Core.Tests/` 全部 485 测试通过（含新增的 34 个 TextIndexConverter 测试）
- `.GetErrors()` 编译无错误

## 后续建议

- 高亮器项目（`LightTextEditorPlus.Highlighters`）中如果有直接使用 `char[]` 或 `ReadOnlySpan<char>` 的场景，可改用 Span 重载减少分配