# XiaoXiIme IMM32 落地排查交接

> 最后更新：2026-08-12
>
> 本文用于下一轮对话直接继续传统 IMM32 `.ime` 的真实安装与按键上屏排查。项目明确不转向 TSF；TSF 不是当前解决路线，系统测试应传入 `--skip-tsf`。

## 当前目标

在管理员 Windows 沙箱中完成以下无人值守闭环：

1. 同时部署 x64/x86 `XiaoXiIme.ime` 到 `System32`/`SysWOW64`。
2. 调用 `ImmInstallIMEW` 注册传统 IMM32 输入法。
3. 创建 Win32 EDIT 控件，激活 XiaoXiIme HKL 并打开 HIMC。
4. 使用 `SendInput` 注入 `x`、`x`。
5. 验证系统调用 `ImeProcessKey`、`ImeToAsciiEx`，最终上屏“`小希`”。
6. 删除键盘布局和两份系统目录文件。

## 用户约束

- 坚持传统 IMM32 `.ime` 实现。
- 不建议或推动迁移到 TSF。
- 测试必须尽量自动化，不等待人工敲键盘。
- 允许管理员沙箱修改 Windows 配置。

## 已完成的工程改动

### CLI 与负载

- `integration-run` 新增 `--skip-tsf`。
- `payload-build` 新增：
  - `--run-integration`
  - `--confirm`
  - `--report`
  - `--skip-tsf`
- `payload-build --no-build` 可从独立发布目录定位包含 `app`、`native` 的发布根目录，不再强制依赖 `XiaoXiIme.slnx`。
- 可在同一沙箱任务内生成负载并立即运行，只拉回最终报告，避免重复回传约 1007 个文件。
- 原生加载探测和二进制校验已扩展必需导出列表。

### 双架构安装与清理

- `IImeInstaller` 新增 `InstallPair(x64, x86, displayName)`。
- x64 模块部署到 `System32`，x86 伴随模块部署到 `SysWOW64`。
- 使用 x64 系统路径调用 `ImmInstallIMEW`。
- 卸载同时清理 `System32` 与 `SysWOW64`。
- 文件名保持 `XiaoXiIme.ime`；不要切换为 `XIAOXI.IME`：
  - `ImmInstallIMEW` 生成的注册表值实际为 `XIAOXIIME.IME`，来自 PE 资源中的 `OriginalFilename`。
  - 只部署短名会导致注册表文件名与实际文件不一致。

### IME 模块

- 新增最小 `CS_IME` UI 窗口类 `XiaoXiImeUiWindow`。
- UI 类在模块初始化时注册，并在 `ImeInquire` 中幂等兜底。
- `cbWndExtra` 当前设置为两个指针大小。
- 模块 HMODULE 改为使用 `GetModuleHandleEx(FROM_ADDRESS | UNCHANGED_REFCOUNT)` 从窗口过程地址获取，不再依赖 DLL 文件名。
- 新增兼容导出：
  - `CtfImeInquire`
  - `CtfImeSelect`
- 新增传统词库导出空实现：
  - `ImeRegisterWord`
  - `ImeUnregisterWord`
  - `ImeGetRegisterWordStyle`
  - `ImeEnumRegisterWord`
- 诊断快照升级到版本 2，记录：
  - `ImeInquire`
  - `ImeSelect`
  - `ImeSetActiveContext`
  - `NotifyIME`
  - `ImeProcessKey`
  - `ImeToAsciiEx`

### 自动按键宿主

- 人工输入已替换为 Win32 `SendInput`。
- x64 `INPUT` 联合体已补齐 `MOUSEINPUT` 最大成员，结构大小正确。
- 注入前重新获取并验证前台窗口与 EDIT 焦点。
- 消息泵会统计：
  - `WM_KEYDOWN`
  - `WM_KEYUP`
  - `WM_CHAR`
  - `WM_IME_STARTCOMPOSITION`
  - `WM_IME_COMPOSITION`
  - `WM_IME_ENDCOMPOSITION`
- 激活 HKL 后调用 `ImmAssociateContextEx(..., IACE_DEFAULT)` 重绑默认输入上下文。

## 已确认的事实

### 安装与加载

- 管理员沙箱可真实写入 `System32`。
- `ImmInstallIMEW` 成功，布局 ID 为 `E0200804`，HKL 为 `0xFFFFFFFFE0200804`。
- `ImmIsIME=True`。
- `ImmGetIMEFileName` 返回 `XIAOXIIME.IME`。
- `ImmGetDescription` 返回 `XiaoXi Input Method`。
- HIMC 存在，`ImeOpen=True`。
- 未签名不是“完全不加载”的直接原因：曾观察到 `LoadedByImmBeforeProbe=True`。
- 直接调用 `ImeInquire` 能成功返回：
  - `Property=0x190000`
  - `ConversionCaps=0x109`
  - `SetCompCaps=0x3`
  - `SelectCaps=0x1`
  - UI 类 `XiaoXiImeUiWindow`

### SendInput

`SendInput` 已被证明工作正常，不应继续把主要精力放在按键注入方式上。最近一次完整消息轨迹确认：

- `KeyDown=2`
- `KeyUp=2`
- 最后键盘消息目标 HWND 就是 EDIT 控件
- 注入前 `SetForegroundWindow=True`
- 前台窗口与焦点正确
- `Char=0`
- `WM_IME_*` 全部为 0

因此真实问题是 IME 初始化/调度没有完成，而不是 `SendInput` 没有发送 `xx`。

## 已排除或不足以解释问题的假设

- 不是沙箱无管理员权限。
- 不是 `SendInput` 结构大小或按键没有投递。
- 不是仅缺 x86 `SysWOW64` 伴随模块；双架构部署后仍无按键调度。
- 不是仅缺 `CtfImeInquire`/`CtfImeSelect` 导出。
- 不是仅缺词库管理导出。
- 不是 HIMC 没有打开。
- 不是固定 DLL 文件名导致 UI 类注册失败；模块句柄获取已改为地址方式。
- 不能再简单归因于未签名：IMM 曾主动加载未签名模块。

## 当前阻塞

补齐完整传统导出后，集成测试宿主在进入真实 IME 场景时出现：

```text
ExitCode = -1073740791
0xC0000409
```

这是 Fast Fail/栈或非托管调用契约损坏。该崩溃发生在输出：

```text
PASS candidate-window-state
PASS ime-host-ipc
```

之后、首条真实 IME 场景诊断之前。

这表示 Windows 已比之前更深入地进入 IME 初始化链，但某个初始化 API、导出 ABI 或 IME UI 窗口过程触发 Fast Fail。

最近尝试把 `CtfImeInquire`/`CtfImeSelect` 改为与 `ImeInquire`/`ImeSelect` 相同参数数量，仍然崩溃。不要继续凭记忆猜测这两个 ABI；下一步必须先精确定位崩溃阶段，并核对权威 Windows SDK 头文件/参考实现中的声明。

## 下一轮必须先做

### 1. 给真实 IME 场景增加立即刷新的阶段标记

在每一步前后输出独立行并立即 `Console.Out.Flush()`：

1. 进入窗口线程。
2. `FindInstalledLayoutId` 前后。
3. `LoadKeyboardLayout` 前后。
4. 创建顶层窗口前后。
5. 创建 EDIT 前后。
6. `ActivateKeyboardLayout` 前后。
7. `ImmAssociateContextEx` 前后。
8. `ImmGetContext` 前后。
9. `ImmSetOpenStatus` 前后。
10. `ImmGetDefaultIMEWnd` 前后。
11. 手动 `LoadLibraryEx` 前后。
12. 读取诊断导出前后。
13. `SendInput` 前后。

目标是把 `0xC0000409` 定位到单个调用区间。当前不要先修改更多 ABI。

### 2. 核对非托管 ABI

重点核对：

- `CtfImeInquire`
- `CtfImeSelect`
- `ImeEnumRegisterWord`
- `ImeGetRegisterWordStyle`
- UI 窗口过程签名和 `cbWndExtra`

特别注意：NativeAOT `UnmanagedCallersOnly`、x86 StdCall 栈清理和 x64 统一调用约定可能让错误在不同架构表现不同。

### 3. 分离导出增量

如果阶段日志显示崩溃发生在 `LoadKeyboardLayout` 或激活期间：

- 先保留完整传统 11 个基础导出。
- 对后来新增的 `CtfIme*` 和 4 个词库导出做最小二分回退。
- 每次只改变一组导出并重跑沙箱。
- 不要回退自动输入、双架构安装或消息轨迹诊断。

### 4. 保持真实验证命令

发布完成后使用：

```text
XiaoXiIme.Cli.exe payload-build --no-build --output payload --run-integration --confirm I-UNDERSTAND-THIS-MODIFIES-WINDOWS --skip-tsf --report results\integration.json
```

沙箱执行时推送：

```text
artifacts/integration-publish
```

只拉回：

```text
results
```

## 最近证据文件

- SendInput 目标窗口证据：
  - `artifacts/sandbox-results/real-install-short-name-input-trace/integration-short-name-input-trace.json`
- 双架构长文件名、模块句柄修复后的证据：
  - `artifacts/sandbox-results/real-install-self-module-handle/integration-self-module-handle.json`
- 补齐完整导出后首次 Fast Fail：
  - `artifacts/sandbox-results/real-install-complete-exports/integration-complete-exports.json`
- 调整 `CtfIme*` 参数数量后仍 Fast Fail：
  - `artifacts/sandbox-results/real-install-ctf-abi-fixed/integration-ctf-abi-fixed.json`

## 2026-08-12 最新增量定位

- 已完成真实场景逐阶段立即刷新日志；当前崩溃区间可以稳定定位。
- 仅保留基础导出以及 `ImeRegisterWord`、`ImeUnregisterWord`、`ImeGetRegisterWordStyle` 时，不发生 Fast Fail，场景可运行到 `SendInput`，但 IMM 仍未调用按键导出。
- 单独补回 `ImeEnumRegisterWord` 后，x64 管理员沙箱稳定在 `ActivateKeyboardLayout` 内触发 `0xC0000409`；此前 `CtfIme*` 已移除，因此该结果与兼容桥接导出无关。
- `ImeEnumRegisterWord` 已按 Windows SDK 声明实现为五参数 StdCall：回调、reading、style、string、data。即使声明表面一致，NativeAOT 直接导出仍会触发系统初始化 Fast Fail，因此不能再次直接补回同样实现。
- 曾错误地新增一个静态原生垫片，把全部 IMM32 系统入口改为空实现，并通过链接器参数强制导出这些符号。该设计不是托管 Core 的有效桥接：它绕过了现有 IME 行为，没有建立可验证的托管转发契约，也没有解决初始化根因。
- 上述错误实现即使加入 UI 类注册并调整 `ImeInquire` 能力位，管理员沙箱仍在 `ActivateKeyboardLayout` 内触发相同的 `0xC0000409`。这证明“用全原生空实现覆盖导出”不是修复方向，不能把它当作传统 IMM32 的正式架构。
- 错误原生垫片目录已删除，模块项目中对其静态库、符号保留和导出参数的残留也已移除。后续不得恢复该实现或据此继续叠加修改。
- 后续应从正常 IMM32 DLL 初始化契约、实际导出 ABI、PE 装载行为以及 Windows 调用顺序继续定位；任何原生边界代码都必须具有明确且可测试的托管 Core 转发语义，不能以空实现替代功能。
- `ImmDisableTextFrameService` 在沙箱线程上可能返回 `False` 且最后错误为 0；测试宿主现只记录结果并继续，不再提前终止真实 IMM32 场景。
- NativeAOT 直接导出阶段证据：`artifacts/sandbox-results/real-install-enum-register-word-v2/integration.json`。
- 错误原生空导出阶段证据：`artifacts/sandbox-results/real-install-all-native-exports/integration.json`、`artifacts/sandbox-results/real-install-native-ui-registration/integration.json`。

## 当前构建与测试状态

最近已通过：

- `XiaoXiIme.ImeModule.Tests`：62 个测试。
- `XiaoXiIme.Cli.Tests`。
- `XiaoXiIme.IntegrationTests`。
- x64/x86 NativeAOT IME 构建与发布。
- x64 CLI 和集成宿主构建与发布。

相关发布目录：

```text
artifacts/integration-publish/native/win-x64/ime
artifacts/integration-publish/native/win-x86/ime
artifacts/integration-publish/app/cli
artifacts/integration-publish/app/tests
```

## 沙箱清理注意事项

正式布局和规范文件名通常会被清理，但多轮测试后 `System32` 中存在被加载占用的退役文件：

```text
XiaoXiIme.retired-*.ime
```

清理逻辑会尝试移动这些文件，但被占用时可能形成新的退役文件。下一轮开始前最好重置/重启沙箱，避免旧进程持有模块句柄干扰结果。不要把退役文件存在误判为当前正式布局仍安装。

## 不要重复的无效工作

- 不要再要求人工输入 `xx`。
- 不要再把问题归因于 `SendInput` 未触发；消息轨迹已证明投递成功。
- 不要建议迁移 TSF。
- 不要只根据 `ImmInstallIMEW` 成功判断输入法可用。
- 不要忽略非零退出码或把 `0xC0000409` 当普通测试失败。
- 不要在未定位崩溃阶段前继续同时修改多个 ABI。

## 2026-08-12 闭环完成

传统 IMM32 `.ime` 的管理员沙箱无人值守闭环已经通过，最终报告退出码为 0：

- x64/x86 外壳与 NativeAOT Core 均成功部署到对应系统目录。
- `ImmInstallIMEW`、`LoadKeyboardLayout`、`ActivateKeyboardLayout`、HIMC 关联与打开均成功。
- `SendInput` 自动注入 `xx` 后，系统成功调度 `ImeProcessKey` 与 `ImeToAsciiEx`。
- EDIT 控件最终文本通过“小希”断言，输出 `PASS real-ime-keystroke-commit`。
- 布局、两份 `.ime` 和两份 Core 文件均成功清理，无重启要求。

最终确认了两个独立的原生 ABI 内存问题：

1. `ImeInquire` 的 UI 类名缓冲区只有 16 个宽字符（包含终止符）。原类名和复制上限会越界，导致 `ActivateKeyboardLayout` 内出现 `0xC0000409`。现已将类名缩短为 `XiaoXiImeUIWnd`，并按真实缓冲区长度写入。
2. 托管 `InputContext` 曾遗漏 `POINT`、`LOGFONTW`、`COMPOSITIONFORM`、四个 `CANDIDATEFORM`、消息缓冲区等字段，并错误地把 `BOOL`/`DWORD` 表达为指针宽度。按键处理写入 `hCompStr` 等字段时因此覆盖原生上下文，导致 `SendInput` 后出现 `0xC0000374`。现已按 Windows SDK 的完整 `INPUTCONTEXT` 顺序和跨架构对齐修正，并增加尺寸及关键偏移测试。

原生外壳还明确转发以下诊断入口到 Core，使真实加载的 `.ime` 可被集成宿主观测：

- `XiaoXiImeResetKeystrokeDiagnostics`
- `XiaoXiImeGetKeystrokeDiagnostics`

最终证据：

```text
artifacts/sandbox-results/real-install-input-context-layout/integration.json
```

后续修改必须保留：短 UI 类名、16 字符写入上限、完整 `INPUTCONTEXT` ABI、诊断转发、双架构部署和 `--skip-tsf` 真实测试。

## 2026-08-12 KeyUp 重复处理修复

后续复测发现两个物理 `x` 会产生按下和释放共四次 `ImeProcessKey` 调用，释放事件此前继续进入按键处理链，导致结果字符串提交两次并上屏“小希小希”。现已在 `ImeProcessKeyManaged` 入口检查 `lKeyData` 的 transition-state 位（位 31），对 KeyUp 直接返回未处理，不再调用运行时按键处理器。

已增加 `ImeProcessKeyManaged_ReturnsFalseForKeyUp` 回归测试，并重新通过：

- `XiaoXiIme.ImeModule.Tests` 全部测试。
- Release 解决方案构建。
- x86/x64 NativeAOT Core 发布。
- x86/x64 原生 `.ime` 外壳构建。
- 管理员沙箱真实安装、HKL 激活、HIMC 打开、`SendInput` 注入 `xx`、EDIT 精确上屏“小希”及完整清理，退出码为 0。

最新证据：

```text
artifacts/sandbox-results/real-install-keyup-filter/integration.json
```

## 2026-08-12 原生实现对照复核

已对照成熟的传统 IMM32 原生实现，重新核查基础导出集合、Windows SDK 函数签名、UI 类注册时机、`ImeInquire` 能力声明、HIMC 锁定流程以及 `ImeProcessKey`/`ImeToAsciiEx` 的调用职责。当前“原生 `.ime` 外壳 + NativeAOT Core”边界与传统 IMM32 调用契约一致，未发现需要继续修改的 ABI 差异。

本轮重新通过：

- `XiaoXiIme.ImeModule.Tests` 全部测试。
- Release 解决方案构建。
- x64/x86 NativeAOT Core 发布。
- x64/x86 原生 `.ime` 外壳构建。
- 管理员沙箱中的双架构部署、`ImmInstallIMEW`、HKL 激活、HIMC 打开、系统调用 `ImeProcessKey`/`ImeToAsciiEx`、自动注入 `xx`、EDIT 精确上屏一次“小希”以及完整清理。

本轮真实安装验证退出码为 0，证据：

```text
artifacts/sandbox-results/real-install-reference-recheck-2/integration.json
```
