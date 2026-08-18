# WinRemoteShell 实现计划

## 1. 实现目标

按照《Commands.md》和《Architecture.md》完整实现 WinRemoteShell：

- 单个 `Microsoft.NET.Sdk.Web` 项目同时承载 Server 与 Client。
- 使用 DotNetCampus.CommandLine 的 Command/CommandHandler 模式完成命令分发。
- Server 为每次 `exec` 创建独立目标进程，并维护一个仅供 `shell`、`cd`、`ls` 使用且可自动重启的 `cmd.exe`。
- 支持 `server`、`exec`、`shell`、`push`、`pull`、`screenshot` 命令。
- 支持 Windows 服务安装与卸载。
- 保持 Native AOT 兼容。
- MSTest 测试尽量调用真实 Host、HTTP、WebSocket、文件系统与命令执行逻辑，减少 Mock/Fake。

## 2. 实现约束

- 不自行实现命令帮助，使用 DotNetCampus.CommandLine 提供的能力。
- 不增加不必要的参数、URL、路径、文件存在性或通配符校验；操作失败时保留框架、网络、文件系统和进程 API 的自然异常。
- Client 的服务器地址仅按“命令行参数 > `WINRS_SERVER` 环境变量”解析。
- `exec` 的命令参数以数组形式通过 HTTP POST 传输，不在 Client 端重建或转义 Windows 命令行。
- 自动端口使用 `Socket` 绑定端口 0 的指定算法取得。
- 安装服务时若未指定端口，先自动选择端口，再把实际端口写入服务启动参数。
- 不以阶段性占位实现替代完整功能。

## 3. 目录与职责

```text
WinRemoteShell/
├── Program.cs
├── Commands/
│   ├── ServerCommand.cs
│   ├── ExecCommand.cs
│   ├── ShellCommand.cs
│   ├── PushCommand.cs
│   ├── PullCommand.cs
│   └── ScreenshotCommand.cs
├── Client/
│   ├── ServerAddressResolver.cs
│   ├── ExecClient.cs
│   ├── ShellClient.cs
│   ├── PushClient.cs
│   ├── PullClient.cs
│   └── ScreenshotClient.cs
├── Server/
│   ├── AvailablePortFinder.cs
│   ├── ServerHost.cs
│   ├── CmdProcess.cs
│   ├── ServiceInstaller.cs
│   └── Endpoints/
│       ├── ExecEndpoint.cs
│       ├── ShellEndpoint.cs
│       ├── PushEndpoint.cs
│       ├── PullEndpoint.cs
│       └── ScreenshotEndpoint.cs
└── Shared/
    └── Models.cs
```

`Program.cs` 仅解析命令行、注册 Handler、运行匹配命令并设置退出码。CommandHandler 负责把解析结果交给具体 Client/Server 类型。网络、文件、进程与服务操作位于业务类型中。

## 4. 命令实现

### 4.1 server

参数：

- `--port`
- `--install-service`
- `--uninstall-service`

行为：

- 普通启动：指定端口则直接使用；否则通过 `AvailablePortFinder.GetAvailablePort(IPAddress.Any)` 选择端口；在控制台输出实际端口后启动 Host。
- 安装服务：指定端口则直接使用；否则自动选择；把 `"<exe>" server --port <actualPort>` 写入 Windows 服务配置。
- 卸载服务：停止并删除服务。

### 4.2 exec

参数：

- `--server`
- `--timeout`
- `--` 后的位置参数数组

Client 将参数数组和超时值作为 JSON POST 到 `/exec`。Server 将第一项作为可执行文件，其余项通过 `ProcessStartInfo.ArgumentList` 逐项传入，每次请求创建独立进程。stdout/stderr 合并为流式 HTTP 响应；超时或请求取消时终止该进程树。需要 cmd 语法时由用户显式传入 `cmd.exe /D /C ...`。

### 4.3 shell

参数：

- `--server`

Client 通过 WebSocket 连接 `/shell`，桥接控制台输入输出。关闭 WebSocket 不关闭 Server 的全局 cmd。

### 4.4 push

参数：

- `--server`
- `--source`
- `--target`

单文件直接流式上传。目录递归枚举后用 ZIP 流上传，Server 解包到目标路径。生产代码不预先判断文件/目录是否存在或是否包含通配符。

### 4.5 pull

参数：

- `--server`
- `--source`
- `--output`

Server 对文件直接返回流；对目录实时生成 ZIP 响应。Client 根据响应元数据保存文件或解包目录。

### 4.6 screenshot

参数：

- `--server`
- `--output`

Server 捕获当前桌面并返回 PNG。Client 未指定输出时在当前目录生成 `screenshot_yyyyMMdd_HHmmss.png`；指定输出时按传入路径保存。

## 5. 共享协议

- `ls` 通过可选 `path` 查询参数指定目录；相对路径基于远端当前工作目录解析，列举操作不修改当前目录。
- `ExecRequest`：参数数组与可选超时秒数。
- 文件上传通过请求头传递远端目标路径和文件/目录类型，请求体承载原始文件流或 ZIP 流。
- 文件下载通过查询参数传递远端源路径，通过响应头说明文件/目录类型及建议文件名。
- 截图为 `image/png` 流。
- JSON DTO 注册进 `JsonSerializerContext`，保持 Native AOT 兼容。

## 6. 进程执行

### 6.1 DirectProcessExecutor

- 每次 exec 创建独立 `Process`。
- 第一项参数为可执行文件，其余参数加入 `ArgumentList`，不经过 shell 拼接。
- 并行读取 stdout/stderr 并写入同一输出通道。
- 超时或 HTTP 请求取消时终止整个进程树。
- 使用全局 cmd 当前目录作为目标进程的启动目录。

### 6.2 CmdProcess

- Server 生命周期内注册为单例，仅供 shell、cd 和 ls 使用。
- 启动 `cmd.exe` 并重定向 stdin/stdout/stderr。
- 进程意外退出时，在下一次操作前自动创建新进程。
- 实现 `IAsyncDisposable`，Host 停止时释放进程。

## 7. Server Host

- 使用 `WebApplication.CreateSlimBuilder`。
- Kestrel 监听明确选择的端口。
- 注册 `CmdProcess`、`DirectProcessExecutor` 和 AOT JSON 上下文。
- 映射五个 Endpoint。
- 普通 CLI 运行等待应用停止；测试可直接创建、启动和停止真实 Host。

## 8. Windows 服务

- 使用 Windows 服务管理 API 或系统提供的服务控制能力安装、停止和删除服务。
- 服务名固定为 `WinRemoteShell`。
- 安装参数始终记录实际端口，不记录端口 0。
- 服务变更测试不纳入默认自动化测试，避免修改开发机系统状态；服务命令参数生成逻辑单独测试。

## 9. 测试计划

测试项目引用主项目，使用 MSTest 4，测试类型为 `public` 实例类，按行为命名。

### 9.1 无环境变更测试

- `AvailablePortFinderTests`：取得真实可用端口并验证可再次绑定。
- `ServerAddressResolverTests`：验证命令行值优先于环境变量，未传命令行值时读取环境变量。
- CommandHandler 解析测试：使用 DotNetCampus.CommandLine 的真实解析逻辑覆盖所有文档命令。
- 服务启动参数测试：验证自动选择后的端口被写入命令参数。

### 9.2 真实集成测试

每个测试使用独立回环端口、真实 `ServerHost` 和随机临时目录：

- List：验证默认目录、绝对目录、相对目录以及列举后当前目录保持不变。
- Exec：直接执行真实应用并验证流式输出。
- Exec 参数：验证参数逐项传递，不依赖隐式 shell。
- Exec cmd 兼容：显式执行 `cmd.exe /D /C` 并验证内置命令可用。
- Exec 工作目录：通过 `cd` 修改全局当前目录后，验证独立进程从该目录启动。
- Push 文件：真实上传文件并核对内容。
- Push 目录：真实上传嵌套目录并核对内容。
- Pull 文件：真实下载文件并核对内容。
- Pull 目录：真实下载嵌套目录并核对内容。
- Shell：真实 WebSocket 发送命令并读取输出，关闭后验证独立 exec 仍可用。
- Screenshot：真实保存 PNG，并验证 PNG 文件头。

测试不 Mock `HttpClient`、WebSocket、文件系统、ASP.NET Core Host 或 `CmdProcess`。截图测试依赖交互式 Windows 桌面；运行环境不支持时由真实调用报告失败，不用伪造截图替代。

## 10. 实施顺序

1. 建立命令模型和 CommandHandler，替换模板入口。
2. 实现服务器地址解析、可用端口选择及服务启动参数生成。
3. 实现共享 DTO、AOT JSON 上下文和 Server Host。
4. 实现 DirectProcessExecutor、CmdProcess、exec Endpoint 与 ExecClient。
5. 实现 shell WebSocket Endpoint 与 ShellClient。
6. 实现 push 文件/目录上传。
7. 实现 pull 文件/目录下载。
8. 实现截图 Endpoint 与 ScreenshotClient。
9. 实现 Windows 服务安装与卸载。
10. 建立真实测试 Host 与全部 MSTest 用例。
11. 构建解决方案并运行测试，修复与本次实现相关的问题。
12. 验证 Release/Native AOT 构建兼容性。

## 11. 完成标准

- 《Commands.md》中列出的全部命令可用。
- 自动端口会输出，并在服务安装时持久化为实际端口。
- exec 直接启动目标应用；shell 使用独立的长期 cmd，二者仅共享远端当前工作目录。
- 文件和目录可真实 push/pull。
- 截图可真实保存为 PNG。
- 默认测试不修改 Windows 服务状态，其余核心路径均由真实集成测试覆盖。
- 解决方案构建和测试通过，并保持 Native AOT 兼容。
