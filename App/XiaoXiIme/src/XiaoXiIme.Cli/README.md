# XiaoXiIme.Cli

小希输入法命令行工具。职责：安装、卸载、重新安装、状态检查、诊断、Host 管理和系统级冒烟测试入口。

当前已实现安全清单输出命令，不会修改系统目录或注册表：

- `publish-checklist`：输出 Native AOT 发布检查步骤。
- `export-checklist [ime-file]`：输出 IME 二进制导出检查命令。
- `install-checklist [ime-file]`：输出人工安装和注册检查步骤。
- `uninstall-checklist`：输出人工卸载和回滚检查步骤。

真实安装、注册和卸载涉及管理员权限、系统目录和注册表，必须由人工在测试机或虚拟机中执行。
