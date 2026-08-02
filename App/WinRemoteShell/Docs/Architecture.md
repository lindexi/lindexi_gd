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
│   每次运行一个操作       │                               │   维护全局单例 cmd.exe       │
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
│   ├── CmdProcess.cs             # cmd.exe 生命周期管理（单例）
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
│   └── ClientConfig.cs           # --server + WINRS_SERVER 解析
└── Shared/
    └── Models.cs                 # 共享 DTO
```

---

## 核心设计决策

### 1. 单 cmd 进程，无锁

Server 端启动时创建**唯一一个** `cmd.exe` 进程，整个 Server 生命周期内复用。所有 `exec` 和 `shell` 请求都操作这个进程。

- **不加锁**：cmd 的 stdin/stdout 本身就是天然串行通道。两个请求同时到达时，谁先写入 stdin 谁先执行，输出可能交错——这是自然行为，不做防御。
- **cmd 死了自动重生**：检测到进程退出后，自动启动新的 `cmd.exe`。
- **单进程就是最好的"锁"**：它本身一次只能处理一件事，无需额外的并发控制。

### 2. 无 Session 机制

客户端不需要管理 session-id。Server 端不区分客户端，所有请求都路由到同一个 cmd 进程。极简设计，避免了：

- session 创建/销毁的生命周期管理
- session 过期清理
- 客户端 session 存储

代价是：
- 多客户端同时操作会相互干扰（接受，内网单用户场景）

### 3. exec 和 shell 共享 cmd 状态

`exec` 和 `shell` 操作同一个 cmd 进程：

```
exec -- cd C:\Windows    → cmd 工作目录变为 C:\Windows
exec -- dir              → 列出 C:\Windows         ✅
shell                    → 进入交互式，当前在 C:\Windows
shell 中执行 cd ..       → cmd 工作目录变为 C:\
shell 退出
exec -- dir              → 列出 C:\                ✅
```

**shell 退出不杀 cmd**，保留状态供后续 exec 使用。

### 4. 协议选择

| Verb | 协议 | 原因 |
|------|------|------|
| `exec` | HTTP POST + chunked streaming | 单向流式输出。Server 往 cmd stdin 写命令，从 stdout 读到分隔标记，chunked 返回给客户端。客户端一行行打印，用户体感实时 |
| `shell` | WebSocket | 需要双向持续交互。WebSocket 直接桥接终端 ↔ cmd stdin/stdout |
| `push` | HTTP POST（stream upload） | 文件上传 |
| `pull` | HTTP GET（stream download） | 文件下载 |
| `screenshot` | HTTP GET（stream download） | 图片下载 |

#### 为什么 exec 可以用 HTTP 实现实时输出

HTTP chunked transfer encoding 允许服务端在响应过程中逐步推送数据：

```csharp
// Server 端：往 cmd.Stdin 写命令，从 stdout 逐行读到分隔标记
await cmd.Stdin.WriteLineAsync(command);
await cmd.Stdin.WriteLineAsync("__WINRS_END__"); // 分隔标记

Response.ContentType = "text/plain";
while (true)
{
    var line = await cmd.Stdout.ReadLineAsync();
    if (line == "__WINRS_END__") break;
    await Response.WriteAsync(line + "\n");
    await Response.Body.FlushAsync(); // 立即推送给客户端
}
```

客户端用 `HttpClient.GetStreamAsync()` 逐行读取并输出，用户看到的就是实时流。

**HTTP 做不到的是**：在同一个请求中途让客户端再发新输入（交互式 shell），这需要 WebSocket。

### 5. exec 超时处理

```
timeout 到达 →
  1. 往 cmd.Stdin 写入 \x03（Ctrl+C），等待命令响应
  2. 命令未结束 → Kill() cmd 进程 → 重新 Start() 新 cmd
  3. 断开 HTTP 连接，返回超时提示
```

保证无论超时与否，cmd 进程最终都处于可用状态。

### 6. exec 的 exit 命令处理

用户执行 `exec -- exit` 时，cmd 会退出。Server 检测到进程退出后自动重启新的 `cmd.exe`。需要拦截的场景：

- `exit`
- `cmd /c exit`
- 任何导致 cmd 进程退出的命令

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

### cmd 进程管理（CmdProcess.cs）

```csharp
// 全局单例模式
class CmdProcess
{
    Process _process;
    StreamWriter _stdin;
    StreamReader _stdout;
    StreamReader _stderr;

    // 启动 cmd.exe，重定向 stdin/stdout/stderr
    void Start();

    // 执行命令，返回输出（用于 exec HTTP endpoint）
    // 往 stdin 写 command + 分隔标记，从 stdout 读到分隔标记
    async IAsyncEnumerable<string> Execute(string command);

    // 桥接 WebSocket ↔ cmd（用于 shell）
    async Task Bridge(WebSocket ws);

    // 发送 Ctrl+C
    void SendCtrlC();

    // 强杀并重启
    void KillAndRestart();
}
```

### ASP.NET Core Minimal API 端点（参考）

```csharp
app.MapPost("/exec", async (HttpContext ctx) =>
{
    var command = await ctx.Request.Body.ReadAsString();
    ctx.Response.ContentType = "text/plain";
    await foreach (var line in cmd.Execute(command))
    {
        await ctx.Response.WriteAsync(line + "\n");
        await ctx.Response.Body.FlushAsync();
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

- 第一个参数是 verb（`server` / `exec` / `shell` / `push` / `pull` / `screenshot`）
- 后续参数按 `--key value` 解析
- `--` 之后的内容作为原始命令字符串传递

### Client 端连接配置解析

```csharp
// 优先级：命令行 --server > 环境变量 WINRS_SERVER
string? server = args.TryGetOption("--server") 
                 ?? Environment.GetEnvironmentVariable("WINRS_SERVER");
if (server == null) throw new Exception("请指定 --server 或设置 WINRS_SERVER 环境变量");
```
