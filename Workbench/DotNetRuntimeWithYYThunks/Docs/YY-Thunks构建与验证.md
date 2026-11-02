# YY-Thunks 构建与验证

## 范围与路径

本文所有项目路径及命令均以 **Builder 根目录**为基准。`<runtime-root>` 表示自行配置的外部 runtime checkout，不是项目内链接。需要 .NET 10 SDK（`global.json` 指定 10.0.100，允许 latestFeature）及已有 Windows 原生构建工具。本文不声明 Win7 兼容认证。

## 两份已发布候选

| 架构 | 候选目录 | 发布 manifest | 原始成功构建目录 |
| --- | --- | --- | --- |
| x86 | `artifacts/runtime-yythunks-win-x86` | `artifacts/runtime-yythunks-win-x86.manifest.json` | `artifacts/build-win7-x86-20261030125311-7969919196624435bcc524b61ab39253` |
| x64 | `artifacts/runtime-yythunks-win-x64` | `artifacts/runtime-yythunks-win-x64.manifest.json` | `artifacts/build-win7-x64-20261030125619-95c42902e51446dcb62ce1860fe89243` |

两候选均为纯 runtime、无 SDK，版本 10.0.4-dev，host commit `081d220c0a`。不要与无 YY 基线 `artifacts/runtime-win-x86`、`artifacts/runtime-win-x64` 混淆。本次未重新发布或覆盖候选及 baseline。

## 构建与审计复现

从 Builder 根执行：

- 构建工具及测试依赖：`dotnet build Builder.sln -c Release`。
- 完整 YY 构建示例：`dotnet run --project Builder/Builder.csproj -c Release -- build --runtime "<runtime-root>" --target-os win7 --arch x86 --full-runtime --timeout 1800`；另一架构改为 x64。不得并发构建同一 runtime checkout。
- Builder 自动还原其 PackageReference 指定的 YY-Thunks 包，使用包内对应架构的预编译 OBJ；显式 `--obj` 可覆盖选择。不在消费端重新编译 YY 源码或运行 FixObj。
- 单独重建审计器：`dotnet build ThunkAudit/ThunkAudit.csproj -c Release`。
- 实际审计已发布候选：`dotnet run --project ThunkAudit/ThunkAudit.csproj -c Release --no-build -- . "<runtime-root>" --published --allow-unverified-third-party`。
- 全部 Builder 测试：`dotnet test Builder.sln`。

已发布候选审计必须明确指定 `--allow-unverified-third-party`。该选项只是确认第三方未覆盖状态，**不会把静态审计失败改成成功**。审计只读取发布目录及 manifest，更新 `artifacts/yythunks-audit` 内的报告和 gate，不修改 baseline 或发布 payload。没有 `--published` 时读取上表原始构建目录。

## 最新收尾验证结果

本次在最后资源豁免条件变更后重新编译 ThunkAudit，并对两份已发布候选实际执行上述显式模式：

- `artifacts/yythunks-audit/candidate-gate.json` 两架构均为 `SelfBuiltPassed=true`、`SandboxHashesMatch=true`。
- `artifacts/yythunks-audit/report.txt` 两架构的 published manifest 与第三方 manifest 比对均为 True；文件全量 SHA256 与记录一致。
- 自编译目标 MAP 中存在 `YY_Thunks_for_Win7.obj` 符号证据；同时核对 OBJ 架构及构建清单哈希、MAP 对应最终二进制哈希、Ninja 构建图和实际链接执行日志。DAC 别名要求与主 DLL 哈希相同。不是只凭文件名或构建参数判定集成成功。
- 明细保存在 `artifacts/yythunks-audit/x86-link-evidence.json`、`artifacts/yythunks-audit/x64-link-evidence.json`，导入与基线差异保存在同目录的 `x86-native-imports.json`、`x64-native-imports.json`。
- 纯资源例外仅限名为 `mscorrc` 或 `clretwrc` 的 DLL：入口地址为 0、普通及延迟导入均为空、所有节均无可执行属性。两架构对应文件满足条件；不要求它们具有 YY MAP 符号。其他原生文件不因零入口或无导入就自动豁免。
- 全部 Builder.Tests 重跑：**51 通过、0 失败、0 跳过**（本次测试为默认 Debug 配置）；ThunkAudit Release 构建成功。

现代 Windows 沙盒重新执行两架构各四项，共 **8/8 退出 0**：`--info`、`--list-runtimes`、`--list-sdks`、托管 Smoke。Smoke 报告预期进程架构，并从候选 runtime 加载 CoreLib 与 host；SDK 列表为空。此次重跑使用已有 Verification 的 `payload-yy`，原始验收全量哈希已由最新审计与发布候选核对一致。新日志在 `artifacts/acceptance-yythunks-final-rerun/evidence`；原验收及哈希在 `artifacts/acceptance-yythunks-verified/evidence`。这八项是现代 Windows 启动回归，不是完整 runtime 测试套件，也不是 Win8 实机或 Win7 测试。

## 未覆盖项和明确风险

预编译第三方 **msquic** 与 **Microsoft.DiaSymReader.Native** 未重新链接 YY OBJ，不属于已通过的自编译目标范围。两架构合计四项，包资产哈希来源比对通过，但兼容性仍未验证：

- msquic：`microsoft.native.quic.msquic.schannel`，版本 `2.4.16`。
- DiaSymReader：`microsoft.diasymreader.native`，版本 `17.10.0-beta1.24272.1`。

msquic 导入包含 `InterlockedPushListSListEx`、`QueryContextAttributesExW` 等 Win7 不提供的 API，存在明确的 Win7 加载/调用风险；不能用现代 Windows Smoke 成功推断 QUIC/TLS 可在 Win7 使用。完整导入、疑似阻断项及来源见 gate 的 `ThirdPartyUncovered`。DiaSymReader 的调试路径和动态 API 也未认证。

因此发布 manifest 保持 `BuildKind=candidate`、`YYThunksIntegrated=false`、`YYThunksIntegratedScope=SelfBuiltNativeTargets`、`StaticAuditExitCode=1`、`Compatibility=Win7Unverified`。`status.json` 的 `Passed=false` 是真实结论。**静态审计 exit 1，未通过兼容认证；Win7 实机尚未测试。** 没有权威 Win7 导出白名单、完整依赖闭包和动态解析 API 的验证，不能宣称整个 runtime 已覆盖 YY-Thunks 或支持 Win7。
