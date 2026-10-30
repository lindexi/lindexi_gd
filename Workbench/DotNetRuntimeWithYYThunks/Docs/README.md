# Builder 使用与交付说明

## 已交付：纯 runtime，无 SDK、无 YYThunks

2026-10-30 已完成 x86/x64 baseline 完整构建组装与 Windows 沙盒启动验收，版本均为 **10.0.4-dev**，host commit 为 `081d220c0a`。

稳定路径（相对工作区）：

- `Builder/artifacts/runtime-win-x86`
- `Builder/artifacts/runtime-win-x64`

各目录根部严格只有 `dotnet.exe`、`host/`、`shared/`、`LICENSE.txt`、`ThirdPartyNotices.txt`；无 sdk、packs、templates、sdk-manifests。发布 manifest 放在目录外的 `runtime-win-x86.manifest.json` / `runtime-win-x64.manifest.json`，明确记录 `BuildKind=baseline`、`YYThunksIntegrated=false`、`IsSdk=false`、版本、来源、验收日志和全部文件 SHA256。

两份均来自本工作区 runtime 源码的官方构建/组装输出，未复制用户 `dotnet/` 或已安装 runtime；已安装 SDK 仅用于编译工具和最小托管验证程序，没有加入交付目录。

**这些是现代 Windows 上验收通过的无 YY 基线，不是 Win7 兼容性认证。** XP/Vista 参数明确拒绝，Win7 实机、API 导入白名单与依赖闭包尚未验证。

## 构建

Builder 为独立 net10.0 控制台工具。普通模式顺序调用官方 `eng/build.ps1` 的 clr.native、libs.native、host.native。完整模式 `--full-runtime` 使用 `clr.runtime+clr.corelib+libs.native+libs.sfx+host.native+host.tools`，然后执行官方 Runtime.CoreCLR.sfxproj 的 GetFilesToPublish 与 Bundle.bundleproj 的 PublishToDisk；当前不做 ReadyToRun。

从工作区执行 `dotnet run --project Builder/Builder/Builder.csproj -c Release -- baseline --target-os win7 --arch x64 --full-runtime --timeout 1800`。改为 x86 可构建另一架构。每次输出独立的 `Builder/artifacts/baseline-win7-<arch>-<时间>-<GUID>/runtime`，保留 plan.json、manifest.json 和 0/1/2.log。组装 manifest 状态仅声明组装完成，启动验收由独立日志证明。

其他命令：doctor 检查前置输入；plan/--dry-run 只生成计划；build 要求有效的架构匹配 YY OBJ；inspect 检查 PE，不认证兼容性。必须指定 `--target-os win7 --arch x86|x64`。支持 --runtime、--yy、--obj、--configuration Release|Debug|Checked、--timeout。Checked 的 libs 使用 Debug。--configure-only 对 Windows libs.native 仍可能实际编译。

官方 bootstrap 可下载仓库本地 SDK/构建依赖，不等于安装系统组件。原生工具使用已有 VS/CMake/Ninja。不得并发对同一 runtime checkout 执行 Builder。Builder 仅清除所选架构配置的生成 CMake 缓存，其他增量输出由官方脚本管理。

## 注入、验证与安全发布

Toolchain.cmake 通过 CMAKE_TOOLCHAIN_FILE 环境传给官方入口，再用 CMAKE_PROJECT_INCLUDE 注册顶层 DEFER，递归审核 CoreCLR/LibsNative/corehost 的 Windows/MSVC 原生最终链接目标。baseline 不添加 YY OBJ；EXE 保留 6.01 subsystem 设置。x64 clrinterpreter 的版本资源与平台 manifest 补充均放在 Builder 内，不修改 runtime 的相应 CMake 或安装项目。

外部 YY 对象默认位于 YY-Thunks/objs/<arch>/YY_Thunks_for_Win7.obj。目前缺有效 OBJ 和官方 FixObj 所需 LibMaker，因此未集成 YY；不能以普通 cl 对象替代已修复对象。

独立 `Builder/Verification` 执行器有 stage、sandbox、publish 三种模式。stage 对照原构建 manifest 的完整哈希后复制 payload；sandbox 真正启动每份 host 查询和托管程序并写 stdout/stderr/退出码/文件哈希；publish 必须读取通过的验收结果和一致的沙盒哈希，拒绝覆盖已有稳定目录或发布 manifest。此执行器是本次指定来源的交付验收辅助程序，不是通用发布 CLI。沙盒路径按执行器位置定位，不依赖远端工作目录。

`Builder/Smoke` 使用现有 SDK 编译，精确请求 10.0.4-dev，禁用版本滚动；通过各架构 host 运行，输出并检查实际进程架构、CoreLib 和 host 路径。未安装任何组件。详细证据及失败重试见《研究与验证》。

## runtime 唯一源码树变更

`runtime/src/libraries/System.Diagnostics.DiagnosticSource/Directory.Build.targets` 原为空 Project，导致父 targets 导入被截断，出现 SR 缺失与类型冲突。前任务已改为导入 `..\Directory.Build.targets`；原文件逐字备份为 `Builder/Docs/DiagnosticSource.Directory.Build.targets.original.bak`。不要直接恢复空文件后声称仍能构建；若要撤销必须同步解决父级导入问题。本次继续执行未新增 runtime/YY-Thunks 源码修改。

## 当前回归结果

Builder.sln Release 编译成功；Builder.Tests 实际 30 通过、0 失败、0 跳过。独立 Verification Release 编译成功；双架构实际沙盒验收 8/8 退出 0。没有覆盖率或 Win7 认证声明。
