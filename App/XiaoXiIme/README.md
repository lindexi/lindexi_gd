# XiaoXiIme

XiaoXiIme 是一个基于 .NET 10 的小希输入法实验项目，用于探索 Windows 传统 IME 模块、输入法核心状态机、候选词窗口、词典服务、进程间通信以及安装发布流程等能力。

当前项目重点是把输入法的核心逻辑、Windows IME 导出模块、宿主进程、IPC 协议和 UI 展示拆分成可测试、可维护的多个项目，便于逐步完善输入体验和系统集成能力。

## 项目内容

本解决方案包含以下主要内容：

- 输入法核心：维护输入上下文、组合文本、候选词、按键处理结果等核心状态。
- 词典服务：提供内存词典接口与实现，支撑编码到候选词的查询。
- Windows IME 模块：提供传统 IME 入口导出、按键翻译、上下文读写和与宿主进程通讯的桥接逻辑。
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
│   ├── XiaoXiIme.ImeModule/           # Native AOT IME 模块与传统 IME 入口导出
│   ├── XiaoXiIme.ImeHost/             # 输入法宿主进程
│   ├── XiaoXiIme.ImeUi.Avalonia/      # 候选窗口 UI 状态与控制逻辑
│   └── XiaoXiIme.Cli/                 # 发布检查和输入法安装工具
└── tests/
    ├── XiaoXiIme.Dictionary.Tests/    # 词典测试
    ├── XiaoXiIme.ImeCore.Tests/       # 输入法核心测试
    ├── XiaoXiIme.ImeIpc.Tests/        # IPC 协议测试
    ├── XiaoXiIme.ImeModule.Tests/     # IME 模块测试
    └── XiaoXiIme.IntegrationTests/    # 集成测试
```

## 构建与测试

在 `App/XiaoXiIme` 目录下执行：

```powershell
dotnet build XiaoXiIme.slnx
```

运行测试：

```powershell
dotnet test XiaoXiIme.slnx
```

发布 IME 模块前，可使用命令行工具输出检查清单：

```powershell
dotnet run --project src/XiaoXiIme.Cli/XiaoXiIme.Cli.csproj -- publish-checklist
```

发布 Native AOT IME 模块的参考命令：

```powershell
dotnet publish src/XiaoXiIme.ImeModule/XiaoXiIme.ImeModule.csproj -c Release -r win-x64 --self-contained true -p:PublishAot=true
```

最终安装包可以使用管理员权限直接调用 `XiaoXiIme.Cli.exe install <ime-file>` 完成注册。CLI 不判断机器用途；后续涉及真实系统安装的集成测试必须仅部署到专用测试机或可还原虚拟机，不能加入开发机默认测试集合。完整步骤见 `src/XiaoXiIme.Cli/README.md`。

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

## 项目开发进度

当前项目处于原型与基础设施完善阶段。

已完成或已有基础：

- 解决方案和多项目结构已建立。
- 基础模型、输入法核心、词典、IPC、IME 模块、宿主进程和候选窗口 UI 项目已拆分。
- IME 模块已包含传统 IME 入口相关导出逻辑。
- CLI 已提供发布、导出和卸载检查清单，以及可供最终安装包调用的 Windows IME 安装命令。
- 已建立核心、词典、IPC、IME 模块和集成测试项目。

持续推进方向：

- 完善输入法核心状态机和候选词选择体验。
- 完善词典数据来源、加载、更新和持久化策略。
- 完善 IME 模块与 Windows 输入法系统的安装、注册和回滚工具链。
- 完善候选窗口视觉表现、定位、交互和跨进程状态同步。
- 扩展 IPC 协议的兼容性说明和异常处理能力。
- 增加更多自动化测试与手工验收清单。

## 贡献与维护建议

- 修改共享模型时，请同步检查 `Foundation`、`ImeIpc`、`ImeCore`、`ImeModule` 和相关测试项目。
- 修改 IPC 消息时，请同步更新协议文档和集成测试。
- 修改 Native AOT 相关代码时，请关注发布警告和导出符号验证。
- 修改安装、卸载或注册流程时，真实安装集成测试必须单独部署到可还原虚拟机或专用测试机，不得由开发机默认测试集合执行。
- 提交前建议至少执行一次 `dotnet build XiaoXiIme.slnx` 和相关测试项目。
