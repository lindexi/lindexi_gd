# Builder 使用与交付说明

YY-Thunks 两架构候选、构建步骤及最新审计边界见 [YY-Thunks 构建与验证](YY-Thunks构建与验证.md)。当前仅自编译原生目标的链接证据通过；静态审计 exit 1，第三方未覆盖，Win7 尚未测试。

## 路径与外部输入约定

本文以 **Builder/ 为项目根**，命令均从该根目录执行；正文中的项目文件路径相对项目根，而 Markdown 文件链接相对当前文档。runtime 和 YY-Thunks 是独立外部仓库，不属于本项目，不要求放在项目根的父目录或同级目录。

命令中的 `<runtime-root>`、`<yy-root>`、`<yy-obj>` 分别替换为自行配置的 runtime checkout、YY-Thunks checkout 和架构匹配的已修复 OBJ 路径；它们不是项目内文件链接。历史环境路径已脱敏，历史构建与验收事实不变；历史产物是否随开源仓库分发需另行确认。

## 已交付：纯 runtime，无 SDK、无 YYThunks

2026-10-30 已完成 x86/x64 baseline 完整构建组装与 Windows 沙盒启动验收，版本均为 **10.0.4-dev**，host commit 为 `081d220c0a`。

历史稳定路径（相对项目根）：

- `artifacts/runtime-win-x86`
- `artifacts/runtime-win-x64`

各目录根部严格只有 `dotnet.exe`、`host/`、`shared/`、`LICENSE.txt`、`ThirdPartyNotices.txt`；无 sdk、packs、templates、sdk-manifests。发布 manifest 放在目录外的 `runtime-win-x86.manifest.json` / `runtime-win-x64.manifest.json`，明确记录 `BuildKind=baseline`、`YYThunksIntegrated=false`、`IsSdk=false`、版本、来源、验收日志和全部文件 SHA256。

两份均来自历史验证环境的外部 runtime 源码官方构建/组装输出，未复制用户安装的 runtime；已安装 SDK 仅用于编译工具和最小托管验证程序，没有加入交付目录。

**这些是现代 Windows 上验收通过的无 YY 基线，不是 Win7 兼容性认证。** XP/Vista 参数明确拒绝，Win7 实机、API 导入白名单与依赖闭包尚未验证。

## 构建

Builder 为独立 net10.0 控制台工具。普通模式顺序调用外部 runtime 的官方 `eng/build.ps1` 的 clr.native、libs.native、host.native。完整模式 `--full-runtime` 使用 `clr.runtime+clr.corelib+libs.native+libs.sfx+host.native+host.tools`，然后执行官方 Runtime.CoreCLR.sfxproj 的 GetFilesToPublish 与 Bundle.bundleproj 的 PublishToDisk；当前不做 ReadyToRun。

从项目根执行 `dotnet run --project Builder/Builder.csproj -c Release -- baseline --runtime "<runtime-root>" --yy "<yy-root>" --target-os win7 --arch x64 --full-runtime --timeout 1800`。改为 x86 可构建另一架构。每次输出独立的 `artifacts/baseline-win7-<arch>-<时间>-<GUID>/runtime`，保留 plan.json、manifest.json 和 0/1/2.log。组装 manifest 状态仅声明组装完成，启动验收由独立日志证明。

其他命令：doctor 检查前置输入；plan/--dry-run 只生成计划；build 要求有效的架构匹配 YY OBJ（显式传入 `--obj "<yy-obj>"`）；inspect 检查 PE，不认证兼容性。必须指定 `--target-os win7 --arch x86|x64`。支持 --runtime、--yy、--obj、--configuration Release|Debug|Checked、--timeout。Checked 的 libs 使用 Debug。--configure-only 对 Windows libs.native 仍可能实际编译。

官方 bootstrap 可下载仓库本地 SDK/构建依赖，不等于安装系统组件。原生工具使用已有 VS/CMake/Ninja。不得并发对同一 runtime checkout 执行 Builder。Builder 仅清除所选架构配置的生成 CMake 缓存，其他增量输出由官方脚本管理。

## NuGet 自动还原

从 Builder 项目根执行 `dotnet build Builder.sln -c Release`，会按 `Builder/Builder.csproj` 中的普通 PackageReference 自动还原 YY-Thunks，并将包内 `build/native/objs/x86/YY_Thunks_for_Win7.obj` 和 x64 对应对象复制到 Builder 输出的 `yy-thunks/<arch>/`。升级只需修改该包引用的 Version。构建从实际还原的包路径生成随输出的 `yy-thunks/package.json`，运行清单读取其中的包版本，并保留对象来源、包内路径和对象 SHA256。包仅作为下载载体，Builder 不导入它的链接集成、不编译 YY-Thunks 源码或运行 FixObj。

无需指定 `--yy` 或 `--obj`：例如 `dotnet run --project Builder/Builder.csproj -c Release -- doctor --runtime "<runtime-root>" --target-os win7 --arch x64`；将 doctor 换成 plan 或 build --dry-run 可只生成构建计划，换成 build 才实际构建。显式 `--obj` 优先；显式 `--yy` 仍按其 `objs/<arch>/YY_Thunks_for_Win7.obj` 选择对象。上文显式 OBJ 示例仍适用于手工输入，不是自动还原模式的必要条件。

包的源码版本以其 nuspec 为准，不能用较新本地源码的符号预期判定该包无效。包内 native targets 按架构和最低系统版本将预编译 OBJ 加入 AdditionalDependencies，Native AOT targets 将同一对象加入 LinkerArg；未要求消费端检测弱符号或重新 FixObj。打包布局将预编译 `objs` 放入 `build/native/objs`。普通未定义 security_cookie 可由 CRT 链接解析，不构成拒绝该对象的依据。Builder 保留 COFF 结构/架构检查，将清单 FixObj 状态记录为 `NotIndependentlyVerified`，不以启发式规则自造认证。

**下载及输入检查通过只说明输入可用，不等于已验证最终 runtime 链接成功，更不等于 Win7 运行兼容性认证。** 上文关于历史缺少修复对象/工具的记录仅描述当时环境，不限制当前官方 NuGet 输入。

## 注入、验证与安全发布

Toolchain.cmake 通过 CMAKE_TOOLCHAIN_FILE 环境传给官方入口，再用 CMAKE_PROJECT_INCLUDE 注册顶层 DEFER，递归审核 CoreCLR/LibsNative/corehost 的 Windows/MSVC 原生最终链接目标。baseline 不添加 YY OBJ；EXE 保留 6.01 subsystem 设置。x64 clrinterpreter 的版本资源与平台 manifest 补充均放在 Builder 内，不修改 runtime 的相应 CMake 或安装项目。

外部 YY 对象由 `--obj "<yy-obj>"` 配置，通常是 `<yy-root>/objs/<arch>/YY_Thunks_for_Win7.obj`。历史验证时缺有效 OBJ 和官方 FixObj 所需 LibMaker，因此未集成 YY；不能以普通 cl 对象替代已修复对象。

独立 `Verification` 执行器有 stage、sandbox、publish 三种模式。stage 对照原构建 manifest 的完整哈希后复制 payload；sandbox 真正启动每份 host 查询和托管程序并写 stdout/stderr/退出码/文件哈希；publish 必须读取通过的验收结果和一致的沙盒哈希，拒绝覆盖已有稳定目录或发布 manifest。此执行器是本次指定来源的交付验收辅助程序，不是通用发布 CLI。沙盒路径按执行器位置定位，不依赖远端工作目录。

`Smoke` 使用现有 SDK 编译，精确请求 10.0.4-dev，禁用版本滚动；通过各架构 host 运行，输出并检查实际进程架构、CoreLib 和 host 路径。未安装任何组件。详细证据及失败重试见[研究与验证](研究与验证.md)。

## runtime 唯一源码树变更

外部 runtime checkout 内 `src/libraries/System.Diagnostics.DiagnosticSource/Directory.Build.targets` 原为空 Project，导致父 targets 导入被截断，出现 SR 缺失与类型冲突。前任务已改为导入父级 Directory.Build.targets；原文件逐字备份为本项目的 `Docs/DiagnosticSource.Directory.Build.targets.original.bak`。不要直接恢复空文件后声称仍能构建；若要撤销必须同步解决父级导入问题。本次继续执行未新增 runtime/YY-Thunks 源码修改。

## 历史回归结果

Builder.sln Release 编译成功；Builder.Tests 实际 30 通过、0 失败、0 跳过。独立 Verification Release 编译成功；双架构实际沙盒验收 8/8 退出 0。没有覆盖率或 Win7 认证声明。以上为历史验证事实，不表示本次文档可移植性修订重新执行了构建或验收。
