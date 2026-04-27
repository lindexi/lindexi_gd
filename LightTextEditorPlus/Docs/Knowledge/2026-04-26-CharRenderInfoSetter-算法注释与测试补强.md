# CharRenderInfoSetter 算法注释与测试补强

## 背景与目标

`CharRenderInfoSetter` 已经能处理 `GlyphCluster` 与 `CharData` 数量不一致的问题，但算法中存在较多隐含前提，阅读成本高。
本次协作的目标有两部分：

- 为 `CharRenderInfoSetter` 补充关键注释，解释 UTF-16 映射、cluster 去重、二分边界与连字区间计算逻辑
- 扩充 `CharRenderInfoSetterTest`，覆盖 emoji、连字、RTL、重复 cluster 等更复杂组合

## 关键文件与模块

- `LightTextEditorPlus.Skia/Platform/Utils/CharRenderInfoSetter.cs`
- `Tests/LightTextEditorPlus.Avalonia.Tests/CharRenderInfoSetterTest.cs`
- `LightTextEditorPlus.Core/Utils/ReadOnlyCharDataList_/ReadOnlyCharDataListExtension.cs`

## 主要决策与原因

1. 保持现有算法主体不变，仅补充说明
   - 复核后没有发现新的功能性缺陷
   - 当前实现已经能正确处理 emoji 的 UTF-16 长度差异，以及 RTL 的倒序 cluster

2. 明确“末尾哨兵”的语义
   - `charDataUtf16StartIndexSpan` 的最后一个槽位不是字符，而是整个 run 的 UTF-16 结束位置
   - 这样最后一个字符也能统一使用 `[start, nextStart)` 区间逻辑，不需要额外末尾分支

3. 明确 cluster 去重的目的
   - 去重并不是多余操作
   - 它用于应对“多个 glyph 属于同一 cluster”的情况，避免把重复边界当成新的逻辑区间

4. 用更接近实际组合的测试来约束算法
   - 仅测试“emoji + 空格”不足以覆盖真实问题
   - 新测试重点覆盖 emoji 位置变化、连字和 RTL 的交叉组合

## 修改点摘要

### `CharRenderInfoSetter`

- 补充注释说明 `GlyphCluster` 基于 UTF-16，而不是 `CharData` 索引
- 补充注释说明 `charDataUtf16StartIndexSpan` 最后一个槽位的哨兵作用
- 补充注释说明 cluster 排序是为恢复逻辑顺序，兼容 RTL
- 补充注释说明 cluster 去重是为处理重复 cluster 的 glyph
- 补充注释说明循环中为什么要分别查询“当前字符起点”和“下一个逻辑边界”
- 补充注释说明：
  - `FindClusterIndex` 中的 `Length - 1` 是二分右边界的最后一个有效元素索引
  - `FindCharDataIndexByUtf16Index` 中的 `Length - 2` 是排除哨兵位后的最后一个真实字符索引

### `CharRenderInfoSetterTest`

新增或强化以下场景：

- `emoji + 空格`
- `emoji + 多个普通字符`
- `普通字符 + emoji + 普通字符`
- `emoji + 连字 + 普通字符`
- `RTL 两字符`
- `RTL 三字符`
- `RTL + emoji`
- `重复 cluster 的多个 glyph`

同时提取测试辅助方法，减少样板代码。

## 验证方式（构建/测试/手工验证）

- 运行命令：
  - `dotnet test Tests/LightTextEditorPlus.Avalonia.Tests/LightTextEditorPlus.Avalonia.Tests.csproj -c Release --filter FullyQualifiedName~LightTextEditorPlus.Avalonia.Tests.CharRenderInfoSetterTest`
- 测试结果：
  - `CharRenderInfoSetterTest` 共 8 个测试，全部通过
- 构建验证：
  - 执行解决方案构建，结果成功

## 后续建议

- 如果后续出现“一个 `CharData` 对应多个 glyph 且需要保留全部 glyph 信息”的需求，需要重新评估 `CharDataInfo` 仅保存单个 `GlyphIndex` 的模型是否足够
- 可继续增加真实阿拉伯文或希伯来文文本的端到端测量测试，验证 shape 输出与 `CharRenderInfoSetter` 映射在真实 RTL 字体下的行为
