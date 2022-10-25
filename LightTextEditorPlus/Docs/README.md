# 文本库

## 调试机制

### 设置调试模式

可以调用 TextEditorCore 的 SetInDebugMode 方法，让单个文本对象进入调试模式。进入调试模式之后，将会有更多的输出信息，和可能抛出 TextEditorDebugException 调试异常

如期望对所有的文本都进入调试模式，可以调用 `TextEditorCore.SetAllInDebugMode` 静态方法

请不要在发布版本开启调试模式，开启调试模式之后，将会影响文本的性能

### 布局原因

通过 TextEditorCore 的 `_layoutUpdateReasonManager` 字段即可了解到框架内记录的触发布局的原因