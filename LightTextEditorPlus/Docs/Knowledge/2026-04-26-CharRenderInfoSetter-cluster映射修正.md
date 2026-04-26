# CharRenderInfoSetter cluster 映射修正

## 背景与目标

`CharRenderInfoSetter` 需要把 HarfBuzz 输出的 `GlyphCluster` 回填到 `CharData`。原实现直接按 glyph 遍历顺序递增 `charDataListIndex`，默认认为 `GlyphCluster` 与 `CharData` 序号一一对应。

这个假设在两个场景下不成立：

- emoji 等 `Rune` 会展开为多个 UTF-16 code unit，但仍只对应一个 `CharData`
- 从右到左文本经过 HarfBuzz shape 后，输出 glyph 的 `GlyphCluster` 可能按倒序排列

目标是让 `CharRenderInfoSetter` 正确处理以上两类情况，并避免越界异常。

## 关键文件与模块

- `LightTextEditorPlus.Skia/Platform/Utils/CharRenderInfoSetter.cs`
- `LightTextEditorPlus.Core/Utils/ReadOnlyCharDataList_/ReadOnlyCharDataListExtension.cs`
- `LightTextEditorPlus.Skia/Platform/SkiaCharInfoMeasurer.cs`
- `Tests/LightTextEditorPlus.Avalonia.Tests/CharRenderInfoSetterTest.cs`

## 主要决策与原因

1. 不修改 `CharData` 的设计
   - `CharData` 表示“人类感知的字符”，可以直接承载一个 `Rune`，这个抽象是正确的
   - 问题不在模型，而在 `GlyphCluster` 到 `CharData` 的映射算法

2. 以 UTF-16 展开结果作为中间映射层
   - `ToRenderCharSpan` 会把每个 `CharData` 的 `Rune` 展开成 1 或 2 个 UTF-16 code unit
   - HarfBuzz 返回的 `GlyphCluster` 基于这个 UTF-16 输入，因此需要先建立 UTF-16 索引到 `CharData` 索引的关系

3. 用逻辑顺序的 cluster 边界判断连字范围
   - 先提取全部 cluster 并去重排序
   - 再用“当前 cluster 起点”和“下一个逻辑 cluster 起点”之间的范围，计算当前 glyph 覆盖哪些 `CharData`
   - 这样可以同时兼容 LTR 和 RTL，不依赖 glyph 的输出顺序

## 修改点摘要

- 在 `CharRenderInfoSetter` 中新增 `CharData` 到 UTF-16 起始索引表构建逻辑
- 在 `CharRenderInfoSetter` 中新增逻辑 cluster 去重排序逻辑
- 用二分查找把 `GlyphCluster` 映射回正确的 `CharData` 索引
- 用逻辑 cluster 边界替换原先的 `charDataListIndex + 1` 判断，修复 emoji 和 RTL 下的错误连字判定
- 强化 `SetCharDataInfoWithEmoji` 测试断言，验证 emoji 与空格分别落到正确 glyph
- 新增 RTL 倒序 cluster 测试，验证 `GlyphCluster = 2,1,0` 时仍能回填到正确的 `CharData`

## 验证方式（构建/测试/手工验证）

- 运行 `CharRenderInfoSetterTest`
  - 结果：2 个测试通过
- 运行 `dotnet test Tests/LightTextEditorPlus.Avalonia.Tests/LightTextEditorPlus.Avalonia.Tests.csproj -c Release --filter FullyQualifiedName~LightTextEditorPlus.Avalonia.Tests.CharRenderInfoSetterTest`
  - 结果：命令执行成功
- 运行解决方案构建
  - 结果：构建成功

## 后续建议

- 如果后续要支持一个 `CharData` 对应多个 glyph 的更复杂 shape 结果，需要进一步确认 `CharDataInfo` 是否足以表达该关系
- 可继续补充包含真实阿拉伯文或希伯来文输入的端到端测量测试，覆盖 RTL + 字体 shaping 的完整链路
