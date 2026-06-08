# UI 框架层添加 CreateDocumentOffsetFromUtf16Index 方法

## 背景与目标

Core 层的 `TextIndexConverter`、`DocumentOffset.FromUtf16Index` 和 `TextEditorCoreTextExtensions.CreateDocumentOffsetFromUtf16Index` 已经提供了完整的三层 API（含 `ReadOnlySpan<char>` 重载）。但持有 `TextEditor`/`SkiaTextEditor` 实例的上层开发者仍需通过 `TextEditorCore` 扩展方法才能调用，没有直接的实例方法入口。

目标：在顶层 UI 框架的 `TextEditor`（WPF/Avalonia/MauiGraphics）和 `SkiaTextEditor` 上提供 `CreateDocumentOffsetFromUtf16Index` 实例方法（`string` + `ReadOnlySpan<char>` 重载），并更新使用说明文档。

## 关键文件与模块

| 文件 | 改动 |
|---|---|
| `Build/Shared/API/TextEditor.Edit.Shared.cs` | 在 `#region Text` 区添加两个重载 |
| `LightTextEditorPlus.Skia/API/SkiaTextEditor.Edit.cs` | 添加 `using LightTextEditorPlus.Core;` 和两个重载 |
| `Docs/使用说明文档.md` | 在"获取文本信息"章节末尾添加 UTF-16 索引与文档偏移转换使用说明 |

## 主要决策

1. **方法放置在 `Text` region 中**：`CreateDocumentOffsetFromUtf16Index` 是文本处理相关方法，与 `GetText` 等放在一起符合逻辑
2. **标记 `[TextEditorPublicAPI]`**：与同区域其他方法保持一致
3. **Skia 版本需添加 `using LightTextEditorPlus.Core`**：扩展方法 `CreateDocumentOffsetFromUtf16Index` 定义在 `LightTextEditorPlus.Core` 命名空间下

## 修改点摘要

- `Build/Shared/API/TextEditor.Edit.Shared.cs`：在 `GetText(StringBuilder)` 之后 `#endregion` 之前添加 `CreateDocumentOffsetFromUtf16Index(string, int)` 和 `CreateDocumentOffsetFromUtf16Index(ReadOnlySpan<char>, int)` 两个实例方法
- `LightTextEditorPlus.Skia/API/SkiaTextEditor.Edit.cs`：补充 `using LightTextEditorPlus.Core;` 和 `using LightTextEditorPlus.Core.Document.Segments;`，在 `Remove` 之后添加两个实例方法
- `Docs/使用说明文档.md`：在"获取文本信息"末尾添加转换方法使用示例（三种方式 + Span 重载）

## 验证方式

- `dotnet build` 全量通过

## 后续建议

- 约束文件 `TextEditor.Edit.Shared.txt` 和 `TextEditor.Edit.Input.txt` 当前未添加新方法名，后续如需强制约束可补充
