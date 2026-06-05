# TextEditorCore 内部事件回调重构

## 背景与目标

为了降低 `TextEditorCore` 内部协作中的委托分配与事件订阅开销，将 `TextEditorCore` 与 `DocumentManager`、`CaretManager` 的内部联动从 `event +=` 方式改为构造注入回调接口方式，同时保持对外公开事件语义不变。

## 关键文件与模块

- `LightTextEditorPlus.Core/TextEditorCore.cs`
- `LightTextEditorPlus.Core/Document/DocumentManagers_/DocumentManager_/DocumentManager.cs`
- `LightTextEditorPlus.Core/Carets/CaretManager.cs`
- `LightTextEditorPlus.Core/Primitive/ReadOnlyParagraphList.cs`
- `Tests/LightTextEditorPlus.Core.Tests/TextEditorCoreTest.cs`

## 主要决策与原因

1. 通过 `IDocumentManagerCallback` 与 `ICaretManagerCallback` 替代内部事件订阅，避免内部 `+=` 形成的委托对象和事件链维护成本。
2. `TextEditorCore` 作为回调实现者，继续在回调中转发其公开事件，保证外部 API 行为一致。
3. `ReadOnlyParagraphList` 原先依赖 `DocumentManager.InternalDocumentChanged` 检测枚举失效，改为 `DocumentManager.ChangeVersion` 版本号比对，减少耦合并兼容新机制。
4. 保留 `DocumentManager(TextEditorCore)` 公有构造函数，新增内部重载用于回调注入，避免可访问性冲突并保持兼容性。

## 修改点摘要

- `DocumentManager`
  - 新增内部回调接口 `IDocumentManagerCallback`。
  - 用 `NotifyDocumentChanging/NotifyDocumentChanged` 替代所有 `InternalDocumentChanging/Changed` 调用。
  - 新增 `ChangeVersion`，在文档变更完成时递增。
  - 保留公有构造，增加内部注入构造。
- `CaretManager`
  - 新增内部回调接口 `ICaretManagerCallback`。
  - 将 `InternalCurrentCaretOffsetChanging/Changed`、`InternalCurrentSelectionChanging/Changed` 改为直接回调调用。
- `TextEditorCore`
  - 实现 `IDocumentManagerCallback` 与 `ICaretManagerCallback`。
  - 构造时改为注入回调，不再进行内部事件订阅。
  - 公开事件转发逻辑保持不变。
- `ReadOnlyParagraphList`
  - 移除对 `InternalDocumentChanged` 订阅。
  - 枚举期间通过 `ChangeVersion` 检测并抛出枚举失效异常。
- 测试
  - `TextEditorCoreTest` 新增 `CaretEventArrange`，验证光标变更时公开事件触发顺序和值。

## 验证方式（构建/测试/手工验证）

- 构建验证：`run_build` 成功。
- 单测验证：
  - `dotnet test Tests\LightTextEditorPlus.Core.Tests\LightTextEditorPlus.Core.Tests.csproj --filter "FullyQualifiedName~TextEditorCoreTest.EventArrange|FullyQualifiedName~TextEditorCoreTest.CaretEventArrange"` 通过。

## 后续建议

1. 如需进一步降低分配，可继续评估 `TextEditorCore` 对外公开事件在高频路径的触发模式。
2. 可补充 `ReadOnlyParagraphList` 的并发/嵌套枚举失效场景单测，强化版本号机制的行为边界。
