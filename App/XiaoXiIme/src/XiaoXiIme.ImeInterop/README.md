# XiaoXiIme.ImeInterop

小希输入法 Win32/IME 互操作项目。职责：定义 Win32、IMM、IME 常量、结构体和必要 P/Invoke。该项目不承载输入法业务逻辑。

当前包含最小 IME 导出和 IMM 写回骨架所需定义：

- IME 能力位、组合字符串标志和基础 IME 窗口消息常量。
- `ImeInquireInfo`、`TransMsg`、`TransMsgList` 和句柄包装类型。
- `ImmLockIMC`、`ImmUnlockIMC`、`ImmLockIMCC`、`ImmUnlockIMCC`、`ImmGenerateMessage` 的 P/Invoke 声明。

后续若实现完整上下文写回，应继续把 `INPUTCONTEXT`、`COMPOSITIONSTRING`、候选列表等 Win32 布局定义放在本项目。
