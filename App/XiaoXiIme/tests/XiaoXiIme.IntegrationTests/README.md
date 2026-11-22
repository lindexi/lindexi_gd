# XiaoXiIme.IntegrationTests

## 双 HIMC 自动化增量

独立宿主新增 `real-ime-multi-himc-isolation`，在单 HIMC 生命周期场景之后运行：同一窗口两个 EDIT、显式创建且不同的 HIMC，通过真实 SendInput 交错组合，第二取消/重组/提交时检查第一组合隔离，最后第一提交并检查两个 EDIT 各上屏一次“你”。复用消息观察、焦点/HKL/上下文身份检查、正式 fullPinyin 外部 Host 只读查询与稳定期，并验证自建上下文清理。此场景不由普通测试启动，本轮只完成代码及 Release 构建，真实 Windows 验收待执行。

`IpcClient_WhenInterleavedSecondSessionCancelsOrRecomposesAndCommitsThenKeepsFirstSession` 的两例参数化测试只固定 Host/IPC 逻辑会话隔离，不证明真实焦点切换保持组合。run-03 已真实复现并修复 `ImeSetActiveContext(active=0)` 错误取消/复位：暂时失焦保留组合，deselect/destroy 仍清理。三个既有真实场景已通过，证据见自动化报告。多应用及 x86/x64 应用矩阵未指定，未引入新应用依赖。详见 `Docs/IMM32-Automation-Validation-Report.md`。

## 既有覆盖

小希输入法系统级集成与冒烟测试项目。托管测试覆盖 Host IPC 基础闭环、Host/UI 状态查询、候选窗状态映射、显示/隐藏、固定每页 9 项的分页、末页页码、跨页选择、唯一高亮和锚点占位。IPC 闭环会通过 `hostBaseDirectory` 加载测试布局中的生产 package，并覆盖全拼 `xiaoxiaimuyi` 与小鹤 `xnxiaimuyi` 连续输入后空格上屏 `XiaoXiIme`。`SetComposition` 覆盖全拼组合的整串替换与清空，不会自动提交。`QueryConversionList` 可查询全拼 `xiaoxiaimuyi` 而不改变当前组合。`QueryReverseConversionList` 可把 `XiaoXiIme` 反查为 `xiao xi ai mu yi` 而不改变当前组合。IMM `ImeConversionList(GCL_REVERSELENGTH)` 只返回该反查缓冲所需字节数。`RegisterWord` 可写入共享用户词典并在新宿主中持久化，不改变当前组合。`ImeConfigure(IME_CONFIG_REGISTERWORD)` 走同一注册路径。同一宿主上两个 `ImeSessionId` 可分别组合 `xiaoxiaimuyi` 与 `aimuyi`；空的 `GetSnapshot`/`GetUiState` 回退到最近活动会话。`ResetSession` 只丢弃指定会话，第一会话仍组合 `xiaoxiaimuyi`。`ImeHostBridge`、`ImeModuleRuntime` 与完整 IMM 路径也覆盖同一提交：`ImeProcessKey` 先吃键，再经同一 HIMC 走 `ImeToAsciiEx` 把 `GCS_RESULTSTR` 写回。`ImeSetCompositionString` 走同一 HIMC 写回组合串。`ImeSelect(select=0)` 取消组合并复位该会话；`ImeSetActiveContext` 保留组合与会话；`NotifyIME` 在同一 HIMC 上取消组合、完成候选、选择当前页候选或回退提交 reading；`ImeDestroy` 复位全部会话。自包含 `XiaoXiIme.IntegrationTestHost` 会从负载 `app/host` 解析全拼 package；该目录缺失时立即失败，不会静默改用最小回退词典。管理员沙箱或可还原 VM 中的真实安装、HKL 激活、HIMC 打开、`SendInput` 自动注入 `ni` 和空格，使单个 EDIT 精确上屏“你”的基础冒烟由 CLI 的 `integration-run --skip-tsf` 调用，不属于普通 `dotnet test`。`ProductionDictionaryKeystrokeTests` 已实现正式词库全拼/小鹤 IPC 逐键输入、首候选和空格提交，以及前缀转精确测试。`CandidateWindowScreenshotTests` 已实现无头渲染截图与尺寸断言。这些测试代码不代表系统验收通过；实际发布 package 的候选交互、多应用/多会话、完整组合交互、生命周期/异常恢复、长时间输入，以及 x86/x64 目标应用矩阵仍待验收。测试所需 `data` 内容由开发者从外部自行获取，本地数据缺失不属于交接缺陷。
