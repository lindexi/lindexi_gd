# XiaoXiIme 纯净 VM 安装排障工作模式

## 目的

本文档用于跨多轮、跨新对话持续排查 XiaoXiIme 在纯净 Windows VM 中的安装问题。开发环境负责构建包含诊断能力的自包含 payload；人类操作者只负责将 payload 复制到 VM、运行指定命令，并原样回传控制台输出和报告。后续分析与代码迭代必须以 VM 返回的证据为准。

## 环境约束

VM 被视为纯净最终用户环境，不假设存在以下组件：

- Visual Studio 或 Visual Studio Build Tools；
- .NET SDK 或系统级 .NET Runtime；
- dumpbin、Dependencies、Process Monitor 等开发或诊断工具；
- NuGet、Git 或源代码；
- 额外 PowerShell 模块。

`payload-build` 发布的 CLI 必须自包含。安装自检必须由 CLI 和 Windows 自带 API 完成，不能要求操作者临时安装工具。

## 固定协作流程

1. 在开发机的 `App\XiaoXiIme` 目录构建新的 payload：

   ```powershell
   dotnet run --project .\src\XiaoXiIme.Cli\XiaoXiIme.Cli.csproj -- payload-build --output .\artifacts\integration-payload
   ```

2. 将整个 `integration-payload` 目录复制到具有快照、可随时还原的 Windows VM。
3. 在 VM 中打开管理员 PowerShell，进入 payload 根目录并运行：

   ```powershell
   $env:XIAOXIIME_ENVIRONMENT = "VirtualMachine"
   .\app\cli\XiaoXiIme.Cli.exe integration-run . --confirm I-UNDERSTAND-THIS-MODIFIES-WINDOWS --report .\results\integration.json
   ```

4. 操作者原样回传以下两部分，不自行筛选或改写：
   - 从命令开始到结束的全部控制台 JSON 行；
   - `results\integration.json` 的完整内容。
5. 开发侧根据结构化证据判断下一步，只添加能区分剩余假设的最小诊断或根因修复。
6. 生成新 payload，重复以上流程。每轮都应注明 payload 的生成时间或 Git 提交，以避免分析旧版本输出。

## 当前诊断阶段

`integration-run` 在调用 `ImmInstallIMEW` 前输出 `diagnostics-pre-install`，其中包含：

- OS 描述、OS 架构、CLI 进程架构和关键目录；
- 源 IME 的绝对路径、文件长度、SHA-256、属性、版本和 Mark of the Web；
- PE Machine、Magic、Subsystem、DLL 标志和导入表；
- 导入模块是否为 API-set，以及普通系统 DLL 能否从系统目录解析；
- Windows `GetBinaryType` 结果；
- 使用 `DONT_RESOLVE_DLL_REFERENCES` 的安全映像映射结果；
- `System32\XiaoXiIme.ime`、System32 临时写入能力和匹配键盘布局注册项的安装前状态；
- 基于以上数据生成的 `Findings`。

安装前还会运行 `native-ime-load-probe`。该阶段在独立的 CLI 子进程中使用正常 Windows 加载器加载项目自身构建的 IME，并用 `GetProcAddress` 验证全部 11 个传统 IME 导出。独立进程可以安全记录 DLL 初始化失败、加载器错误、异常退出和超时，而不破坏主 `integration-run`。

如果 `ImmInstallIMEW` 返回失败，还会输出 `diagnostics-post-install-failure`，以便比较调用前后的系统目录和注册表状态。`install-x64` 阶段会记录传给 API 的绝对路径和原始 Win32 错误码。

## 当前已知问题

首次 VM 输出为：

```text
ImmInstallIME failed with Win32 error 2: 系统找不到指定的文件。
```

已知该错误发生前 payload manifest、文件长度、SHA-256、IME VERSIONINFO 和要求的导出函数均已通过校验。因此不能仅凭错误 2 断言源 `.ime` 文件不存在。当前待区分的主要假设是：

1. PE 架构或映像格式与 x64 CLI/Windows 不匹配；
2. IME 导入的原生系统模块在 VM 中无法解析；
3. Windows 能读取文件但无法将其映射为原生映像；
4. `ImmInstallIMEW` 内部复制到系统目录或创建布局时失败；
5. VM 的 Windows SKU、组件或安全策略不支持当前传统 IMM32 IME 安装路径；
6. 系统目录或键盘布局注册表存在调用前后不一致的残留状态。

### 2026-07-26 第一轮增强诊断结果

VM 返回的数据进一步确认：

- 源文件存在、可读且 SHA-256 稳定，没有 Mark of the Web；
- PE 为 `Amd64`、`PE32Plus`、`WindowsGui` DLL；
- CLI 进程和操作系统均为 x64；
- 10 个导入模块均可分类或从 System32 解析；
- Windows 能使用 `DONT_RESOLVE_DLL_REFERENCES` 映射该映像；
- 调用前后 System32 均没有 `XiaoXiIme.ime`，也没有匹配布局注册项。

该轮唯一 finding 是 `GetBinaryType` 返回 error 193。此 API 面向可执行程序，不能用它对 DLL/IME 的失败结果判定镜像无效，因此后续版本不再将该结果视为 finding，但仍保留原始数据供参考。

下一轮判断方式：

- 若 `native-ime-load-probe` 失败，优先分析其 stdout 中的 `LoadErrorCode`，问题位于真实加载、NativeAOT 初始化或导出解析；
- 若原生加载成功但 System32 写入探测失败，问题位于权限、安全策略或系统目录访问；
- 若原生加载和 System32 写入均成功，而 `ImmInstallIMEW` 仍返回 2，则证据将集中指向 API 的复制/注册调用语义，而非当前 IME 二进制本身。

### 2026-07-26 第二轮增强诊断结果

VM 返回结果确认：

- `native-ime-load-probe` 使用正常 `LoadLibraryExW` 成功加载 IME；
- 11 个传统 IME 必需导出全部能由 `GetProcAddress` 解析；
- System32 随机临时文件创建和删除成功；
- 常规 `ImmInstallIMEW` 仍返回错误 2；
- 调用后 System32 没有目标文件，键盘布局注册表也没有变化。

这些证据排除了 NativeAOT 初始化失败、真实依赖缺失、导出缺失和 System32 权限问题。剩余主要变量是源文件所在目录与传统 IME 文件名兼容性。

下一版在常规安装失败后输出 `imm-install-variant-probe`，测试以下可回滚矩阵：

1. `payload-short-name`：保持 payload 目录，仅把私有副本命名为 `XIAOXI.IME`；
2. `system32-original-name`：复制到 System32，保持 `XiaoXiIme.ime`；
3. `system32-short-name`：复制到 System32，并使用 `XIAOXI.IME`。

结果解释：

- 只有两个短名变体成功：文件名兼容性是主要约束；
- 只有两个 System32 变体成功：文件必须先进入系统目录；
- 只有 `system32-short-name` 成功：目录和短文件名都是前置条件；
- 三个都成功而原路径失败：原始 payload 路径或路径长度存在约束；
- 三个都失败：停止继续猜路径，转向签名、Windows SKU/语言组件或 ImmInstallIME 的其他传统验证规则。

每个变体均不覆盖已有文件；成功注册后立即 `ImmUninstallIME`，并只删除本轮创建的私有副本。任何卸载或清理失败都会进入报告。

### 2026-07-26 第三轮变体诊断结果与根因

VM 返回的变体矩阵为：

- `payload-short-name`：失败，Win32 error 2；
- `system32-original-name`：成功注册；
- `system32-short-name`：成功注册。

因此文件名长短不是约束，决定性条件是：**调用 `ImmInstallIMEW` 前，传统 IME 文件必须已经位于 System32。** 原正式流程把 payload 中的绝对路径直接传给 API，并错误假设 API 会负责复制文件，这是本次安装失败的根因。

该轮也暴露了探测清理缺陷：`imm32.dll` 不导出代码中错误声明的 `ImmUninstallIME`，两个成功探测布局未能立即卸载，而探测副本已被删除，可能在 VM 留下 `XiaoXi IME Probe [...]` 的悬空布局项。后续版本已做以下修复：

- 正式安装先复制到 System32，校验长度和 SHA-256，再调用 `ImmInstallIMEW`；
- 不覆盖内容不同的既有 System32 同名文件；
- 注册失败时只回滚本次创建的副本；
- 不再调用不存在的 `ImmUninstallIME`；
- 通过返回的布局 ID、`Layout Text` 和 `Ime File` 精确验证并删除探测布局；
- `uninstall-old` 会识别并清理历史 `XiaoXi IME Probe [...]` 布局、preload 引用，以及不再被布局引用的 `XiaoXiIme.ime`/`XIAOXI.IME` 文件。

下一版首次在同一 VM 运行时，`uninstall-old` 应先报告移除了第三轮残留的布局 ID，随后正式 `install-x64` 应从 `C:\Windows\System32\XiaoXiIme.ime` 注册成功。若 System32 文件因进程占用无法删除，日志会明确保留该残留，不应人工静默忽略。

## 下一轮必须回传的信息

下一次运行至少应包含以下 stage：

- `uninstall-old`；
- `diagnostics-pre-install`；
- `native-ime-load-probe`；
- `install-x64`；
- 若安装失败，`diagnostics-post-install-failure`；
- 若安装失败，`imm-install-variant-probe`；
- `report`。

如果控制台粘贴受长度限制，应优先回传完整的 `results\integration.json`，不得只回传错误消息摘要。

## 诊断设计原则

- 不执行来源不明 DLL 的初始化代码；加载探测只使用安全标志。
- API-set 名称不按磁盘缺失文件处理，避免产生错误结论。
- 不通过跳过校验、吞掉错误或强行写注册表来让流程表面通过。
- 每个新增诊断都应回答一个明确问题，并进入结构化 JSON 报告。
- 所有可能修改系统的操作继续要求管理员权限和一次性 VM 确认令牌。
- 在根因确认前，不把 VM 特例硬编码为产品安装逻辑。

## 新对话接续提示

在新的对话中，先阅读本文档和用户回传的最新 `integration.json`，再检查当前 `ImeInstallationDiagnostics.cs`、`WindowsImeInstaller.cs` 与 `IntegrationTestRunner.cs`。不要从最初的 Win32 错误 2 重新猜测，也不要要求 VM 安装开发工具；应从最新一轮结构化字段继续缩小问题范围。
