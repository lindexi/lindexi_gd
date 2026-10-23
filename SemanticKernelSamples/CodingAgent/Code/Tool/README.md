# CodingAgent 一次性源码迁移工具

## 运行

需要 **.NET 10 SDK**。工具是单个 `Migrate.cs` file-based app，不需要 csproj、生成器或额外 manifest。

在 **SemanticKernelSamples 根目录**执行：

`dotnet run --file CodingAgent/Code/Tool/Migrate.cs`

不传参数。只运行一次，**不要重复运行**。没有路径安全验证、存在检查、预检、冲突容错、备份、dry-run、回滚或重试；真实源码移动失败直接抛出异常，不忽略。

本次只更新工具，**未执行迁移**。编译检查使用：

`dotnet build CodingAgent/Code/Tool/Migrate.cs`

## 470 条显式 Source → Destination 映射

`Migrate.cs` 中的 `moves` 固定写出每个文件的源和目标；公共路径前缀是编译期字符串常量，不在运行时发现文件。没有递归文件枚举、顶层目录扫描或 `Directory.Move`。

| 源范围 | 目标范围 | 文件数 |
| --- | --- | ---: |
| `AgentLib/` | `CodingAgent/Code/Lib/AgentLib/` | 266 |
| `PrismCodingAgent/Code/PrismCodingAgent.AvaloniaShell/` | `CodingAgent/Code/App/PrismCodingAgent.AvaloniaShell/` | 93 |
| `PrismCodingAgent/Tests/PrismCodingAgent.AvaloniaShell.Tests/` | `CodingAgent/Code/Test/PrismCodingAgent.AvaloniaShell.Tests/` | 45 |
| `PrismCodingAgent/Docs/` | `CodingAgent/Docs/` | 41 |
| `PrismCodingAgent/README.md` | `CodingAgent/README.md` | 1 |
| `PrismCodingAgent/README.en.md` | `CodingAgent/README.en.md` | 1 |
| `PrismCodingAgent/PrismCodingAgent.slnx` | `CodingAgent/PrismCodingAgent.slnx` | 1 |
| `PrismCodingAgent/Directory.Build.props` | `CodingAgent/Directory.Build.props` | 1 |
| `DeepSeekSDK/` | `CodingAgent/Code/Lib/DeepSeekSDK/` | 13 |
| `Microsoft.Agents.AI.Extensions/` | `CodingAgent/Code/Lib/Microsoft.Agents.AI.Extensions/` | 5 |
| `Microsoft.Agents.AI.Reasoning/` | `CodingAgent/Code/Lib/Microsoft.Agents.AI.Reasoning/` | 3 |
| **合计** | | **470** |

Prism 合计 183 个文件。清单由实际递归目录盘点取得，包含库内部源码、测试、文档、demo、配置与资产，完整保留内部层级。

每条先 `Directory.CreateDirectory` 创建目标父目录，再 `File.Move` 移动该文件。不移动或删除任何源目录，也不清理源空目录。

清单不包含 `bin`、`obj`、`.vs`、`artifacts`、`.git`、`.coding-agent`、`.review-build` 等缓存目录内文件，以及 `*.user`、`*.suo`、`*.tmp`。缓存和清单外文件自然留在原位，不额外遍历或删除。

## 固定引用改写范围

移动后只对下面 **9 个文件**执行明确的静态替换或完整写入，不扫描全仓 XML/Markdown，不校验替换唯一性：

1. `CodingAgent/Code/App/PrismCodingAgent.AvaloniaShell/PrismCodingAgent.AvaloniaShell.csproj`：AgentLib.Coding 引用改为 `../../Lib/AgentLib/AgentLib.Coding/AgentLib.Coding.csproj`。
2. `CodingAgent/Code/Test/PrismCodingAgent.AvaloniaShell.Tests/PrismCodingAgent.AvaloniaShell.Tests.csproj`：应用引用改为 `../../App/PrismCodingAgent.AvaloniaShell/PrismCodingAgent.AvaloniaShell.csproj`。
3. `ChatRoom/Code/ChatRoom.AvaloniaShell/ChatRoom.AvaloniaShell.csproj`：原位更新 AgentLib.ChatRoom 引用。
4. `ChatRoom/Code/ChatRoom.Shell.Tests/ChatRoom.Shell.Tests.csproj`：原位更新 AgentLib.ChatRoom 引用，保留应用引用。
5. `ChatRoom/ChatRoom.slnx`：原位更新 AgentLib、Extensions、Reasoning 路径，其余结构保留。
6. `ChatRoom/README.md`：原位更新库链接和目录树中的库位置。
7. `CodingAgent/Code/Lib/DeepSeekSDK/README.md`：末尾离线回归测试引用改为指向同级 AgentLib 目录族的相对 Markdown 链接。
8. `CodingAgent/README.md`：更新启动命令、构建命令和完整目录树。
9. `CodingAgent/README.en.md`：同上。

上述共 9 个替换目标，另完整写入 `CodingAgent/PrismCodingAgent.slnx`，即 **10 个既有文件改写内容**；再从外根 `.gitignore` 复制内容并追加 `artifacts/`，创建 `CodingAgent/.gitignore`。外根 `.gitignore` 不变。文本替换兼容 LF/CRLF。

主解决方案按 **App / Test / Lib / Docs** 分组，包含应用、主测试、Docs，以及完整正向依赖 AgentLib、AgentLib.Coding、Microsoft.Extensions.AI.DeepSeek、Microsoft.Agents.AI.Reasoning、DeepSeekSDK；对应现有测试 AgentLib.Tests、AgentLib.Coding.Tests、DeepSeekSDK.Tests 放在 Test 分组。不加入 ChatRoom 应用、ChatRoom Docs、AgentLib.ChatRoom、其测试或无关 demo。

已读取 Prism 应用/测试项目、主 slnx、中英文 README，以及 ChatRoom 应用/测试项目、slnx、README。AgentLib 自带 slnx、AgentLib.csproj 和 AgentLib.Coding.csproj 的相对引用经过核对：四个库目录族迁移后仍为同级，内部布局不变，跨库相对引用仍成立，不改写库项目或库 slnx。Directory.Build.props 原样移动。

历史归档与其他设计文档仅移动，不改内容；不改 TOML 示例路径、机器密钥路径、TFM、SDK、包版本、程序集名或 namespace。ChatRoom 的其余文件、历史迁移工具和 Docs 保持原位不变。

## 迁移后

从 SemanticKernelSamples 根目录：

- 构建：`dotnet build CodingAgent/PrismCodingAgent.slnx`
- 启动：`dotnet run --project CodingAgent/Code/App/PrismCodingAgent.AvaloniaShell/PrismCodingAgent.AvaloniaShell.csproj --launch-profile PrismCodingAgent.AvaloniaShell`
- ChatRoom：继续使用 `ChatRoom/ChatRoom.slnx`。
