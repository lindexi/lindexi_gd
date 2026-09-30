# XiaoXiIme.ImeInterop

小希输入法 Win32/IME 互操作项目。职责：定义 Win32、IMM、IME 常量、结构体和必要 P/Invoke。该项目不承载输入法业务逻辑。

当前包含已用于真实 IMM32 安装和上屏链路的互操作定义：

- IME 能力位、组合字符串标志、`NotifyIME` 的 `NI_SELECTCANDIDATESTR`（`0x0012`）/`NI_CHANGECANDIDATELIST`/`NI_COMPOSITIONSTR`/`CPS_*` 常量、`ImeConversionList` 的 `GCL_CONVERSION`/`GCL_REVERSECONVERSION`/`GCL_REVERSE_LENGTH` 常量、`ImeEscape` 的 `IME_ESC_QUERY_SUPPORT`/`IME_ESC_IME_NAME` 常量、`ImeConfigure` 的 `IME_CONFIG_REGISTERWORD` 常量、用户词样式常量和基础 IME 窗口消息常量。
- `ImeInquireInfo`、`TransMsg`、`TransMsgList` 和句柄包装类型。
- 与 Windows SDK 字段顺序和跨架构对齐一致的 `InputContext`、`CompositionString`、`CandidateInfo`、`GuideLine` 等布局。
- `ImmLockIMC`、`ImmUnlockIMC`、`ImmLockIMCC`、`ImmUnlockIMCC`、`ImmReSizeIMCC`、`ImmGenerateMessage` 的 P/Invoke 声明。

新增 Win32/IMM 布局时应继续放在本项目，并为 x86/x64 的结构大小和关键字段偏移增加 ABI 测试。
