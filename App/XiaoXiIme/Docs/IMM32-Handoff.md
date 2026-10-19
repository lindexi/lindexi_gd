# XiaoXiIme IMM32 状态与验证交接

> 历史记录所标更新日期：2026-08-12（不是本次盘点日期）
>
> 当前发布差距及证据库存统一见 [主交接第 4.8 节](../../Docs/next-session-handoff.md#48-统一发布差距清单)。本次仅核对文件与实现，不代表业务复验通过。
>
> 本文记录传统 IMM32 `.ime` 的当前状态、已确认约束和回归验证方式。历史排障细节见 [VM 安装诊断记录](./VM-Ime-Installation-Diagnostics.md)。项目当前主线是传统 IMM32；TSF 不是默认验收路径。

## 当前结论

历史交接记载传统 IMM32 输入法管理员沙箱无人值守基础冒烟曾多次通过（本次未读取到其原始结果，也未复跑）：

1. 同时部署 x64/x86 `XiaoXiIme.ime` 到 `System32`/`SysWOW64`。
2. 调用 `ImmInstallIMEW` 注册布局 `E0200804`。
3. 创建 Win32 EDIT 控件，激活 XiaoXiIme HKL，关联并打开 HIMC。
4. 使用 `SendInput` 自动注入 `x`、`x`。
5. Windows 调用 `ImeProcessKey` 与 `ImeToAsciiEx`。
6. EDIT 控件最终精确上屏一次“小希”。
7. 测试结束后删除布局并清理正式部署路径。

历史记录所引复测报告退出码为 0，所记路径为：

```text
artifacts/sandbox-retest-results/retest.json
```

当前工作区未找到上述结果文件，不能据此声称当前负载复验通过，也不据此否定历史成功记录。历史记载的覆盖仅为单个 Win32 EDIT、单会话和固定 `xx -> 小希` 输入序列，用于证明当时基础 IMM32 调用链可达；完整传统 IME 发布验收及多应用、完整多会话和 x86/x64 目标应用矩阵仍未闭环；组合/取消/提交与双 EDIT/不同 HIMC 的限定场景已有后续 run-05 PASS 归档，不能仍按本段旧单会话冒烟范围判为从未实现或从未验证。

真实按键测试依赖 Windows 前台窗口和 EDIT 焦点。沙箱启动器或控制台偶尔会短暂争抢前台窗口，导致测试在注入按键前主动失败；相同负载复测成功时可判定为桌面焦点时序竞争，而不是输入法回归。测试宿主会验证前台窗口和焦点，避免将按键发送到其他窗口。

## 历史增量：生命周期场景与验证边界

- 已新增 `real-ime-composition-lifecycle`：真实 `SendInput` 执行 `n/i → Escape → n/i → Space`，同步捕获同一 EDIT/HIMC 的 START、组合文本/读音/光标、取消、结果“你”和 END；结果串在 EDIT 默认处理前读取，再观察控件文本及稳定期。
- 只读查询默认外部 Host 状态和实际 HIMC 对应会话；要求正式 `fullPinyin` package、无 fallback/load error，且 `ni` 首候选及提交为“你”（`ni -> 你`）。负载正式全拼 package 校验已增加相同查询要求。原 `xx -> 小希` 冒烟保留，并增加稳定观察，防止延迟或重复提交。
- 已修复组合更新重复 START：按处理前会话状态决定 START，后续更新仅发组合消息。收尾复核将成功/失败结果发布移至资源清理后，避免连续场景释放竞争；回调委托保持存活至窗口销毁完成。回调异常不跨越原生边界，HIMC 查询成对释放，阶段等待泵送消息且有超时。
- 本轮独立宿主 Release 构建（含收尾修复后复验）0 警告、0 错误；模块测试 136/136。代理报告 CLI 相关 14/14、`ProductionDictionaryKeystrokeTests` 18/18。CLI 全量 5 分钟超时，不能声称通过；收尾未重跑其他代理测试范围。
- **代码已实现不代表 Windows 验证完成。该历史轮次未执行真实沙箱测试，所记执行限制仅适用于当轮；没有该轮真实 PASS 或产品验收完成结论。本次盘点未运行业务测试。** 上文 `xx` 通过记录仅为历史证据。该轮当时的组合/取消/提交待验收状态保留为历史；后续 run-05 已记载该限定场景 PASS，见下方当前/历史边界，其他未覆盖产品级要求不据此关闭。

## 双 EDIT/多 HIMC 增量（历史：当轮待真实验收）

**当前/历史边界：** 下文“待真实验收”“失活即取消”是该增量轮次的原始事实，不是当前实现结论。[自动化增量验证记录的 run-05 最终归档](./IMM32-Automation-Validation-Report.md#2026-10-13-run-05-最终归档历史批次结论)已记录后续修复：临时 `ImeSetActiveContext(active=0)` 保留组合与 Host 会话，取消/清理仍由 Escape、NotifyIME CPS_CANCEL、deselect 等各自处理；真实单 HIMC 取消/重组/提交及双 EDIT/不同 HIMC 交错、回切均 PASS。第二轮候选定位归档另记载单屏96 DPI的定位/移动/边缘局部 PASS。这里只回读现有文档归档，未重新读取这些原始报告或执行真实验收；原始路径当前缺失不表示没有实现，也不将旧局部 PASS 扩大为全发布签收。

当前仍缺真正销毁/句柄复用世代、活动身份与激活传播、同 HWND 多 HIMC、新激活无缓存锚点等闭环；双 EDIT/不同 HWND 的通过不能替代同 HWND 多 HIMC 验证。最新 FdwInit/样式最小修复与15/15、模块227/227、CandidateAnchorTests19/19及模块Release零警告/零错误见[候选定位专门计划](../src/XiaoXiIme.ImeIpc/Docs/Win32InputTargetAndCandidateAnchorDevelopmentPlan.md#本次源码调查与最小修复g02-仍开放)；全Integration超时、真实IMM32未重跑，G02仍开放。正式词典内容、全拼/小鹤两包及Host/IPC处理已存在，G01剩余固定含原生模块发布负载的双方案真实IMM32端到端验收；不要求重新实现词典。

独立宿主已追加 `real-ime-multi-himc-isolation`，沿用既有顺序在单 HIMC 生命周期之后执行。同一窗口两个 EDIT 分别关联显式创建的不同 HIMC，SendInput 交错组合 `ni`，第二取消、重组、提交时严格检查第一真实组合与 Host 会话隔离，最后第一提交；检查每个上下文的消息生命周期、精确结果、稳定期、原生调用计数及资源清理。没有新增应用依赖或修改产品模块。

本轮新增托管 IPC 参数化测试 2/2 通过，Host/IPC 与正式词库筛选合计 41/41，模块全量 136/136；最终宿主 Release 构建 0 警告、0 错误。上述不代表真实场景通过：该轮记录因执行限制未执行真实 Windows 验收；不能将当轮限制当作当前沙箱能力不可用的结论。该轮当时产品 `ImeSetActiveContext(active=0)` 会取消并复位会话，真实焦点切换可能使严格隔离断言失败；当时要求取得真实消息和会话证据后确认失焦语义及修复，不放宽断言（后续修复及 run-05 通过见上方当前/历史边界，不再作为当前缺陷）。具体多应用清单/版本/控件及 x86/x64 应用矩阵尚需确认。详细记录见 [自动化增量验证报告](./IMM32-Automation-Validation-Report.md)，产品级验收仍未完成。

## 固定验证命令

先在开发环境构建完整负载：

```powershell
dotnet run --project .\src\XiaoXiIme.Cli\XiaoXiIme.Cli.csproj -- payload-build --output .\artifacts\integration-payload
```

如果已经按项目发布清单生成 `artifacts\integration-publish`，可只重新组织负载：

```powershell
.\artifacts\integration-publish\app\cli\XiaoXiIme.Cli.exe payload-build --no-build --output .\artifacts\integration-payload
```

在管理员沙箱或可还原 VM 中执行传统 IMM32 验收：

```powershell
.\app\cli\XiaoXiIme.Cli.exe integration-run . --confirm I-UNDERSTAND-THIS-MODIFIES-WINDOWS --skip-tsf --report .\results\integration.json
```

`payload-build` 支持 `--output`、`--no-build`、`--dictionary-source` 和 `--staging-directory`，不会直接运行集成测试。构建负载与执行 `integration-run` 是两个独立生命周期。

若只需保留安装供人工体验：

```powershell
.\app\cli\XiaoXiIme.Cli.exe install . --confirm I-UNDERSTAND-THIS-MODIFIES-WINDOWS
```

## 必须保留的实现约束

### 双架构部署

- x64 模块部署到 `System32`。
- x86 模块部署到 `SysWOW64`。
- 文件名保持 `XiaoXiIme.ime`；资源中的 `OriginalFilename` 会影响注册表记录的 IME 文件名。

### 原生 ABI

- UI 类名必须保持在 `ImeInquire` 的 16 个宽字符缓冲区限制内；当前值为 `XiaoXiImeUIWnd`。
- `InputContext` 必须保持与 Windows SDK `INPUTCONTEXT` 一致的字段顺序、字段宽度和跨架构对齐。
- `TRANSMSG` 只包含 `message`、`wParam`、`lParam`，不得增加窗口句柄字段。
- x86/x64 的结构大小和关键字段偏移必须由测试覆盖。
- `ImeProcessKey` 必须忽略 transition-state 位表示的 KeyUp，避免一次输入重复提交。

### 真实按键诊断

原生模块保留以下只读诊断入口，供集成宿主确认 Windows 实际调用链：

- `XiaoXiImeResetKeystrokeDiagnostics`
- `XiaoXiImeGetKeystrokeDiagnostics`

自动按键宿主还会记录：

- 当前 HKL、HIMC 和 IME 打开状态；
- 前台窗口与 EDIT 焦点；
- `WM_KEYDOWN`、`WM_KEYUP`、`WM_CHAR` 和 `WM_IME_*` 消息；
- `ImeProcessKey`、`ImeToAsciiEx` 等诊断计数。

## 已解决的关键根因

1. UI 类名和复制上限曾越过 `ImeInquire` 的真实缓冲区，导致 `ActivateKeyboardLayout` 内出现 `0xC0000409`。
2. 托管 `InputContext` 曾遗漏字段并把 `BOOL`/`DWORD` 错误表达为指针宽度，写入组合上下文时会破坏原生内存。
3. `TRANSMSG` 曾错误包含 `HWND`，导致 Windows 按错误偏移解释返回消息。
4. x64 `INPUT` 联合体曾未按 Windows ABI 声明，`SendInput` 因结构大小错误返回 Win32 error 87。
5. KeyUp 曾重复进入按键处理链，导致上屏“小希小希”。
6. 被加载的旧 System32 映像可能无法立即删除；清理逻辑会在确认无布局引用后将其移动为严格格式的 `XiaoXiIme.retired-<UTC>-<GUID>.ime`，释放正式路径供当前沙箱继续验证。

## TSF 边界

当前默认验收目标是传统 IMM32。执行 `integration-run` 时应传入 `--skip-tsf`。

不传 `--skip-tsf` 会额外执行 x86/x64 TSF ABI 和隔离 COM 激活验证。TSF ABI/vtable 可以通过，但在未完成相应 COM 注册的环境中，COM 激活可能返回 `0x80040154 (REGDB_E_CLASSNOTREG)`；这不代表传统 IMM32 安装或真实上屏链路失败。

## 沙箱清理注意事项

多轮测试后，System32 中可能存在仍被旧进程映射的退役文件：

```text
XiaoXiIme.retired-*.ime
```

这些文件不再被键盘布局引用，也不占用正式 `XiaoXiIme.ime` 路径。若需要验证完全删除，应重置沙箱或等待持有模块的进程退出；不要把退役文件存在误判为正式布局仍安装。

## 后续完整验收缺口

- 使用正式词库覆盖候选生成、排序、分页选择和提交。
- 覆盖组合开始、更新、取消、完成、回退提交及候选选择的 Windows 消息和 HIMC 状态。
- 覆盖多个 HIMC、多个目标应用、应用切换、Host 重启和 IPC 异常恢复。
- 分别使用 32 位和 64 位目标应用验证对应架构 IME，不只检查双架构文件已部署。
- 覆盖安装、升级、卸载、清理和用户数据保留策略。

## 回归判断

修改 IME ABI、上下文布局、按键转换、安装器或真实按键宿主后，至少保留以下基础回归：

- Release 解决方案构建；
- `XiaoXiIme.ImeModule.Tests`；
- x86/x64 Native AOT 发布；
- 负载 manifest、长度和 SHA-256 校验；
- 管理员沙箱中的 `integration-run --skip-tsf`；
- `PASS real-ime-keystroke-commit` 和整体退出码 0；
- 测试后的布局与正式部署路径清理。

不要只根据 `ImmInstallIMEW` 成功判断输入法可用，也不要用 TSF COM 激活结果替代传统 IMM32 的真实按键与上屏验收。
