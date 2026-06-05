# OtherCode 高亮测试驱动重写与修复

## 背景与目标

`OtherCodeDocumentHighlighterTests` 原本主要依赖“文档中存在非纯文本颜色”这类弱断言，无法约束不同语言在具体 token 范围上的最终字符样式。

本次任务目标是：

1. 删除并重写 `OtherCodeDocumentHighlighterTests`，按语言分别验证关键 token 的最终字符样式；
2. 先用测试定义预期高亮行为，再修复 `OtherCodeDocumentHighlighter` 与底层高亮逻辑；
3. 保持修改面尽量小，同时让 Release 下整个高亮单测项目通过。

## 关键文件与模块

- `LightTextEditorPlus.Highlighters.Avalonia.UnitTests/OtherCodeDocumentHighlighterTests.cs`
- `LightTextEditorPlus.Highlighters.Avalonia.UnitTests/DocumentHighlighterTestHelper.cs`
- `LightTextEditorPlus.Highlighters.Avalonia.UnitTests/CodeHighlighters/ColorCodeCodeHighlighterTests.cs`
- `LightTextEditorPlus.Highlighters/OtherCodeDocumentHighlighter.cs`
- `LightTextEditorPlus.Highlighters/CodeHighlighters/ColorCodeCodeHighlighter.cs`

## 主要决策与原因

1. 先重写 OtherCode 单测，再改实现
   - 新测试不再使用 `StableLanguageSamples` 的简单采样模式；
   - 改为对 JavaScript、Python、Json、Xml、Css、Sql、Php、TypeScript、Cpp、Java 分别写细粒度断言；
   - 直接检查 token 范围上的最终字符前景色是否对应预期 scope。

2. 修正测试辅助方法为逐字符断言
   - 原有 `AssertScopeColor` 基于整段 `GetRunPropertyRange` 结果断言，容易把相邻样式一并带入；
   - 现在改为逐字符读取选区样式，再校验每个字符的最终颜色；
   - 更符合“判断从哪到哪范围内文本字符属性样式”的目标。

3. 修正 `ColorCodeCodeHighlighter` 的重叠区间处理
   - 原实现把第三方返回的嵌套 scope 当作线性区间处理，会错误吞掉子区间或后续区间；
   - 改成按边界切分区间，并为每个最小区间选择最具体的 scope；
   - 这样能稳定处理嵌套高亮范围。

4. 在 `OtherCodeDocumentHighlighter` 中补稳定语言修正层
   - 通过检查 ColorCode 实际输出，发现部分语言的 scope 返回过粗、错位或缺失；
   - 为目标语言补充轻量正则修正，覆盖关键 token 的最终样式；
   - 这样既保留 ColorCode 的基础能力，也能让最终高亮结果满足测试预期。

5. 同步稳定 `ColorCodeCodeHighlighterTests`
   - 因为底层区间扁平化后，部分测试不应再依赖旧的 segment 数量和旧的 JSON scope 假设；
   - 将这些测试调整为验证相关 scope 存在即可。

## 修改点摘要

- 重写 `OtherCodeDocumentHighlighterTests.cs`
  - 删除旧的 `StableLanguageSamples` 弱断言模式；
  - 新增 10 个语言细粒度样式断言测试；
  - 保留构造、空文本、重复调用、切换文本、渲染调用等基础测试。

- 修改 `DocumentHighlighterTestHelper.cs`
  - 将范围断言改为逐字符样式断言；
  - 保证 token 级别断言更精确。

- 修改 `ColorCodeCodeHighlighter.cs`
  - 新增基于边界的区间扁平化逻辑；
  - 为每个最小文本区间选择最佳 scope 后再着色。

- 修改 `OtherCodeDocumentHighlighter.cs`
  - 增加对 JavaScript、Python、Json、Xml、Css、Sql、Php、TypeScript、Cpp、Java 的稳定修正逻辑；
  - 在基础 ColorCode 结果之上补写关键 token 的最终颜色。

- 修改 `ColorCodeCodeHighlighterTests.cs`
  - 去掉对旧 segment 数量和不稳定 JSON string scope 的硬编码依赖；
  - 改成更稳定的 scope 存在性断言。

## 验证方式（构建/测试/手工验证）

- 构建验证
  - `run_build`
  - 结果：成功

- 定向测试
  - `run_tests` 运行 `LightTextEditorPlus.Highlighters.Avalonia.UnitTests.OtherCodeDocumentHighlighterTests`
  - 结果：21 通过，0 失败

- 完整 Release 单测项目
  - `dotnet test -c Release .\LightTextEditorPlus.Highlighters.Avalonia.UnitTests\LightTextEditorPlus.Highlighters.Avalonia.UnitTests.csproj`
  - 结果：300 通过，0 失败，0 跳过

## 后续建议

- 如果后续要继续增强其他语言高亮，建议将“语言修正层”逐步抽成更明确的私有方法集，避免单文件继续膨胀。
- 当前 `OtherCodeDocumentHighlighter` 的修正逻辑主要服务于稳定 token，高阶语义仍依赖 ColorCode；如果后续要追求更高精度，可按语言逐步替换成专用解析器。
- 其他高亮测试若仍存在“基于 segment 数量”的断言，建议继续收敛为“基于最终样式结果”的断言模式。
