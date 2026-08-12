# XiaoXiIme.Cli

小希输入法命令行工具。职责：安装、卸载、重新安装、状态检查、诊断、Host 管理和系统级冒烟测试入口。

本项目使用 `DotNetCampus.CommandLine` 进行命令行解析。命令建模、处理器注册、内置帮助、异步入口、Native AOT 注意事项和踩坑记录见 [DotNetCampus.CommandLine 使用与踩坑记录](./DotNetCampus.CommandLine.md)。

当前支持以下检查和安装命令：

- `system-test-plan [--json]`：输出覆盖传统 IME、TSF、Host、IPC、UI、安装和回滚的全局系统测试计划。
- `system-test-run <abi-host> <tsf-dll> --confirm I-UNDERSTAND-THIS-MODIFIES-WINDOWS`：仅在可还原 VM 中执行隔离 ABI/COM 测试并生成 JSON 报告。
- `payload-build [--output <directory>] [--no-build]`：在开发机上构建或收集 x86/x64 组件并生成负载，不修改 Windows 输入法配置。
- `integration-run [payload-directory] --confirm I-UNDERSTAND-THIS-MODIFIES-WINDOWS [--skip-tsf] [--report <file>]`：仅在可还原 VM 中执行完整验证，并始终在结束时清理输入法。
- `install [payload-directory] --confirm I-UNDERSTAND-THIS-MODIFIES-WINDOWS`：校验负载，安装 x64/x86 输入法并保留，供人工体验。
- `uninstall --confirm I-UNDERSTAND-THIS-MODIFIES-WINDOWS`：卸载输入法并清理已部署文件。

真实安装和注册涉及管理员权限及系统注册表，`install`、`uninstall` 和 `integration-run` 必须在管理员终端及可还原 Windows 环境中执行。

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
dotnet run --project .\src\XiaoXiIme.Cli\XiaoXiIme.Cli.csproj -- payload-build --output .\artifacts\integration-payload
```

该命令依次执行解决方案 Release 构建。传统 IME、TSF InProc DLL 和 TSF ABI Host 分别发布 `win-x86` 与 `win-x64` 两套，因为这些组件必须匹配加载它们的目标进程架构。CLI、ImeHost、IPC 上层应用和集成测试是独立进程或托管逻辑，通过 IPC 通讯，只发布一套 `win-x64` 自包含共享应用负载。负载生成与安装、测试相互独立，避免一个命令同时承担多种生命周期。

负载目录结构：

```text
integration-payload/
├── xiaoxiime-payload.json
├── native/
│   ├── win-x86/
│   │   ├── ime/      # 32 位 XiaoXiIme.ime
│   │   ├── tsf/      # 32 位 TSF InProc DLL
│   │   └── tools/    # 32 位 TSF ABI Host
│   └── win-x64/
│       ├── ime/      # 64 位 XiaoXiIme.ime
│       ├── tsf/      # 64 位 TSF InProc DLL
│       └── tools/    # 64 位 TSF ABI Host
└── app/
	├── cli/          # VM 命令入口
	├── host/         # IPC 上层宿主应用
	└── tests/        # 集成测试程序集及运行依赖
```

`xiaoxiime-payload.json` 仅保存相对路径，并记录每个文件的长度和 SHA-256。复制到 VM 后，`integration-run` 会在修改系统前验证全部文件。

如果已提前生成 `artifacts\integration-publish` 下的全部发布结果，可使用 `--no-build` 只重新组织负载。

## 在 VM 中执行一键集成验证

将整个负载目录复制到已创建快照的 Windows VM。使用管理员 PowerShell 执行：

```powershell
$env:XIAOXIIME_ENVIRONMENT = "VirtualMachine"
.\app\cli\XiaoXiIme.Cli.exe integration-run . --confirm I-UNDERSTAND-THIS-MODIFIES-WINDOWS --report .\results\integration.json
```

命令会依次完成：

1. 校验 manifest、文件长度和 SHA-256。
2. 仅卸载注册表中明确归属于 `XiaoXi IME` / `XiaoXiIme.ime` 的旧布局。
3. 将 x64/x86 原生 IME 分别以资源中声明的 `XiaoXiIme.ime` 部署到 `System32`/`SysWOW64`，并使用 x64 系统路径调用 `ImmInstallIME`。
4. 分别使用 x86/x64 ABI Host 验证对应架构的 TSF ABI/vtable 和隔离 COM 激活。
5. 执行负载中的集成测试程序集，覆盖 Host、IPC 和上层逻辑；真实按键场景通过 `SendInput` 自动向测试窗口注入 `xx`，并验证 EDIT 控件精确上屏一次“小希”。
6. 输出单行 JSON 控制台事件并写入完整 JSON 报告。
7. 卸载测试输入法并清理部署文件。

如果只需安装后开始人工体验，不运行测试宿主，请执行：

```powershell
.\app\cli\XiaoXiIme.Cli.exe install . --confirm I-UNDERSTAND-THIS-MODIFIES-WINDOWS
```

体验结束后执行：

```powershell
.\app\cli\XiaoXiIme.Cli.exe uninstall --confirm I-UNDERSTAND-THIS-MODIFIES-WINDOWS
```

控制台每一行都是独立 JSON，包含 `timestampUtc`、`level`、`stage`、`message` 和 `data`，便于 LLM 或自动化脚本实时判断当前阶段、退出码、标准输出和错误输出。

安装前会额外输出 `diagnostics-pre-install` 阶段。该阶段完全由 CLI 自身完成，不要求 VM 安装 .NET SDK、Visual Studio、dumpbin 或 Dependencies，内容包括：

- Windows、操作系统架构和 CLI 进程架构；
- IME 绝对路径、长度、SHA-256、文件属性和 Mark of the Web；
- PE Machine、Magic、Subsystem、DLL 标志和导入模块；
- API-set 与普通系统 DLL 的分类和解析结果；
- `GetBinaryType` 与不执行 DLL 初始化代码的映像映射探测；
- `System32` 目标文件和匹配键盘布局注册表状态。

如果 `ImmInstallIME` 失败，还会输出 `diagnostics-post-install-failure`，立即记录调用后的文件和注册表状态。排障时应同时保留完整控制台输出和 `results\integration-*.json`；报告中的 `Data` 字段包含未被控制台摘要省略的诊断对象。

## 集成测试约束

- CLI 不判断当前机器是否为开发机、测试机或最终用户机器。
- `payload-build` 不修改系统，可在开发机执行。
- `integration-run` 和 `install` 会修改系统，只能部署到专用测试机或可还原虚拟机。
- 普通开发机上的 `dotnet test`、Visual Studio Test Explorer 和默认测试集合不得调用 `integration-run` 或 `install`。
- 安装包调用 CLI 时，应等待进程结束并检查退出码；退出码为 `0` 表示安装 API 调用成功。
