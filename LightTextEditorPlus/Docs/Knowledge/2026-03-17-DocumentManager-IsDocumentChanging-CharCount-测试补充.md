# 背景与目标

本次任务是在 `DocumentManager` 新增 `IsDocumentChanging` 属性后，补充对应单元测试，并同时验证 `CharCount` 在文档变更过程与变更完成后的行为。

# 关键文件与模块

- `LightTextEditorPlus.Core/Document/DocumentManagers_/DocumentManager_/DocumentManager.cs`
- `Tests/LightTextEditorPlus.Core.Tests/Document/DocumentManagers/DocumentManagerTests.cs`

# 主要决策与原因

- 采用事件驱动断言（`DocumentChanging` / `DocumentChanged`）来验证 `IsDocumentChanging` 的阶段性状态。
- 在事件回调内直接读取 `CharCount`，验证“变更中读取到旧值、变更后读取到新值”的行为。
- 增加样式变更场景，验证仅样式变化时 `CharCount` 保持不变。

# 修改点摘要

在 `DocumentManagerTests` 中新增 `IsDocumentChangingAndCharCount` 合约测试组，包含 3 个用例：

1. 追加文本场景：
   - `DocumentChanging` 阶段 `IsDocumentChanging == true`
   - `DocumentChanging` 阶段 `CharCount` 为变更前值
   - `DocumentChanged` 阶段 `IsDocumentChanging == false`
   - `DocumentChanged` 阶段 `CharCount` 为变更后值

2. 样式变更场景（`SetRunProperty`）：
   - 变更过程与变更后均验证 `IsDocumentChanging` 状态
   - `CharCount` 在两个阶段都保持不变

3. 删除文本场景：
   - `DocumentChanging` 阶段读取到删除前 `CharCount`
   - `DocumentChanged` 阶段读取到删除后 `CharCount`

# 验证方式（构建/测试/手工验证）

执行命令：

- `dotnet test Tests/LightTextEditorPlus.Core.Tests/LightTextEditorPlus.Core.Tests.csproj --filter "DocumentManagerTests.IsDocumentChangingAndCharCount"`

结果：

- 总计 3，失败 0，成功 3，跳过 0。

# 后续建议

- 可继续补充 `Backspace` 与 `Delete` 单独入口下的同类阶段性断言，增强编辑入口覆盖面。
- 可补充跨段编辑（含换行）下 `CharCount` 与事件阶段状态验证，进一步保障复杂文本结构场景。