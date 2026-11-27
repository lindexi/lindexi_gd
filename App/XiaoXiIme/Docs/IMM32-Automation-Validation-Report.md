# IMM32 自动化增量验证记录

## `-=` 翻页、自然码 `dd` 与 WPF 重复上屏回归

- `-` / `=`：修复候选翻页按键的路由，本轮保留既有改动并运行托管回归；没有在 VM 中重新做翻页交互实测。
- 自然码 `dd`：保留双声母回退，不将 `dd` 判为合法单音节；选词仅消费原始输入范围，其他双拼方案不启用该回退。本轮由 ImeCore 和 Dictionary 的用例覆盖，未重新做真实窗口取词实测。
- WPF：旧式独立组合 UI 声明使一次结果进入 `WM_IME_CHAR` 和键盘字符预处理两条路径；诊断宿主阻断 `END` 仍重复，单独阻断 `WM_IME_CHAR` 后只插入一次。产品改为声明行内组合，异步取消先发空组合更新再发一次 `END`。未屏蔽产品字符消息，未做文本去重。

完整测试（通过 / 失败 / 跳过）：ImeModule.Tests **248 / 0 / 0**、ImeCore.Tests **694 / 0 / 0**、Dictionary.Tests **429 / 0 / 0**。

专用 Windows x64 VM 仅使用自有 WPF TextBox 和原生 EDIT。`da` 只有预编辑、无结果；Esc 后两者为空、0 次结果和 1 次结束；空格后均为“大”、1 次结果和 1 次结束；连续两次 `da` + 空格均为“大大”、2 次结果和 2 次结束。WPF 直接消费 `WM_IME_COMPOSITION`；EDIT 仍收到 `WM_IME_CHAR`/`WM_CHAR` 回退。部分提交仍组合由消息级测试覆盖，未做部分提交的系统实测；未测用户白板、其他 WPF 版本或 x86。

工作区原始日志位于 `.coding-agent/sandbox-results/<任务ID>/text-trace.log`：基线 `6bad541d5c7d42258185379afa68f21f`，逐消息隔离及调用栈 `a10ed9763c0d4f5f8665ce7b616d5b5c`，最终带计数复跑 `0308aba35daf4d009f917a6bd4731a4f`。复跑通过功能断言并恢复及卸载临时 HKL `E0810804`；测试进程内删除映像 `XX82accb.ime` 时访问被拒。进程退出后独立清理日志 `d4a6221f5da64227b36ab596a27372f5` 确认临时注册和映像均已删除、原版映像 SHA-256 未变。未部署正式产品。

## 本轮最终结果（真实失败与阻塞）

v4b报告与49次焦点快照已读：7 PASS/1 FAIL，foreground/window=263684、focus/edit=525786，HIMC=0；测试宿主未创建关联HIMC。仅修复真实测试上下文建立和成对清理，不改业务代码、不弱化断言。精准托管ProductSettings/UI34/34、Integration113/113、IPC27/27、Module148/148，零失败跳过；最终增量Release0警告0错误，宿主/runner已发布，之后未改代码。

产品最终报告已回读，任务4364a7698abc4a4494ee1960a2b6983e，7 PASS/1 FAIL。新焦点数据197944/0/0，推送目录遗留.stop导致目标退出，旧focus日志前49行不是本轮数据。此轮不证明HIMC修复有效，不判定bar抢焦点；真实关于和正常退出/锁重获通过。UI publish/actual DLL SHA均1F8E0F320ED58603A6D6AE0366C3B7AED9214CCE7A82D54A98DFE65CE03E70CC；EXE均E90C359F2AA675755D50E94D468E6E18F56FB6609F9AC5002E90930B83A4CB85。source SHA、宿主/runner actual身份尚缺。产品cleanup正常请求退出无Kill，无全系统残留枚举。

本轮IMM尝试在推送product-ui-batch阶段HTTP100秒超时，客户端-1073740791；integration-run未启动，xx→小希、单HIMC、多HIMC均未执行，没有本轮原生actual SHA或安装/卸载清理证据。负载尚未收缩为真正共享runtime，不重复大包上传。剩余真实验收：干净紧凑产品重跑、前台授权诊断、设置应用/取消/默认、隐藏恢复及bar不抢焦点、IMM三场景/原生身份/清理。bar拖拽菜单、活动原生组合保存未覆盖；两方案重启真实取词本批明确不完成；不扩DPI、不用fake窗口冒充IME。原始报告未收录在本仓库。

## 本轮 product-ui v4b 原始报告回读（历史起始记录）

v4b 实测报告已回读：7 PASS / 1 FAIL。real-bar、tested-pid（9460）、about-real-button、normal-ui-exit、entry-lock-reacquisition、reopened-normal-exit、native-target-normal-exit 通过。关闭关于后 RestoreTargetAsync 抛出真实 EDIT 未恢复稳定 foreground/focus/HIMC；流程提前失败，不能视作状态栏不抢焦点或设置交互通过。cleanup 标记 single-instance-exit-request-no-kill。IMM xx/单/多未在该报告执行；其余 pending 为 bar-drag-menu、active-native-composition-save、scheme-save-restart 以及 real-edit-focus-HIMC。先诊断 Windows 前台授权与产品 noactivate，保留严格断言，不增加延迟刷通过。本轮新编译、冻结身份及真实 IMM 结果尚待执行，不冒用下方历史证据。

## G02 第二轮证据回读归档（历史限定批次）

本次已完成原始证据回读及文档写入，未追加代码、构建或重跑；下方run-05保留历史身份。下文保留逐项回读摘要；原始证据未收录在本仓库。Windows x64管理员任务 `346c21d6abff471f9f4a0d447d4c1cfa`，integration UTC 09:36:45–09:37:03、exit0且未超时，skip-tsf；不是首轮exit0推断定位。

- xx → 小希 PASS（展开日志35–36行）；单HIMC PASS（72–73行，START=2/END=2/Results=[你]）；多HIMC PASS（135–138行，两处各Results=[你]、CallbackError空）。
- 真实候选UI五项独立PASS（112–127行），DPI=96/scaling=1、候选HWND=0x250430；第一EDIT rect=(130,204,646,293)，第二=(130,249,646,338)，无键回切=(130,204,646,293)，移动=(260,284,776,373)，工作区边缘=(1404,921,1920,1010)，work=(0,0,1920,1032)。瞬态match=False后收敛，未放宽断言。xx段未记录实际候选矩形/DPI，单HIMC只有96 DPI锚点，不冒用多HIMC位置数据。
- 系统实际加载 `C:\WINDOWS\system32\XIAOXIIME.IME` SHA=`CE79122E014152350ED842094D88367999D9C7E71D932F0E6EA3A20DE6DC31B7`（26/60/97行），与安装源、manifest4480–4482行、publish-sha2561322/1324行一致。source-sha256冻结源码与publish中对应身份已核对，未重算当前工作树全部哈希。
- 两owned HIMC销毁及第二EDIT销毁True；UI最终hidden、主动Kill后Exited=True/ExitCode=-1；cleanup PASS移除E0200804及System32/SysWOW64 IME，无重启/延迟删除。Host StopExisting路径已核对，无清理后全系统进程枚举。ProjectTermSmoke exit0且stdout明确passed，不等于其它真实应用矩阵。
- 既有CandidateAnchorTests19/19、Integration76/76，本次未重跑；阶段一模块148/148、IPC27/27及更旧测试均不计作第二轮新执行。准确警告边界：本轮增量0警告/0错误，不否定历史非增量18 CA1416 + 1 CS8604（共19警告）/0错误；原“19个CA1416”已修正。payload-build --no-build不是新的编译计数，解决方案仅还原exit0不计全测试通过。

限定批次无最终失败，无需修复/发布重跑或扩大环境。G02保持开放：FdwInit/来源合同、COMPOSITIONFORM/CFS_EXCLUDE、坐标单位/多DPI、句柄世代、同HWND多HIMC、新激活无缓存锚点仍为发布合同/验收缺口及本批未覆盖风险，未伪称通过或确定失败；G03多DPI/多屏另保持未完成，没有批准延期分级或扩展执行。

## 2026-10-13 run-05 最终归档（历史批次结论）

本节优先于下方历史轮次的“未执行”“待真实验收”“停止产品修改”等状态。本次仅读取已有证据并修改文档，没有修改业务/测试代码，没有构建、发布或重跑测试。

### 轮次、根因与修复边界

- 历史托管增量的 136/136、18/18、41/41 与旧 `retest.json` 引用不属于 run-05；旧报告未找到的结论不表示当前没有业务证据。
- 首轮 Unicode 复验的 xx/单 HIMC PASS、多 HIMC FAIL 与整体 exit 14 保留为历史；本仓库未收录 run-02 原始报告，不能声称本次重新读取过。首个切换后第一组合丢失与字符串单位错误是不同问题。
- 失活错误 Escape/ResetSession 的根因：将 `ImeSetActiveContext(active=0)` 的临时焦点失活误当作 HIMC 逻辑会话结束，主动取消并删除第一会话，破坏交错输入保留合同。当前 `ImeExports.cs:332–348` 已保留组合与 Host 会话；真正取消和清理仍由 Escape、NotifyIME CPS_CANCEL、ImeSelect(select=0)、ImeDestroy 各自负责，不通过重建第一组合或放宽断言制造 PASS。
- run-04 已取得三场景 PASS，历史证据摘要在第 214、246、279 行记录了这三项结果；其安装源 SHA 记录在该摘要第 31 行，不是 run-05 身份。已读取的 run-04 展开没有 run-05 的 actual-loaded 路径/SHA 闭环；最终诊断异常修复后的证据以 run-05 为准，不替代最终批次。
- 独立复核修复了可选生命周期诊断的异常边界：环境变量读取、原生查询和文件写入置于保护范围；对 IOException、UnauthorizedAccessException、ArgumentException、NotSupportedException、SecurityException、DllNotFoundException、EntryPointNotFoundException 写 Debug 诊断，避免预期诊断失败逃逸导出。不是屏蔽业务失败，也不宣称捕获全部异常。
- 精确异常回归范围见 `tests/XiaoXiIme.ImeModule.Tests/ImeLifecycleDiagnosticsTests.cs`：ImeSetActiveContext 的 null/空/空白/非法路径与 active=0/1 共 6 例（不是各路径与 active 的完整笛卡尔积）；ImeSelect(select=1) 的 null/非法路径共 2 例，HIMC=0。未直接注入磁盘满、权限拒绝、SecurityException、DLL/入口缺失，也未覆盖非零真实 HIMC 的异常路径；非 Windows 会提前返回，不能当作 Windows 文件错误覆盖。

### run-05 原始文件逐场景核验

原始文件未收录在本仓库。以下为主报告 `integration.json` 第 203–225 行及执行记录的回读摘要：Windows 10.0.26200 x64 管理员任务 `af8a5792d9a7427abdf2b66aa437c0b8`，UTC 08:23:46–08:24:04，exitCode=0、timedOut=false、skip-tsf。三个 PASS 均在主报告第 207 行原始 StandardOutput 中，不仅依据整体退出码。

| 场景 | 实际核验结果 |
| --- | --- |
| xx → 小希 | `PASS real-ime-keystroke-commit`；真实 SendInput，HIMC=0x13D0497，精确上屏及稳定观察通过。 |
| 单 HIMC 取消/重组/提交 | `PASS real-ime-composition-lifecycle`；HIMC=0x13F0497、session=himc:13f0497，START=2、END=2、Results=[你]，Updates=[(n,n,1),(ni,ni,2),(n,n,1),(ni,ni,2)]，CallbackError 为空。 |
| 双 EDIT/多 HIMC 交错与回切 | `PASS real-ime-multi-himc-isolation`；第一 EDIT=0x2A0444/HIMC=0xE9057D/session=himc:e9057d；第二 EDIT=0x4304A4/HIMC=0x2600343/session=himc:2600343。第二 Escape 取消后重组提交，回切第一保留 ni 并提交；两处各精确上屏一次“你”，未跳过焦点切换。 |

生命周期日志第 15–26 行显示 active=0 前后第一 n/ni 的 IMM 长度和 cached reading 长度均保持；第 27 行第二 Escape 清空 Host 组合；第 30–31 行回切第一仍为长度 2；第 39–43 行 deselect 仍执行 Escape/ResetSession。暂时失活保留不等于永久保留逻辑会话。

ProjectTermSmoke 结果第 5 行 exitCode=0，第 260–264 行 `ime-set-active-preserve:fullPinyin:xiaoxiaimuyi` expectedText/actual 均为 `xiaoxiaimuyi`、count=1；标准输出为 passed。该结果是模块合同冒烟，不是额外真实目标应用矩阵。

### 实际负载、身份与清理

- 主报告第 207 行 `loaded-ime-identity`：实际加载 `C:\WINDOWS\system32\XIAOXIIME.IME`，LoadedByImmBeforeProbe=True，SHA256=`D52ABB2B687193624FEB433764B433E3F25453705A5D4B194843EC18EDC0C274`。不是仅以待安装源文件身份推断系统负载。
- 同一 SHA 与负载清单第 4460–4462 行的 `native/win-x64/ime/XiaoXiIme.ime`（4695552 bytes）及发布 SHA 清单第 1099/1103 行一致。发布来源经清单核对为 run-05，不是 run-02。
- 当时找到了最终源码 SHA 清单。未在本次重算全部源码/负载哈希，不能将清单存在说成全部文件现状重新验签。最终 schemaVersion=3 payload 含 x86/x64 native、CLI、Host、tests、UI 等文件清单；TSF 随负载存在不表示已验收。
- 外部 Host 报告 fullPinyin、IsUsingFallbackDictionary=false；真实场景要求无 load error。主报告记录 FirstDestroyed=True、SecondDestroyed=True、SecondEDITDestroyed=True；cleanup Succeeded=true/exitCode=0，E0200804、System32/SysWOW64 IME 文件删除，PendingDeletePaths/RetiredFilePaths 均空、RebootRequired=false。不能外推完整 Host 停止、升级或用户数据策略矩阵。

### 已有测试记录及证据完整性

- 最后一次专用测试日志已回读：生命周期筛选 `ImeSetActiveContext|ImeSelect|ImeDestroy|ImeLifecycleDiagnosticsTests`，15/15，失败/跳过均 0，exit 0，244 ms。它不是 15 条真实 Windows 系统场景；本次未重跑。
- 上一轮摘要：诊断异常回归 8/8、模块 148/148、Integration 57/57、Release 0 警告/0 错误；x86 AOT 既有 5 条 IL2026、1 条 IL3053。8 例定义已核对，但本次未取得这些摘要对应的独立原始测试/构建/发布日志，故不能把 8/148/57 及警告数写成已从原始日志复核的计数，也不能把 Release 零警告外推为 x86 AOT 零警告。
- 最终证据归档当时包含两阶段 stdout/stderr/execution、主 integration.json、生命周期 log、ProjectTermSmoke result/stdout/stderr、源码/发布 SHA 清单和 payload manifest；evidence-summary 是辅助展开，不替代原始文件。当时未找到 TRX，也未取得覆盖率报告；缺失项明确保留，不补造。
- **本批三个既有 x64 Win32 EDIT 场景完成；G04/G05/G14 完整矩阵仍不关闭。** 多应用/版本/控件、x86 实际目标进程、不同线程/进程多会话、真实候选 UI 定位/DPI、补充字符/result-reading/异常 UTF-16、Host/IPC 故障恢复及长时间稳定性仍未由本批覆盖。14 项发布差距总数和分类不因此变更。

---

以下为历史记录；其中“本轮/当前/最新”仅指各自记录时点，不代表 run-05 最终状态。

> 本文“本轮/当前”指各历史增量轮次，摘要不是本次盘点重跑。已知未覆盖项见上方各批次摘要。本仓库未收录历史冒烟复测报告或这些轮次的构建产物；本次未执行业务复验；基础设施探针不证明输入法功能。

## 文档基线与本次范围

当时的范围与 [IMM32 交接](./IMM32-Handoff.md) 一致：保留传统 IMM32 主线，不引入 TSF 验收、RIME 或参考项目运行时依赖。

现有 `real-ime-composition-lifecycle` 已实现真实 `SendInput` 的 `n/i → Escape → n/i → Space`、同一 EDIT/HIMC 的 START/组合/取消/结果/END、正式 fullPinyin package 与外部 Host 会话检查；代码存在不等于 Windows 验证完成。本轮未重复实现该场景，也未修改产品代码或生成代码。

本轮扩展现有多 HIMC 仿真：第二 HIMC 取消后重新组合并提交，验证第一 HIMC 的组合、候选及 Host 会话不受影响。测试仍使用真实 Host/IPC 与内存 IMM 上下文，不是 Windows 系统 HIMC 测试。

## 修改

- `tests/XiaoXiIme.ImeModule.Tests/ImeHostBridgeTests.cs`：扩展并重命名现有隔离测试为 `ImeToAsciiEx_WhenSecondHimcCancelsRecomposesAndCommitsThenKeepsFirstSession`；新增 Escape 取消、无结果、Host 会话清空、第一会话保持原 reading、第二会话重新组合和 Space 提交验证。
- `tests/XiaoXiIme.ImeModule.Tests/README.md`：注明覆盖与仿真边界。
- 本文件：保存专用构建/测试结果和真实验证阻碍。

## 本轮专用工具验证

1. 新增断言首次编译发现 CS1061：`CompositionText` 无 `Text` 属性。读取模型后改为既有 `Reading` 属性，未修改公共模型。
2. 定向测试：通过 1/1，失败 0，跳过 0，208 ms，net10.0。
3. `XiaoXiIme.ImeModule.Tests` 全量：通过 136/136，失败 0，跳过 0，586 ms，net10.0。扩展原测试而非新增测试，因此测试数量不变。
4. `XiaoXiIme.IntegrationTestHost` Release 构建：成功，0 警告、0 错误，1.36 s。
5. `ProductionDictionaryKeystrokeTests`：通过 18/18，失败 0，跳过 0，16 s，net10.0。该结果不是实际发布 package 的系统验收。

构建产物：`artifacts/bin/XiaoXiIme.IntegrationTestHost/release/XiaoXiIme.IntegrationTestHost.dll`。
测试产物：`artifacts/bin/XiaoXiIme.ImeModule.Tests/debug/XiaoXiIme.ImeModule.Tests.dll` 和 `artifacts/bin/XiaoXiIme.IntegrationTests/debug/XiaoXiIme.IntegrationTests.dll`。
结果来自专用构建/测试工具返回的日志摘要；本文件为人工整理记录，不是 TRX。未生成本轮 TRX 或覆盖率文件，未运行 CLI 全量或完整解决方案测试。

## 真实 Windows 沙箱阻碍

历史当轮记录因执行限制没有运行真实沙箱测试。该限制不是当前环境结论：专用 Windows 沙箱执行能力可用；本次文档盘点按范围不运行业务测试，也没有取得新的功能 PASS。

**本轮没有真实沙箱 PASS，也没有本轮沙箱证据路径。** 本文件不是沙箱证据。旧复测报告只属于历史 `xx → 小希` 基础冒烟，不可引用为本轮生命周期或多 HIMC 验证结果。

## 后续分派范围（未完成）

1. 在允许运行编译后测试执行器的管理员 Windows 沙箱中，以最新完整双架构负载、正式 fullPinyin package、外部 Host 执行现有 `xx` 与生命周期场景；按现有主线跳过 TSF，保留报告、消息轨迹、HIMC/会话与模块调用计数和清理结果。
2. 上述真实生命周期通过后，执行下文已实现的真实双 EDIT/不同 HIMC 场景：交错组合、第二上下文取消/重组/提交、第一上下文状态隔离、焦点切换和上下文销毁；此前“再扩展”的代码工作已经推进，待完成的是实测与失败诊断，不把内存测试当作真实验收。
3. 不同应用及 x86/x64 目标应用矩阵仍未实现或验收。现有文档未指定具体应用清单，需监督者确认可自动化目标后单独分派，避免臆造产品矩阵。
4. 上述真实验收未完成前，交接文档的全链路、多应用、多会话、组合/取消/提交及架构验收勾选继续保持未完成。

## 后续增量：真实双 EDIT/多 HIMC 场景（代码完成，待真实验收）

本节追加本轮结果，不覆盖上述上一轮测试记录。此前第 2 项的代码扩展已推进；真实生命周期通过及双 HIMC 验收仍未完成。

### 文档评估与实现

当时核对了交接约束（参见 [IMM32 交接](./IMM32-Handoff.md)）和既有自动化记录：要求多会话、多应用及 x86/x64 目标进程，但未指定具体多应用清单或逐应用测试矩阵。本轮仅扩展既有 Win32 EDIT 宿主，不选择新应用、不增加外部应用依赖、不修改产品模块。

- `tests/XiaoXiIme.IntegrationTestHost/Program.cs`：注册 `real-ime-multi-himc-isolation`，沿用原场景执行顺序，位于单 HIMC 生命周期之后。创建同一窗口中的第二 EDIT，并用 `ImmCreateContext` 为两个 EDIT 显式关联不同的真实 HIMC，验证关联身份及打开状态。
- 真实场景代码通过 `SendInput` 交错输入第一 `n`、第二 `n`、第一 `i`、第二 `i`；第二 Escape 取消，再 `n/i/Space` 重组提交；最后第一 Space 提交。检查每步真实组合更新、读音、光标、START/END/result、`ni` 首候选“你”、实际 HIMC 对应外部 Host 会话、焦点/HKL、稳定期及隔离。要求正式 fullPinyin package，无 fallback/load error；最终两个 EDIT 均精确上屏一次“你”。
- 收尾恢复先前关联、销毁两个自建 HIMC 与第二 EDIT，记录并断言清理结果；原窗口清理由既有窗口线程完成。输出两个 EDIT/HIMC/会话、观察器轨迹及累计原生调用计数，要求至少 9 次 `ImeProcessKey`/`ImeToAsciiEx`。不增加新的启动器或调用路径。
- `tests/XiaoXiIme.IntegrationTests/ImeHostIpcIntegrationTests.cs`：新增参数化交错输入隔离测试 2 例，检查第二会话取消、重组后再次取消或提交时第一会话仍保留 `ni`/“你”。使用逻辑 HIMC 会话 ID 与测试 package；这是托管 Host/IPC 合同测试，不是系统 HIMC 测试。
- `tests/XiaoXiIme.IntegrationTests/README.md` 和 `Docs/IMM32-Handoff.md`：追加场景说明及待验收边界。

### 本轮专用工具验证

- 新增参数化定向测试：2/2 通过，失败 0，跳过 0，219 ms。
- `ImeHostIpcIntegrationTests` 与 `ProductionDictionaryKeystrokeTests` 合并筛选：41/41 通过，失败 0，跳过 0，52 s。此数字是合计，不是单独正式词库测试的新摘要。
- `XiaoXiIme.ImeModule.Tests` 全量复验：136/136 通过，失败 0，跳过 0，588 ms。保留上一轮多 HIMC 内存测试，未重复覆盖。
- `XiaoXiIme.IntegrationTestHost` Release：初次构建成功，0 警告、0 错误，1.47 s；补齐原生计数断言及关联失败清理保护后最终复验成功，0 警告、0 错误，1.52 s。

上述测试均为 net10.0。宿主产物仍为 `artifacts/bin/XiaoXiIme.IntegrationTestHost/release/XiaoXiIme.IntegrationTestHost.dll`。结果为专用工具日志摘要，未生成本轮 TRX/覆盖率，未运行完整 IntegrationTests、CLI 或解决方案测试。

### 真实验收阻碍与后续确认

**本轮没有启动宿主或 Windows 沙箱，没有真实多 HIMC PASS 或证据路径。** 历史当轮仅调用文件工具和专用构建/测试工具，未执行沙箱；当轮限制不表示当前专用沙箱能力不可用，构建成功不能替代真实 SendInput 验收。

静态检查发现 `ImeSetActiveContextManaged(active=0)` 当前会发送 Escape 并复位该 HIMC 会话。真实焦点切换可能触发此路径，使第一组合被清空，与双 HIMC 保留组合断言冲突。场景保持严格断言，不跳过焦点切换、不重建第一组合来伪造隔离通过；本轮未改变产品取消语义。需先在获准的管理员 Windows 环境执行单 HIMC 生命周期，再执行此场景，保留切换阶段消息/原生计数/Host 会话证据；如失败，根据真实证据和预期的失焦行为确认产品修复，不能仅凭托管测试判定 Windows 行为。

待确认多应用目标包括：具体应用及版本、控件类型、x86/x64 目标进程、可自动化输入/候选/提交操作及验收矩阵。文档未明确前不自行选型。所有产品级验收勾选继续保持未完成。

## 2026-10-13 接续：Unicode 合同修复与真实复验

本节为最新执行结果，上方未执行描述仅适用于其历史轮次。

### 合同及修改状态

已读取上一轮 writer/reader/InMemoryImmContext 相关实现和已改变的测试断言，核对本机 Windows SDK 10.0.26100.0 `um/immdev.h` 的 COMPOSITIONSTRING 字段次序。头文件仅声明 DWORD，不说明长度单位；不能用 sizeof(DWORD) 推导字符串单位。Unicode 字符串 CompStrLength/CompReadStrLength/ResultStrLength 为 UTF-16 单元数，不是编码字节数或 Unicode scalar 数；offset、Size、clause 数组长度为字节，clause 元素端点为 UTF-16 单元索引，属性一单元一字节，cursor/delta 是单元位置。未生成 result-reading 的当前提交路径保持这些字段为零，不增加新行为。ImmGetCompositionStringW 字符串查询返回字节数，既有 EDIT 观察器按字节分配并解码，不改为字符数。

保留上一轮 writer/reader/InMemoryImmContext 修复；真实 n/ni/你消息验证该诊断成立。完成 `ImeExportsTests.cs` 中 composition/result clause、result 长度和单字提交断言替换，增加 ASCII/BMP/合法 surrogate 两单元参数检查；修正 `ProjectTermSmoke.cs` 两个内存字符串长度检查。没有批量改写其他按字节接收的原生 API，也没有改生命周期/失焦断言。surrogate 仅托管验证，真实 API surrogate、result-reading 非空及异常 UTF-16 尚未系统验收，不宣称合同全矩阵完成。

### 工具测试/构建

- 先单个 composition 布局测试 1/1 PASS（9 ms）。
- 新增 Unicode 参数测试 3/3 PASS；模块扩展前 136/136 PASS，最终 139/139 PASS（595 ms）。
- ImeHostIpcIntegrationTests + ProductionDictionaryKeystrokeTests：41/41 PASS（24 s）。
- 整个 XiaoXiIme.slnx Release 构建 PASS：0 警告、0 错误（2.80 s）。
- Native AOT IME win-x64/win-x86、self-contained x64 SandboxRunner/IntegrationTestHost 发布成功。CLI/Host/UI/工具及词库沿用既有完整布局，未声称全量重建。未生成 TRX 或覆盖率。

run-02 的源码和发布身份由当时的 Stage 哈希任务固定并归档。x64 IME 源及安装诊断匹配 SHA256 `D4AEF9B2333D6823226EE6DB7DF516B453845370216CD5585A12C1BD349E1377`；x86 和其余负载身份记录在原始清单中；本仓库未收录该清单。

### 真实业务结果（按顺序，非仅退出码）

Windows 10.0.26200 x64 管理员任务 `50efb4d26a404e90aa8291f8f2573e8d`，执行时间 UTC 06:37:04–06:37:19。payload-build 成功；安装布局 E0200804，同时部署 System32 x64 与 SysWOW64 x86；外部 Host fullPinyin，无 fallback/load error。integration-run exit=14、timedOut=false 是多 HIMC 失败导致，不是整体 PASS。

| 场景 | 实际结果及原始观察 |
| --- | --- |
| xx → 小希 | PASS real-ime-keystroke-commit，真实 SendInput、稳定期/精确上屏断言均完成；HIMC 0x20301A1。 |
| 单 HIMC n/i → Escape → n/i → Space | PASS real-ime-composition-lifecycle；HIMC 0x20401A1、Host session himc:20401a1；START=2、END=2、Results=[你]、Updates=[(n,n,1),(ni,ni,2),(n,n,1),(ni,ni,2)]，无 CallbackError。上一轮 n\0 超时已消失。 |
| 双 EDIT/多 HIMC | FAIL：第一 n、第二 n 后 CheckFirst(n) 失败（Program.cs:457/506），第一组合不再保留；第一观察器 START=1、END=0、Results=[]、Updates=[(n,n,1)]，无 CallbackError。后续交错 ni/取消/提交步骤未执行。 |

该轮原始归档包含集成报告、负载和两个阶段的标准输出、标准错误及执行记录；证据摘要由原始 JSON 展开，不是人工重建的轨迹。归档原件未收录在本仓库。

### 失败诊断、清理及明确阻碍

已核对 `ImeSetActiveContextManaged(active=0)` 会 ApplyControlKey(Escape) 并 ResetSession；这与焦点切换后保留第一组合的严格要求冲突，是明确嫌疑。但当前失败报告没有两个自建 HIMC/Host session 身份、失活调用参数和逐条原生消息计数，无法从现有证据排除其他焦点/关联因素。本轮停止继续修改产品取消策略，不隐藏失败；下一轮仅补该失败路径证据并确认根因，不增加候选定位任务。

原始清理记录 FirstDestroyed=True、SecondDestroyed=True、SecondEDITDestroyed=True；安装布局及两个系统 IME 文件删除成功，PendingDeletePaths/RetiredFilePaths 均空、RebootRequired=false。未验证完整 Host 停止/用户数据保留矩阵。两个自建 HIMC 的销毁成功不弥补其失败身份轨迹缺口。

辅助证据展开首次误推整个带 payload 的目录，100 s 上传超时；改为紧凑 reader + 单份 JSON 后成功。辅助读取与身份归档工具未收录在本仓库，也不是产品依赖；reader 仅处理证据，没有触碰系统安装。

当前没有未编译的本轮产品改动；托管修复已构建测试，双架构 IME 已发布且 x64 真实单 HIMC 已验证。多 HIMC 产品修复未实施、x86 真实目标进程未验证，result-reading/surrogate 真实矩阵仍待验证。G04 失败未闭环；G05 仅本 x64 EDIT 生命周期 PASS，不关闭 G04/G05 全矩阵或 G14。
