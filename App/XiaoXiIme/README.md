# XiaoXiIme（小希输入法）

XiaoXiIme 是一个基于 .NET 10 与 Native AOT 构建的 Windows 中文输入法项目。项目采用“原生输入法模块 + 进程外托管宿主 + Avalonia 候选与设置界面 + 自有离线词库”的分层设计，不依赖 RIME 或第三方输入法框架。

> 本仓库当前实现了完整核心引擎、自有词库编译与加载链路、IPC 通信协议、Avalonia 候选窗/状态栏/体验调试窗口，以及传统的 Windows IMM32（`XiaoXiIme.ime`）主线链路。TSF（`XiaoXiIme.TsfModule`）尚处于基础框架与 ABI 验证建设阶段，未作为默认发布验收路径。

## 功能特性

- **输入方案**：支持全拼及六种双拼方案——小鹤双拼、搜狗双拼、微软双拼、智能 ABC、拼音加加、自然码双拼（已将大牛、小月双拼移出当前产品范围，仅在方案目录保留禁用兼容占位）。
- **音形与模糊音**：`XiaoXiIme.ImeCore` 支持基于合法声韵与音形证据的路径解码、大小写形码策略、逐字/词后补码、10 项双向模糊音规则（如 `zh/z`、`sh/s`、`ch/c`、`ang/an`、`eng/en`、`in/ing`、`ian/iang`、`uan/uang`、`ai/an`、`un/ong`），以及可选自动上屏策略；并保持默认兼容模式（含 `xx -> 小希` 快捷触发）。
- **自有离线词库链路**：支持音码（`*.phonetic.tsv`）、形码（`*.shape.tsv`）与符号（`*.symbols.tsv`）原生 TSV 源格式及可选 YAML 元数据头部，离线编译为二进制分片包（package）以保证按键查询路径零磁盘 I/O；支持用户词典学习、后台有界合并与原子保存。
- **Windows 原生集成**：
  - **IMM32（当前主线）**：`XiaoXiIme.ImeModule` 以 Native AOT 编译为跨架构（`win-x64` / `win-x86`）`XiaoXiIme.ime`，支持组合串读写、候选列表、翻页/选词、反查与多 HIMC 上下文隔离。
  - **TSF（建设中）**：`XiaoXiIme.TsfModule` 提供 COM 类工厂、文本服务和 ABI 宿主测试基础。
- **桌面界面与体验模式**：`XiaoXiIme.ImeUi.Avalonia` 基于 Avalonia 11 实现候选窗、浮动状态栏、托盘菜单、产品设置窗口，以及免系统安装的独立体验/词库调试窗口（无参或 `--experience` / `--dictionary-debug` 启动）。

## 仓库结构

```text
XiaoXiIme/
├── XiaoXiIme.slnx                     # 解决方案文件（.NET 10）
├── Directory.Build.props              # 全局构建配置（启用 UseArtifactsOutput）
├── data/
│   └── dictionaries/                  # 默认词库源文件与上游许可证（Git 默认忽略生成/本地数据）
├── Docs/                              # 仓库内部设计、定制、验证与沙箱工作流文档
├── src/
│   ├── XiaoXiIme.Foundation/          # 共享 DTO、枚举、会话 ID、设置与诊断契约
│   ├── XiaoXiIme.Dictionary/          # 词库源解析、方案目录、二进制包编译/加载与用户词典
│   ├── XiaoXiIme.ImeCore/             # 输入状态机、音形解码、候选排序与提交/取消逻辑
│   ├── XiaoXiIme.ImeInterop/          # Win32 / IMM32 常量、结构体与 P/Invoke 声明
│   ├── XiaoXiIme.ImeIpc/              # 基于 dotnetCampus.Ipc 的进程间通信契约与 AOT 序列化
│   ├── XiaoXiIme.ImeHost/             # 进程外输入法宿主服务、会话管理与后台用户词学习
│   ├── XiaoXiIme.ImeModule/           # Native AOT IMM32 DLL（发布重命名为 XiaoXiIme.ime）
│   ├── XiaoXiIme.TsfModule/           # Native AOT TSF COM 模块（建设中）
│   ├── XiaoXiIme.ImeUi.Avalonia/      # Avalonia 候选窗、状态栏、设置及体验调试 UI
│   └── XiaoXiIme.Cli/                 # 安装、卸载、词库编译/检查与集成测试命令行工具
└── tests/
    ├── XiaoXiIme.Dictionary.Tests/    # 词库编译、排序、学习与性能基准测试
    ├── XiaoXiIme.ImeCore.Tests/       # 核心状态机、双拼、音形扩展与候选测试
    ├── XiaoXiIme.ImeIpc.Tests/        # IPC 路由与 Native AOT JSON 往返测试
    ├── XiaoXiIme.ImeModule.Tests/     # 内存 IMM 上下文仿真与生命周期测试
    ├── XiaoXiIme.TsfModule.Tests/     # TSF 模块单元测试
    ├── XiaoXiIme.Cli.Tests/           # CLI 命令行与安装诊断单元测试
    ├── XiaoXiIme.IntegrationTests/    # 端到端 IPC、生产词库按键与无头 UI 截图测试
    ├── XiaoXiIme.IntegrationTestHost/ # 自包含集成测试验证宿主
    ├── XiaoXiIme.TsfAbiHost/          # TSF ABI 与 COM 激活检查宿主
    └── XiaoXiIme.SandboxRunner/       # Windows 沙箱命令执行与日志收集启动器
```

## 架构概览

1. **进程内原生模块（`XiaoXiIme.ImeModule` / `XiaoXiIme.TsfModule`）**
   加载到目标 Win32 应用进程中，接收 Windows IMM32/TSF 回调，将物理按键翻译为与平台无关的 `ImeKey`，通过命名管道 IPC 发往后端宿主，并将返回结果写回系统输入上下文（`COMPOSITIONSTRING`、`CANDIDATEINFO`、`ResultStr` 与 `TRANSMSG`）。当宿主不可达时，`ImeHostBridge` 会降级为进程内最小上下文以保证系统调用边界安全。
2. **宿主与 UI（`XiaoXiIme.ImeHost` / `XiaoXiIme.ImeUi.Avalonia`）**
   - **正式输入服务**：`XiaoXiIme.ImeHost` 管理按 `ImeSessionId`（与系统 `HIMC` 映射）隔离的 `ImeContext` 会话、加载已编译词库包、执行后台有界用户词学习及持久化设置；`XiaoXiIme.ImeUi.Avalonia` 连接宿主接收状态并展示候选窗、浮动状态栏、托盘与设置界面。
   - **体验 / 调试模式**：直接运行 `XiaoXiIme.ImeUi.Avalonia`（无参或传入 `--experience` / `--dictionary-debug`）时，在单实例窗口内直接调用 `ImeCore` 与 `Dictionary` 进行交互试打与候选调试，无需安装系统输入法或启动独立宿主。

## 环境要求

- [.NET 10 SDK](https://dotnet.microsoft.com/)
- Windows 10 / 11（构建与运行完整桌面 UI、Native AOT `.ime`/`.dll` 及真实输入法安装测试）
- 执行 Native AOT（`PublishAot=true`）需安装支持 C++ 桌面开发的 Visual Studio 生成工具 / Windows SDK

## 快速开始

### 1. 构建解决方案

```powershell
dotnet build XiaoXiIme.slnx -c Release
```

说明：解决方案启用了 `UseArtifactsOutput=true`（见 `Directory.Build.props`），编译与发布产物默认输出在仓库根目录的 `artifacts/` 下。

### 2. 运行单元测试与集成测试

可按项目逐个执行测试（推荐，避免跨框架/跨目标一次运行遗漏测试发现）：

```powershell
dotnet test tests/XiaoXiIme.Dictionary.Tests/XiaoXiIme.Dictionary.Tests.csproj
dotnet test tests/XiaoXiIme.ImeCore.Tests/XiaoXiIme.ImeCore.Tests.csproj
dotnet test tests/XiaoXiIme.ImeIpc.Tests/XiaoXiIme.ImeIpc.Tests.csproj
dotnet test tests/XiaoXiIme.ImeModule.Tests/XiaoXiIme.ImeModule.Tests.csproj
dotnet test tests/XiaoXiIme.TsfModule.Tests/XiaoXiIme.TsfModule.Tests.csproj
dotnet test tests/XiaoXiIme.Cli.Tests/XiaoXiIme.Cli.Tests.csproj
dotnet test tests/XiaoXiIme.IntegrationTests/XiaoXiIme.IntegrationTests.csproj
```

> 若尚未在 `data/dictionaries/` 中准备完整词库源文件（见下文“词库数据准备”），依赖完整源文件的部分集成/编译测试可能需要先准备对应 TSV 文件。

### 3. 运行体验 / 词库调试窗口（无需系统安装）

```powershell
dotnet run --project src/XiaoXiIme.ImeUi.Avalonia/XiaoXiIme.ImeUi.Avalonia.csproj -- --experience
```

常用参数：
- `--experience` 或 `--dictionary-debug`（无参启动默认等效体验模式）
- `--settings`：启动时自动打开设置界面
- `--debug-settings-path <绝对路径>`：指定体验模式设置文件路径
- `--debug-package-path <绝对路径>`：在 `--dictionary-debug` 模式下指定已编译词库包目录

### 4. 词库数据准备与编译

仓库内的 `data/` 目录默认通过 `data/.gitignore` 忽略本地数据文件，已跟踪的 `data/dictionaries/SOURCES.md` 与 `data/dictionaries/licenses/` 记录了默认词典的来源与许可证（包含来自 `rime-frost` 的 GPL-3.0 词表及 `rime-shuangpin-fuzhuma` 的 MIT 形码/符号表）。

构建默认词库包需在 `data/dictionaries/`（或通过 `--dictionary-source` 指定的目录）具备以下四类 XiaoXiIme 原生 TSV：
- `phonetic/sewzc-default.phonetic.tsv`
- `phonetic/xiaoxiime-project-terms.phonetic.tsv`（亦可由 `XiaoXiImeProjectTerms.WriteTo` 从嵌入资源导出）
- `shape/sewzc-moqi.shape.tsv`
- `symbols/sewzc-default.symbols.tsv`

使用 CLI 编译与管理词库：

```powershell
# 从原生 TSV 编译各支持方案的二进制词库包到指定输出目录
dotnet run --project src/XiaoXiIme.Cli/XiaoXiIme.Cli.csproj -- dictionary-build-packages data/dictionaries artifacts/dict-host

# 编译或增量更新单个方案的词库包（支持 fullPinyin / xiaoheDoublePinyin 等）
dotnet run --project src/XiaoXiIme.Cli/XiaoXiIme.Cli.csproj -- dictionary-update data/dictionaries artifacts/dict-pkg --scheme fullPinyin

# 校验词库包版本、计数与来源署名
dotnet run --project src/XiaoXiIme.Cli/XiaoXiIme.Cli.csproj -- dictionary-inspect artifacts/dict-pkg --json

# 回滚到上一次保留的 .previous 词库包
dotnet run --project src/XiaoXiIme.Cli/XiaoXiIme.Cli.csproj -- dictionary-rollback artifacts/dict-pkg
```

## CLI 构建负载与系统安装说明

> **注意**：`install`、`uninstall` 与 `integration-run` 会向 `System32` / `SysWOW64` 部署原生 `.ime` 文件并修改 Windows 键盘布局注册表。**请仅在可还原虚拟机或 Windows 沙箱中以管理员权限执行**，CLI 会强制校验 `--confirm I-UNDERSTAND-THIS-MODIFIES-WINDOWS` 确认令牌。

```powershell
# 1. 构建完整集成与安装负载（含 x86/x64 原生模块、Host、UI、CLI、测试宿主与词库包）
dotnet run --project src/XiaoXiIme.Cli/XiaoXiIme.Cli.csproj -- payload-build --output artifacts/integration-payload

#    若仅需组装免安装的独立体验 UI 包：
dotnet run --project src/XiaoXiIme.Cli/XiaoXiIme.Cli.csproj -- payload-build --experience --output artifacts/experience-payload

# 2. 在可还原 VM / 沙箱（管理员权限）中安装并启动 Host
.\app\cli\XiaoXiIme.Cli.exe install . --confirm I-UNDERSTAND-THIS-MODIFIES-WINDOWS

# 3. 运行系统级集成冒烟（当前默认跳过尚未完备的 TSF 路径）
.\app\cli\XiaoXiIme.Cli.exe integration-run . --confirm I-UNDERSTAND-THIS-MODIFIES-WINDOWS --skip-tsf --report .\results\integration.json

# 4. 卸载（默认保留用户词典；传入 --purge-user-data 可同时删除）
.\app\cli\XiaoXiIme.Cli.exe uninstall --confirm I-UNDERSTAND-THIS-MODIFIES-WINDOWS
```

更多细节可参阅仓库内 `Docs/` 目录及各子项目的 `README.md`：
- 定制显示名称与版本资源：[Docs/Ime-Customization.md](Docs/Ime-Customization.md)
- Windows 沙箱构建与测试流程：[Docs/Windows-Sandbox-Test-Workflow.md](Docs/Windows-Sandbox-Test-Workflow.md)
- IMM32 状态与自动化验证记录：[Docs/IMM32-Handoff.md](Docs/IMM32-Handoff.md)、[Docs/IMM32-Automation-Validation-Report.md](Docs/IMM32-Automation-Validation-Report.md)
- 音形扩展需求与输入方案思考：[Docs/音形扩展-TDD阶段1需求与用例.md](Docs/音形扩展-TDD阶段1需求与用例.md)、[Docs/输入方案的思考.md](Docs/输入方案的思考.md)
