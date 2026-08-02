# WinRemoteShell

Windows 远程管理工具。在被控机器上启动 Server 端监听，从控制端通过 CLI 远程执行命令、传输文件、获取屏幕截图。

- **单 exe 发布**：.NET Native AOT 编译，约 10MB
- **无状态客户端**：每次命令一个独立进程，无需 session 管理
- **cmd 状态持久**：Server 端维护全局 cmd 进程，`exec` 之间保留工作目录和环境变量

## 快速开始

### Server 端

```powershell
# 启动监听（自动选择端口）
WinRemoteShell.exe server

# 指定端口
WinRemoteShell.exe server --port 12399

# 注册为 Windows 服务，开机自启
WinRemoteShell.exe server --port 12399 --install-service
```

### Client 端

```powershell
# 设置环境变量（可选）
set WINRS_SERVER=192.168.1.100:12399

# 远程执行命令
WinRemoteShell.exe exec -- whoami
WinRemoteShell.exe exec -- dir C:\
WinRemoteShell.exe exec --timeout 30 -- ping -n 10 127.0.0.1

# 交互式 Shell
WinRemoteShell.exe shell

# 文件传输
WinRemoteShell.exe push --source "C:\local\file.txt" --target "D:\remote\"
WinRemoteShell.exe pull --source "D:\remote\file.txt" --output "C:\local\"

# 远端截图
WinRemoteShell.exe screenshot --output "C:\screenshots\"
```

## 文档

- [命令参考](Docs/Commands.md) — 所有命令、参数、行为说明
- [架构与设计决策](Docs/Architecture.md) — 实现方案、决策原因、架构框架

## 构建

```powershell
dotnet publish -c Release -r win-x64
```

需要 .NET SDK（支持 Native AOT）。
