# Native AOT RunProperty 导出与 PInvoke 补充

## 背景与目标

在 `LightTextEditorPlus.Skia.AotLayer` 中，已有 `RunProperty` 的创建、释放和部分设置能力。目标是在现有实现基础上：

- 补齐 `SkiaTextRunProperty` 各属性的设置接口；
- 增加对应的属性获取接口；
- 在 `TextEditorWrapper` 增加完整的 PInvoke 对接，提供托管侧便捷调用。

## 关键文件

- `LightTextEditorPlus.Skia.AotLayer/NativeTextEditorRunPropertyExporter.cs`
- `LightTextEditorPlus.Skia.AotLayer/NativeTextEditors/TextEditorWrapper.cs`
- `LightTextEditorPlus.Skia/Document/Property_/RunProperty_/SkiaTextRunProperty.cs`
- `LightTextEditorPlus.Skia.AotLayer/NativeTextEditors/ErrorCode.cs`

## 主要决策

1. **统一返回错误码，不通过异常做跨边界错误传递**
   - 继续沿用 `ErrorCode` 约定，保持 Native API 一致性。

2. **`UnmanagedCallersOnly` Getter 改用指针输出，而非 `out/ref`**
   - 构建时出现 `CS8977`，说明 `UnmanagedCallersOnly` 签名不能使用 `out/ref/in`；
   - 最终将 getter 导出签名改为 `T*`/`IntPtr` 指针写回。

3. **字符串读取采用“两段式”**
   - `GetRunPropertyFontName` 支持先传空指针获取长度，再按长度读取内容；
   - `TextEditorWrapper` 中提供 `out string` 便捷封装。

4. **装饰集合跨边界使用位标志**
   - 使用 `Underline/Strikethrough/EmphasisDots` 三个 bit flag，降低互操作复杂度。

## 修改摘要

### `NativeTextEditorRunPropertyExporter`

- 新增设置导出：
  - `SetRunPropertyFontSize`
  - `SetRunPropertyOpacity`
  - `SetRunPropertyForegroundColor`
  - `SetRunPropertyBackgroundColor`
  - `SetRunPropertyStretch`
  - `SetRunPropertyFontWeight`
  - `SetRunPropertyFontStyle`
  - `SetRunPropertyIsBold`（过时）
  - `SetRunPropertyIsItalic`（过时）
  - `SetRunPropertyFontVariant`
  - `SetRunPropertyDecorationFlags`
- 新增获取导出（指针返回）：
  - `GetRunPropertyFontName`
  - `GetRunPropertyFontSize`
  - `GetRunPropertyOpacity`
  - `GetRunPropertyForegroundColor`
  - `GetRunPropertyBackgroundColor`
  - `GetRunPropertyStretch`
  - `GetRunPropertyFontWeight`
  - `GetRunPropertyFontStyle`
  - `GetRunPropertyIsBold`（过时）
  - `GetRunPropertyIsItalic`（过时）
  - `GetRunPropertyFontVariant`
  - `GetRunPropertyDecorationFlags`
- 抽取通用更新/读取辅助逻辑，减少重复的取对象与错误码分支。

### `TextEditorWrapper`

- 增加 RunProperty PInvoke 对接：
  - 创建/释放
  - 全量设置接口
  - 全量获取接口
- getter 底层改用 `IntPtr` 指针参数对接导出函数；
- 对外提供托管友好的 `out` 包装方法（包括 `GetRunPropertyFontName(out string)`）。

## 验证记录

- 执行构建验证：`run_build`
- 结果：**生成成功**。
- 过程中修复了 `CS8977`（`UnmanagedCallersOnly` 禁止 `out/ref/in`）后再次构建通过。

## 风险与后续建议

- 当前 `CreateRunPropertyFromStyle` 返回 `long`（可承载负错误码），而后续多数接口使用 `uint runPropertyId`；建议后续补一个统一的 `RunPropertyId` 托管封装，减少调用方误用。
- 如需支持更多 `Foreground` 画刷类型（如渐变），建议扩展 Native 协议（当前 getter 以实色读取为主）。
- 可补充一组互操作测试（创建 -> 设置 -> 获取 -> 释放）覆盖核心路径与错误码一致性。
