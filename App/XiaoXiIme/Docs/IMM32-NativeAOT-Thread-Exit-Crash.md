# IMM32 / NativeAOT 线程退出崩溃 — 调查交接

**状态：产品和独立 x64 NativeAOT 对照均已复现同一线程退出故障路径。产品记事本场景与产品双线程宿主均有匹配私有 PDB 的转储；双线程宿主确认异常在线程 A，取证 `VERIFIED`、产品验收 `FAIL`。早期独立最小对照 `Imm32AotControl.ime` 有私有符号解析的完整 live CDB 异常栈，但未取得转储。其复测后归档清理记录返回 `2`，对照文件当时残留；当前状态未核验。修复版已完成本地 ABI 和发布验证，尚未完成真实 IMM32 系统验收。** 本文只记录这条 `0xC0000602` 线程退出故障。候选窗、键盘回退、IPC 断线等是独立问题；不能仅凭相同异常码把其他事故合并进来。

本文所列命令、清理和回滚均为历史记录或供另行授权复现时参考，不是本次文档核对要执行的步骤。

本文的**产品线程 A**是产品双线程宿主中先退出的窗口线程；**原生 ABI 变体 A/B**是直接调用导出的两个原型；**[诊断组 A](../../.coding-agent/imm32-checkout-nativeaot/evidence/DIAGNOSTIC-A.md)** 是跳过 FLS 回调内运行时线程清理的混合重链产物。三者的结果不可互相替代。

## 当前交接：根 SDK 双架构本地验证

工作区根 `DotNetSDK` 已更新至 `xiaoxiime-sdk-10.0.115-win-x64-x86-validation.2`。产品项目不含实验开关或本机工具链路径；根 SDK 根据目标 RID 自行选择 x64 宿主 ILC、对应的 JIT 及 WorkstationGC，输出仍为 `XiaoXiIme.ime`。默认其他 UCO 导出不变。Windows x86 `ImeSelect` 仅接受 `int(nint, int)`、`stdcall`：正常调用原样转参，已分离时返回 `0` 且不执行 C#；保留正常 FLS 清理和 `IsDetached()`。

独立 x86 探针实测：指针宽度 4，正常返回 `73`，同线程正常 FLS 清理后返回 `0`，托管调用计数 `0 → 1 → 1`，两次 ESP 差值均为 `0`。根 SDK 隔离发布真实 `win-x64`、`win-x86` 均成功，系统 SDK Release 解决方案编译成功，Builder SDK 选择测试 4 项、ImeModule 单测 236 项全部通过。路径和复验入口见 [SDK 分发验证说明](../../SdkDistribution/README.md)。

未运行 Builder 完整分发，未上传产品验收 VM、未安装或激活 IME。仍需验证真实 IMM32 线程退出（含记事本关闭）、双线程宿主中线程 A 退出后线程 B 继续输入，以及正常布局切换；隔离探针结果不构成产品放行。

## 产品 `ImeSelect` 门控集成（本地 Windows x64，历史记录）

下文仅记录旧的 checkout 实验流程，不适用于当前根 SDK。

当时的实际构建入口是 [`XiaoXiIme.ImeModule.csproj`](../src/XiaoXiIme.ImeModule/XiaoXiIme.ImeModule.csproj)，不是独立演示。只有显式设置 `XiaoXiImeDetachedImeSelectExperiment=true` 才启用门控；该配置要求 `Release`、`win-x64`、NativeAOT 共享库和单独指定的 `--artifacts-path`。它固定使用 `10.0.15` 的 NativeAOT 包资产，并从本地 checkout 选用修改过的 ILC、配套 JIT 和实验 `Runtime.WorkstationGC.lib`，不替换 NuGet 缓存中的文件。省略此属性时仍按默认流程发布 `XiaoXiIme.ime`。

从工作区根目录使用 .NET 10 SDK 和 Windows NativeAOT 构建工具，先准备下列本地输入，再发布：

1. 如果尚未准备产品版 ILC，将 checkout 中的 `ILCompiler.csproj` 发布到产品配置指定的位置：

   ```text
   dotnet publish dotnetruntime/src/coreclr/tools/aot/ILCompiler/ILCompiler.csproj -c Release -p:TargetOS=windows -p:TargetArchitecture=x64 --output .coding-agent/imm32-checkout-nativeaot/DetachedInt32/ilc-xiaoxiime
   ```

2. 确认 `dotnetruntime/artifacts/bin/coreclr/windows.x64.Release/` 下已有配套 `clrjit.dll` 和 `imm32-detached-int32/aotsdk/Runtime.WorkstationGC.lib`。本次复用了 [现有实验构建配置](../../.coding-agent/imm32-checkout-nativeaot/DetachedInt32/BuildRuntime.proj) 的原生库；该配置只打开 `NATIVEAOT_IMM32_DETACHED_INT32_PROBE`，不启用跳过 FLS 清理的诊断宏。
3. 从实际产品项目发布。下例使用本轮验证时的路径；复跑时应换用新的独立输出目录和日志文件，保留这轮证据：

   ```text
   dotnet publish XiaoXiIme/src/XiaoXiIme.ImeModule/XiaoXiIme.ImeModule.csproj -c Release -r win-x64 --artifacts-path .coding-agent/imm32-checkout-nativeaot/DetachedInt32/xiaoxiime-product-local-verified -p:XiaoXiImeDetachedImeSelectExperiment=true -p:IlcSingleThreaded=true -bl:.coding-agent/imm32-checkout-nativeaot/DetachedInt32/xiaoxiime-product-local-verified.binlog -v:n
   ```

**产物与工具链证据：** 以下路径相对于 `.coding-agent/imm32-checkout-nativeaot/DetachedInt32/`。

- `xiaoxiime-product-local-verified/publish/XiaoXiIme.ImeModule/release_win-x64/` 包含 `XiaoXiImeDetachedImeSelect.ime` 和对应的 `XiaoXiIme.ImeModule.pdb`。发布日志记录 `ilc-xiaoxiime/ilc` 调用，使用下列 ILC 响应文件；二进制日志为 `xiaoxiime-product-local-verified.binlog`。
- `xiaoxiime-product-local-verified/obj/XiaoXiIme.ImeModule/release_win-x64/native/` 下，`XiaoXiIme.ImeModule.ilc.rsp` 第 1–2 行分别是 `--experimental-xiaoxiime-detached-ime-select` 和指向本地 `clrjit.dll` 的 `--jitpath`。`link.rsp` 第 53 行是唯一一份 `Runtime.WorkstationGC.lib`，来自本地 `imm32-detached-int32/aotsdk/`，没有链接包里的同名库。`XiaoXiIme.ImeModule.map.xml` 仅记录一个门控跳板，目标是产品 `ImeSelect`。

**行为边界：** 编译器仅对 `XiaoXiIme.ImeModule` 的 Windows x64 `int(nint, int)` 导出 `ImeSelect` 加门控。正常 FLS 回调仍执行 `RuntimeThreadShutdown`，运行时的 `IsDetached()` 检查保留。门控在进入托管代码前检查该状态：已分离则原生返回 `0`，否则把原实参转交原入口；不因 `select` 为零而跳过正常取消逻辑，不改变其他导出的门控行为。

**本地验证及限制：** `ImeSelect` 定向测试 5 通过、0 失败、0 跳过；随后完整 `XiaoXiIme.ImeModule.Tests.csproj` 实际运行 236 项，236 通过、0 失败、0 跳过。解决方案命令仅输出还原信息，不计为测试。托管测试与发布证据不能代替实际 IMM32 验证：未安装或激活该产物、未操作 VM，未验证真实线程退出/记事本关闭、双线程宿主验收或正常布局切换；x86 不在此配置范围内。**不构成产品放行。**

## `ImeSelect` ABI 原型复核与正常切换基线（x64）

- **原型实证**：[Imm32SelectAbi](../tests/Imm32SelectAbi/README.md) 的专用宿主在独立进程中直接调用测试 DLL 的 `BOOL WINAPI ImeSelect(HIMC, BOOL)`。README 记录无调试器 A/B 均为 `PASS`、退出码 0：A（现有 NativeAOT 对照）冷 `FALSE`、`TRUE`、热 `FALSE` 均返回 1；B（原生门控）保留完整 64 位测试标记，拒绝低 32 位相同的错误标记 `TRUE` 且保留原状态，匹配 `FALSE` 清槽、不匹配或重复取消为空操作，`0x100` 仍按非零转发。B 的冷 `FALSE` 后托管测试方法计数为 0，热 `FALSE` 序列前后均为 2，`0x100` 后为 3。工作区未找到这两轮无调试器运行的原始 stdout/退出码记录；这些动态结果来自 README，不能用模块加载时即退出的 CDB 记录替代。
- **独立核对入口**：`.coding-agent/sandbox-results/3cb677f7916a4516a4a6e47a54832518/inspection-A.txt` 第 57–92 行与 `5944b43098114854a075817cc423a767/inspection-B.txt` 第 57–125 行记录的映像哈希与 README 一致。A 在导出 RVA `0x15D0` 的 `+0xA` 无条件调用 `RhpReversePInvoke`；B 的原生导出 RVA `0x86F10` 用 `test edx,edx` 检查完整 32 位 `BOOL`，零分支只做指针宽度的 `lock cmpxchg` 后返回 1，没有调用；非零分支调用另名托管导出 RVA `0x13E0`，在其 `+0x1C` 才进入 `RhpReversePInvoke`。不以计数或检查工具的成功码单独证明零分支无托管转换。
- **业务与生命周期边界**：B 的进程内单槽和合成 `HIMC` 不代表真实输入上下文或会话代际，也未覆盖并发、句柄重用、真实 IMM32 卸载、FLS 分离后的回调或 x86。A 正常返回不是线程退出故障的负对照；B 的非零分支仍依赖有效的 NativeAOT 线程状态。产品正常 `ImeSelect(FALSE)` 会停用上下文、使异步输入失效、取消组合并重置会话；原型没有验证候选关闭、旧结果丢弃或资源清理，不能把零分支直接当作产品修复。
### 原版正常切换：2026-11-23 取证复核（x64，UTC）

证据根目录：`.coding-agent/imm32-normal-switch-product/`。下述四个 `result.txt` 均显示 System32 原版磁盘哈希前后匹配。09:06 / 09:08 的旧自动轮在获得前台前停止，未送键；不能用来解释下面已实发按键的轮次。

- **无产品 Host（`actual/evidence/`）：未得到可比较的切换。** `normal-switch-ae52ce32b91b41ba955c0f51a68aaea6` 的不切换 `ni` + 空格对照（PID `8800` / TID `4572` / HIMC `0x14F0751`，`stages.log` 51–67）没有采到 `ni` 组合，EDIT 只收到普通空格。`normal-switch-14252547d4f5480ca24ad53696541674` 的切换轮（PID `1896` / TID `2752` / HIMC `0x4F1068D`，51–57）实际送了 `ni`，但切前采样为 `compositionBytes=0`，在 `switch-away` 前停止。两轮均 `NOT_RUN` / 2；`cleanup-not-baseline` 的 `RESTORED_ENTRY_HKL` 不是测试切出，且没有回切。不能据此比较 Host 有无对取消的影响。
- **有产品 Host（`with-host/evidence/`）：不切换对照通过。** `product-host-3c37fb2360864aad9a9e1dd2f300de63/startup.log` 记录产品 Host PID `2092`、`PIPE_READY`，比较结束时仍存活。`normal-switch-d8867c33fc6d48ca876a4dd209926c5d/stages.log` 55–74 在 PID `2860` / TID `11992` / HIMC `0x290859` 上采到未提交的 `ni`，随后空格产生 `GCS_RESULTSTR=你`、组合结束且 EDIT 为「你」；`PASS` / 0 仅适用于不切换提交，清理还原布局不算试验切出。
- **有产品 Host：实际切出并回切，旧组合在两个后置采样点仍可见。** `normal-switch-8b75afc629dc457caa3610cd5fe6c559/stages.log` 56–67：同一 PID `7996`、UI TID `1028`、EDIT `0xC025E`。切前 `11:39:26.3251703`（`+1892ms`）已见未提交 `ni`；`ACTIVATE` 和 EDIT 的 `WM_INPUTLANGCHANGE` 后，`ACTIVE` 确认切到 `00000804`（HKL `0x08040804`），再回到 `E0200804`（HKL `0xFFFFFFFFE0200804`，记录 System32 模块路径）。切出后 `11:39:26.7934854`（`+2361ms`）、切回后 `11:39:27.2587809`（`+2826ms`）才采样，分别距该段 `ACTIVATE` 约 467 / 465 ms，均在约 450 ms 消息泵等待之后，不是切换调用瞬间。三次 EDIT 快照的 HIMC 均为 `0x505B5`，`compositionBytes=4`、`composition="ni"`、EDIT 为空、`candidateLists=0`。两段 `SWITCH_EVIDENCE` 均未记录结果标志、字符提交、组合结束、候选关闭或 EDIT 变化。切回后因非空组合而停止，`NOT_RUN` / 2；这不是「未发生切换」，也没有进入回切后的新 `ni` + 空格验证（68–72 行是清理及停止）。
- **证据边界：没有实际切出或回切期间的 `ImeSelect(FALSE)` 直接记录。** 阶段日志是 HKL、窗口消息和状态快照；`PRODUCT_COUNTERS` 是按键诊断，`PRODUCT_BOUNDARY offset=263571 length=263571 ownEntries=0` 仅表明采到的边界日志没有新增本 PID 条目，不是导出入口探针。`0x505B5` 是从 EDIT 取得的快照句柄，不是回调的实参；相同数值也不证明会话代际相同或两次快照间从未清空、重写。不能判定回调未走、走在别的 HIMC、或确已在同一 HIMC 上执行但未清空／后来重写。候选列表始终采到 0，且产品 Host `candidate-status.txt` 只留下回切快照后约 7 ms 的 `no-visible-candidates`（`11:39:27.2656424`），不足以证明切前候选曾打开、何时关闭或从未打开。线程退出实验中的 `select=0` 不可替代这里的调用证据；反过来，正常布局切换也不是线程退出路径的直接证据。
- **下一项最有判别力的检查：采一次真实切换的导出入口时序。** 在原版哈希匹配且产品 Host 就绪时，从诊断进程初次激活原版之前开始，对实际映射的 `XIAOXIIME.IME!ImeSelect` 做自动继续的入口跟踪，不直接调用导出。确认探针在入口生效并保留正控命中；记录每次调用的时间、PID/TID、完整 `RCX`（HIMC）、`EDX`（32 位 `BOOL`，0 为 FALSE）及调用栈，不先按已知 HIMC 或 TID 过滤。与同一次试验的两次 `ACTIVATE`、HKL / 消息及切前、切出后、切回后的 EDIT HIMC 和组合样本对齐，把初次激活与清理单独标记。先区分覆盖有效时未见 `FALSE`、命中其他 HIMC、命中同一 HIMC 三种情况；若命中同一 HIMC，再比对回调返回附近与延迟样本，区分未清空和清空后重写。此项仅为后续建议，本轮未执行。

## 产品双线程宿主：原版失败基线（x64）

`2026-11-23 07:11:27–07:11:30 UTC`，专用 VM `DESKTOP-6BGIATF`。原始证据位于 `.coding-agent/sandbox-results/01a499158859425a85a4eaa58c7d04b7/evidence/`。独立诊断宿主 `Imm32ThreadExitHost.exe` 在同一进程中用两个窗口线程测试已部署的产品 `XiaoXiIme.ime`；这不是不处理按键的 `Imm32AotControl.ime` 最小对照，也不是记事本关闭实验。

- **真实输入提交**：`stages.log` 第 5–17 行记录宿主 PID `2816` 中，B（`B-before`，TID `13712`，HIMC `0xD580E07`）和 A（`A-before-exit`，TID `9640`，HIMC `0x67C2095D`）依次输入 `ni` 加空格，各收到一次真实 IME 提交：`starts=1`、`ends=1`，`GCS_RESULTSTR` 和 EDIT 文本均为 `你`。两次提交均记录 KLID `E0200804`、HKL `0xFFFFFFFFE0200804` 和进程内实际加载的 `C:\WINDOWS\system32\XIAOXIIME.IME` 路径。`result.txt` 核对该磁盘文件运行前后 SHA-256 均为 `96A33BABEE4136BDFA9A7FB6ADFAE4321D4B34ADC28E94979CD74DBEECBE1EC9`。
- **失败边界**：`stages.log` 第 18–19 行最后记录 `A_EXIT_READY` 和 A 的 `THREAD_RETURN`，仅证明 A 即将返回，不代表原生线程已退出。没有 `THREAD_EXIT_CONFIRMED` 或 `HOST_ALIVE_AFTER_A_EXIT`，也未进入 `B-after-A-exit` 的第二次 B 输入。`result.txt` 记录子进程退出码 `0xC0000602`（`-1073740286`），结果为 **FAIL（原版失败基线）**。本轮没有转储或异常线程栈，不能确认异常发生在哪个线程，也不能仅凭退出码确认与记事本或最小对照的内部故障路径相同；本轮未验证修复。
- **独立复现入口（仅供参考）**：按[宿主 README](../tests/Imm32ThreadExitHost/README.md) 发布 Windows x64 诊断宿主后，在专用 VM 的交互式测试会话中运行 `Imm32ThreadExitHost.exe --acceptance`。该模式只验收已安装且哈希匹配的原版和现成的 `E0200804` 布局，不安装布局，也不操作现有记事本。监督进程将阶段日志、标准错误和实际退出码分别保存到 `evidence/stages.log`、`evidence/stderr.log` 和 `evidence/result.txt`。

### 产品双线程宿主：线程 A 的匹配符号转储（x64）

`2026-11-23 08:33 UTC`，任务 `1f6f81093c314f718a9c7c08fb21f289`。证据目录：`.coding-agent/sandbox-results/1f6f81093c314f718a9c7c08fb21f289/evidence/capture-9fedef180356419ea78eded668ee628d/`。取证入口为 `Imm32ThreadExitHost.exe --acceptance-capture`，与上面的无调试器 `--acceptance` 基线分开记录。

- **取证 `VERIFIED`**：`capture.txt` 第 57–74 行显示 CDB 已就绪，在 PID/TID `3828.3cec` 上捕获第二次机会 `0xC0000602` 并成功写出 `thread-exit.dmp`（1,586,112 字节，SHA-256 `FDF942CB979B869DDB7D70962A55F32AE5479696CA25E17EBB37F43083705651`）。`reopen.txt` 第 39–81 行显示另一 CDB 会话以 `SYMOPT_EXACT_SYMBOLS` 离线重开，加载匹配的私有 `XiaoXiIme.ImeModule.pdb`（任务的 `symbols/` 目录）；原版映像时间戳为 `0x6B02E936`，映像大小 `0x4F9000`。`result.txt` 第 2–12 行记录产品磁盘哈希前后不变、两次 CDB 退出码均为 `0`，且活体和转储均对应 PID `14376`、TID `15596`，等于阶段日志中的 A，不是 B（TID `6164`）。前一次焦点失败轮的 B 栈不作为本轮证据；本转储也不是最小对照或记事本的转储。
- **异常路径**：`reopen.txt` 第 59–62、97–116 行，按调用方向，原始 `kv` 完整链为 `ntdll+0xc9c6c → kernel32+0x2ddb0 → ntdll+0xc9cf6 → ntdll+0xca134 → ntdll+0x1b2e3 → ntdll+0x12147a → imm32+0x812f → imm32+0x84ec → XIAOXIIME!XiaoXiIme_ImeModule_XiaoXiIme_ImeModule_ImeExports__ImeSelect+0x15 → XIAOXIIME!RhpReversePInvokeAttachOrTrapThread2+0xa2 → XIAOXIIME!ThreadStore::AttachCurrentThread+0x3d → XIAOXIIME!RaiseFailFastException+0x44 → KERNELBASE+0x10a608`。其中 `Thread::ReversePInvokeAttachOrTrapThread+0x91`、`ThreadStore::AttachCurrentThread+0x37` 为内联帧；末帧在活体日志中解析为 `KERNELBASE!RaiseFailFastException+0x188`。异常地址是产品模块的 `RaiseFailFastException+0x44`（RVA `0x21F34C`）。该产品路径与记事本的线程退出链一致，失败在 `ImeSelect` 托管方法体之前；本轮离线系统符号未完整解析，系统帧按原始偏移记录，未独立采到 FLS 分离时序或 `ImeSelect(FALSE)` 的实参，这两点仍以既有活体断点记录为准。
- **产品验收 `FAIL`，阶段不同于无调试器基线**：`stages.log` 第 10、17 行记录 B、A 退出前各真实提交一次「你」。第 18–30 行在 `A_EXIT_READY`、`IMM32_CAPTURE_A_WAITING` 和 A 的 `THREAD_RETURN` 之后，仍记录了 `THREAD_EXIT_CONFIRMED code=0`、`HOST_ALIVE_AFTER_A_EXIT`，以及 B 在同一 TID/HIMC 上再次提交「你」（EDIT 为「你你」），最后记录 B 的 `THREAD_RETURN`，没有 `PASS`。活体 CDB 约在 `08:33:12.037 UTC` 已捕获 A 的异常，早于约 `08:33:12.405 UTC` 的阶段确认；这是附加调试器、写转储后分离的取证轮，不能据这些后续阶段判定 A 无异常或产品通过。`result.txt` 第 11–12 行分别为 `captureResult=VERIFIED` 和 `result=FAIL exitCode=-1073740286`（`0xC0000602`）。

## 最小对照后续（x64）

独立的 `Imm32AotControl.ime` 不含产品项目引用、VC-LTL 或 YY-Thunks。早期对照 `09e90e73c520474d8412a06a6deae8ba` 在零按键关闭记事本 PID `8572` 时，在线程 `9492` 上记录到 FLS 分离、运行时分离、`ImeSelect(FALSE)` 和第二次机会 `0xC0000602`（`evidence/notepad-close-trace.txt` 第 163–215 行）。以下任务 ID 均位于工作区 `.coding-agent/sandbox-results/`。

### `775e462c7ea145d9924bc04e6ebced4e` 复测前：清理与复核

- 早期 `a4177145f456407eb3931c1425529864/evidence/cleanup.txt` 记录了 `mmc.exe` PID `12428` 映射对照文件；`f60b3b3108f541cf801b267c15fb0199/evidence/mmc-close.txt` 记录该管理控制台正常关闭并退出。
- 据监督者现场确认，`5599dbac1f8f43d7a80d4decd639bbeb` 与 `7fd50072663044538bca71a43683b872` 均在 `775e462c7ea145d9924bc04e6ebced4e` 复测之前。前者的 `evidence/cleanup.txt` 记录清理成功：`FileRemaining=False`、`ControlSha256After=<missing>`、`LayoutsAfter` 为空、产品哈希不变、`ExitCode=0`；后者的同名日志记录文件在检查时已缺失，`ExitCode=0`。两次记录只证明复测前的状态。

### `775e462c7ea145d9924bc04e6ebced4e`：完整活体异常栈，未取得 dmp

- `evidence/notepad.txt` 第 2–3 行确认零按键、HKL `0xFFFFFFFFE0210804`、已加载 `C:\Windows\System32\Imm32AotControl.ime`。`evidence/control.txt` 第 3 行记录对照文件 SHA-256 `35B9268CA8EE16E1DBEE86009B9D704A7650F5B106B39FDB1E36BCC3C522CF38`。
- `evidence/notepad-close-trace.txt` 第 167–222 行显示同一进程/线程 `3378.2a90`（PID `13176`、TID `10896`）依次命中 `FiberDetachCallback`、`ThreadStore::DetachCurrentThread`、`ImeSelect(FALSE)`（`edx=0`）和第二次机会 `0xC0000602`；异常记录地址解析为 `IMM32AOTCONTROL!ThreadStore::AttachCurrentThread+0x37`。
- 同日志第 223–243 行是完整 live CDB `kv` 栈，从 `KERNELBASE!RaiseFailFastException+0x188` 到 `ntdll!RtlUserThreadStart+0x2c`。按调用方向，关键帧为 `ntdll!LdrShutdownThread → IMM32!ImmEnumInputContext → Imm32AotControl_Imm32AotControl_Exports__Select+0xf → RhpReversePInvokeAttachOrTrapThread2+0xa2 → ThreadStore::AttachCurrentThread → KERNELBASE!RaiseFailFastException`（省略中间帧，原始帧和偏移见日志）。第 245–251 行确认 CDB 加载了 `crash-input/Imm32AotControl.pdb` 的私有符号，对应 System32 中的对照模块，PE 时间戳 `0x6B038B8F`，映像大小 `0xEE000`。该栈证明产品项目引用、VC-LTL 和 YY-Thunks 不是复现同一路径的必要条件，但不单独判定是运行时缺陷还是退出生命周期契约问题。
- 第 265–278 行显示 `.sympath` 把后续 `.dump /m …; qd` 当作符号路径文本处理，报 `Error: Empty Path.`，随后运行器记录 `ControlEvidence=failed: Debugger trace timed out.`（`evidence/control.txt` 记录 `DebuggerExit=2`）。`ControlDump=` 只是预定输出路径，回收目录中没有该 `.dmp`。不能把这次失败记为「未复现」，也不能把活体栈记成转储取证成功。交接记录所述的 `NotepadCrashEvidence.cs` 对照异常命令已不在 `.dump` 前调用 `.sympath`；真实对照尚未验证写入和离线重开。

### `f11a6ef3988d4477b1f2fb05c4c0efd5`：历史复测后的清理阻塞（当前状态未核验）

- `evidence/cleanup.txt` 第 1–7、25–30 行：清理前后均未发现对照布局注册项；删除 System32 对照文件报 `Win32Error=5`，扫描看见 `Notepad`（当时 PID `15484`）映射该文件，部分进程模块无法枚举。`FileRemaining=True`、对照哈希前后不变，`ExitCode=2`。产品 SHA-256 前后均为 `96A33BABEE4136BDFA9A7FB6ADFAE4321D4B34ADC28E94979CD74DBEECBE1EC9`。`15484` 不是活体栈所属的 `13176`；扫描不证明它是唯一映射者。
- 后续归档的清理日志 `3d57350b128b4c9993fef1c3bf2aa0ec/evidence/cleanup.txt` 当时仍报 `Win32Error=5`、`FileRemaining=True`、`ExitCode=2`，再次记录记事本 PID `15484` 映射；未发现对照布局注册项，对照和产品哈希均不变。复测前的成功记录不适用于这次残留。
- `74aee966bdba4787a4d341f35f4f59c4/evidence/window-identity.txt` 复核该 PID 在同一交互会话中映射了确切模块：有一个可见窗口，但无匹配诊断标题（`MatchingDiagnosticTitles=0`），不能确认它属于诊断任务，未执行窗口操作（`WindowAction=None`）。该记录不证明文件或映射现在仍在；若另行授权清理，应重新核验，不按历史 PID 或仅凭模块映射关闭窗口。

### 独立原生 CDB 冒烟（不是对照转储）

`52aa86912d30416f9e50c95b3b1c160b/evidence/smoke-375695a179d2452fae4fa35ac84dbe31/` 的 `capture.txt`、`reopen.txt` 和 `result.txt` 记录：独立原生测试目标 `CdbSmokeTarget.exe` 触发第二次机会 `0xC0000602` 后，CDB 写出 `self-produced.dmp`（11,031 字节），另一 CDB 会话成功重开并读出异常。此结果只验证调试器的写入和重开流程，不是记事本或 `Imm32AotControl.ime` 对照转储。

### 正式独立 demo（区别于早期最小对照）

正式 demo `Imm32AotThreadExitRepro.ime` 当时在独立 STA 的原生 EDIT 场景中零按键复现，活体跟踪记录同线程分离、`ImeSelect(FALSE)` 和第二次机会 `0xC0000602`，并写出小型转储。该 demo 的记事本尝试在确认前台编辑器布局和模块映射前超时，尚无记事本结论。证据及场景区分见 [ISSUE.md](../../Docs/NativeAotImm32ThreadExit/ISSUE.md)。

## 历史环境及故障版本

下表记录取证时的工作区与 VM；当前本地及远程环境未核验。

| 项目 | 当时的值 |
| --- | --- |
| 开发工作区 | `C:\lindexi\Code\Workspace\IME`，产品源码在 `XiaoXiIme\`，用户提供的运行时源码在 `dotnetruntime\` |
| 历史测试 VM | `DESKTOP-6BGIATF`，Windows x64 build `26340`，交互用户 `linde`，沙箱以管理员身份运行 |
| 虚拟机发布目录 | `C:\lindexi\IME`；宿主实际加载 `C:\Windows\System32\XIAOXIIME.IME` |
| 应用 | Microsoft Windows Notepad `11.2609.11.0` x64，包名 `Microsoft.WindowsNotepad_11.2609.11.0_x64__8wekyb3d8bbwe` |
| 已确认的故障 `.ime` | 文件版本 `1.0.0.0`；PE 时间戳 `0x6B02E936`；SHA-256 `96A33BABEE4136BDFA9A7FB6ADFAE4321D4B34ADC28E94979CD74DBEECBE1EC9` |
| 匹配的私有 PDB | `XiaoXiIme.ImeModule.pdb`，GUID `d214a16c-0c59-4b10-ada7-4d9dd20661e8`，Age `34` |
| 用户报告 | `85294de7-c3e8-48b6-abc8-27e5e5e6d555`；PID `0x3698` / `13976`，异常线程 `0x30C0` / `12480` |
| 故障 | `0xC0000602`；RVA `0x21F34C`，解析为 `.ime!RaiseFailFastException+0x44` |

这是分析时实际核对的版本，不应假设虚拟机或本机发布目录以后仍是这版。重新构建和部署前先保留原二进制、PDB 和转储，不用同文件版本的其他 PDB 替代。

### 构建与源码来源核对

| 核对对象 | 已证实的值与边界 |
| --- | --- |
| 对照项目还原与链接 | `XiaoXiIme/artifacts/obj/Imm32AotControl/project.assets.json` 的 `net10.0-windows/win-x64` 目标解析到 `Microsoft.DotNet.ILCompiler` 和 `runtime.win-x64.Microsoft.DotNet.ILCompiler`，均为 `10.0.15`；下载依赖含 `Microsoft.NETCore.App.Runtime.NativeAOT.win-x64` `10.0.15`。同目录 `release_win-x64/native/link.rsp` 指向该 NativeAOT 包的原生库，活体调试加载的对照 PDB 也包含该版本的 `Runtime.WorkstationGC.lib` 路径。 |
| 对照包的提交元数据 | 本机 NuGet 缓存中上述三个包的 `10.0.15` `.nuspec` 均声明 `https://github.com/dotnet/dotnet` 提交 `8e0f8ed2312954727f8919d7d1c2f78b483b4ee7`。x64 ILCompiler 包内 `tools/ilc.exe` 含 `10.0.15+8e0f8ed2312954727f8919d7d1c2f78b483b4ee7`，对照 PDB 中的 NativeAOT `System.Private.CoreLib` Source Link 也指向该提交。 |
| 故障产品二进制 | 与故障 `.ime` 匹配的归档 `XiaoXiIme.ImeModule.pdb`（见下文初始转储证据）记录了 `Microsoft.NETCore.App.Runtime.NativeAOT.win-x64/10.0.15` 下的 `Runtime.WorkstationGC.lib` 等链接输入路径，NativeAOT `System.Private.CoreLib` Source Link 也指向同一 `dotnet/dotnet` 提交。当时核对的产品 assets 与 x64 链接响应文件同样指向 `10.0.15`，但不是故障当次的编译记录；归档 PDB 的 NativeAOT 库路径和 Source Link 也不等于当次 `ilc.exe` 的精确身份或全部输入的证明。 |
| 本地 `dotnetruntime` | `.git/config` 的 `origin` 为 `dotnet/runtime.git`，`.git/HEAD` 为 `081d220c0a773ffb7c6bea6b48727833576a65ef`；工作树 `eng/Versions.props` 的主、次、补丁版本为 `10.0.4`。这与包声明的 `dotnet/dotnet` 提交不是同一仓库中的同一提交；目前无已核实的上游组件提交映射或源码树比对能证明本地 checkout 精确对应包或故障构建。 |

因此可确认对照与故障模块的部分 NativeAOT 链接/源码来源一致，不能宣称完整工具链精确一致、故障当次编译器已唯一确认，或本地运行时源码与任一产物逐文件对应。要补齐证据，需查到可绑定故障 `.ime`/PDB 的原始构建记录及实际工具/包标识，并取得该 `dotnet/dotnet` 提交的 `src/runtime` 到 `dotnet/runtime` 的可验证映射。不要用启动 UI 或诊断运行器所用的 .NET 版本代替。

## 最短的已证实产品复现流程（独立复现参考）

1. 在真实虚拟机里使用已部署的故障版。打开新版记事本的空文档，确认前台焦点在正文控件上。
2. 确认正文线程已选中小希布局（示例 HKL `0xFFFFFFFFE0200804`），并确认该进程加载了 `System32\XIAOXIIME.IME`。
3. **不发送任何字符或选词按键，也不刻意等待空闲。** 对刚创建的测试窗口发出正常关闭请求 `WM_CLOSE`；不要强杀进程，也不要直接调用 `ImeSelect(FALSE)` 代替系统清理。
4. 判定标准：产生新的记事本转储，核对 PID、`.ime` 时间戳和匹配 PDB，确认异常 `0xC0000602` 且退出栈命中 `LdrShutdownThread → imm32 → ImeSelect → RhpReversePInvokeAttachOrTrapThread2 → ThreadStore::AttachCurrentThread → RaiseFailFastException`。复现时外部观察到的进程退出码曾是 `0xFF`，**不能仅靠退出码判断是否是同一个崩溃**。

| 对照 | 观察 |
| --- | --- |
| 正常输入后保持打开 7 分钟 | 两次均在等待期间存活，发出正常关闭请求后异常退出；其中 PID `15172` 的新转储确认同一故障栈 |
| 正常输入后立即关闭 | PID `14900`，不到 7 秒便取得新转储，命中同一栈 |
| 零按键、零等待关闭 | PID `13828`，`LoadedIme=True`，约 7 秒取得新转储，确认同一异常码、入口和退出栈 |
| 零按键、附加调试器后关闭 | PID `0x1960`，在同一线程上观察到分离、IMM32 回调、fail-fast 的完整顺序，见下节 |

因此具体输入、词库匹配、候选显示以及长时间空闲不是必要条件。尚未验证所有应用或架构，也没有证据说明每次关闭必崩。

## 已验证的直接机制

匹配私有 PDB 的转储将 `ImeSelect` 定位到原生进入托管的序言 `+0x15`，早于 C# 方法体。`ThreadStore::AttachCurrentThread` 的反汇编先调用 `Thread::IsDetached()`；为真时直接进入 `RaiseFailFastException`。这不是从 `ImeSelectManaged` 抛出的可捕获异常，也不是缓冲区溢出的证据。

活体断点实验在记事本进程 `0x1960`、**同一线程 `0x10F8`** 上按顺序记录到：

1. `LdrShutdownThread` 期间进入 `FiberDetachCallback`，随后进入 `ThreadStore::DetachCurrentThread`。
2. 同一条线程仍在 `LdrShutdownThread`，IMM32 调用 `.ime` 的 `ImeSelect` 导出；记录到 `select=0`，即 `ImeSelect(FALSE)`。
3. 同一线程报告第二次机会异常 `0xC0000602`。已有转储进一步确认它来自 `AttachCurrentThread` 的分离检查。

本地运行时源码与故障模块私有符号反汇编的行为对照（不是精确源码提交映射）：

| 源码（相对于 `dotnetruntime/src/coreclr/nativeaot/Runtime/`） | 关注点 |
| --- | --- |
| `windows/PalMinWin.cpp` | `PalInitComAndFlsSlot` 经 `FlsAlloc` 注册析构回调；`PalAttachThread` 把运行时线程关联到 FLS；`FiberDetachCallback` 调用 `RuntimeThreadShutdown` |
| `startup.cpp` | `RuntimeThreadShutdown` 区分整个进程关闭和单线程关闭；单线程退出进入 `DetachCurrentThread` |
| `threadstore.cpp` | `DetachCurrentThread` 从 GC 线程列表移除线程，调用 `Detach` 和 `Destroy`；`AttachCurrentThread` 拒绝已分离线程 |
| `thread.cpp` | `Thread::Detach` 清理 GC 分配上下文并设置分离状态；`Destroy` 随后释放线程资源 |
| `XiaoXiIme/src/XiaoXiIme.ImeModule/Exports/ImeExports.cs`（产品仓库） | `ImeSelect` 的 `UnmanagedCallersOnly` 导出是这次回调经过的边界，而不是发生故障的 C# 方法体 |

**已确认的问题是两套线程退出生命周期发生冲突：FLS 析构先结束 NativeAOT 线程状态，之后 IMM32 仍在这条线程上回调托管导出。** 产品转储与独立对照的活体栈均支持这一路径；产品专有链接依赖不是触发的必要条件。是否应由 NativeAOT 或调用方处理仍未定责。不要尝试删除 `IsDetached()` 的保护：此时 GC 线程状态已被清理，跳过检查不会让托管执行重新变得安全。

## 自动化入口与证据位置

诊断代码在 `XiaoXiIme/tests/XiaoXiIme.SandboxRunner`。在构建机发布测试运行器，在虚拟机沙箱只运行已编译产物。以下是历史诊断与独立复现参考中的运行器参数（`evidence` 或 `analysis` 为沙箱输出目录），不是本次操作指令或产品安装命令：

| 参数 | 用途 |
| --- | --- |
| `probe-notepad-close evidence 0` | 最短复现：不敲键，正常关闭；留下异常退出及对应 PID 转储。参数 `1` 是单键对照，零按键已足够，不需要它来证明触发 |
| `observe-notepad-idle evidence 0` | 完整键盘用例后立即关闭；第三个参数为等待秒数，省略默认 420 秒 |
| `trace-notepad-close evidence` | 调试器附加到新建的记事本进程，等 `TRACE_READY` 后关闭。记录 `FLS_DETACH`、`RUNTIME_DETACH`、`IME_SELECT` 和异常；不要附加到用户正在编辑的记事本 |
| `analyze-notepad-dump analysis` | 离线解析转储、异常上下文、调用栈和模块符号 |
| `trace-imm32-aot-control evidence` | 独立 x64 对照：活体异常栈与私有 PDB 已验证，小型转储产出与离线重开尚未验证 |
| `cleanup-imm32-aot-control evidence` | 独立清理并核验对照布局、残留文件和产品哈希；按日志判断结果 |

`NotepadKeyboardSmoke.cs` 负责启动、确认焦点与布局、正常关闭和转储回收。`NotepadCrashEvidence.cs` 负责取证及调试器调用。`XiaoXiIme.SandboxRunner.csproj` 的可选发布属性：`CrashEvidencePath` 指向保存了 `faulting-XIAOXIIME.IME` 与 `XiaoXiIme.ImeModule.pdb` 的证据目录；`CrashDumpPath` 可指定新转储，运行器会按当前固定输入名读取它；`CrashDebuggerPath` 指向构建机 Windows SDK 的 x64 Debuggers 目录。默认产品发布不带转储或调试器。

历史测试 VM 当时为 **`Notepad.exe` 单独启用了 WER 小型转储**，当前 WER 状态未核验：注册表 `HKLM\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps\Notepad.exe`，`DumpType=1`、`DumpCount=3`，目录 `C:\Users\linde\AppData\Local\XiaoXiIme\CrashDumps`。转储可能含进程数据，应保留在本地诊断目录，不提交仓库、不随产品分发。最多三份会轮转，先保存已匹配的证据。

以下产品证据路径相对于工作区根目录；对照活体栈、PDB 和清理日志见前文。若新会话拿不到本地沙箱产物，先确认是否需要另行补采，不要依赖旧的临时路径或用产品转储代替对照转储：

- 初始匹配转储、`.ime`、PDB：`.coding-agent/sandbox-results/3e875e5e35a749fd854d65a63623bd9f/evidence/`
- 初始调用栈及 `IsDetached` 反汇编：`.coding-agent/sandbox-results/7b5c696eea3c4d5080b62f51491c76c9/analysis/notepad-stack.txt`
- 零按键日志及转储：`.coding-agent/sandbox-results/4d7ea359b6164b0780635db3e21b952f/evidence/`
- 零按键转储的调用栈：`.coding-agent/sandbox-results/3efbdf742c05457b906788b372ef71b3/analysis/notepad-stack.txt`
- 故障二进制的 FLS/运行时清理反汇编：`.coding-agent/sandbox-results/a40445b0280a4c9eb11ba1e3c174df3e/analysis/notepad-stack.txt`
- **活体顺序证据**：`.coding-agent/sandbox-results/a7d30a646bfb4db3aae37cc659b954b4/evidence/notepad-close-trace.txt`，第 162–202 行

## 后续独立复现参考和放行条件（不在本次范围）

1. **先完成复测后的清理核验。** 上述历史清理返回 `2`；若另行授权清理，先核验当前文件及映射状态，不按历史 PID 操作。清理成功后核验对照布局、文件及产品哈希，再决定是否补采独立 x64 对照转储。验证 `.dump` 确实独立执行、文件非空且能用对应 PDB 离线重开，再把它登记为对照转储。复测前后核验对照布局、文件及产品哈希；不以退出码或打印出的转储路径替代证据。
2. **以独立对照作为运行时回归输入。** 先补齐构建到源码的映射，再定位 FLS 清理与 Windows 后续回调之间的生命周期契约。现有活体证据已排除「只有产品能复现」的假设，但不等于修复或最终定责。
3. **修复必须通过同路径验证。** 保留 C# / NativeAOT 架构，除非另有明确决策。不要以重写 C/C++、禁用保护、猜测私有 TLS 偏移、忽略所有 `ImeSelect(FALSE)`，或增加 C# `catch` 代替根因修复。正常的 `ImeSelect(FALSE)` 负责停用输入上下文、使异步输入失效、取消组合并重置会话，不能统一跳过。若改运行时，明确验证 GC 线程注册与清理的安全性，不能让已清理的状态重入。
4. **回归验收。** 以归档的产品线程 A 转储作为产品失败基线；需要进一步比对时，先修正离线系统符号路径，必要时另采 A 上 FLS 分离与 `ImeSelect(FALSE)` 的先后顺序。产品放行用无调试器的 `--acceptance` 单独验证：A 确认正常退出，同一宿主存活，B 在同一 TID/HIMC 再次真实提交，且最终 `PASS`、进程退出码为 `0`。另验证记事本零按键关闭不再崩溃；正常切换、取消组合和关闭重开仍有效，旧会话不误提交。x64 和 x86 分开验证，必要时保留修复前后断点顺序和私有 PDB。取证 `VERIFIED`、一次键盘冒烟或几次未复现均不构成放行。

**本轮仅离线核对并更新文档。** 未修改产品、运行器或 `dotnetruntime`，未操作 VM。早期 `Imm32AotControl.ime` 已复现并归档完整活体栈及私有 PDB；其历史复测后清理返回 `2`，当前清理状态未核验。该早期对照的 dmp、精确构建来源映射和修复验证仍未完成。
