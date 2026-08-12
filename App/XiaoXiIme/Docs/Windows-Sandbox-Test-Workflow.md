# Windows 沙箱中的源码构建与测试工作流

> 当前传统 IMM32 真实安装与自动按键排查进度见 [IMM32 排查交接](./IMM32-Handoff.md)。

本文记录如何将 XiaoXiIme 完整代码仓库推送到 Windows 沙箱，并利用沙箱内已安装的 .NET 10 SDK 执行 `dotnet run`、构建和测试。该工作流适合隔离验证 Windows 相关托管代码，并为后续 Native AOT、输入法安装和 TSF 系统级测试提供基础。

## 已验证结论

已经实际验证以下流程可用：

1. 将整个 XiaoXiIme 仓库推送到 Windows 沙箱。
2. 通过仓库内的可执行入口启动子进程。
3. 在子进程中调用沙箱里的 `dotnet`。
4. 在仓库根目录执行等价于以下命令的操作：

   ```powershell
   dotnet run --project src/XiaoXiIme.Cli/XiaoXiIme.Cli.csproj -- system-test-plan --json
   ```

5. NuGet 还原、CLI 编译和 CLI 启动成功。
6. 命令退出码为 `0`，JSON 输出成功写入并拉回工作区。
7. 在沙箱中执行 `dotnet build XiaoXiIme.slnx -c Release` 也已成功。

因此，普通托管代码修改可以采用“推送完整源码，在沙箱内构建和运行”的模式，不必为每次修改重新生成完整的自包含集成测试负载。

## 为什么需要仓库内的启动入口

沙箱执行接口不是通用终端。它要求：

- 推送一个位于工作区内的目录；
- 指定该目录中的一个可执行文件作为入口；
- 入口文件必须随目录一起上传；
- 不能直接把 `dotnet run ...` 作为任意命令字符串交给沙箱执行。

因此需要一个仓库内的 AppHost，例如未来可正式建立：

```text
tests/XiaoXiIme.SandboxRunner/
```

沙箱首先启动 `XiaoXiIme.SandboxRunner.exe`，再由该程序使用 `ProcessStartInfo` 调用沙箱中的 `dotnet.exe`。

概念流程如下：

```text
工作区完整仓库
    ↓ 推送
Windows 沙箱任务目录
    ↓ 启动仓库内 SandboxRunner.exe
SandboxRunner.exe
    ↓ ProcessStartInfo("dotnet")
dotnet build / dotnet test / dotnet run
    ↓
sandbox-results/
    ↓ 拉回
工作区 artifacts/sandbox-results/
```

## 推荐的启动器行为

正式的沙箱启动器应完成以下工作：

1. 根据自身位置定位仓库根目录，而不是依赖固定的绝对路径。
2. 使用 `ProcessStartInfo("dotnet")` 启动 SDK 命令。
3. 设置 `WorkingDirectory` 为仓库根目录。
4. 使用 `ArgumentList` 逐个传递参数，避免手工拼接和转义命令行。
5. 设置 `UseShellExecute = false`。
6. 重定向标准输出和标准错误。
7. 并行读取标准输出和标准错误，避免输出缓冲区造成进程阻塞。
8. 为命令设置超时；超时后终止整个进程树。
9. 将退出码、标准输出、标准错误和执行信息写入固定结果目录。
10. 将子进程的非零退出码传递给沙箱执行接口，避免失败被报告为成功。

推荐的结果目录结构：

```text
sandbox-results/
├── result.json
├── stdout.txt
├── stderr.txt
├── test-results/
│   └── *.trx
└── logs/
```

`result.json` 至少应记录：

- 执行的操作；
- 项目或解决方案路径；
- 完整参数列表；
- 开始和结束时间；
- 退出码；
- 是否超时；
- 标准输出和标准错误文件的相对路径。

## 运行 `dotnet run`

用于验证 CLI 编译和启动的推荐命令为：

```powershell
dotnet run --project src/XiaoXiIme.Cli/XiaoXiIme.Cli.csproj -- system-test-plan --json
```

启动器应按以下方式构造参数，而不是将整行命令作为一个字符串：

```text
run
--project
<仓库根目录>\src\XiaoXiIme.Cli\XiaoXiIme.Cli.csproj
--
system-test-plan
--json
```

该命令是非破坏性的，适合作为沙箱环境的基础连通性检查。它可以验证：

- .NET 10 SDK 是否可用；
- NuGet 还原是否可用；
- CLI 是否能成功编译；
- AppHost 和托管运行时是否能启动；
- 中文及 JSON 输出是否能写入结果文件。

## 运行构建

沙箱中的完整 Release 构建命令为：

```powershell
dotnet build XiaoXiIme.slnx -c Release
```

已确认该命令能够在沙箱中完成整个解决方案的还原和构建，包括普通测试项目和 Windows 目标项目。

建议启动器将构建输出保存为文件，并保留非零退出码。后续可增加二进制日志：

```powershell
dotnet build XiaoXiIme.slnx -c Release -bl:sandbox-results/logs/build.binlog
```

## 运行测试

不要只根据 `dotnet test` 进程退出码判断测试已经执行。曾观察到对整个 `.slnx` 执行测试时退出码为 `0`，但输出中没有测试发现或测试执行摘要。这可能形成“假绿”。

更稳妥的方式是逐个执行测试项目，并生成 TRX：

```powershell
dotnet test tests/XiaoXiIme.Dictionary.Tests/XiaoXiIme.Dictionary.Tests.csproj --logger "trx;LogFileName=dictionary.trx" --results-directory sandbox-results/test-results
dotnet test tests/XiaoXiIme.ImeCore.Tests/XiaoXiIme.ImeCore.Tests.csproj --logger "trx;LogFileName=ime-core.trx" --results-directory sandbox-results/test-results
dotnet test tests/XiaoXiIme.ImeIpc.Tests/XiaoXiIme.ImeIpc.Tests.csproj --logger "trx;LogFileName=ime-ipc.trx" --results-directory sandbox-results/test-results
dotnet test tests/XiaoXiIme.ImeModule.Tests/XiaoXiIme.ImeModule.Tests.csproj --logger "trx;LogFileName=ime-module.trx" --results-directory sandbox-results/test-results
dotnet test tests/XiaoXiIme.IntegrationTests/XiaoXiIme.IntegrationTests.csproj --logger "trx;LogFileName=integration.trx" --results-directory sandbox-results/test-results
```

Windows TSF 测试可单独运行：

```powershell
dotnet test tests/XiaoXiIme.TsfModule.Tests/XiaoXiIme.TsfModule.Tests.csproj --logger "trx;LogFileName=tsf-module.trx" --results-directory sandbox-results/test-results
```

启动器还应验证：

- 预期的 TRX 文件确实存在；
- TRX 中发现的测试数大于零；
- 没有失败测试；
- 没有因路径或筛选错误导致零测试执行。

## 哪些修改适合直接在沙箱内构建和测试

以下类型通常不需要重新生成完整集成负载：

- `XiaoXiIme.Foundation` 模型修改；
- `XiaoXiIme.Dictionary` 逻辑修改；
- `XiaoXiIme.ImeCore` 状态机修改；
- `XiaoXiIme.ImeIpc` 协议和 IPC 托管逻辑修改；
- 候选窗口状态映射与控制逻辑修改；
- 普通 CLI 命令逻辑修改；
- xUnit 单元测试和非安装型集成测试修改。

推荐流程：

```text
修改代码
→ 本地快速 build/test
→ 推送完整仓库到沙箱
→ 沙箱内 dotnet build/test/run
→ 拉回 TRX、日志和 JSON 结果
```

## Native AOT 和系统级测试的边界

沙箱内存在 .NET 10 SDK，并不意味着所有 Native AOT 发布一定可用。发布下列项目通常还需要 Visual C++ 链接器和对应架构工具链：

- `XiaoXiIme.ImeModule`；
- `XiaoXiIme.TsfModule`；
- x86/x64 原生 ABI 测试宿主。

已尝试在沙箱中运行 `payload-build`：

- 解决方案 Release 构建成功；
- 后续某个 Native AOT 发布步骤失败；
- 流程退出码为 `10`；
- 当前日志不足以确定是缺少 C++ Build Tools、x86 工具链还是具体发布参数问题。

因此，在确认沙箱具有完整 Native AOT 前置条件之前，应采用分层策略：

### 托管测试层

在沙箱内直接执行 `dotnet build`、`dotnet test` 和 `dotnet run`。

### 原生发布层

分别发布 `win-x86` 和 `win-x64` 原生组件，并保留完整发布日志。失败时应拉回日志，而不是只拉取成功后才存在的负载目录。

### 系统安装层

只有验证以下行为时才执行完整负载流程：

- 传统 IME 安装和卸载；
- TSF 注册与 COM 激活；
- Windows 注册表变化；
- 系统输入法切换；
- 真实键盘输入和上屏；
- 测试后的清理与回滚。

这些测试会修改 Windows，必须继续使用显式确认参数，并且只能在可丢弃、可还原的沙箱或专用虚拟机中执行。

## x86/x64 负载为什么仍然需要保留

传统 IME DLL 和 TSF InProc COM DLL 会加载到目标应用进程中，DLL 位数必须与加载进程一致。因此系统级验证仍需同时提供：

```text
win-x86
win-x64
```

沙箱内具备 .NET 10 SDK，只能减少托管代码反复发布的成本，不能消除原生组件的架构要求。

## 输出编码注意事项

实验中发现：

- 沙箱远端实时控制台中的中文可能显示为乱码；
- 重定向后写入并拉回的 UTF-8 文本内容正常。

因此不要依赖远端控制台的中文显示判断结果。推荐：

1. 始终重定向标准输出和标准错误；
2. 将原始输出写入结果文件；
3. 使用 UTF-8 写入 JSON 和文本报告；
4. 以拉回的文件作为诊断依据。

## 结果拉取注意事项

沙箱执行接口可以拉取指定文件或目录。输出路径不存在时，测试命令即使已经失败，结果拉取本身也可能继续报错，掩盖原始失败原因。

启动器应在启动子进程前创建固定结果目录，并且无论成功、失败还是超时，都至少写入：

```text
sandbox-results/result.json
sandbox-results/stdout.txt
sandbox-results/stderr.txt
```

不要只在所有步骤成功后才创建结果目录。

## 安全要求

以下操作不得作为默认沙箱测试自动执行：

- 安装或卸载系统输入法；
- 修改系统级输入法注册表；
- 注册 TSF Profile；
- 需要管理员权限的真实系统集成流程；
- 等待人工键盘输入的交互场景。

这类操作必须满足：

- 使用一次性或可还原环境；
- 使用显式破坏性操作确认参数；
- 设置 `XIAOXIIME_ENVIRONMENT=VirtualMachine` 或项目约定的测试环境值；
- 无论测试成功或失败都执行清理；
- 拉回完整安装、测试和清理报告。

## 后续优化建议

建议正式增加 `XiaoXiIme.SandboxRunner`，提供以下操作：

```text
XiaoXiIme.SandboxRunner.exe build
XiaoXiIme.SandboxRunner.exe test
XiaoXiIme.SandboxRunner.exe run --project <project> -- <arguments>
XiaoXiIme.SandboxRunner.exe publish-native
```

其中：

- `build`：构建解决方案并保存 binlog；
- `test`：逐个测试项目运行并验证非零测试发现数；
- `run`：运行指定项目，用于 CLI 和宿主验证；
- `publish-native`：单独验证 Native AOT 环境，并保存每个 RID 的发布日志。

所有操作都应使用固定结果目录、结构化 JSON 报告、超时控制和准确的退出码。这样可以把当前已经验证成功的实验流程转化为稳定、可重复的项目测试基础设施。
