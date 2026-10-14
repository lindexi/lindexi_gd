# XiaoXiIme.ImeUi.Avalonia

小希输入法外部 UI 项目。已实现候选窗、独立浮动状态栏、设置、关于、托盘和同用户同管道单实例恢复入口，通过 Host IPC 接收真实状态并保存产品设置；不提供主题编辑器。

## 当前产品窗口收尾（优先于历史记录）

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

只加载预编译 package，不读取或编译 TSV、不使用回退词库。默认从应用输出目录加载 `XiaoXiIme.DictionaryPackage`；`XIAOXIIME_INPUT_SCHEME=xiaoheDoublePinyin` 时使用输出目录下的 `XiaoXiIme.DictionaryPackages/xiaoheDoublePinyin`。也可通过环境变量 `XIAOXIIME_DEBUG_PACKAGE` 指定任意已编译 package 目录。修改词库后需另行编译并重启调试应用。加载在后台执行，标题仅显示简短状态；日志区记录实际 package 路径、加载状态、完整异常与堆栈及按键处理结果，不占用结果 TextBox。

支持字母、数字 1–9 选词、空格提交候选、Enter 提交 reading、退格、Esc 取消、上下选择、PageUp/PageDown 翻页、左右移动组合光标、Home/End 选择首末候选及 `/` 符号输入。TextBox 仅接收本地核心的提交结果；窗口与 TextBox 禁用系统输入法通道，拦截 TextInput，不将系统输入法文本送入核心。失去窗口激活时取消组合并隐藏候选窗口，关闭主窗口退出应用。

带参数启动仍使用原有 IPC 模式：第一个参数为管道名称，第二个可选参数为状态文件路径。

当前已有候选状态映射、显示/隐藏、分页、高亮和锚点字段基础。分页和选中状态交互已覆盖固定每页 9 项、末页实际数量、跨页选择、返回上一页、显示索引和唯一高亮同步。尚未完成真实窗口定位与屏幕工作区边界处理，以及多 DPI/缩放比例下的视觉自动化测试；锚点字段或托管状态测试通过不代表候选窗口产品级验收完成。
