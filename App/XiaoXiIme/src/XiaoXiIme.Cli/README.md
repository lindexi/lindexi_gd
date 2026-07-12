# XiaoXiIme.Cli

小希输入法命令行工具。职责：安装、卸载、重新安装、状态检查、诊断、Host 管理和系统级冒烟测试入口。

本项目使用 `DotNetCampus.CommandLine` 进行命令行解析。命令建模、处理器注册、内置帮助、异步入口、Native AOT 注意事项和踩坑记录见 [DotNetCampus.CommandLine 使用与踩坑记录](./DotNetCampus.CommandLine.md)。

当前支持以下检查和安装命令：

- `system-test-plan [--json]`：输出覆盖传统 IME、TSF、Host、IPC、UI、安装和回滚的全局系统测试计划。
- `system-test-run <abi-host> <tsf-dll> --confirm I-UNDERSTAND-THIS-MODIFIES-WINDOWS`：仅在可还原 VM 中执行隔离 ABI/COM 测试并生成 JSON 报告。
- `payload-build [--output <directory>] [--runtime win-x64]`：在开发机上构建并收集完整集成测试负载，不修改 Windows 输入法配置。
- `integration-run <payload-directory> --confirm I-UNDERSTAND-THIS-MODIFIES-WINDOWS`：仅在可还原 VM 中执行旧版卸载、新版安装、TSF 验证、集成测试和清理。
- `install <ime-file> --allow-system-changes`：显式调用 Windows API 安装输入法；同时要求 `XIAOXIIME_ENVIRONMENT=Test` 或 `VirtualMachine`。

真实安装和注册涉及管理员权限及系统注册表。执行 `install` 即表示调用方要求安装，CLI 会在基本参数检查通过后调用 `ImmInstallIME`。

## 运行命令

在 `App\XiaoXiIme` 目录下，可以通过 `dotnet run` 执行命令：

```powershell
dotnet run --project .\src\XiaoXiIme.Cli\XiaoXiIme.Cli.csproj -- <command> [arguments]
```

如果已经发布或生成了 `XiaoXiIme.Cli.exe`，也可以直接运行：

```powershell
.\XiaoXiIme.Cli.exe <command> [arguments]
```

使用 `--help` 查看当前支持的命令：

```powershell
dotnet run --project .\src\XiaoXiIme.Cli\XiaoXiIme.Cli.csproj -- --help
```

## 构建可复制的集成测试负载

在开发机的 `App\XiaoXiIme` 目录执行：

```powershell
dotnet run --project .\src\XiaoXiIme.Cli\XiaoXiIme.Cli.csproj -- payload-build --output .\artifacts\integration-payload\win-x64 --runtime win-x64
```

该命令依次执行解决方案 Release 构建，并以 `win-x64` 自包含方式发布传统 IME NativeAOT 模块、TSF NativeAOT 模块、CLI、ImeHost、TSF ABI Host 和集成测试程序集。输出目录可以整体复制到 VM，也可以直接作为安装包负载。

负载目录结构：

```text
win-x64/
├── xiaoxiime-payload.json
├── ime/       # XiaoXiIme.ime 及 NativeAOT 依赖
├── tsf/       # XiaoXiIme.TsfModule.dll
├── cli/       # VM 命令入口
├── host/      # IPC 上层宿主应用
├── tools/     # XiaoXiIme.TsfAbiHost
└── tests/     # 已发布的集成测试程序集及运行依赖
```

`xiaoxiime-payload.json` 仅保存相对路径，并记录每个文件的长度和 SHA-256。复制到 VM 后，`integration-run` 会在修改系统前验证全部文件。

如果已提前生成 `artifacts\integration-publish\win-x64` 下的全部发布结果，可使用 `--no-build` 只重新组织负载。

## 在 VM 中执行一键集成验证

将整个负载目录复制到已创建快照的 Windows VM。使用管理员 PowerShell 执行：

```powershell
$env:XIAOXIIME_ENVIRONMENT = "VirtualMachine"
.\cli\XiaoXiIme.Cli.exe integration-run . --confirm I-UNDERSTAND-THIS-MODIFIES-WINDOWS --report .\results\integration.json
```

命令会依次完成：

1. 校验 manifest、文件长度和 SHA-256。
2. 仅卸载注册表中明确归属于 `XiaoXi IME` / `XiaoXiIme.ime` 的旧布局。
3. 安装负载中的新 `XiaoXiIme.ime`。
4. 执行 TSF ABI/vtable 和隔离 COM 激活验证。
5. 执行负载中的集成测试程序集，覆盖 Host、IPC 和上层逻辑。
6. 输出单行 JSON 控制台事件并写入完整 JSON 报告。
7. 默认卸载测试输入法；传入 `--keep-installed` 才保留安装状态，以便继续人工输入测试。

控制台每一行都是独立 JSON，包含 `timestampUtc`、`level`、`stage`、`message` 和 `data`，便于 LLM 或自动化脚本实时判断当前阶段、退出码、标准输出和错误输出。

## 集成测试约束

- CLI 不判断当前机器是否为开发机、测试机或最终用户机器。
- `payload-build` 不修改系统，可在开发机执行。
- `integration-run` 和 `install` 会修改系统，只能部署到专用测试机或可还原虚拟机。
- 普通开发机上的 `dotnet test`、Visual Studio Test Explorer 和默认测试集合不得调用 `integration-run` 或 `install`。
- 安装包调用 CLI 时，应等待进程结束并检查退出码；退出码为 `0` 表示安装 API 调用成功。
