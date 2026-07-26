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

该轮唯一 finding 是 `GetBinaryType` 返回 error 193。此 API 面向可执行程序，不能用它对 DLL/IME 的失败结果判定镜像无效。后续版本先停止将该结果视为 finding；在完整安装闭环验证通过后，已从诊断模型中移除此项，避免继续输出“%1 不是有效的 Win32 应用程序”这一干扰信息。IME 映像有效性改由 PE 解析、安全映像映射和独立进程真实加载共同验证。

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

### 2026-07-26 第四轮正式安装验证结果

VM 返回结果确认根因修复有效：

- `uninstall-old` 成功移除第三轮残留布局 `E0200804`、`E0210804`；
- `diagnostics-pre-install` 和 `native-ime-load-probe` 均通过；
- 正式 `install-x64` 从 `C:\Windows\System32\XiaoXiIme.ime` 注册成功，返回布局 `E0200804`；
- x86/x64 的 TSF ABI 和隔离 COM 激活均通过；
- `cleanup` 成功移除正式布局和不再被引用的 System32 IME 文件。

安装、TSF 验证和清理均已成功。该轮最终未生成 `report`，原因不是 IME 安装失败，而是 payload 在纯净 VM 中硬编码执行 `dotnet vstest`；VM 按约束未安装 `dotnet`，因此 `IntegrationTestRunner` 抛出 Win32 error 2。

后续版本将集成冒烟场景发布为 `win-x64` 自包含 `XiaoXiIme.IntegrationTestHost.exe`，由 CLI 直接执行，不再依赖 VM 的 SDK、Runtime 或测试平台。外部 stage 的进程启动失败也必须转为结构化失败结果并写入报告，不能再以未处理异常结束。

### 2026-07-26 第五轮完整闭环验证结果

VM 返回结果确认纯净最终用户环境中的完整流程已经通过：

- `uninstall-old` 未发现上一轮残留；
- 安装前诊断无 finding，原生加载与 11 个传统 IME 导出验证全部通过；
- `install-x64` 从 `C:\Windows\System32\XiaoXiIme.ime` 成功注册布局 `E0200804`；
- x86/x64 的 TSF ABI 和隔离 COM 激活全部通过；
- 自包含 `XiaoXiIme.IntegrationTestHost.exe` 成功执行，输出 `PASS candidate-window-state` 和 `PASS ime-host-ipc`，不再依赖 VM 中的 `dotnet`；
- `cleanup` 成功移除布局和不再被引用的 System32 IME 文件；
- 生成了退出码为 0 的结构化集成报告。

至此，最初的 `ImmInstallIMEW` Win32 error 2、探测布局残留和纯净 VM 缺少测试平台三个问题均已完成根因修复与实机验证。当前输出还显示 `report` 控制台事件早于 `cleanup`；虽然报告文件随后会被重写并包含清理结果，但事件顺序容易造成误解，且清理失败不会改变原成功退出码。后续实现改为先执行并记录 `cleanup`，再写入和输出最终 `report`；当业务阶段全部成功但清理失败时，整体返回非零退出码。

### 2026-07-26 第六轮真实按键注入结果

VM 返回结果确认安装、x86/x64 TSF 验证以及前两个自包含集成场景继续通过，但 `real-ime-keystroke-commit` 在调用 `SendInput` 时失败：

```text
SendInput injected 0 of 4 keyboard events. Win32 error 87: 参数错误。
```

错误发生在四个键盘事件进入系统输入队列之前，因此本轮没有证据指向 `ImeProcessKey`、`ImeToAsciiEx`、HIMC 结果字符串或 `WM_IME_COMPOSITION` 链路。检查集成宿主发现其 `INPUT` P/Invoke 结构依赖运行时自动推导联合体对齐；`SendInput` 会严格校验 `cbSize == sizeof(INPUT)`，错误 87 与 x64 `INPUT` 大小或联合体偏移不匹配一致。

后续版本已将自包含 `win-x64` 测试宿主的 `INPUT` 显式声明为 Windows x64 ABI：总大小 40 字节，输入联合体位于偏移 8；调用前还会验证实际大小和偏移。若 `SendInput` 仍失败，stderr 会额外记录进程架构、`INPUT` 大小、联合体偏移、前台窗口和焦点窗口句柄，以便区分 ABI、前台焦点和 UIPI 完整性级别限制。

该轮 `cleanup` 已移除布局 `E0200804`，但删除 `C:\Windows\System32\XiaoXiIme.ime` 时收到拒绝访问。测试宿主在真实按键场景中已加载该 IME，文件可能在测试进程退出与系统卸载之间仍被映射；下一轮需同时观察 ABI 修复后真实按键提交是否通过，以及清理阶段能否删除 System32 文件。若按键场景通过但文件仍无法删除，应单独修复测试宿主退出、布局卸载与清理重试之间的生命周期，而不能静默忽略残留。

开发机验证结果：`XiaoXiIme.IntegrationTestHost` 和完整解决方案生成成功，`XiaoXiIme.ImeModule.Tests` 的 59 个测试全部通过。当前 Visual Studio Test Explorer 使用项目筛选时未发现 `XiaoXiIme.IntegrationTests`，这是开发机测试发现问题，不改变 VM 必须执行自包含宿主的验收要求。

### 2026-07-26 第七轮旧 System32 映像阻塞结果

VM 在新 payload 启动时仍存在上一轮留下的 `C:\Windows\System32\XiaoXiIme.ime`。该文件没有布局引用，但无法立即删除；其长度与新源文件相同，SHA-256 不同，因此正式安装按安全约束拒绝覆盖。该轮没有进入 TSF 或集成测试，不能用于判断上一版 `SendInput` ABI 修复是否有效。

本轮输出还确认旧清理逻辑存在两个控制流问题：

- `uninstall-old` 将 System32 文件删除失败仅写入消息，仍返回成功，导致流程继续到必然失败的安装；
- 正式安装因“既有文件内容不同”而失败后仍运行 `imm-install-variant-probe`，短文件名变体成功并不能解决正式文件的版本冲突，属于无关诊断。

后续版本已增加确定性的重启恢复路径：当目标 IME 已无任何布局引用但因仍被系统映射而无法立即删除时，CLI 使用 `MoveFileExW(..., MOVEFILE_DELAY_UNTIL_REBOOT)` 安排下次 Windows 重启时删除。`uninstall-old` 会返回失败，并在结构化 `Data` 中输出 `RebootRequired: true` 和 `PendingDeletePaths`；本轮随即写报告并停止，不再继续安装。文件冲突也已与真正的 `ImmInstallIMEW` API 失败分类，只有后者才会运行变体探测。

下一轮需要分两次操作：

1. 使用包含此修复的新 payload 运行一次 `integration-run`。预期 `uninstall-old` 报告已安排删除并要求重启，进程非零退出；确认 `PendingDeletePaths` 包含 `C:\Windows\System32\XiaoXiIme.ime`。
2. 完整重启 Windows VM，不只是关闭 PowerShell或注销。重启后确认旧文件已消失，再使用同一 payload 重新运行 `integration-run`。第二次运行才用于验证 `PASS real-ime-keystroke-commit` 和最终清理。

如果第一次运行连延迟删除也无法安排，必须回传其中的原始 Win32 错误码；不要手工取得文件所有权、修改 ACL 或强制覆盖。开发机验证为 `XiaoXiIme.Cli.Tests` 22 个测试全部通过，完整解决方案生成成功。

### 2026-07-26 沙盒约束修正：被加载文件优先移动

实际实验环境在 Windows 重启后会丢失整个沙盒内容，因此上一版“安排重启删除并停止，重启后再运行”的恢复路径不适用于当前验证。后续清理策略调整为：

1. 确认目标 IME 文件已无任何键盘布局引用；
2. 先尝试立即删除；
3. 删除因映像仍被加载而失败时，优先使用 `MoveFileExW` 在 System32 同卷重命名为严格格式的隔离文件：`XiaoXiIme.retired-<UTC>-<GUID>.ime`；
4. 移动成功后，正式 `XiaoXiIme.ime` 路径已经释放，`uninstall-old` 保持成功并继续当前 `integration-run`；隔离路径进入 `Data.RetiredFilePaths`，不能描述成已删除；
5. 后续运行只扫描并清理严格匹配上述格式的隔离文件，不处理其他文件；
6. 只有移动也失败时才尝试安排重启删除并返回 `RebootRequired: true`。在当前沙盒中这只是最后诊断，不能作为正常恢复步骤。

下一版 payload 的预期行为是：`uninstall-old` 报告旧正式文件已移动到 `RetiredFilePaths`，随后安装新 `XiaoXiIme.ime` 并继续执行 TSF、IPC 和真实按键场景。若移动失败，必须回传移动操作的 Win32 错误码；不得要求操作者重启后继续同一沙盒实验。

开发机验证结果更新为：`XiaoXiIme.Cli.Tests` 29 个测试全部通过，完整解决方案生成成功。

### 2026-07-26 第八轮真实 IME 未激活结果

VM 返回结果确认上一版 `INPUT` x64 ABI 修复有效：`SendInput` 已成功注入全部四个键盘事件，不再返回 Win32 error 87。安装、x86/x64 TSF 验证、候选窗口状态和 IPC 场景也继续通过，但真实 EDIT 控件的最终文本为 `xxxx`，而不是“小希”。

`xxxx` 说明按下与抬起事件都进入了目标窗口并被普通键盘翻译为字符，但该轮宿主只调用了 `LoadKeyboardLayout`/`ActivateKeyboardLayout`，没有验证窗口线程实际 HKL，也没有获取、打开和复核 EDIT 控件的 IMM 输入上下文。因此该结果不能证明按键已经进入 XiaoXiIme 的 `ImeProcessKey`/`ImeToAsciiEx`，当前证据优先指向测试宿主没有确定性地启用目标 IME，而不是核心 `xx -> 小希` 或 HIMC 结果字符串逻辑失败。

后续版本在发送按键前增加以下严格前置条件：

- 设置前台窗口与 EDIT 焦点后激活 XiaoXiIme HKL，并用 `GetKeyboardLayout(0)` 验证当前窗口线程的实际 HKL 与预期一致；
- 使用 `ImmGetContext` 获取 EDIT 的 HIMC；
- 若输入上下文尚未打开，调用 `ImmSetOpenStatus(TRUE)`，随后再次用 `ImmGetOpenStatus` 验证；
- 最终文本仍不匹配时，在 stderr 中输出预期 HKL、实际 HKL、HIMC 和 IME 打开状态，区分布局回退、无输入上下文、IME 关闭和已经进入 IME 但提交失败。

下一轮若在注入前因 HKL 或 HIMC 前置条件失败，应直接分析新增状态，不再把普通字符上屏归因于结果字符串实现。只有实际 HKL 等于 XiaoXiIme、HIMC 非零且 `ImeOpen=true`，最终文本仍不是“小希”时，才继续向 `ImeProcessKey`/`ImeToAsciiEx` 调用可见性和 IMM32 消息生成方向增加诊断。

本轮还观察到 retired 映像仍可能因加载生命周期无法立即删除，并被再次移动到新的 retired 路径。该问题不影响正式路径复用，但在真实按键链路通过后仍需继续验证测试宿主退出与清理之间是否能最终释放所有 retired 文件，不能把 `RetiredFilePaths` 描述成已删除。

开发机验证结果：核心 `ProcessKey_SecondXAutomaticallyCommitsXiaoXi` 测试通过，`XiaoXiIme.ImeModule.Tests` 59 个测试全部通过，`XiaoXiIme.IntegrationTestHost` 和完整解决方案生成成功。

### 2026-07-26 第九轮 `TRANSMSG` ABI 根因

VM 返回结果确认真实按键场景的全部激活前置条件均成立：预期 HKL 与实际 HKL 都是 `E0200804`，EDIT 的 HIMC 非零，且 `ImeOpen=True`。最终文本从上一轮的 `xxxx` 变为 `xxx`，说明至少部分按键已经进入 IME 路径，但 IMM32 返回消息没有被 Windows 按预期解释。

检查发现项目将 Windows 原生 `TRANSMSG` 错误声明为包含 `HWND` 的结构。真实 `TRANSMSG` 仅包含 `message`、`wParam`、`lParam`；多出的首字段会导致 Windows 从 `TRANSMSGLIST` 读取时把后续所有字段错位。在 x64 下错误结构大小为 32 字节，而正确大小为 24 字节。原测试只使用同一错误托管声明写入和读取，因此无法发现与 Windows ABI 的偏差；测试缓冲区还按 `sizeof(uint)` 计算首消息偏移，没有考虑 x64 下 `TRANSMSGLIST.TransMsg` 位于偏移 8。

后续版本已做以下修复：

- 从 `TransMsg` 删除不存在的 `Hwnd` 字段；
- 消息构造器不再接受或写入窗口句柄；
- 测试按 `sizeof(TRANSMSGLIST) + sizeof(TRANSMSG)` 为两条消息分配缓冲区；
- 新增原生 ABI 断言：x64 下 `TRANSMSG` 大小为 24，字段偏移依次为 0、8、16，`TRANSMSGLIST` 首消息偏移为 8；x86 下对应为 12 和 0、4、8，首消息偏移为 4。

下一轮预期 Windows 能正确解释 `WM_IME_STARTCOMPOSITION`、带 `GCS_RESULTSTR` 的 `WM_IME_COMPOSITION` 和 `WM_IME_ENDCOMPOSITION`。若最终仍不是“小希”，新增诊断应转向实际 `ImeProcessKey`/`ImeToAsciiEx` 调用次数和每次返回消息，不再继续修改 HKL 或 HIMC 激活逻辑。

开发机清理旧增量输出后，`XiaoXiIme.ImeModule.Tests` 60 个测试全部通过。

### 2026-07-26 第十轮 payload 版本可辨识与按键调用轨迹

最新回传再次得到 `xxxx`，同时严格前置状态为 `ExpectedHkl=ActiveHkl=E0200804`、HIMC 非零且 `ImeOpen=True`。该输出与第八轮完全一致，不能单独证明第九轮 `TRANSMSG` ABI 修复后的二进制仍然失败：当前工作区已经包含正确的三字段 `TRANSMSG` 和 x86/x64 ABI 断言，而回传材料没有包含 payload 的生成日志、manifest 生成时间或可识别该修复版本的导出。

默认 `payload-build` 会重新执行 Release build 和每个 RID 的 NativeAOT publish；只有显式使用 `--no-build` 才会复用 `artifacts\integration-publish` 中的既有产物。后续构建和复制 payload 时不得使用 `--no-build`，并应保留控制台中的 payload 创建时间与 manifest SHA-256，以排除新测试宿主搭配旧 IME 的情况。

为使下一轮证据不再依赖行为推断，IME 新增两个只读诊断导出：

- `XiaoXiImeResetKeystrokeDiagnostics`：发送按键前清零调用轨迹；
- `XiaoXiImeGetKeystrokeDiagnostics`：返回固定 40 字节、版本为 1 的快照，包含 `ImeProcessKey`/`ImeToAsciiEx` 调用次数、最后虚拟键、Handled、HIMC 写入是否成功、写入的 `TRANSMSG` 数量和最终返回值。

自包含集成宿主会从 System32 加载已安装的同一 IME 并解析这两个导出。若导出不存在，场景会明确报告 payload 版本不匹配，要求不带 `--no-build` 重新构建并复制整个 payload。若最终文本仍不是“小希”，stderr 中的 `ImeTraceVersion=1` 按以下方式判断：

- `ImeProcessKeyCalls=0`：尽管 HKL/HIMC 状态成立，IMM32 没有调用项目导出，继续调查线程布局切换或系统 IME 选择；
- `ImeProcessKeyCalls>=2` 但 `ImeToAsciiExCalls=0`：`ImeProcessKey` 返回语义或 Windows 后续转换调度异常；
- `LastProcessHandled=False`：虚拟键或 key data 被错误翻译，先检查实际 VK 和修饰键；
- `ImeToAsciiExCalls>=2` 且 `LastToAsciiHandled=True`，但 `CompositionWriteSucceeded=False`：HIMC 内部锁定、扩容或 `ImmGenerateMessage` 失败；
- `MessageCount=2`、`ReturnValue=2` 且仍为普通字符：Windows 已收到成功返回但未正确消费 `TRANSMSGLIST`，继续核对实际 payload 中的原生布局与消息内容；
- 文本为“小希”且两个调用次数均至少为 2：第九轮 ABI 修复完成实机闭环。

本轮清理仍显示已加载的 retired 映像无法立即删除，只能再次移动到新的 retired 路径。新增测试宿主会显式 `FreeLibrary` 其诊断加载引用，但 Windows/IMM32 自身可能继续持有映像；下一轮同时观察 `cleanup` 是否停止产生新的 `RetiredFilePaths`。该残留问题与真实按键提交分开判定，不得用移动成功代替实际删除成功。

## 后续回归验证必须回传的信息

安装问题已闭环，后续仅在修改安装、TSF、IPC、payload 或集成运行流程后执行回归。回归输出至少应包含以下 stage，并保持 `cleanup` 早于 `report`：

- `uninstall-old`；
- `diagnostics-pre-install`；
- `native-ime-load-probe`；
- `install-x64`；
- `tsf-abi-x64`、`tsf-com-activation-x64`；
- `tsf-abi-x86`、`tsf-com-activation-x86`；
- `integration-tests`，其 stdout 应包含 `PASS candidate-window-state`、`PASS ime-host-ipc` 和 `PASS real-ime-keystroke-commit`；
- `cleanup`；
- 若安装失败，`diagnostics-post-install-failure`；
- 若安装失败，`imm-install-variant-probe`；
- `report`。

成功回归应满足进程退出码为 0、`cleanup` 成功，并且最终报告中的 `results` 包含 `cleanup`。如果 `--keep-installed` 被显式启用，则可以不包含 `cleanup`，但必须由操作者负责后续卸载。

真实按键上屏回归必须在已登录的交互式 Windows 桌面会话中执行。运行命令前应关闭可能抢占前台焦点的程序，不得通过计划任务的非交互会话、断开桌面的服务会话或远程后台执行器启动。测试宿主会短暂创建并聚焦一个标题为 `XiaoXiIme Integration Test` 的窗口；若无法取得前台窗口、焦点或 `SendInput` 被完整性级别阻止，`integration-tests` 必须失败并在 stderr 中给出原因。

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

## `xx` 输入“小希”的真实输入闭环实施计划

安装排障已经闭环，下一阶段转为验证真实 Windows 编辑控件中的按键、组合和结果字符串上屏链路。最小目标是：激活已安装的 XiaoXiIme 后，在 Win32 `EDIT` 控件中连续输入两次 `x`，第二个 `x` 到达后由 IME 直接提交“小希”，最终控件文本必须严格等于“小希”。本阶段暂不要求显示可交互候选窗口，也不要求再按空格确认。

实施步骤：

1. 在默认内存词典中增加 `xx -> 小希` 的确定性词条。
2. 在 IME 核心中保留第一个 `x` 的组合状态，并在第二个 `x` 后自动提交唯一候选“小希”、清空组合状态。
3. 增加核心回归测试，验证第一个 `x` 只建立组合，第二个 `x` 返回完整的双字符结果字符串。
4. 增加 IME 消息层回归测试，验证提交结果继续通过 HIMC 的 `GCS_RESULTSTR` 和 `WM_IME_COMPOSITION` 传递，而不是绕过输入法协议直接写宿主文本。
5. 扩展自包含 `XiaoXiIme.IntegrationTestHost.exe`：创建真实 Win32 `EDIT` 控件，查找并激活 XiaoXiIme 布局，将窗口置于前台，使用系统输入注入发送 `xx`，泵送窗口消息，并在有限超时内读取控件文本。
6. 集成场景成功时输出 `PASS real-ime-keystroke-commit`；无法取得交互式前台窗口、无法激活布局、输入注入失败、超时或最终文本不匹配时必须输出明确失败原因并返回非零退出码。
7. 在开发机运行核心、IME 模块和集成宿主测试，并构建完整解决方案与 payload，随后复制到纯净 VM 做实机回归。

实现约束：

- 必须经过现有 `ImeProcessKey` → `ImeToAsciiEx` → composition/result string → `WM_IME_*` 链路；测试代码不得直接把“小希”设置到编辑框。
- VM 仍不得依赖 .NET SDK、系统级 Runtime、Visual Studio 或额外诊断工具。
- 真实按键场景要求交互式桌面会话；`SendInput` 的前台窗口和完整性级别限制必须作为可诊断前置条件处理。
- 测试必须使用有限超时和消息泵，不得无限等待。
- 若真实宿主测试暴露消息顺序、HIMC 缓冲区或多字符结果字符串问题，应修复协议实现，不能降低断言或伪造通过。

验收标准：

- 核心层：第一个 `x` 返回正在组合且 reading 为 `x`；第二个 `x` 返回 `CommitText == "小希"` 且组合结束。
- 消息层：提交结果包含 `GCS_RESULTSTR`，HIMC 中保存的结果字符串为完整的“小希”。
- VM 端：`integration-tests` 的 stdout 在原有两个 PASS 之外包含 `PASS real-ime-keystroke-commit`。
- 完整集成流程仍保持 `cleanup` 早于 `report`，最终进程退出码为 0，报告包含成功的 `cleanup` 与 `integration-tests`。

开发机完成实现后重新构建 payload：

```powershell
dotnet run --project .\src\XiaoXiIme.Cli\XiaoXiIme.Cli.csproj -- payload-build --output .\artifacts\integration-payload
```

复制到 VM 后仍执行文档开头的 `integration-run` 命令。新的成功输出必须同时证明安装、x86/x64 TSF 验证、IPC 测试、真实 `xx` 按键提交“小希”和最终清理均通过。
