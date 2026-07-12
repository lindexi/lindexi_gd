# XiaoXiIme.Cli

小希输入法命令行工具。职责：安装、卸载、重新安装、状态检查、诊断、Host 管理和系统级冒烟测试入口。

本项目使用 `DotNetCampus.CommandLine` 进行命令行解析。命令建模、处理器注册、内置帮助、异步入口、Native AOT 注意事项和踩坑记录见 [DotNetCampus.CommandLine 使用与踩坑记录](./DotNetCampus.CommandLine.md)。

当前支持以下检查和安装命令：

- `publish-checklist`：输出 Native AOT 发布检查步骤。
- `export-checklist [ime-file]`：输出 IME 二进制导出检查命令。
- `install-checklist [ime-file]`：输出人工安装和注册检查步骤。
- `install <ime-file>`：调用 Windows API 安装输入法，可由最终安装包直接调用。
- `uninstall-checklist`：输出人工卸载和回滚检查步骤。

真实安装和注册涉及管理员权限及系统注册表。执行 `install` 即表示调用方要求安装，CLI 会在基本参数检查通过后调用 `ImmInstallIME`。

## 运行命令

在 `App\XiaoXiIme` 目录下，可以通过 `dotnet run` 执行命令：

```powershell
dotnet run --project .\src\XiaoXiIme.Cli\XiaoXiIme.Cli.csproj -- <command> [arguments]
```

如果已经发布或生成了 `XiaoXiIme.Cli.exe`，也可以直接运行：

```powershell
.\XiaoXiIme.Cli.exe <command> [arguments]
```

使用 `--help` 查看当前支持的命令：

```powershell
dotnet run --project .\src\XiaoXiIme.Cli\XiaoXiIme.Cli.csproj -- --help
```

## 安装输入法

### 1. 发布并检查 IME 文件

先输出 Native AOT 发布检查步骤：

```powershell
dotnet run --project .\src\XiaoXiIme.Cli\XiaoXiIme.Cli.csproj -- publish-checklist
```

发布 IME 模块后，检查二进制是否导出了必需的传统 IME 入口点：

```powershell
dotnet run --project .\src\XiaoXiIme.Cli\XiaoXiIme.Cli.csproj -- export-checklist ".\src\XiaoXiIme.ImeModule\bin\Release\net10.0\win-x64\publish\XiaoXiIme.ImeModule.dll"
```

必须确认以下入口点均已导出，才能继续注册：

- `ImeInquire`
- `ImeProcessKey`
- `ImeToAsciiEx`
- `ImeSelect`
- `NotifyIME`

### 2. 执行安装

将位于稳定安装目录中的 `.ime` 文件完整路径传给 `install`。当前进程需要具有管理员权限：

```powershell
dotnet run --project .\src\XiaoXiIme.Cli\XiaoXiIme.Cli.csproj -- install "C:\Program Files\XiaoXiIme\XiaoXiIme.ime"
```

最终安装包可以直接调用已发布的 CLI：

```powershell
.\XiaoXiIme.Cli.exe install "C:\Program Files\XiaoXiIme\XiaoXiIme.ime"
```

CLI 会在调用 `ImmInstallIME` 前检查：

1. 文件存在且扩展名为 `.ime`。
2. 当前进程具有管理员权限。

成功后命令会输出 `ImmInstallIME` 返回的 HKL / 布局 ID。安装包应记录命令退出码和输出，以便诊断安装失败问题。

安装后继续人工验证：

1. 检查 `HKLM\SYSTEM\CurrentControlSet\Control\Keyboard Layouts\<layout id>` 中的输入法元数据。
2. 注销并重新登录；如果布局仍未出现，重启 Windows。
3. 从 Windows 输入法切换界面选择小希输入法并执行冒烟测试。
4. 在测试机验证时，测试完成后按卸载清单回滚并恢复虚拟机快照。

仍可使用 `install-checklist [ime-file]` 输出不修改系统的检查清单。

> 建议仅在虚拟机或专用测试机中首次安装，并在修改注册表前创建备份或系统还原点。

## 集成测试约束

- CLI 不判断当前机器是否为开发机、测试机或最终用户机器。
- 后续执行真实安装的集成测试必须部署到专用测试机或可还原虚拟机。
- 不应让普通开发机上的默认测试集合自动调用 `install`。
- 安装包调用 CLI 时，应等待进程结束并检查退出码；退出码为 `0` 表示安装 API 调用成功。

## 卸载输入法

输出人工卸载和回滚清单：

```powershell
dotnet run --project .\src\XiaoXiIme.Cli\XiaoXiIme.Cli.csproj -- uninstall-checklist
```

该命令不会删除文件或注册表项。请使用管理员权限，并根据安装时记录的布局 ID 依次完成：

1. 在所有用户会话中切换到其他输入法，避免小希输入法仍被进程占用。
2. 尽可能使用 `UnloadKeyboardLayout` 卸载安装时记录的 HKL。
3. 删除当前用户 `Keyboard Layout\Preload` 中引用小希输入法布局 ID 的项目。
4. 如果安装时写入过默认用户配置，删除 `HKEY_USERS\.DEFAULT\Keyboard Layout\Preload` 中对应的项目。
5. 删除 `Control Panel\International\User Profile` 中引用该布局 ID 的项目（如果存在）。
6. 备份后删除 `HKLM\SYSTEM\CurrentControlSet\Control\Keyboard Layouts\<layout id>` 注册项。
7. 确认布局已卸载且文件未被进程占用后，再删除安装目录中的 `.ime` 文件。
8. 如果输入法仍显示在系统列表中，重启相关应用；必要时重启 Windows。

卸载前应保存布局注册项备份和安装时记录的布局 ID，以便卸载失败时回滚。不要删除无法确认归属的键盘布局或注册表项。
