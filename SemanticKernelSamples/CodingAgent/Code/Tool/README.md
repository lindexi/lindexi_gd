# CodingAgent 一次性源码迁移工具

## 运行

需要 **.NET 10 SDK**。这是一个 file-based app，仅有 `Migrate.cs`，不需要 csproj，也不创建传统项目。

工作目录必须是 **SemanticKernelSamples**（当前工作区根目录，包含 `PrismCodingAgent`、`AgentLib`、`ChatRoom`、`CodingAgent` 等目录），运行：

`dotnet run --file CodingAgent/Code/Tool/Migrate.cs`

不传参数。执行会直接移动源码并改写引用文件；仅执行一次，不支持重复执行。没有路径、链接、仓库边界检查，没有 dry-run、确认、备份、回滚、容错或重试；文件操作错误直接抛出。

本次交付只创建工具，**没有运行迁移**。

## 明确移动清单

| 原路径（相对于 SemanticKernelSamples） | 新路径 |
| --- | --- |
| `PrismCodingAgent/Code/PrismCodingAgent.AvaloniaShell` | `CodingAgent/Code/App/PrismCodingAgent.AvaloniaShell` |
| `PrismCodingAgent/Tests/PrismCodingAgent.AvaloniaShell.Tests` | `CodingAgent/Code/Test/PrismCodingAgent.AvaloniaShell.Tests` |
| `PrismCodingAgent/Docs` | `CodingAgent/Docs` |
| `PrismCodingAgent/README.md` | `CodingAgent/README.md` |
| `PrismCodingAgent/README.en.md` | `CodingAgent/README.en.md` |
| `PrismCodingAgent/PrismCodingAgent.slnx` | `CodingAgent/PrismCodingAgent.slnx` |
| `PrismCodingAgent/Directory.Build.props` | `CodingAgent/Directory.Build.props` |
| `AgentLib` | `CodingAgent/Code/Lib/AgentLib` |
| `DeepSeekSDK` | `CodingAgent/Code/Lib/DeepSeekSDK` |
| `Microsoft.Agents.AI.Extensions` | `CodingAgent/Code/Lib/Microsoft.Agents.AI.Extensions` |
| `Microsoft.Agents.AI.Reasoning` | `CodingAgent/Code/Lib/Microsoft.Agents.AI.Reasoning` |

按文件移动而非复制，库的内部文档、测试、demo、config 结构保持不变。不删除旧目录或清单外的用户文件；旧目录可能留下空目录及本地生成物，但已迁移源码不会留下副本。

枚举时排除 `bin`、`obj`、`artifacts`、`.vs`、`.git`、`.coding-agent`、已发现的 `.review-build` 目录，以及 `.user`、`.suo`、`.tmp` 文件。这些是源码清单筛选，不会搬运或删除本地缓存。

## 引用改写机制

1. 移动前枚举旧文件，保存原路径 → 新物理路径映射，读取项目、props、targets、slnx XML 和相关 Markdown；不读取 SDK 配置或密钥文件内容。
2. XML 使用原文件目录解析引用目标，再映射引用方和目标的新位置，计算相对路径；只保存实际变化的 XML。
3. 仅处理 slnx 的 `Project/File Path`，以及 `ProjectReference`、`Content`、`None`、`Compile`、`EmbeddedResource`、`Resource`、`AvaloniaResource`、`AdditionalFiles` 的 `Include/Update/Remove`。`Link` 属性和子元素仅在确实指向已枚举物理文件或目录时重算，保留虚拟 IDE 链接名。
4. 不处理 NuGet Include、namespace、程序集名、TFM；包含 MSBuild 表达式或多项列表的属性不改写。
5. 对迁移范围和 ChatRoom 内 Markdown 的路径 token 作单轮替换。先按已有仓库根路径匹配，再按文档相对路径匹配；只有指向已枚举文件或目录的 token 才重算，避免把新路径再次当成旧路径替换。包括 README 的构建/运行命令、Markdown 文件链接及文档源码路径。两个主 README 的目录树按新布局更新；不把归档文档中的拟议文件名当成现有物理文件。
6. 所有改写内容先在内存中计算，再移动文件，最后写入新位置。
7. 从当前根 `.gitignore` 复制已有内容到 `CodingAgent/.gitignore`，追加 `artifacts/`；不移动或改写外根 `.gitignore`。

## 已调查的引用范围

- 主程序对 `AgentLib.Coding` 的引用，以及主测试对主程序的引用，需要按新 App/Test/Lib 层级重算。
- `AgentLib/AgentLib` 对 `../../Microsoft.Agents.AI.Extensions`、`../../Microsoft.Agents.AI.Reasoning` 的引用，以及 `AgentLib.Coding` 对 `../../DeepSeekSDK` 的引用，迁移后仍然有效；目录族仍是同级，工具保持原值。
- 主 `PrismCodingAgent.slnx` 保留应用、主测试和主 Docs，沿真实 ProjectReference 补齐正向依赖：AgentLib、AgentLib.Coding、Microsoft.Extensions.AI.DeepSeek、Microsoft.Agents.AI.Reasoning、DeepSeekSDK，并加入已有对应测试 AgentLib.Tests、AgentLib.Coding.Tests、DeepSeekSDK.Tests。不加入 ChatRoom 应用、ChatRoom Docs、AgentLib.ChatRoom 或其测试，不额外加入无关 demo。
- 库自带 slnx 按相同映射计算引用；同族内部布局不变时不改动。
- ChatRoom 应用、tests、Docs、props、targets、slnx 保持原位。仅改写其受影响的 ProjectReference、solution 路径及 README 库链接，不将 ChatRoom Docs 放入主 solution，因此不会引入两个 Docs 项目同名冲突。
- 已调查 Prism 主测试、ChatRoom.Shell.Tests、AgentLib 测试、DeepSeekSDK 测试：未发现依赖旧目录查找仓库源码的定位代码。截图使用输出目录、测试资产使用现有相对项目布局，均不改测试源码。
- ImageGeneration、MiniMaxSdk 等其他目录不迁移；其无关引用不改写。

## 迁移后的常用命令

以下同样从 `SemanticKernelSamples` 运行：

- 构建：`dotnet build CodingAgent/PrismCodingAgent.slnx`
- 启动：`dotnet run --project CodingAgent/Code/App/PrismCodingAgent.AvaloniaShell/PrismCodingAgent.AvaloniaShell.csproj --launch-profile PrismCodingAgent.AvaloniaShell`
- ChatRoom 仍使用原来的 `ChatRoom/ChatRoom.slnx`。

本工具本身可用 `dotnet build CodingAgent/Code/Tool/Migrate.cs` 编译；编译不会执行迁移。不要用运行迁移的方式做编译验证。
