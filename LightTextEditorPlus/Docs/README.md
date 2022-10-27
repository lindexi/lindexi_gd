# 文本库

## 各个项目的作用

### LightTextEditorPlus.Core

文本库的平台无关实现，实现了文本的基础排版布局功能。提供给具体平台框架对接的接口，可以在不同的平台框架上，使用具体平台框架的文本渲染引擎提供具体的文本排版布局信息，以及在排版布局完成之后，对接具体平台的渲染

入口类型：TextEditorCore

## 调试机制

### 设置调试模式

可以调用 TextEditorCore 的 SetInDebugMode 方法，让单个文本对象进入调试模式。进入调试模式之后，将会有更多的输出信息，和可能抛出 TextEditorDebugException 调试异常

如期望对所有的文本都进入调试模式，可以调用 `TextEditorCore.SetAllInDebugMode` 静态方法

请不要在发布版本开启调试模式，开启调试模式之后，将会影响文本的性能

### 布局原因

通过 TextEditorCore 的 `_layoutUpdateReasonManager` 字段即可了解到框架内记录的触发布局的原因