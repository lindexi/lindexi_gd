# PrismCodingAgent

[English](README.en.md) | 简体中文

基于 Avalonia 和 .NET 10 的 AI 编程智能体桌面应用。围绕本地代码工作目录组织任务，与 OpenAI 兼容模型服务对话，通过工具读取、分析和修改代码，并在界面中查看执行过程。

![PrismCodingAgent 中文界面](Docs/Images/coding-chat-zh-Hans.png)

## 功能

- **工作任务**：创建、切换、重命名任务，为任务选择工作目录、模型与思考强度；支持多个任务各自运行。
- **历史与归档**：查看并恢复历史会话，归档任务并从归档列表恢复；重启恢复任务信息，不会自动恢复执行。
- **模型服务**：配置 OpenAI 兼容 endpoint、API key 和模型，管理多个服务商与模型；提供 Chat 与 Responses API 运行选项，具体能力取决于服务端支持。
- **工具与子智能体**：展示工具调用与结果；提供文件操作、Roslyn 代码查询和 .NET 构建/测试等工具。Chat 模式支持编程子智能体委托；也可使用图片分析工具。
- **界面与连接设置**：简体中文、英文界面，可选网络代理及 Windows 沙盒工具连接。

## 使用

### 首次配置与开始任务

当前可从源码启动，步骤见下方“开发入门”。首次运行后：

1. 在欢迎界面或设置中添加模型服务，填写 **endpoint**（例如 `https://api.openai.com/v1`，以服务商要求为准）、**API key** 和服务商提供的 **模型 ID**；选择默认模型并保存。
2. 新建或选择工作任务，在工作目录输入框填写本地项目目录并应用；选择该任务使用的模型，按需调整思考强度。
3. 在输入框描述目标，点击“发送”或按 **Ctrl+Enter**；Enter 用于换行。运行期间可查看流式回复、工具调用和子智能体详情。
4. 点击“停止”取消当前任务的运行；切换任务可查看其他任务，停止操作不影响其他任务。
5. 使用历史入口恢复会话；不再活跃的任务可通过任务菜单归档，需要时从归档列表恢复。

工具可能修改工作区文件或执行构建、测试；请在可信项目中使用，并通过版本控制检查变更。模型需支持相应工具调用；Responses API 与 Chat 的能力并不完全相同。

### 语言、设置与本地数据

- 首次运行按系统 UI 语言及其父语言匹配 `zh-Hans` 或 `en`，无法匹配时使用简体中文。
- 在设置中选择语言，**保存后**立即应用并持久化；下次启动优先使用已保存的语言。模型、代理和沙盒配置也需保存；更改运行环境配置后重新打开工作区。
- 网络代理为可选项。Windows 沙盒默认不启用；启用前需准备可用的 `WinRemoteShell.exe` 和服务器地址，可在设置中测试连接。
- 数据保存在系统 `LocalApplicationData` 下的 `CodingChatRoom` 目录：`AgentConfiguration.json` 保存模型配置，`ShellSettings.json` 保存界面与连接设置，`WorkTasks.json` 保存任务信息，另有 `Sessions`、`Logs` 和 `Abilities`。模型配置含 API key，请勿提交或公开这些本地数据。

### 界面演示

启动配置提供 `中文演示`（`--demo-zh`）和 `英文演示`（`--demo-en`）。演示使用内置任务、消息、工具与子智能体展示数据，**不会真实调用模型或执行工具**，不能用来验证服务连接或编程结果。

## 开发入门

### 环境与目录

需要 **.NET 10 SDK**。请保留完整的 **SemanticKernelSamples 父工作区**，不要只复制 `PrismCodingAgent` 目录：应用通过项目引用依赖相邻的 `AgentLib/AgentLib.Coding` 及其依赖项目，不是独立 NuGet 依赖。

```text
SemanticKernelSamples/
├─ AgentLib/                         # 共享智能体与编程工具
└─ PrismCodingAgent/
   ├─ PrismCodingAgent.slnx          # 产品解决方案
   ├─ Code/PrismCodingAgent.AvaloniaShell/
   ├─ Tests/PrismCodingAgent.AvaloniaShell.Tests/
   └─ Docs/                         # 设计、调查与图片
```

代码语义工具使用 `roslyn-language-server`，需使其启动命令可用；构建、测试等工具也依赖对应 SDK 与本机环境。Avalonia 桌面界面不代表所有工具都支持所有平台，Windows 沙盒相关能力需要相应 Windows 环境与外部服务。

### 构建、启动与测试

**以下所有命令均在 `SemanticKernelSamples` 根目录执行**，不是在 `PrismCodingAgent` 目录执行。

```sh
dotnet build PrismCodingAgent/PrismCodingAgent.slnx

dotnet run --project PrismCodingAgent/Code/PrismCodingAgent.AvaloniaShell/PrismCodingAgent.AvaloniaShell.csproj --launch-profile PrismCodingAgent.AvaloniaShell

dotnet test PrismCodingAgent/Tests/PrismCodingAgent.AvaloniaShell.Tests/PrismCodingAgent.AvaloniaShell.Tests.csproj --filter "TestCategory!=LiveApi"
```

常规测试使用 MSTest 与 Avalonia.Headless。上述过滤器排除会访问真实模型服务的 `LiveApi` 测试；真实服务测试需单独准备有效配置，可能产生 API 费用。

中文、英文演示可按 profile 启动：

```sh
dotnet run --project PrismCodingAgent/Code/PrismCodingAgent.AvaloniaShell/PrismCodingAgent.AvaloniaShell.csproj --launch-profile "中文演示"
dotnet run --project PrismCodingAgent/Code/PrismCodingAgent.AvaloniaShell/PrismCodingAgent.AvaloniaShell.csproj --launch-profile "英文演示"
```

也可绕过 profile，直接传入参数（将 `--demo-en` 换成 `--demo-zh` 可切换语言）：

```sh
dotnet run --project PrismCodingAgent/Code/PrismCodingAgent.AvaloniaShell/PrismCodingAgent.AvaloniaShell.csproj --no-launch-profile -- --demo-en
```

`调试欢迎界面` profile 对应 `--debug-onboarding`，使用独立的临时数据目录调试首次配置流程。

### 项目入口

下列应用内路径均相对于 `Code/PrismCodingAgent.AvaloniaShell/`：

| 入口 | 职责 |
| --- | --- |
| [Program.cs](Code/PrismCodingAgent.AvaloniaShell/Program.cs) | Avalonia 桌面启动与平台检测 |
| [App.axaml.cs](Code/PrismCodingAgent.AvaloniaShell/App.axaml.cs) | 解析启动选项，加载设置、任务与主窗口；分流演示模式 |
| [ViewModels/MainViewModel.cs](Code/PrismCodingAgent.AvaloniaShell/ViewModels/MainViewModel.cs) | 工作任务、归档、历史入口与全局设置协调 |
| [ViewModels/ChatViewModel.cs](Code/PrismCodingAgent.AvaloniaShell/ViewModels/ChatViewModel.cs) | 输入、模型选择、工作目录、发送与停止交互 |
| [Services/WorkTasks/CodingWorkTaskRuntimeFactory.cs](Code/PrismCodingAgent.AvaloniaShell/Services/WorkTasks/CodingWorkTaskRuntimeFactory.cs) | 装配每个任务的模型、会话存储、控制器与编程 Agent |
| [Services/WorkTasks/CodingWorkTaskRuntime.cs](Code/PrismCodingAgent.AvaloniaShell/Services/WorkTasks/CodingWorkTaskRuntime.cs) | 持有任务运行时资源 |
| [Services/Chat](Code/PrismCodingAgent.AvaloniaShell/Services/Chat) | Chat / Responses 执行适配 |
| [AgentLib.Coding](../AgentLib/AgentLib.Coding)（父工作区） | `CodingAgent.cs` 编程执行入口，`CodingWorkspaceCache.cs` 工具装配，`CodingSubAgentTools.cs` 编程委托，`RoslynAgentTools.cs` / `DotNetCliTools.cs` 等工具实现 |

更多设计与调查资料见 [Docs](Docs)。其中含历史记录与未实施方案，当前功能及路径应以源码为准。
