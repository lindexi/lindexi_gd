# XiaoXiIme.ImeModule

小希输入法进程内 IME 模块。职责：以 Native AOT 形式发布为 `XiaoXiIme.ime`、导出传统 IME ABI 入口、注册 IME UI 窗口类、接收系统调用、通过 IPC 转发输入事件并写回 IMM 输入上下文。

当前已具备经过管理员沙箱真实安装和自动上屏基础冒烟验证的 IMM32 链路：

- `ImeExports` 直接提供非托管导出入口，具体逻辑转发到可测试的托管运行时。
- `ImeUiWindowClass` 使用 P/Invoke 注册 `CS_IME` UI 窗口类，类名保持为满足 `ImeInquire` 缓冲区限制的 `XiaoXiImeUIWnd`。
- `ImeModuleRuntime` 负责按键转换、调用 `ImeHostBridge`，并按 HIMC 对应的 `ImeSessionId` 缓存 `ImeSessionSnapshot`，供 `ImeProcessKey` 基于组合态判断是否吃键。`ImeHostBridge` 只在宿主不可达时使用按会话隔离的最小回退，并把 `ImeUiState.IsHostUnavailable` 设为 true；宿主内部词库降级继续走 IPC，由 `IsUsingFallbackDictionary` 报告，不得伪装成桥接不可达。双 package 布局下，`ImeProcessKey` 会吃下全拼 `xiaoxiaimuyi` 或小鹤 `xnxiaimuyi`；同一 HIMC 再经 `ImeToAsciiEx` 连续输入后空格上屏 `XiaoXiIme`，并把 `GCS_COMPSTR`/`GCS_RESULTSTR` 写回 HIMC。`ImeConversionList(GCL_CONVERSION)` 按源串查询候选；`ImeConversionList(GCL_REVERSECONVERSION)` 把 `XiaoXiIme` 反查为规范 reading `xiao xi ai mu yi` 并写入调用方 `CANDIDATELIST`；`ImeConversionList(GCL_REVERSELENGTH)` 只返回该反查缓冲所需字节数、不写目标缓冲，也不改变当前 HIMC 组合。`NotifyIME(NI_SELECTCANDIDATESTR)` 使用 Windows SDK 值 `0x0012`。两个 HIMC 可分别组合 `xiaoxiaimuyi` 与 `aimuyi`。`ImeSelect(select=0)` 与 `ImeSetActiveContext(active=0)` 先取消组合并清空 HIMC，再经 IPC `XiaoXiIme.ResetSession` 丢弃该 HIMC 会话，不写 `GCS_RESULTSTR`；`NotifyIME(NI_COMPOSITIONSTR, CPS_CANCEL)` 只取消组合，不丢会话；`CPS_COMPLETE` 提交当前候选，`CPS_REVERT` 提交 reading；`NotifyIME(NI_SELECTCANDIDATESTR)` 提交当前页指定候选。`ImeEscape(IME_ESC_QUERY_SUPPORT)` 报告支持 `IME_ESC_IME_NAME`，`ImeEscape(IME_ESC_IME_NAME)` 写入 `XiaoXi IME` 显示名，不改变当前 HIMC 组合。`ImeRegisterWord`/`ImeUnregisterWord`/`ImeGetRegisterWordStyle`/`ImeEnumRegisterWord` 写入共享用户词典并报告用户样式，不改变当前 HIMC 组合。`ImeConfigure(IME_CONFIG_REGISTERWORD)` 用 `REGISTERWORD` 走同一注册路径，不打开配置 UI，也不改变当前 HIMC 组合。`ImeDestroy` 复位全部会话。桥接与 `ImeModuleRuntime` 覆盖同一提交与复位路径。
- `ImeTransMsgBuilder` 把提交文本或组合态转换为最小 `TRANSMSG` 序列。
- `ImeTransMsgWriter` 把托管生成的消息写入 `ImeToAsciiEx` 传入的 `TRANSMSGLIST` 缓冲区。
- `ImmContextAccessor`、`ImeCompositionContextReader` 和 `ImeCompositionContextWriter` 已实现输入上下文锁定，以及组合字符串、候选信息、引导信息和私有数据的读写。
- x86/x64 IME 文件部署、HKL 激活、HIMC 打开、`SendInput` 注入 `xx` 和单个 EDIT 精确上屏“小希”已由管理员沙箱自动冒烟验证。

该结果不等于传统 IME 产品级全链路完成。后续仍需覆盖正式词库候选排序与选择、组合/取消/提交、多 HIMC、多应用、Host/IPC 异常恢复，以及分别由 32 位和 64 位目标应用加载对应 IME 的环境矩阵。
