# WinRemoteShell 命令参考

## 概述

WinRemoteShell 是一个单 exe 工具，通过命令行参数切换角色——既可以作为 **Server** 在被控机器上监听，也可以作为 **Client** 从控制端发起操作。

所有 Client 命令都支持两种方式指定服务器地址：

1. **命令行参数**：`--server ip:port`
2. **环境变量**：`WINRS_SERVER=ip:port`

优先级：命令行参数 > 环境变量 > 报错提示。

---

## Server 端命令

启动 ASP.NET Core 服务监听。服务维护一个供 `shell`、`cd` 和 `ls` 使用的全局 `cmd.exe`；每次 `exec` 则创建独立目标进程。

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

### `ls` — 列举远端目录

```
WinRemoteShell.exe ls
WinRemoteShell.exe ls C:\Windows
WinRemoteShell.exe ls ..\logs
WinRemoteShell.exe ls --server 10.0.0.5:12399 "C:\Program Files"
```

| 参数 | 必需 | 说明 |
|------|------|------|
| `--server` | 否 | 服务器地址（`ip:port`），可从环境变量读取 |
| `path` | 否 | 要列举的远端目录；不传时使用远端当前工作目录 |

**行为**：

- 绝对路径直接使用
- 相对路径基于远端当前工作目录解析
- 仅列举指定目录，不改变远端当前工作目录
- 输出目录中的文件和子目录名称
- 服务端返回包含完整路径、类型、大小和时间信息的结构化数据，由客户端负责格式化输出

### `exec` — 远程执行命令

```
WinRemoteShell.exe exec -- <command>
WinRemoteShell.exe exec --server 10.0.0.5:12399 -- <command>
WinRemoteShell.exe exec --server 10.0.0.5:12399 --timeout 30 -- <command>
```

| 参数 | 必需 | 说明 |
|------|------|------|
| `--server` | 否 | 服务器地址（`ip:port`），可从环境变量读取 |
| `--timeout` | 否 | 超时秒数。超时后终止本次启动的进程及其进程树 |
| `--` 之后的内容 | 是 | 第一个参数是要直接启动的应用，其余参数逐项传递给该应用 |

**行为**：

- Server 不再隐式使用 `cmd.exe`，而是直接启动第一个参数指定的可执行文件
- 参数通过 `ProcessStartInfo.ArgumentList` 逐项传递，不会先拼接为命令行，也不会进行 shell 展开
- stdout 和 stderr 通过 HTTP chunked streaming 实时返回
- 每次 `exec` 使用独立进程，不保留上一次执行的环境变量或进程状态
- 工作目录取自远端 `cd`/`shell` 维护的当前目录
- 超时或客户端取消请求时，终止本次进程及其进程树，不影响全局交互式 `cmd.exe`
- `dir`、`echo`、管道、重定向和环境变量展开等 cmd 内置语法必须显式使用 `cmd.exe /D /C`

**示例**：

```
WinRemoteShell.exe exec -- whoami.exe
WinRemoteShell.exe exec -- ping.exe -n 4 127.0.0.1
WinRemoteShell.exe exec -- dotnet.exe --info
WinRemoteShell.exe exec --timeout 10 -- ping.exe -t 127.0.0.1
WinRemoteShell.exe exec -- cmd.exe /D /C dir C:\
WinRemoteShell.exe exec -- cmd.exe /D /C "echo %TEMP% & dir"
```

### `shell` — 交互式 Shell

```
WinRemoteShell.exe shell
WinRemoteShell.exe shell --server 10.0.0.5:12399
```

通过 WebSocket 建立双向通道，桥接本地终端与远端 `cmd.exe` 的 stdin/stdout。体验类似 SSH。

**行为**：

- 进入后获得远端 cmd 提示符，可连续输入命令
- `shell` 使用长期运行的全局 `cmd.exe`；`exec` 使用独立进程，不与 shell 共享 stdin/stdout 或环境状态
- shell 中改变的工作目录会作为后续 `exec` 独立进程的启动目录
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

### `ps` — 列举远端进程

```
WinRemoteShell.exe ps
WinRemoteShell.exe ps --details
WinRemoteShell.exe ps --json
WinRemoteShell.exe ps --server 10.0.0.5:12399 --details --json
```

| 参数 | 必需 | 说明 |
|------|------|------|
| `--details` | 否 | 请求更多进程信息，包括可执行文件路径、启动时间、工作集、专用内存和线程数 |
| `--json` | 否 | 直接输出服务端返回的结构化 JSON；默认由客户端格式化为表格 |

**行为**：

- Server 端始终返回结构化 JSON，而不是预格式化文本
- 默认响应只填充进程号 `id` 和进程名 `name`
- `--details` 对无权限读取或平台不支持的字段返回 `null`，不会导致整个列表失败
- 结果按进程名、进程号排序

### `kill` — 终止远端进程

```
WinRemoteShell.exe kill --pid 1234
WinRemoteShell.exe kill --pid 1234 --tree
WinRemoteShell.exe kill --name notepad
WinRemoteShell.exe kill --name notepad.exe --json
```

| 参数 | 必需 | 说明 |
|------|------|------|
| `--pid` | 二选一 | 按进程号终止单个进程，必须大于 0 |
| `--name` | 二选一 | 按进程名终止所有匹配进程；匹配不区分大小写，`.exe` 后缀可省略 |
| `--tree` | 否 | 同时终止目标进程的整个进程树 |
| `--json` | 否 | 输出每个匹配进程的结构化执行结果 |

`--pid` 和 `--name` 必须且只能指定一个。按名称匹配时会尝试终止全部匹配项，每项分别返回 `id`、`name`、`killed` 和 `error`。没有匹配进程时返回空列表，不视为错误；部分终止失败时命令返回码为 `1`，参数错误返回码为 `2`。

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
