# WinRemoteShell 架构与设计决策

## 项目概述

WinRemoteShell 是一个 Windows 远程管理工具。在被控机器上启动 Server 端，从控制端通过 CLI 远程执行命令、传输文件、获取屏幕截图。最终输出为**单个 AOT 编译的 exe 文件**，约 10MB。

---

## 命名决策

### 工具名：WinRemoteShell

| 候选 | 结论 | 原因 |
|------|------|------|
| WinShell | ❌ | 已被[知名工具](https://github.com/hempsec/WinShell)占用，存在命名冲突和语义歧义 |
| WinRemoteShell | ✅ | 语义清晰，无冲突 |
| WinRShell | 备选 | 如果嫌长可以考虑，类似 WinRAR 风格 |

### 被控端：Server（非 Slave）

- ASP.NET Core 本身就是 web server，名实相符
- 避免 slave 术语的潜在争议
- `--server ip:port` 读起来比 `--slave ip:port` 自然

### 上传/下载 Verb：push / pull

| 候选 | 结论 | 原因 |
|------|------|------|
| upload/download | ❌ | 偏长 |
| put/get | 可用 | FTP 风格，也不错 |
| **push/pull** | ✅ | Git 风格，短（4 字符），语义清晰：push = 本地→远端，pull = 远端→本地 |

---

## 架构总览

```
┌─────────────────────┐         HTTP / WebSocket        ┌──────────────────────────┐
│   Client 端           │ ◄───────────────────────────► │   Server 端                │
│   短进程 CLI           │                               │   ASP.NET Core 自托管       │
│   每次运行一个操作       │                               │   exec 创建独立目标进程       │
│   无状态               │                               │   支持注册为 Windows 服务     │
└─────────────────────┘                               └──────────────────────────┘
```

**项目结构**：单个 csproj（`Microsoft.NET.Sdk.Web` + AOT），一个 exe。通过 subcommand 分发角色。

```
WinRemoteShell/
├── WinRemoteShell.csproj         # 唯一项目，AOT 发布
├── Program.cs                    # 入口，verb 路由
├── Server/
│   ├── ServerHost.cs             # ASP.NET Core Minimal API 启动
│   ├── CmdProcess.cs             # shell/cd/ls 的 cmd.exe 生命周期管理
│   ├── DirectProcessExecutor.cs  # exec 独立进程执行与流式输出
│   ├── Endpoints/
│   │   ├── ExecEndpoint.cs       # POST /exec
│   │   ├── ShellEndpoint.cs      # WebSocket /shell
│   │   ├── PushEndpoint.cs       # POST /push
│   │   ├── PullEndpoint.cs       # GET /pull
│   │   └── ScreenshotEndpoint.cs # GET /screenshot
│   └── ServiceInstaller.cs       # Windows 服务注册/卸载
├── Client/
│   ├── ExecClient.cs             # exec 逻辑
│   ├── ShellClient.cs            # WebSocket 交互式客户端
│   ├── PushClient.cs             # 文件上传
│   ├── PullClient.cs             # 文件下载
│   ├── ScreenshotClient.cs       # 截图下载
│   ├── ProcessClient.cs          # 结构化进程列表
│   ├── KillClient.cs             # 终止进程
│   └── ClientConfig.cs           # --server + WINRS_SERVER 解析
└── Shared/
    └── Models.cs                 # 共享 DTO
```

---

## 核心设计决策

### 1. exec 直接执行目标应用

每次 `exec` 请求都创建独立 `Process`。参数数组的第一项作为 `ProcessStartInfo.FileName`，其余项加入 `ArgumentList`，不会经过 `cmd.exe` 拼接、解析或展开。

- stdout 和 stderr 并行读取并合并到同一 HTTP 流
- 不使用结束标记，不受目标程序输出内容影响
- 不共享 stdin/stdout、环境变量变更或子进程状态
- 超时或请求取消时终止本次目标进程树
- 需要 cmd 内置命令、管道或重定向时，由用户显式执行 `cmd.exe /D /C ...`

### 2. shell 保留单例 cmd

Server 仍维护唯一一个长期运行的 `cmd.exe`，但只供 `shell`、`cd` 和 `ls` 使用。

- cmd 退出后按需自动重启
- shell 关闭 WebSocket 不杀 cmd
- `cd` 和 shell 修改的当前目录，会成为后续 exec 独立进程的启动目录
- exec 不向该 cmd 写入命令，因此不会与交互式输出互相串扰

### 3. 无 Session 机制

客户端不需要管理 session-id。所有客户端共享 shell 的当前目录；每个 exec 请求本身则拥有独立进程生命周期。

### 4. 协议选择

| Verb | 协议 | 原因 |
|------|------|------|
| `ls` | HTTP GET + JSON | 可选 `path` 查询参数；相对路径基于远端当前目录解析，返回结构化目录列表 |
| `exec` | HTTP POST + chunked streaming | 直接创建目标进程，并行读取 stdout/stderr 后实时返回；请求取消时终止该进程树 |
| `shell` | WebSocket | 需要双向持续交互。WebSocket 直接桥接终端 ↔ cmd stdin/stdout |
| `push` | HTTP POST（stream upload） | 文件上传 |
| `pull` | HTTP GET（stream download） | 文件下载 |
| `screenshot` | HTTP GET（stream download） | 图片下载 |
| `ps` | HTTP GET + JSON | 返回结构化进程列表，由客户端负责表格或 JSON 输出 |
| `kill` | HTTP POST + JSON | 按 PID 或进程名提交目标，并逐项返回终止结果 |

#### 为什么 exec 可以用 HTTP 实现实时输出

HTTP chunked transfer encoding 允许服务端在响应过程中逐步推送数据。Server 同时读取目标进程的 stdout 和 stderr，将完整行写入响应并立即刷新：

```csharp
var process = Process.Start(startInfo);
var stdoutTask = CopyLinesAsync(process.StandardOutput, response);
var stderrTask = CopyLinesAsync(process.StandardError, response);
await process.WaitForExitAsync(cancellationToken);
await Task.WhenAll(stdoutTask, stderrTask);
```

客户端逐行读取响应流并输出。HTTP 不提供后续 stdin 交互，因此交互式终端仍由 WebSocket `shell` 提供。

### 5. exec 超时与取消

```
timeout 或客户端断开 →
  1. 取消本次读取和等待操作
  2. Kill(entireProcessTree: true) 终止目标进程树
  3. 释放独立 Process，不影响 shell 的全局 cmd.exe
```

直接终止独立进程避免向控制台发送 `Ctrl+C` 时被 winexe 等程序截获。

### 6. cmd 兼容方式

`exec` 不识别 `dir`、`echo`、`&&`、管道或重定向等 shell 语法。需要这些能力时显式调用：

```
exec -- cmd.exe /D /C "dir C:\ & echo %TEMP%"
```

### 7. 端口管理

**自动选择端口**：

```
WinRemoteShell.exe server
→ [INFO] Server started on port 52341
```

通过 `IPEndPoint` 指定端口 0，OS 自动分配可用端口。

**服务注册持久化**：

```
WinRemoteShell.exe server --port 12399 --install-service
```

端口号直接写入 Windows 服务启动参数，不依赖外部配置文件：

```csharp
sc.Create("WinRemoteShell", $"\"{exePath}\" server --port {port}");
```

改端口需要重装服务（罕见操作，可接受）。

### 8. 文件传输

- 递归传输整个文件夹
- 不支持通配符
- 不压缩（内网场景，带宽充足）
- 不支持断点续传（内网稳定环境）

### 9. 安全性

内网场景，不考虑：
- HTTPS / TLS
- 认证 token
- 用户密码

用户如需安全传输，自行配置 HTTPS：`--server https://ip:port`。

### 10. 发布方式

使用 .NET Native AOT 编译，生成单个自包含 exe 文件：

```
dotnet publish -c Release -r win-x64
```

- 无需目标机器安装 .NET Runtime
- 体积约 10MB
- 启动速度快
- 使用 `Microsoft.NET.Sdk.Web` + Minimal API（AOT 兼容）

---

## 关键技术实现要点

### 进程执行器（DirectProcessExecutor.cs）

```csharp
class DirectProcessExecutor
{
    async IAsyncEnumerable<string> ExecuteAsync(
        IReadOnlyList<string> arguments,
        string workingDirectory,
        CancellationToken cancellationToken);
}
```

执行器使用 `ProcessStartInfo.ArgumentList` 直接启动目标应用，并发读取 stdout/stderr。取消令牌触发时终止整个目标进程树。

### cmd 进程管理（CmdProcess.cs）

`CmdProcess` 是全局单例，只负责交互式 shell 以及共享工作目录查询和修改，不参与 `/exec` 的目标应用执行。

### ASP.NET Core Minimal API 端点（参考）

```csharp
app.MapPost("/exec", async (ExecRequest request, HttpContext context) =>
{
    var workingDirectory = await cmd.GetWorkingDirectoryAsync(context.RequestAborted);
    await foreach (var line in executor.ExecuteAsync(
        request.Arguments,
        workingDirectory,
        context.RequestAborted))
    {
        await context.Response.WriteAsync(line + "\n");
        await context.Response.Body.FlushAsync();
    }
});

app.Map("/shell", async (HttpContext ctx) =>
{
    if (ctx.WebSockets.IsWebSocketRequest)
    {
        var ws = await ctx.WebSockets.AcceptWebSocketAsync();
        await cmd.Bridge(ws);
    }
});
```

### 命令行解析策略

不使用 `System.CommandLine`（AOT 兼容性差），采用手动轻量解析：

- 第一个参数是 verb（如 `server`、`exec`、`shell`、`push`、`pull`、`screenshot`、`ps`、`kill`）
- 后续参数按命令模型解析
- `exec` 的 `--` 之后内容保留为参数数组；第一项是应用，其余项是应用参数

### Client 端连接配置解析

```csharp
// 优先级：命令行 --server > 环境变量 WINRS_SERVER
string? server = args.TryGetOption("--server") 
                 ?? Environment.GetEnvironmentVariable("WINRS_SERVER");
if (server == null) throw new Exception("请指定 --server 或设置 WINRS_SERVER 环境变量");
```
