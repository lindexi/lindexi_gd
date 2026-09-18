# ProcdumpExec

## 用法

```text
ProcdumpExec64.exe [ProcDump 参数] -- <目标程序> [目标程序参数]
ProcdumpExec.exe   [ProcDump 参数] -- <目标程序> [目标程序参数]
```

`--` 用于分隔 ProcDump 参数和目标程序命令行：

- `--` 之前的参数会原样传递给 ProcDump。
- `--` 之后的第一个参数是要启动的程序。
- 后续参数会传递给目标程序。
- ProcdumpExec 会在 ProcDump 参数末尾自动追加目标进程 PID。

### 捕获启动即崩溃的进程

```cmd
ProcdumpExec64.exe -accepteula -ma -e 1 -- Xxxx.exe args1 args2
```

等效的 ProcDump 监控参数为：

```text
procdump64.exe -accepteula -ma -e 1 <目标进程 PID>
```

### 完整测试示例

以下命令使用仓库中的测试程序立即触发访问冲突，并将 dump 写入当前目录：

```cmd
mkdir C:\Temp\ProcdumpExecDumps 2>nul
cd /d C:\Temp\ProcdumpExecDumps
"c:\lindexi\Code\lindexi_gd\lindexi_gd\App\ProcdumpExec\artifacts\publish\ProcdumpExec\release_win-x64\ProcdumpExec64.exe" -accepteula -ma -e 1 -- "c:\lindexi\Code\lindexi_gd\lindexi_gd\App\ProcdumpExec\artifacts\publish\ProcdumpExec\release_win-x64\ProcdumpExec.TestTarget.exe" crash
```

成功时可以看到类似输出：

```text
Exception: C0000005.ACCESS_VIOLATION
Dump 1 initiated: C:\Temp\ProcdumpExecDumps\ProcdumpExec.TestTarget.exe_....dmp
Dump 1 complete
```

检查生成的 dump：

```cmd
dir C:\Temp\ProcdumpExecDumps\*.dmp
```

### 文件布局

x64 版本要求 `ProcdumpExec64.exe` 和 `procdump64.exe` 位于同一目录：

```text
release_win-x64\
    ProcdumpExec64.exe
    procdump64.exe
```

x86 版本要求 `ProcdumpExec.exe` 和 `procdump.exe` 位于同一目录：

```text
release_win-x86\
    ProcdumpExec.exe
    procdump.exe
```

### 退出码和控制台输出

- ProcdumpExec 完整等待 ProcDump 退出。
- ProcDump、目标程序继承当前控制台。
- ProcdumpExec 不重定向或修改 ProcDump 的标准输出和标准错误。
- ProcdumpExec 原样返回 ProcDump 的退出码。
- 参数中缺少 `--` 或 `--` 后没有目标程序时，输出用法并返回 `1`。

## 发布

发布 x64 NativeAOT 版本：

```cmd
dotnet publish Code\ProcdumpExec\ProcdumpExec.csproj -c Release -r win-x64
```

产物：

```text
artifacts\publish\ProcdumpExec\release_win-x64\ProcdumpExec64.exe
```

发布 x86 NativeAOT 版本：

```cmd
dotnet publish Code\ProcdumpExec\ProcdumpExec.csproj -c Release -r win-x86
```

产物：

```text
artifacts\publish\ProcdumpExec\release_win-x86\ProcdumpExec.exe
```

## 实现细节

ProcdumpExec 使用 .NET NativeAOT 发布为 Windows x86/x64 原生可执行文件。

目标程序通过 Win32 `CreateProcessW` 和 `CREATE_SUSPENDED` 创建。创建成功后，目标程序的主线程仍处于挂起状态，此时 ProcdumpExec 会：

1. 根据自身架构选择同目录下的 ProcDump：
   - x64 使用 `procdump64.exe`。
   - x86 使用 `procdump.exe`。
2. 将 `--` 之前的参数原样添加到 ProcDump 参数列表。
3. 将新建目标进程的 PID 添加到参数列表末尾。
4. 启动 ProcDump。
5. 通过 `CheckRemoteDebuggerPresent` 等待 ProcDump 完成调试附加。
6. 调用 `ResumeThread` 恢复目标进程的主线程。
7. 等待 ProcDump 退出并返回其退出码。

等待 ProcDump 完成附加后再恢复目标进程，可以避免目标程序在 ProcDump 完成监听前快速退出或启动即崩溃。

目标程序命令行按照 Windows 命令行转义规则构造，支持空参数、包含空格的参数、双引号以及尾部反斜杠。
