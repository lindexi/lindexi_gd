# WinRemoteShell 命令参考

## 概述

WinRemoteShell 是一个单 exe 工具，通过命令行参数切换角色——既可以作为 **Server** 在被控机器上监听，也可以作为 **Client** 从控制端发起操作。

所有 Client 命令都支持两种方式指定服务器地址：

1. **命令行参数**：`--server ip:port`
2. **环境变量**：`WINRS_SERVER=ip:port`

优先级：命令行参数 > 环境变量 > 报错提示。

---

## Server 端命令

启动 ASP.NET Core 服务监听，维护一个全局的 `cmd.exe` 进程。

### `server` — 启动监听

```
WinRemoteShell.exe server
WinRemoteShell.exe server --port 12399
```

| 参数 | 必需 | 说明 |
|------|------|------|
| `--port` | 否 | 监听端口。不传则自动选择可用端口，启动后在控制台输出实际端口 |

### `server --install-service` — 注册 Windows 服务

```
WinRemoteShell.exe server --port 12399 --install-service
WinRemoteShell.exe server --install-service
```

将当前 exe 注册为 Windows 服务，开机自启。端口号写入服务启动参数，持久化。

### `server --uninstall-service` — 卸载 Windows 服务

```
WinRemoteShell.exe server --uninstall-service
```

停止并移除已注册的 Windows 服务。

---

## Client 端命令

### `exec` — 远程执行命令

```
WinRemoteShell.exe exec -- <command>
WinRemoteShell.exe exec --server 10.0.0.5:12399 -- <command>
WinRemoteShell.exe exec --server 10.0.0.5:12399 --timeout 30 -- <command>
```

| 参数 | 必需 | 说明 |
|------|------|------|
| `--server` | 否 | 服务器地址（`ip:port`），可从环境变量读取 |
| `--timeout` | 否 | 超时秒数。超时后向远端 cmd 发送 `Ctrl+C`，终止失败则强杀 cmd 并重启 |
| `--` 之后的内容 | 是 | 要执行的命令，原样传递给远端 `cmd.exe` |

**行为**：

- 通过 HTTP chunked streaming 实时返回 stdout/stderr
- 每次 `exec` 都在同一个远端 `cmd.exe` 进程中执行，状态保留（如 `cd` 后环境持续生效）
- 客户端 `Ctrl+C` 会断开 HTTP 连接，远端命令可能继续执行（取决于命令自身行为）

**示例**：

```
WinRemoteShell.exe exec -- dir C:\
WinRemoteShell.exe exec -- cd C:\Windows
WinRemoteShell.exe exec -- dir                  # 列出 C:\Windows
WinRemoteShell.exe exec --timeout 10 -- ping -t 127.0.0.1
```

### `shell` — 交互式 Shell

```
WinRemoteShell.exe shell
WinRemoteShell.exe shell --server 10.0.0.5:12399
```

通过 WebSocket 建立双向通道，桥接本地终端与远端 `cmd.exe` 的 stdin/stdout。体验类似 SSH。

**行为**：

- 进入后获得远端 cmd 提示符，可连续输入命令
- 与 `exec` 共享同一个 `cmd.exe` 进程——`shell` 退出后，`exec` 可以继续在相同状态下工作
- 输入 `exit` 退出交互式 Shell（不杀远端 cmd）

### `push` — 上传文件/文件夹

```
WinRemoteShell.exe push --source <本地路径> --target <远端路径>
WinRemoteShell.exe push --server 10.0.0.5:12399 --source "C:\file.txt" --target "D:\remote\"
```

| 参数 | 必需 | 说明 |
|------|------|------|
| `--source` | 是 | 本地文件或文件夹路径 |
| `--target` | 是 | 远端目标路径 |

**行为**：

- 递归上传整个文件夹
- 不支持通配符，每次操作一个文件或文件夹

### `pull` — 下载文件/文件夹

```
WinRemoteShell.exe pull --source <远端路径> --output <本地路径>
WinRemoteShell.exe pull --server 10.0.0.5:12399 --source "D:\remote\file.txt" --output "C:\local\"
```

| 参数 | 必需 | 说明 |
|------|------|------|
| `--source` | 是 | 远端文件或文件夹路径 |
| `--output` | 是 | 本地保存路径 |

**行为**：

- 递归下载整个文件夹
- 不支持通配符，每次操作一个文件或文件夹

### `screenshot` — 远端屏幕截图

```
WinRemoteShell.exe screenshot
WinRemoteShell.exe screenshot --output "C:\screenshots\capture.png"
WinRemoteShell.exe screenshot --server 10.0.0.5:12399 --output "C:\screenshots\"
```

| 参数 | 必需 | 说明 |
|------|------|------|
| `--output` | 否 | 保存路径。可以是完整文件名（含 `.png`），也可以只是目录。不传则自动生成文件名保存到当前目录 |

**行为**：

- Server 端截取当前屏幕，返回 PNG 图片
- 文件名自动生成格式：`screenshot_YYYYMMDD_HHmmss.png`

---

## 环境变量

| 变量 | 说明 |
|------|------|
| `WINRS_SERVER` | 服务器地址，格式 `ip:port`。所有 Client 命令在未指定 `--server` 时读取此变量 |

**示例**：

```
set WINRS_SERVER=192.168.1.100:12399
WinRemoteShell.exe exec -- whoami        # 自动连接 192.168.1.100:12399
WinRemoteShell.exe exec --server 10.0.0.5:9999 -- whoami  # 命令行覆盖环境变量
```
