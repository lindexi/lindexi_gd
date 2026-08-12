# XiaoXiIme.ImeModule

小希输入法进程内 IME 模块。职责：以 Native AOT 形式发布为 `XiaoXiIme.ime`、导出传统 IME ABI 入口、注册 IME UI 窗口类、接收系统调用、通过 IPC 转发输入事件并写回 IMM 输入上下文。

当前已具备经过管理员沙箱真实安装和自动上屏验证的 IMM32 链路：

- `ImeExports` 直接提供非托管导出入口，具体逻辑转发到可测试的托管运行时。
- `ImeUiWindowClass` 使用 P/Invoke 注册 `CS_IME` UI 窗口类，类名保持为满足 `ImeInquire` 缓冲区限制的 `XiaoXiImeUIWnd`。
- `ImeModuleRuntime` 负责按键转换、调用 `ImeHostBridge`，并缓存最近一次 `ImeSessionSnapshot`，供 `ImeProcessKey` 基于组合态判断是否吃键。
- `ImeTransMsgBuilder` 把提交文本或组合态转换为最小 `TRANSMSG` 序列。
- `ImeTransMsgWriter` 把托管生成的消息写入 `ImeToAsciiEx` 传入的 `TRANSMSGLIST` 缓冲区。
- `ImmContextAccessor`、`ImeCompositionContextReader` 和 `ImeCompositionContextWriter` 已实现输入上下文锁定，以及组合字符串、候选信息、引导信息和私有数据的读写。
- x86/x64 双架构安装、HKL 激活、HIMC 打开、`SendInput` 注入 `xx` 和 EDIT 精确上屏“小希”已由管理员沙箱自动验证。

后续工作应聚焦完整输入体验、候选窗口交互和更多应用兼容性，而不是重复实现已经存在的基础 IMM 上下文布局。
