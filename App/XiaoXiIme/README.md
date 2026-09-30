# XiaoXiIme

XiaoXiIme 是一个基于 .NET 10 的小希输入法实验项目，用于探索 Windows 传统 IME 模块、输入法核心状态机、候选词窗口、词典服务、进程间通信以及安装发布流程等能力。

当前项目重点是把输入法的核心逻辑、Windows IME 导出模块、宿主进程、IPC 协议和 UI 展示拆分成可测试、可维护的多个项目，便于逐步完善输入体验和系统集成能力。

## 项目内容

本解决方案包含以下主要内容：

- 输入法核心：维护输入上下文、组合文本、候选词、按键处理结果等核心状态。
- 词典服务：提供原生 TSV、离线二进制 package、全拼/小鹤双拼索引、形码、符号及用户学习，支撑内存候选查询。
- Windows IME 模块：提供传统 IME 入口导出、按键翻译、`ImeSetCompositionString` 整串替换、`ImeConversionList` 独立查询/`GCL_REVERSELENGTH` 反查长度、`ImeEscape` 名称查询、`ImeRegisterWord` 用户词注册、`ImeConfigure(IME_CONFIG_REGISTERWORD)` 配置注册、上下文读写和与宿主进程通讯的桥接逻辑。
- 宿主进程：承载输入法运行时服务，通过 IPC 与 IME 模块交互。
- IPC 协议：定义输入法模块与宿主进程之间的请求、响应和通知模型。
- Avalonia UI：维护候选窗口状态映射和候选词展示控制逻辑。
- 命令行工具：输出发布、导出检查和卸载清单，并提供可由最终安装包调用的输入法安装命令。
- 自动化测试：覆盖核心、词典、IPC、IME 模块以及端到端集成场景。

## 项目结构

```text
XiaoXiIme/
├── Docs/                              # 文档占位项目，用于承载解决方案级文档
├── src/
│   ├── XiaoXiIme.Foundation/          # 输入法基础模型和值对象
│   ├── XiaoXiIme.Dictionary/          # 词典接口与实现
│   ├── XiaoXiIme.ImeCore/             # 输入法核心上下文与处理逻辑
│   ├── XiaoXiIme.ImeInterop/          # Windows IME 互操作常量与导出契约
│   ├── XiaoXiIme.ImeIpc/              # IME 模块与宿主进程的 IPC 消息与协议
│   ├── XiaoXiIme.ImeModule/           # Native AOT 传统 IME 模块与入口导出
│   ├── XiaoXiIme.TsfModule/           # TSF InProc 模块与 ABI 验证目标
│   ├── XiaoXiIme.ImeHost/             # 输入法宿主进程
│   ├── XiaoXiIme.ImeUi.Avalonia/      # 候选窗口 UI 状态与控制逻辑
│   └── XiaoXiIme.Cli/                 # 发布、诊断、安装和系统测试工具
└── tests/
    ├── XiaoXiIme.Cli.Tests/           # CLI、负载与安装逻辑测试
    ├── XiaoXiIme.Dictionary.Tests/    # 词典测试
    ├── XiaoXiIme.ImeCore.Tests/       # 输入法核心测试
    ├── XiaoXiIme.ImeIpc.Tests/        # IPC 协议测试
    ├── XiaoXiIme.ImeModule.Tests/     # 传统 IME 模块测试
    ├── XiaoXiIme.TsfModule.Tests/     # TSF 模块测试
    ├── XiaoXiIme.IntegrationTests/    # 托管集成测试
    └── XiaoXiIme.IntegrationTestHost/ # 自包含真实安装与按键测试宿主
```

## 构建与测试

在 `App/XiaoXiIme` 目录下执行：

```powershell
dotnet build XiaoXiIme.slnx
```

运行测试时应逐个执行测试项目并确认实际发现了非零测试数；不要只依赖 `dotnet test XiaoXiIme.slnx` 的退出码，因为 `.slnx` 曾出现退出码为 0 但没有测试摘要的“假绿”。完整命令见 `Docs/Windows-Sandbox-Test-Workflow.md`。

发布 IME 模块前，可使用命令行工具输出检查清单：

```powershell
dotnet run --project src/XiaoXiIme.Cli/XiaoXiIme.Cli.csproj -- publish-checklist
```

发布 Native AOT IME 模块的参考命令：

```powershell
dotnet publish src/XiaoXiIme.ImeModule/XiaoXiIme.ImeModule.csproj -c Release -r win-x64 --self-contained true -p:PublishAot=true
```

最终安装包应携带 `payload-build` 生成的完整双架构负载，并使用管理员权限调用 `XiaoXiIme.Cli.exe install <payload-directory> --confirm I-UNDERSTAND-THIS-MODIFIES-WINDOWS` 完成注册。CLI 不判断机器用途；涉及真实系统安装的集成测试必须仅部署到专用测试机、Windows 沙箱或可还原虚拟机，不能加入开发机默认测试集合。完整步骤见 `src/XiaoXiIme.Cli/README.md`。

## 维护文档

维护文档用于记录项目演进过程中的约定、决策和操作步骤。建议按以下方式持续补充：

- 架构说明：记录 IME 模块、宿主进程、IPC、核心状态机、UI 的职责边界。
- IPC 协议：维护请求、响应、通知消息的路由、载荷和兼容性要求。
- 发布流程：维护 Native AOT 发布、导出符号检查、文件命名和签名要求。
- 安装与卸载：维护 Windows IME 注册、回滚和手工验证步骤。
- 测试策略：记录单元测试、集成测试、手工冒烟测试的覆盖范围。
- 已知问题：记录系统兼容性、Native AOT 警告、输入法注册限制和待确认行为。

当前已有文档：

- `src/XiaoXiIme.ImeIpc/Docs/JsonIpcDirectRouted.md`：直接路由 JSON IPC 通讯方式说明。
- `Docs/Ime-Customization.md`：输入法名称、任务栏“简体”、TSF 和文件属性的客制化说明。
- `Docs/VM-Ime-Installation-Diagnostics.md`：纯净 VM 安装排障工作模式与历史验证记录。
- `Docs/Windows-Sandbox-Test-Workflow.md`：将完整源码推送到 Windows 沙箱，并在沙箱内使用 .NET 10 SDK 构建、测试和运行项目的工作流。

## 项目开发进度

当前项目处于“基础链路已建立、产品级验证尚未完成”阶段。完整状态和剩余工作见工作区根目录 `Docs/next-session-handoff.md`。

已完成或已有基础：

- 解决方案和多项目结构已建立。
- 基础模型、输入法核心、原生词库、IPC、传统 IME 模块、宿主进程和候选窗口 UI 已拆分。
- 全拼/小鹤 package、正式词库转换、用户学习、Host 会话、HIMC 写回和安装诊断已有实现与局部自动化覆盖。
- 管理员沙箱已有单 Win32 EDIT、单输入序列 `xx -> 小希` 的真实按键冒烟。
- CLI 已提供词库、发布、安装、卸载、诊断和系统测试入口。

尚未完成：

- 正式词库高层按键测试后置；候选排序与选择、全拼/小鹤编译 package 的 ImeContext 参数化覆盖已完成。
- 候选窗口分页/选中交互已完成；真实定位、屏幕边界和多 DPI 视觉测试后置。
- 传统 IME 完整自动化、多应用、多会话及组合/取消/提交验证。
- 用户词库学习、注册、删除、持久化和损坏恢复的完整链路测试。
- Host、IPC、候选窗口生命周期，异常恢复与降级测试。
- 正式词库性能、长时间连续输入稳定性测试。
- 安装、升级、卸载、真实应用以及 x86/x64 环境矩阵验证。

已有单元测试、托管集成测试和单场景真实按键冒烟不得表述为上述产品级验收已完成。

## 贡献与维护建议

- 修改共享模型时，请同步检查 `Foundation`、`ImeIpc`、`ImeCore`、`ImeModule` 和相关测试项目。
- 修改 IPC 消息时，请同步更新协议文档和集成测试。
- 修改 Native AOT 相关代码时，请关注发布警告和导出符号验证。
- 修改安装、卸载或注册流程时，真实安装集成测试必须单独部署到可还原虚拟机或专用测试机，不得由开发机默认测试集合执行。
- 提交前建议至少执行一次 `dotnet build XiaoXiIme.slnx` 和相关测试项目。
