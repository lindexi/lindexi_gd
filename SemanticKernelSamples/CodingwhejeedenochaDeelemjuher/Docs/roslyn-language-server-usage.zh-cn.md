# roslyn-language-server 使用指南

`roslyn-language-server` 是 Roslyn 仓库提供的独立语言服务器，以 Language Server Protocol（LSP）向编辑器、IDE、命令行工具和 AI 编码客户端提供 C#、Visual Basic 与 Razor 语言功能。

它支持代码补全、悬停信息、转到定义、查找引用、重命名、代码操作与重构、诊断、格式化、语义标记、调用层次结构、类型层次结构和内嵌提示等功能。

> `roslyn-language-server` 主要供 LSP 客户端作为子进程启动，不是交互式命令行程序。启动后必须通过标准输入输出或命名管道交换带 `Content-Length` 消息头的 JSON-RPC/LSP 消息。
>
> 命令行参数属于工具集成接口，未来版本可能调整。本文以当前仓库代码为准。

## 前置条件

当前仓库将语言服务器构建为 `net10.0` 工具，因此运行当前版本需要可用的 .NET 10 Runtime 或 SDK。被分析的项目仍可使用其自身支持的较早 .NET SDK；运行语言服务器的 Runtime 与项目目标框架不是同一概念。

确认环境：

```shell
dotnet --info
```

## 安装与获取

### 安装 NuGet 工具包

当前包通常以预发行版本提供：

```shell
dotnet tool install --global roslyn-language-server --prerelease
```

已安装时可更新：

```shell
dotnet tool update --global roslyn-language-server --prerelease
```

确认命令可用：

```shell
roslyn-language-server --help
```

### 从当前仓库构建

在 Roslyn 仓库根目录构建可执行项目：

```shell
dotnet build src/LanguageServer/Microsoft.CodeAnalysis.LanguageServer/Microsoft.CodeAnalysis.LanguageServer.csproj
```

也可以发布指定平台的框架依赖产物：

```shell
dotnet publish src/LanguageServer/Microsoft.CodeAnalysis.LanguageServer/Microsoft.CodeAnalysis.LanguageServer.csproj -c Release -r win-x64
```

支持的 RID 包括：

- `win-x64`、`win-arm64`
- `linux-x64`、`linux-arm64`
- `linux-musl-x64`、`linux-musl-arm64`
- `osx-x64`、`osx-arm64`

项目将发布输出放在：

```text
artifacts/LanguageServer/<Configuration>/net10.0/<RID>/
```

开发构建产物的确切路径受仓库构建属性影响。需要定位 DLL 时，可在 `artifacts/bin/Microsoft.CodeAnalysis.LanguageServer/` 下查找 `Microsoft.CodeAnalysis.LanguageServer.dll`，然后使用：

```shell
dotnet "/path/to/Microsoft.CodeAnalysis.LanguageServer.dll" --stdio --autoLoadProjects
```

## 最简单的启动方式

对大多数第三方 LSP 客户端，使用标准输入输出：

```shell
roslyn-language-server --stdio --autoLoadProjects
```

其中：

- `--stdio`：通过进程的 stdin/stdout 传输 LSP 消息。
- `--autoLoadProjects`：根据客户端在 `initialize` 中提供的 `workspaceFolders` 自动发现并加载解决方案或项目。

必须在 `--stdio` 与 `--pipe` 中选择且只能选择一个。两者都不指定或同时指定时，服务器会退出并报告错误。

### stdio 集成的重要要求

- 客户端必须把 stdin/stdout 专用于 LSP 数据，不能把普通文本写入服务器 stdin。
- 服务器会把普通控制台输出重定向到 stderr，以避免破坏 stdout 上的 LSP 消息。
- 客户端应单独捕获 stderr，用于查看启动日志和错误。
- 消息使用 LSP 标准的带消息头 JSON-RPC 帧，基本形式为 `Content-Length: N\r\n\r\n<JSON>`；`N` 是编码后 JSON 正文的字节数，而不是字符数，也不能使用逐行 JSON 代替。

## LSP 生命周期

客户端连接后应遵循标准 LSP 生命周期：

1. 发送 `initialize` 请求。
2. 等待服务器返回 `InitializeResult` 与能力列表。
3. 发送 `initialized` 通知。
4. 发送文档同步和语言功能请求。
5. 结束时发送 `shutdown` 请求。
6. 收到 `shutdown` 响应后发送 `exit` 通知，并关闭传输。

服务器只有在收到 `initialized` 后才会运行项目自动加载等初始化服务。只发送 `initialize` 而不发送 `initialized`，可能得到服务器能力，但不会完成工作区初始化。

### 最小 initialize 参数

通用客户端至少应提供 `capabilities`；启用项目自动加载时还应提供文件 URI 形式的 `workspaceFolders`：

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "initialize",
  "params": {
    "processId": 12345,
    "rootUri": "file:///workspace/MyRepo",
    "capabilities": {
      "window": {
        "workDoneProgress": true
      },
      "textDocument": {
        "hover": {
          "contentFormat": ["markdown", "plaintext"]
        },
        "semanticTokens": {
          "requests": {
            "range": true
          }
        }
      },
      "workspace": {
        "workspaceFolders": true
      }
    },
    "workspaceFolders": [
      {
        "uri": "file:///workspace/MyRepo",
        "name": "MyRepo"
      }
    ]
  }
}
```

随后发送：

```json
{
  "jsonrpc": "2.0",
  "method": "initialized",
  "params": {}
}
```

注意：

- Windows 文件 URI 应使用标准 URI 表示，例如 `file:///C:/Code/MyRepo`。
- `workspaceFolders` 只支持有效且存在的本地 `file:` 目录用于自动发现项目。
- `processId` 可让服务器监控客户端进程；也可以在命令行使用 `--clientProcessId`。成功注册后，客户端退出会触发服务器终止。
- 客户端声明的能力会影响服务器返回的能力。例如客户端支持 semantic tokens range 时，服务器优先使用范围语义标记。

## 项目与解决方案加载

语言服务器必须加载项目上下文，才能提供完整的跨文件语义分析、引用查找和项目诊断。只打开一个未归属项目的 `.cs` 文件时，部分功能仍可工作，但结果通常不如加载解决方案完整。

### 自动加载

启动时添加：

```shell
roslyn-language-server --stdio --autoLoadProjects
```

`--autoLoadProjects` 可带一个正整数作为最多自动加载的项目数：

```shell
roslyn-language-server --stdio --autoLoadProjects 200
```

- 不带数值时，当前默认上限为 `500`。
- 数值必须大于 `0`。
- 不指定该参数时，不执行基于工作区目录的自动项目发现。

服务器在收到 `initialized` 后，按以下顺序选择加载方式。

### 1. 读取 dotnet.defaultSolution

服务器检查各工作区目录中的 `.vscode/settings.json`：

```jsonc
{
  "dotnet.defaultSolution": "src/MyApp.slnx"
}
```

值可以是绝对路径，也可以是相对于工作区目录的路径。指向 `.sln` 或 `.slnx` 可避免大型仓库中扫描和加载不需要的项目。

单工作区场景下可以禁用自动加载：

```jsonc
{
  "dotnet.defaultSolution": "disable"
}
```

在多工作区场景中，某个目录设置为 `disable` 不会禁用整个自动加载流程，也不会把该目录从后续 `*.csproj` 扫描中排除。服务器会继续查找其他工作区中显式配置的解决方案；如果未找到，仍会进入项目发现。因此多工作区客户端若要完全控制加载内容，应显式发送 `solution/open` 或 `project/open`，或不启用 `--autoLoadProjects`。

### 2. 加载根目录唯一解决方案

如果只有一个工作区目录，且其根目录中恰好只有一个 `.sln` 或 `.slnx` 文件，服务器会加载该解决方案。

如果根目录存在多个解决方案，服务器不会猜测要使用哪一个，而会继续执行项目发现。大型仓库应显式设置 `dotnet.defaultSolution`。

### 3. 递归发现 C# 项目

若未选择解决方案，服务器会递归扫描所有工作区目录中的 `*.csproj` 并逐个加载。自动发现不扫描 `*.vbproj`；Visual Basic 项目需要由解决方案包含，或由客户端通过 `project/open` 显式加载。

当发现数量超过 `--autoLoadProjects` 上限时，服务器会先排除路径组件名为 `test` 或 `tests` 的项目；如果仍然超出上限，则按当前文件系统枚举顺序截取前 N 个项目，客户端不应依赖该顺序稳定。此模式可能不能完整保持大型仓库预期的解决方案边界，因此优先推荐指定 `.sln` 或 `.slnx`。

### 由客户端显式加载

服务器还实现了非标准通知，可由了解 Roslyn 扩展协议的客户端显式加载解决方案或项目。通用客户端通常优先使用 `--autoLoadProjects`，因为下列方法不是 LSP 标准的一部分。

加载一个解决方案：

```json
{
  "jsonrpc": "2.0",
  "method": "solution/open",
  "params": {
    "solution": "file:///workspace/MyRepo/MyRepo.slnx"
  }
}
```

加载多个项目：

```json
{
  "jsonrpc": "2.0",
  "method": "project/open",
  "params": {
    "projects": [
      "file:///workspace/MyRepo/src/App/App.csproj",
      "file:///workspace/MyRepo/src/Library/Library.csproj"
    ]
  }
}
```

客户端若声明 `window.workDoneProgress`，加载过程中可接收进度通知。

## 文档同步

服务器声明增量文本同步：

- 打开文件：`textDocument/didOpen`
- 修改文件：`textDocument/didChange`，使用 `Incremental` 变更
- 保存文件：`textDocument/didSave`，无需在保存通知中附带全文
- 关闭文件：`textDocument/didClose`

在请求补全、悬停、定义、引用等功能之前，客户端应先发送 `didOpen`，并为每次编辑维护递增的文档版本号。文档 URI 应保持一致且使用绝对文件 URI。

## 常见客户端接入

### 通用编辑器或自研客户端

客户端进程管理器可按以下方式启动服务器：

```text
command: roslyn-language-server
args: [--stdio, --autoLoadProjects, --logLevel, Information]
languages: [csharp, vb, razor]
extensions: [.cs, .vb, .razor, .cshtml]
```

实际配置字段取决于客户端。核心要求是：

1. 以子进程方式启动命令。
2. 将子进程 stdin/stdout 连接到客户端的 LSP JSON-RPC 实现。
3. 把仓库目录放入 `initialize.params.workspaceFolders`。
4. 完成 `initialize` 与 `initialized` 握手。
5. 实现文档同步并按服务器返回的 capabilities 决定可用功能。
6. 把 stderr 保存为日志，不要混入 LSP 输入流。

### VS Code 扩展开发

在 VS Code 扩展中，可使用 `vscode-languageclient` 创建 `LanguageClient`，并把服务器选项配置为命令 `roslyn-language-server`、参数 `--stdio --autoLoadProjects`。客户端应把 VS Code 工作区文件夹传入初始化参数，并把文档选择器至少配置为 `csharp`；需要 Razor 时再加入对应语言与文件模式。

正式集成应以服务器初始化响应中的 capabilities 为准，而不是假定所有功能始终可用。

### GitHub Copilot CLI

本仓库已经在 `.github/copilot/settings.json` 中启用了 `dotnet/skills` 插件，并在 `.vscode/settings.json` 中设置：

```jsonc
{
  "dotnet.defaultSolution": "Roslyn.slnx"
}
```

因此支持该插件配置的 Copilot CLI 可以自动获得 C# LSP。相关细节见 [Roslyn Language Server - Copilot Plugin](roslyn-language-server-copilot-plugin.md)。

## 命名管道模式

命名管道适用于能够自行创建管道服务端的宿主：

```shell
roslyn-language-server --pipe /tmp/roslyn-lsp.sock --clientProcessId 12345
```

连接方向与常见“服务器创建管道”模式相反：

1. LSP 客户端先创建并监听命名管道。
2. 客户端启动 `roslyn-language-server`。
3. 客户端通过 `--pipe` 传入完整管道路径。
4. 语言服务器作为管道客户端连接该地址。
5. 连接建立后，双方在同一双向流上交换 LSP 消息。

路径格式：

- Windows：`\\.\pipe\<name>`
- Unix：Unix domain socket 的完整路径，例如 `/tmp/<name>.sock`

Windows 下服务器内部会移除 `\\.\pipe\` 前缀后再创建 `NamedPipeClientStream`。管道以当前用户权限和异步模式连接。

对于没有明确管道宿主需求的第三方客户端，优先使用 `--stdio`。

## 命令行参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `--stdio` | `false` | 使用 stdin/stdout 传输 LSP；与 `--pipe` 互斥。 |
| `--pipe <path>` | 无 | 连接客户端预先创建的命名管道或 Unix socket；与 `--stdio` 互斥。 |
| `--autoLoadProjects [maximum]` | 未启用；省略数值时为 `500` | 根据 `workspaceFolders` 自动加载解决方案或项目。可选上限必须为正整数。 |
| `--logLevel <level>` | `Information` | 最低日志级别：`Trace`、`Debug`、`Information`、`Warning`、`Error`、`Critical` 或 `None`。 |
| `--extensionLogDirectory <path>` | 无 | 扩展日志目录。目录不存在时服务器会创建它。具体是否写入文件取决于加载的日志组件。 |
| `--clientProcessId <pid>` | 无 | 监控客户端进程；客户端退出时终止服务器。标准 `initialize.processId` 也能注册监控。 |
| `--sourceGeneratorExecutionPreference <mode>` | `Automatic` | 源生成器执行策略：`Automatic` 在项目变化后自动运行；`Balanced` 通常在保存、构建等较大事件后运行，并需要客户端支持刷新流程。 |
| `--debug` | `false` | 启动时附加或等待调试器。Windows 调用调试器启动；其他平台最多等待约 2 分钟。 |
| `--telemetryLevel <level>` | `off` | 遥测级别，支持场景包括 `all`、`crash`、`error`、`off`。 |
| `--sessionId <id>` | 无 | 为遥测会话指定 ID。 |
| `--extension <assembly-path>` | 无 | 加载扩展程序集完整路径；可传入多个路径。属于高级宿主扩展接口。 |
| `--devKitDependencyPath <path>` | 无 | C# Dev Kit 集成使用的 Roslyn 依赖路径。设置后启用 Dev Kit 模式，并禁止 `--autoLoadProjects`。普通独立客户端不要使用。 |
| `--csharpDesignTimePath <path>` | 无 | C# design-time targets 路径，供特定项目系统或产品集成使用。 |
| `--brokeredServicePipeName <name>` | 无 | 连接远程 brokered service 进程的管道名，供高级产品集成使用。 |

> 当前命令行解析器接受 `--brokeredServicePipeName`，但它没有写入 `ServerConfiguration`，当前独立启动路径不会消费该值。不要把它作为通用客户端所需参数。

## 主要标准 LSP 能力

服务器返回的能力会根据客户端 capabilities 调整。当前默认能力包括：

各项能力对应的请求方法、`params` 输入格式、`result` 输出格式、能力协商条件和完整 JSON 示例，见 [roslyn-language-server 标准 LSP 能力输入输出参考](roslyn-language-server-standard-lsp-capabilities.zh-cn.md)。

- 代码补全与 completion resolve
- 签名帮助
- 悬停
- 转到定义、类型定义与实现
- 查找引用
- 文档高亮
- 文档符号与工作区符号
- 重命名与 prepare rename
- Quick Fix、重构及 code action resolve
- 全文、范围和输入时格式化
- 折叠范围与选择范围
- 调用层次结构与类型层次结构
- 语义标记；始终支持 range，客户端声明支持 range 时不提供 full，否则提供 full
- CodeLens
- Inlay Hint
- 增量文档同步
- `workspace/willRenameFiles` 文件重命名前请求（客户端声明文件操作能力且存在匹配 listener 时）
- Pull diagnostics；客户端明确支持动态注册时在初始化后动态注册，明确不支持动态注册时通过初始化结果静态声明

Roslyn 还实现了一些 Visual Studio LSP 扩展和自定义消息。通用客户端不应依赖这些扩展，除非已经实现相应协议类型与能力协商。

## 日志与排障

### 启动后立即退出

检查是否恰好指定了一个传输参数：

```shell
roslyn-language-server --stdio
```

以下两种启动都会失败：

```shell
roslyn-language-server
roslyn-language-server --stdio --pipe some-pipe
```

### 没有加载项目

依次检查：

1. 启动参数是否包含 `--autoLoadProjects`。
2. 客户端是否发送了 `initialized`。
3. `initialize.params.workspaceFolders` 是否为非空的本地 `file:` URI。
4. 工作区路径是否存在。
5. `.vscode/settings.json` 中的 `dotnet.defaultSolution` 是否指向存在的 `.sln` 或 `.slnx`。
6. 项目能否在命令行正常 `dotnet restore` 和 `dotnet build`。
7. stderr 中是否存在 SDK、MSBuild、项目评估或扩展加载错误。

### 大型仓库加载过慢

- 使用 `dotnet.defaultSolution` 指定一个解决方案，不要递归加载所有 `.csproj`。
- 给 `--autoLoadProjects` 设置合理上限。
- 使用 `--logLevel Debug` 或 `Trace` 查看项目发现和加载过程。
- 客户端应支持 `window.workDoneProgress`，以显示加载进度。

### stdout 出现无法解析的数据

stdio 模式下 stdout 只能包含 LSP 帧。不要通过包装脚本向 stdout 打印提示信息；包装脚本日志应写入 stderr。客户端也应分别处理 stdout 和 stderr。

### 源生成器结果不刷新

通用客户端优先使用默认的：

```shell
--sourceGeneratorExecutionPreference Automatic
```

`Balanced` 模式要求客户端正确处理源生成文档内容刷新等流程，不适合尚未实现 Roslyn 相关刷新支持的简单客户端。

## 安全与稳定性建议

- 只加载可信的 `--extension` 程序集；扩展代码会进入语言服务器进程。
- 不要让不可信输入控制 `--devKitDependencyPath`、`--csharpDesignTimePath` 或扩展程序集路径。
- 使用 `--clientProcessId` 或 `initialize.processId` 避免客户端崩溃后残留服务器进程。
- 客户端应对服务器异常退出、管道断开和 JSON-RPC 错误实现重启与退避策略。
- 升级预发行工具后重新读取 `--help` 并测试初始化能力，因为命令行和扩展协议可能发生变化。

## 代码位置

- 可执行项目：`src/LanguageServer/Microsoft.CodeAnalysis.LanguageServer/`
- 命令行定义：`src/LanguageServer/Microsoft.CodeAnalysis.LanguageServer/LanguageServerCommandLine.cs`
- 程序入口与传输连接：`src/LanguageServer/Microsoft.CodeAnalysis.LanguageServer/Program.cs`
- 项目自动加载：`src/LanguageServer/Microsoft.CodeAnalysis.LanguageServer/HostWorkspace/AutoLoadProjectsInitializer.cs`
- 标准 LSP 实现与协议类型：`src/LanguageServer/Protocol/`
- 默认服务器能力：`src/LanguageServer/Protocol/DefaultCapabilitiesProvider.cs`

## 相关资料

- [Language Server Protocol](https://microsoft.github.io/language-server-protocol/)
- [项目自带的 roslyn-language-server README](../src/LanguageServer/Microsoft.CodeAnalysis.LanguageServer/README.md)
- [Roslyn Language Server - Copilot Plugin](roslyn-language-server-copilot-plugin.md)
- [C# Language Server Ecosystem Glossary](Glossary%20-%20C%23%20Language%20Server%20Ecosystem.md)
