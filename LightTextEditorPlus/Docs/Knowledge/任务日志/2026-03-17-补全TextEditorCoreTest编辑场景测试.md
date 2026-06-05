# 补全 `TextEditorCoreTest` 编辑场景测试

## 背景与目标

`TextEditorCoreTest` 中多个用例主要覆盖 `AppendText` 路径，缺少在“已有文本内容”前提下调用 `EditAndReplace` 的行为验证。本次目标是补齐关键事件顺序与布局结果的测试覆盖。

## 关键文件

- `Tests/LightTextEditorPlus.Core.Tests/TextEditorCoreTest.cs`
- `Tests/LightTextEditorPlus.Core.Tests/API/Extensions_/TextEditorCoreEditExtensionTests.cs`（参考既有 `EditAndReplace` 测试风格）

## 主要决策

- 保持原有 `ContractTestCase` + 多个中文描述子用例的组织方式，不拆分类和方法。
- 重点补充用户指出的关键点：`CaretEventArrange`、`EventArrange`、`GetDocumentBounds`，并补充 `TestDebugName` 的编辑后可用性。
- 使用 `FixCharSizePlatformProvider` + `UseFixedLineSpacing` 让文档尺寸变化断言更稳定。

## 修改摘要

- 在 `CaretEventArrange` 新增已有文本下 `EditAndReplace` 导致光标变化时的事件顺序测试。
- 在 `EventArrange` 新增已有文本下 `EditAndReplace` 的 `DocumentChanging -> DocumentChanged -> LayoutCompleted` 顺序测试，并校验替换后的文本内容。
- 在 `GetDocumentBounds` 新增已有文本替换后文档尺寸变化测试（宽度增大，高度保持）。
- 在 `TestDebugName` 新增已有文本替换后的调试名可用性测试。

## 验证记录

- 执行：
  - `dotnet test Tests/LightTextEditorPlus.Core.Tests/LightTextEditorPlus.Core.Tests.csproj --filter "FullyQualifiedName~TextEditorCoreTest"`
- 结果：
  - 通过，`TextEditorCoreTest` 共 19 个测试，失败 0。

## 风险与后续建议

- 当前补充集中在 `TextEditorCoreTest`；若后续继续提升覆盖，可对其他仅 `AppendText` 路径的测试类按同样模式补充“已有文本 + `EditAndReplace`”场景。
