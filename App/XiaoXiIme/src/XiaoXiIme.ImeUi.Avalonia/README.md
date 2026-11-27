# XiaoXiIme.ImeUi.Avalonia

本文描述重构前的 UI 入口、独立 Host 链路和历史验收。下文 EXE 路径与启动参数只适用于对应旧产物。

小希输入法外部 UI 项目。已实现候选窗、独立浮动状态栏、设置、关于、托盘和同用户同管道单实例恢复入口，通过 Host IPC 接收真实状态并保存产品设置；不提供主题编辑器。

## 当前体验与范围

旧 Experience 包的入口为 `dist/Experience/app/ui/XiaoXiIme.ImeUi.Avalonia.exe`，双击无参启动。

重复启动writer锁崩溃旧“进行中”已由Builder b56dbd81f0474827a7623f8b43cb7bb2 及锁门禁 fe722c4126b44fa18369e39848fb21f3 的5项通过覆盖：首次输入、二次exit0且首进程存活可见、首次正常退出、重开无锁崩溃且可输入、重开正常退出。本次已回读 `dist/Experience/evidence/lock/report.json`；未独立断言foreground激活，不扩大签收。

自然码ShS指定场景最新 `.coding-agent/shs-fixed-final/evidence/natural-shs-fixed-final/report.json` 本次回读19/19通过，含8/6/4/2候选消费及续输、mz/uohw无增音节预测和正常退出。产品UI已实现；完整13项、正式IME全链与全部profile/fuzzy矩阵不能据此全部签收。本次仅修正文档，未构建或运行测试。

**大牛双拼、小月双拼：用户决定不做**，移出当前产品/发布范围，不再列功能缺口、待补定义或待索取资料；不是已实现。当前为全拼及小鹤、搜狗、微软、智能ABC、拼音加加、自然码六双拼。catalog兼容占位及禁用表现保留，不编表、不删除。下文缺规则提示只描述现有表现，历史八双拼all缺口不再作为当前待办。

证据与未验边界见[Experience验收](../../../../Docs/experience-delivery-acceptance.md)和[主交接](../../../../Docs/next-session-handoff.md)；以下所有旧批次状态不覆盖本节。

## 历史交付：ExperienceSentenceVerified，零配置双击体验

用户双击 `C:\lindexi\Code\Workspace\IME\dist\ExperienceSentenceVerified\Experience\app\ui\XiaoXiIme.ImeUi.Avalonia.exe`（工作区相对路径 `dist/ExperienceSentenceVerified/Experience/app/ui/XiaoXiIme.ImeUi.Avalonia.exe`）。无需参数、产品环境变量、终端、编辑JSON、安装IME或额外配置；直接在窗口打开设置。清单为同成品根目录 `xiaoxiime-payload.json`，不以staging目录作为入口。

新Builder成品已有真实Windows 7/7 PASS：加载与engine可用、bicu组合、选笔只消费bi保留cu、cu继续选择、be词库证据前缀组合、设置入口、正常退出。actual记录异cwd、arguments=[]、productEnvironmentOverrides=[]；UI DLL SHA256=`19EE89B1EC7795848B84AFF425FEBBC247033C514E1137098D8010ED810E8533` 与manifest匹配。真实库Top比粗/被额，不硬编码受控样例排序。

旧 ExperienceCoreVerified 仅覆盖此前nihao核心体验，不是本次连续音节修复；下面旧dist/Experience入口与“未通过”均为历史。本次仅文档归档，不改代码、不构建、不运行。有限签收不代表全图统一、任意长串性能或完整设置/正式IME全链已验。详情见 [最终验收](../../../../Docs/experience-delivery-acceptance.md) 与 [主交接](../../../../Docs/next-session-handoff.md)。

## 历史：旧零配置 Experience（以下状态不覆盖当前交付）

现成品入口：`dist/Experience/app/ui/XiaoXiIme.ImeUi.Avalonia.exe`（相对工作区根目录），清单位于 `dist/Experience/xiaoxiime-payload.json`。双击无参启动；无需环境变量、终端或编辑 JSON。通过窗口设置选择方案和模糊规则，体验设置独立持久化，不修改正式设置。包以应用目录定位，不依赖当前工作目录。

最终独立验收：**未通过签收**。本次 Release exit0、0 errors/6 warnings（CLI.Tests CA1416），51行日志已回读。最新 Core583/Dictionary413/Integration252/IPC27/Module212/CLI targeted50 均 pass 是用户交接，非此次运行。Windows 同任务 Builder exit0（346项，含验收设施），真实成品 PID8456 无参异 cwd 启动并正常退出，但仍加载词库，诊断快照过期发生在输入前；nihao 候选/提交与设置入口小门禁、双拼/十规则保存取消默认重开、完整13项均未完成。延长等待后的调用遇客户端100秒超时，无日志回传，已停止重试并保存错误。actual EXE/UI DLL哈希与冻结manifest记录一致，不代表整包与本次源码可复现对应；没有Git身份声明。全清单/七包独立哈希审计尚未完成。用户唯一操作为双击 `C:\lindexi\Code\Workspace\IME\dist\Experience\app\ui\XiaoXiIme.ImeUi.Avalonia.exe`，不提供环境变量配置要求。证据、残留限制见 [Experience 验收](../../../Docs/experience-delivery-acceptance.md)，以下旧结果均为历史。

## 自然码历史生产交付：托管1484全绿，真实UI待验

FullPinyin加六双拼（小鹤、搜狗、微软、智能ABC、拼音加加、自然码）具备完整生产代码支持；大牛、小月由用户决定不做，不在当前产品/发布范围；兼容占位与现有缺规则提示/禁用表现保留。自然码资源为正常名称，不再禁用。不编表或借其他方案替代，不再将大牛/小月列为八双拼all缺口。

[自然码生产说明](../../../Docs/natural-code-production.md)已实际创建，详列26键/12零声母/ü/u:/v/n-l ue/jqxy/yw/er规则。用户授权规则回忆是产品基线，非官方或图片权威。[catalog](../XiaoXiIme.Dictionary/PhoneticSchemeCatalog.cs) 的SourcePath `Docs/natural-code-production.md`已落盘；scheme=`naturalCodeDoublePinyin`、持久ID6、sourceVersion=`common-natural-code-v1`、projectionVersion=`natural-code-v1`，Rules UTF-8 SHA-256是规则指纹，包scheme/version/hash必须匹配，不以fallback冒充目标支持。

Host正式保存后下次启动加载对应完整正式包，非正式活动会话热切换；debug先准备目标包与会话→持久提交→发布，失败保留旧会话、reading/caret/output及十规则事务；保留旧reading不代表按新编码自动重输。旧持久0/1与legacy shape不变。

最终用户交接：Core583/583、Dictionary413/413、Integration249/249、IPC27/27、Module212/212，总1484/1484、0失败0跳过；Release增量成功0 errors/0 warnings。旧CLI/CLI.Tests 19条CS8604/CA1416未证明根治，增量零警告不代表clean/rebuild零警告。本次只写文档并回读，不改代码、不构建、不测试，不声称新原始日志。新增Host十特殊码提交及包版本/hash各缺失与篡改四拒绝用例已计入全量；完整源包/Host重启/debug准备已覆盖自然码，专项不叠加。

真实Windows UI完整13项、动态fuzzy/分页、保存取消默认重开/持久化、actual身份与正常退出仍待独立签收；历史失败不由托管全绿覆盖。本次不发起后续任务。同步见[source audit](../../../Docs/eight-double-pinyin-source-audit.md)、[提取规范](../../../Docs/eight-double-pinyin-extracted-layouts.md)、[设计](../../../Docs/multi-pinyin-fuzzy-settings-design.md)、[主交接](../../../Docs/next-session-handoff.md)。

## 历史：五双拼开发交付（以下旧数值及自然码缺表状态均非当前）

本轮只做日志回读、IPC全量、Release及文档，不改代码/测试，不新增根因测试。FullPinyin加五双拼（小鹤、搜狗、微软、智能ABC、拼音加加）已具备生产代码支持，旧“仅小鹤可用、七项disabled”仅为历史。自然码、大牛、小月仍缺具体对应图/完整定义，身份未决项不造表，继续禁用；用户要求all中这三项明确未完成。

用户已确认SG/MS ing物理键`;`及四新方案零声母/特殊拼写常识产品基线；不是图片/官方权威。详细12零声母逐码及ü/u:/v、n/l、jqxy、y/w、加加缺ve按ue规则见仓库根`Docs/eight-double-pinyin-extracted-layouts.md`最终表。不再索取已提供五图；小鹤van/vn仅观察，不扩旧规则。

formal方案保存后下次Host启动加载对应完整正式包，非活动会话热切换；scheme/version/规则指纹必须一致。debug先准备目标包与会话，持久提交成功后发布，失败不污染旧会话，保留reading/caret/output及十规则事务。旧枚举0/1兼容；不能以fallback冒充方案加载。五双拼正式包由统一DictionaryPackageLocations解析，下文仅列两包的旧描述不代表当前只支持两项。

|最终验证|通过/总数|失败/跳过|本轮证据|
|---|---|---|---|
|Module|203/203|0/0|用户交接已全绿，非此次新跑|
|Core|562/562|0/0|用户交接已全绿，非此次新跑|
|Dictionary|345/345|0/0|用户交接已全绿，非此次新跑|
|Integration|234/234|0/0|22行完整回读，exit0，4m26s|
|IPC|27/27|0/0|此次执行，16行完整回读，exit0，106ms|
|Release全solution构建|成功|0 errors/0 warnings|37行完整回读，exit0，2.34s，非Release测试|

全量合计1371，不叠加专项。完整package21 tests（FullPackageReadingTests8+ProductionSchemeLifecycleTests13）覆盖六方案全源词条/canonical、alias、Host保存重启和五双拼debug准备及legacy0/1，含于Integration234；此前126 fuzzy专项为六方案十规则双向120+分号6，含于Core全量，后加legacy边界不冒称仍只有126。旧19 warnings为CLI/CLI.Tests的CS8604/CA1416历史；本次是增量构建零警告，不声称clean/rebuild根治，其他项目仅编译不虚报测试。

Module四测试类共享`Lifecycle environment` collection、DisableParallelization=true，隔离ImeExports/Host桥静态状态、callback/writer与进程环境变量，避免并行相互重置污染；没有超时现场stack/dump，不能宣称精确死锁根因闭环。本轮不扩测试。

开发交付给下一独立验收：真实Windows UI完整13项、动态fuzzy/分页、保存取消默认重开/持久化、实际进程身份与正常退出仍待签收；本轮未跑UI，历史失败不由托管全绿覆盖。完整来源与合同见仓库根`Docs/eight-double-pinyin-source-audit.md`、`Docs/multi-pinyin-fuzzy-settings-design.md`、`Docs/next-session-handoff.md`。以下旧轮次保留为历史，不覆盖本节。

## a40 debug 体验入口与冻结（历史）

无 IME 安装体验：双击成品 `XiaoXiIme.ImeUi.Avalonia.exe` 无参启动。内部测试可显式传入隔离设置和包路径；旧产品环境变量不再作为配置入口。工具栏提供设置及真正可点击的上一页/下一页，Name/AutomationId为 `DictionaryDebugSettings`、`DictionaryDebugPreviousPage`、`DictionaryDebugNextPage`。分页复用session页操作，不改正式候选窗；按钮不抢焦点，点击恢复输出输入区，保留reading/caret/output。首末边界、加载前、无候选和设置活动时禁用。

设 `XIAOXIIME_VALIDATION_BOUNDS=<绝对bounds.json>` 后，实际UI每200ms导出当前可见控件 pid/HWND/title、Name/AutomationId、enabled/focused、screen中心x/y与left/top/width/height。独立验收必须从当前debug主窗记录取坐标，不复用旧bounds、不猜页码文本位置。本轮未实跑Windows click/SendInput，不提供虚构坐标。

十rules已产品配置化；八双拼仅小鹤可用、七项disabled，资料未完成，不能冒称八方案已交付。测试先red7项，实现后专项7/7、增强焦点断言后的相关55/55、完整Integration211/211；Release全解决方案exit0，0警告0错误。源码冻结后无追加代码。真实13项合同待主监督派独立验收，详见 `Docs/multi-pinyin-fuzzy-settings-design.md`、`Docs/next-session-handoff.md`。

## 当前接续实查（历史）

已读 a35 报告：10/13，applied、zang、reopened失败。另发现 a36 报告仅完成9项：3 PASS / 6 FAIL，点击 FuzzyZhZ 超时；正常退出564通过，不能算13项验收通过。正在核查真实完整候选分页及配置生效/持久掩码，不假定“张”必在首屏。reading/caret/output、保存取消默认重开、actual身份与正常退出仍须完整复验。七种缺资料方案继续具名禁用，不假实现。

## 动态模糊规则接续（历史起点）

起始状态：共享 debug 设置入口已存在，十项 checkbox 仍禁用；schema/legacy Host 全会话原子规则更新未实现。UI 链接 Host store 源码的复用方式待收敛。当前开发目标是保留 reading/caret/output 的准备→持久提交→发布；七种缺资料双拼具名不可用，不冒称全部方案完成。本轮测试、Release、无 IME Windows 实际点击证据尚待执行；下文均为历史归档。

## 当前产品窗口收尾（历史记录）

clean-7d91起始13 PASS/1 FAIL已归档；最终任务f3282e1719674e038014fe94c9e435a7完整报告已回读14 PASS/0 FAIL，仅产品窗口场景完成。证据 `.coding-agent/product-ui-clean-7d91/results-defaults-e164/evidence/`，source清单位于该批根目录；UI/runner/真实EDIT宿主publish与actual EXE/DLL哈希已读一致，详细值见Docs/product-ui-v1-design.md。

defaults失败是测试前置已默认：隐藏revision1，恢复入口保存默认revision2，再点默认不产生Dirty/新revision。不是旧fixed坐标或未awaitsave；已有实际bounds点击不变。验证器独立建立非默认baseline3后恢复默认，草稿dirty=true，实际/持久revision4，FullPinyin/visible=true/position=null，真实barVisible=true；精确baseline+1断言未削弱。只补诊断和场景前置，未改业务默认合同。

本轮真实托管单项1/1、ProductSettings/UI34/34、Integration113/113；Release自包含发布与唯一干净白名单input-defaults-d953后运行。UI/重开UI/原生宿主正常exit0，无Kill，锁重获PASS；最终执行后仅改文档，无业务/测试代码追加。缺.NET10与远端相对路径失败尝试已保留并分别由共享自包含runtime/既有compact入口解决。

未覆盖：IMM xx/单HIMC/多HIMC；全拼/小鹤两方案保存重启真实取词；活动原生组合期间保存；状态栏拖动与菜单。当前空组合EDIT/HIMC证据不证明活动组合。不扩DPI/TSF，不宣布整个输入法验收完成。

## 最新聚焦验证归档（v3，历史记录）

紧凑 v3 成功传输并真实执行：12 PASS / 1 FAIL；bar-title-click-foreground before=198144、after=197944。原因未确认，不能证明真实 EDIT/HIMC 保留；最新关闭关于后的等待修改未实测。原始报告 `.coding-agent/product-ui-focused-evidence-v3/evidence/product-ui-report.json`。最终冻结测试/Release、真实 IMM xx/单/多、模块运行身份及清理尚须补证，以下历史计数不代表最新改动已经验证。

## 接续执行起始归档（历史轮次）

上轮真实产品 UI 未闭环：锁路径隔离、3 项故障测试、HTTP 100s 传输失败保留为失败/待验；最后 runner 路径修改待重新编译测试。本轮先检查既有 Stage 与沙箱紧凑目录，仅推 runtime 必需组件，避免重复 runtime/artifacts/source/历史回拉；大 package 沿用紧凑路径。按钮驱动、被测 PID、正常退出 exit=0 和锁重获不能由历史托管测试代替。具体原文与新证据将在实测后补录。

B1/B2/B3 已由主智能体独立复核并修复，后续独立复核由主监督安排。

## 产品入口与本轮验证

`XiaoXiIme.ImeUi.Avalonia.exe <server-name>` 进入 IPC 产品模式；加 `--settings` 打开设置。重复入口复用现有进程，普通入口恢复隐藏栏。关于显示 SeWZC 署名、版本和诊断。方案保存下次 Host 启动生效，不热切换；草稿取消不持久化。设置默认 `%LOCALAPPDATA%/XiaoXiIme/product-settings.json`；正式全拼包为应用目录 `XiaoXiIme.DictionaryPackage`，小鹤为 `XiaoXiIme.DictionaryPackages/xiaoheDoublePinyin`。

已修复无 NativeMenuExporter 时保存隐藏被直接拒绝、导致应用后取消仍为 revision0/显示的问题；恢复依赖现有单实例入口而非要求原生托盘导出器。原精确断言保留，测试限时等待真实 revision 和 UI 保存完成。代码已在最终验证前冻结。

最终验证：问题单测 1/1；ProductSettings/UI 31/31；Integration 107/107；IPC 27/27；Module 148/148；零失败零跳过。全解决方案 Release 成功（含 SandboxRunner 的 product-ui-validation），0 错误、19 警告：CLI/CLI.Tests 中 1 CS8604、18 CA1416；未修改 SDK/包或扩 DPI/TSF。

真实窗口入口：`XiaoXiIme.SandboxRunner.exe product-ui-validation <ui-executable> <package-root> <output-directory>`，报告 `product-ui-report.json`，隔离设置 `product-settings.json`。目前包含真实栏 HWND/标题点击前台 HWND、设置单实例、Host/IPC 保存重启和隐藏恢复；尚缺关于/真实设置应用取消默认按钮驱动、EDIT/HIMC 同时观测和正常退出（现强杀清理）。该轮未真实执行，不能宣称覆盖全部产品验收。

真实 EDIT/HIMC 和原 xx/单/多三个场景沿用 `imm32-validation` 的 IMM 编排，不由假 Host 会话代替。下一轮最小负载为 Windows runner/UI/Host/CLI 加正式两套 package，先执行产品入口，再执行 IMM 编排；任务根 `sandbox-output`/`final-evidence` 保存 IMM 证据。详细冻结记录与真实待办见工作区 `Docs/product-ui-v1-design.md`、`Docs/next-session-handoff.md`。独立只读复核仍由主智能体另派。

## 独立词库调试

不带参数启动应用，打开正常窗口，上方只读 TextBox 显示提交结果，下方只读 TextBox 显示带时间的调试日志，中间可拖动调整高度。日志区支持 Ctrl+A 全选、Ctrl+C 复制；焦点在日志区时不处理词库按键。无需安装输入法或启动 Host；按键交给真实 `ImeContext`，复用候选窗口显示，提交结果仅追加到本窗口，不向其他应用发送输入，也不保存用户学习。

运行：`dotnet run --project src/XiaoXiIme.ImeUi.Avalonia/XiaoXiIme.ImeUi.Avalonia.csproj`。

只加载预编译 package，不读取或编译 TSV、不使用回退词库。默认从应用输出目录加载 `XiaoXiIme.DictionaryPackage`；其他已支持方案通过设置选择，使用输出目录下的 `XiaoXiIme.DictionaryPackages/<scheme>`。产品不读取旧方案或词库环境变量。修改词库后需另行编译并重启调试应用。加载在后台执行，标题仅显示简短状态；日志区记录实际 package 路径、加载状态、完整异常与堆栈及按键处理结果，不占用结果 TextBox。

支持字母、数字 1–9 选词、空格提交候选、Enter 提交 reading、退格、Esc 取消、上下选择、PageUp/PageDown 翻页、左右移动组合光标、Home/End 选择首末候选及 `/` 符号输入。TextBox 仅接收本地核心的提交结果；窗口与 TextBox 禁用系统输入法通道，拦截 TextInput，不将系统输入法文本送入核心。失去窗口激活时取消组合并隐藏候选窗口，关闭主窗口退出应用。

带参数启动仍使用原有 IPC 模式：第一个参数为管道名称，第二个可选参数为状态文件路径。

当前已有候选状态映射、显示/隐藏、分页、高亮和锚点字段基础。分页和选中状态交互已覆盖固定每页 9 项、末页实际数量、跨页选择、返回上一页、显示索引和唯一高亮同步。尚未完成真实窗口定位与屏幕工作区边界处理，以及多 DPI/缩放比例下的视觉自动化测试；锚点字段或托管状态测试通过不代表候选窗口产品级验收完成。
