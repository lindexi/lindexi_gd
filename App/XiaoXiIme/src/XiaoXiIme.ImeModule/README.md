# XiaoXiIme.ImeModule

小希输入法进程内 IME 模块。职责：Native AOT 发布、导出 IME ABI 入口、接收系统调用、通过 IPC 转发输入事件并写回必要 IME 上下文。

当前已具备最小 IMM 转换消息骨架：

- `ImeExports` 保持非托管导出外壳，具体逻辑转发到可测试的托管运行时。
- `ImeModuleRuntime` 负责按键转换、调用 `ImeHostBridge`，并缓存最近一次 `ImeSessionSnapshot`，供 `ImeProcessKey` 基于组合态判断是否吃键。
- `ImeTransMsgBuilder` 把提交文本或组合态转换为最小 `TRANSMSG` 序列。
- `ImeTransMsgWriter` 把托管生成的消息写入 `ImeToAsciiEx` 传入的 `TRANSMSGLIST` 缓冲区。

后续仍需实现真实 IMM 输入上下文锁定、组合字符串内存布局、候选列表写回和 `.ime` 注册加载验证。
