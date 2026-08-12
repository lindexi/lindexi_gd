# XiaoXiIme IMM32 状态与验证交接

> 最后更新：2026-08-12
>
> 本文记录传统 IMM32 `.ime` 的当前状态、已确认约束和回归验证方式。历史排障细节见 [VM 安装诊断记录](./VM-Ime-Installation-Diagnostics.md)。项目当前主线是传统 IMM32；TSF 不是默认验收路径。

## 当前结论

传统 IMM32 输入法的管理员沙箱无人值守闭环已经完成并多次通过：

1. 同时部署 x64/x86 `XiaoXiIme.ime` 到 `System32`/`SysWOW64`。
2. 调用 `ImmInstallIMEW` 注册布局 `E0200804`。
3. 创建 Win32 EDIT 控件，激活 XiaoXiIme HKL，关联并打开 HIMC。
4. 使用 `SendInput` 自动注入 `x`、`x`。
5. Windows 调用 `ImeProcessKey` 与 `ImeToAsciiEx`。
6. EDIT 控件最终精确上屏一次“小希”。
7. 测试结束后删除布局并清理正式部署路径。

最近一次复测报告退出码为 0：

```text
artifacts/sandbox-retest-results/retest.json
```

真实按键测试依赖 Windows 前台窗口和 EDIT 焦点。沙箱启动器或控制台偶尔会短暂争抢前台窗口，导致测试在注入按键前主动失败；相同负载复测成功时可判定为桌面焦点时序竞争，而不是输入法回归。测试宿主会验证前台窗口和焦点，避免将按键发送到其他窗口。

## 固定验证命令

先在开发环境构建完整负载：

```powershell
dotnet run --project .\src\XiaoXiIme.Cli\XiaoXiIme.Cli.csproj -- payload-build --output .\artifacts\integration-payload
```

如果已经按项目发布清单生成 `artifacts\integration-publish`，可只重新组织负载：

```powershell
.\artifacts\integration-publish\app\cli\XiaoXiIme.Cli.exe payload-build --no-build --output .\artifacts\integration-payload
```

在管理员沙箱或可还原 VM 中执行传统 IMM32 验收：

```powershell
.\app\cli\XiaoXiIme.Cli.exe integration-run . --confirm I-UNDERSTAND-THIS-MODIFIES-WINDOWS --skip-tsf --report .\results\integration.json
```

`payload-build` 只支持 `--output` 和 `--no-build`，不会直接运行集成测试。构建负载与执行 `integration-run` 是两个独立生命周期。

若只需保留安装供人工体验：

```powershell
.\app\cli\XiaoXiIme.Cli.exe install . --confirm I-UNDERSTAND-THIS-MODIFIES-WINDOWS
```

## 必须保留的实现约束

### 双架构部署

- x64 模块部署到 `System32`。
- x86 模块部署到 `SysWOW64`。
- 文件名保持 `XiaoXiIme.ime`；资源中的 `OriginalFilename` 会影响注册表记录的 IME 文件名。

### 原生 ABI

- UI 类名必须保持在 `ImeInquire` 的 16 个宽字符缓冲区限制内；当前值为 `XiaoXiImeUIWnd`。
- `InputContext` 必须保持与 Windows SDK `INPUTCONTEXT` 一致的字段顺序、字段宽度和跨架构对齐。
- `TRANSMSG` 只包含 `message`、`wParam`、`lParam`，不得增加窗口句柄字段。
- x86/x64 的结构大小和关键字段偏移必须由测试覆盖。
- `ImeProcessKey` 必须忽略 transition-state 位表示的 KeyUp，避免一次输入重复提交。

### 真实按键诊断

原生模块保留以下只读诊断入口，供集成宿主确认 Windows 实际调用链：

- `XiaoXiImeResetKeystrokeDiagnostics`
- `XiaoXiImeGetKeystrokeDiagnostics`

自动按键宿主还会记录：

- 当前 HKL、HIMC 和 IME 打开状态；
- 前台窗口与 EDIT 焦点；
- `WM_KEYDOWN`、`WM_KEYUP`、`WM_CHAR` 和 `WM_IME_*` 消息；
- `ImeProcessKey`、`ImeToAsciiEx` 等诊断计数。

## 已解决的关键根因

1. UI 类名和复制上限曾越过 `ImeInquire` 的真实缓冲区，导致 `ActivateKeyboardLayout` 内出现 `0xC0000409`。
2. 托管 `InputContext` 曾遗漏字段并把 `BOOL`/`DWORD` 错误表达为指针宽度，写入组合上下文时会破坏原生内存。
3. `TRANSMSG` 曾错误包含 `HWND`，导致 Windows 按错误偏移解释返回消息。
4. x64 `INPUT` 联合体曾未按 Windows ABI 声明，`SendInput` 因结构大小错误返回 Win32 error 87。
5. KeyUp 曾重复进入按键处理链，导致上屏“小希小希”。
6. 被加载的旧 System32 映像可能无法立即删除；清理逻辑会在确认无布局引用后将其移动为严格格式的 `XiaoXiIme.retired-<UTC>-<GUID>.ime`，释放正式路径供当前沙箱继续验证。

## TSF 边界

当前默认验收目标是传统 IMM32。执行 `integration-run` 时应传入 `--skip-tsf`。

不传 `--skip-tsf` 会额外执行 x86/x64 TSF ABI 和隔离 COM 激活验证。TSF ABI/vtable 可以通过，但在未完成相应 COM 注册的环境中，COM 激活可能返回 `0x80040154 (REGDB_E_CLASSNOTREG)`；这不代表传统 IMM32 安装或真实上屏链路失败。

## 沙箱清理注意事项

多轮测试后，System32 中可能存在仍被旧进程映射的退役文件：

```text
XiaoXiIme.retired-*.ime
```

这些文件不再被键盘布局引用，也不占用正式 `XiaoXiIme.ime` 路径。若需要验证完全删除，应重置沙箱或等待持有模块的进程退出；不要把退役文件存在误判为正式布局仍安装。

## 回归判断

修改 IME ABI、上下文布局、按键转换、安装器或真实按键宿主后，至少验证：

- Release 解决方案构建；
- `XiaoXiIme.ImeModule.Tests`；
- x86/x64 Native AOT 发布；
- 负载 manifest、长度和 SHA-256 校验；
- 管理员沙箱中的 `integration-run --skip-tsf`；
- `PASS real-ime-keystroke-commit` 和整体退出码 0；
- 测试后的布局与正式部署路径清理。

不要只根据 `ImmInstallIMEW` 成功判断输入法可用，也不要用 TSF COM 激活结果替代传统 IMM32 的真实按键与上屏验收。
