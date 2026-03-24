# Shift + 鼠标点击扩展选区

## 背景与目标

为文本库补充 `Shift + 鼠标点击` 扩展选区能力，并同时覆盖 WPF 与 Avalonia 两个平台的交互入口与单元测试。

## 关键文件与模块

- `Build/Shared/API/Handlers/TextEditorHandler.Shared.cs`
- `LightTextEditorPlus.Wpf/Editing/Handlers_/TextEditorHandler.cs`
- `LightTextEditorPlus.Avalonia/Editing/Handlers_/TextEditorHandler.ava.cs`
- `Tests/LightTextEditorPlus.Tests/TextEditorHandlerTest.cs`
- `Tests/LightTextEditorPlus.Avalonia.Tests/TextEditorHandlerTest.cs`

## 主要决策与原因

1. 将扩展选区逻辑放入共享的 `TextEditorHandler` 单击处理分支中。
   - 原因：WPF 与 Avalonia 的单击命中和选区更新行为本质一致，平台层只负责读取修饰键状态。
2. 扩展选区时使用 `CurrentSelection.StartOffset` 作为锚点。
   - 原因：现有 `Selection` 已保留方向信息，`StartOffset` 更符合“从原锚点扩展到新点击位置”的语义。
3. 扩展选区时强制将 `_isHitSelection` 设为 `false`。
   - 原因：避免 Shift 点击被误判为“点击已有选区后准备拖拽文本”。
4. 测试直接通过派生 `TextEditorHandler` 调用受保护的单击逻辑。
   - 原因：可稳定验证平台项目中的交互行为，而不需要构造复杂的原生鼠标事件对象。

## 修改点摘要

- 为共享单击逻辑新增带 `isExtendSelection` 参数的重载。
- WPF 在鼠标按下时读取 `Keyboard.Modifiers`，将 Shift 状态传入共享逻辑。
- Avalonia 在指针按下时读取 `e.KeyModifiers`，将 Shift 状态传入共享逻辑。
- 新增 WPF 测试：
  - 从当前光标扩展选区
  - 从已有反向选区锚点继续扩展
- 新增 Avalonia 测试：
  - 从当前光标扩展选区
  - 从已有反向选区锚点继续扩展

## 验证方式

- `dotnet test Tests\LightTextEditorPlus.Tests\LightTextEditorPlus.Tests.csproj --filter TextEditorHandlerTest`
- `dotnet test Tests\LightTextEditorPlus.Avalonia.Tests\LightTextEditorPlus.Avalonia.Tests.csproj --filter TextEditorHandlerTest`
- `run_build` 全量构建验证通过

## 后续建议

- 如后续补充 Ctrl/Shift 组合点击、三击选段等行为，建议继续沿用“平台入口只解析输入状态，共享层处理选择语义”的模式。
- 可继续补充真实输入事件级别的集成测试，验证平台事件对象中的修饰键解析与命中测试链路。