# 某应用交互卡住：IMM32 焦点回调同步等待 IPC 调查

## 结论

在 Windows 沙箱的故障现场，某应用的 UI 线程执行 `SetFocus` 时，Windows 在同一线程上调用了输入法的 `ImeSetActiveContext`。完整用户态转储结合与部署版本匹配的 x86 私有符号，确认该线程在输入法的 [`ImeHostBridge.UpdateInputTarget`](../src/XiaoXiIme.ImeModule/Runtime/ImeHostBridge.cs#L65) 中同步等待宿主 IPC。**已确认的缺陷是：输入法在应用的焦点回调中等待后台应答，导致焦点操作不能及时返回。**

同一现场还确认，目标 UI 线程无法接收新的异步窗口消息，Windows 返回 `1816`（`ERROR_NOT_ENOUGH_QUOTA`）。单份转储不能确定等待时长，也没有提供完整的消息队列和发送者记录。**不能据此认定输入法填满了队列，或断言这次等待构成了永久死锁。** 本文只记录调查及修复建议，不代表已修复或部署。

## 故障现象与目标定位

某应用是多进程桌面应用。用户报告在编辑界面中点击控件、切换页面等操作无响应。外层主窗口与承载编辑内容的嵌入窗口属于不同进程；键盘焦点和实际鼠标点击都指向嵌入窗口。承载该窗口的进程没有独立的顶层主窗口，不能仅靠 `Process.MainWindowHandle` 判断它是否参与界面交互。

初查只验证了外层主窗口能否回应同步消息，遗漏了实际接收交互的窗口。此前还有一个已退出的应用实例曾出现全部线程被挂起的现象，挂起来源未查明。那是独立现场，不应与本次交互故障混为一谈。

## 现场验证：同步探测与异步投递

在同一故障现场，分别向外层窗口和实际接收交互的嵌入窗口发送空消息：

| 目标 | `SendMessageTimeout(WM_NULL)` | `PostMessage(WM_NULL)` |
| --- | --- | --- |
| 外层窗口 | 成功 | 成功 |
| 嵌入窗口 | 成功 | 失败，错误 `1816` |

同步探测成功只表示窗口能处理这条同步消息，不能证明线程已回到正常消息循环，也不能代替真实鼠标或键盘测试。嵌入窗口的异步投递失败说明当时其线程的消息队列不能接受新的投递。应用自身也记录了 WPF `HwndTarget.UpdateWindowSettings` 中的 `Win32Exception (1816)`，与现场现象一致。进程的 USER/GDI 对象数不能用来推算线程消息队列长度。

## 转储与源码证据

调查未重启应用，也未切换输入法。在现场采集了嵌入窗口所属进程的完整用户态转储，随后在沙箱离线分析。托管栈表明，应用在处理跨进程焦点通知时，WPF 的 `PreviewGotKeyboardFocus` 内又发起一次 `Focus()`，最终进入 `TrySetFocus`。这是一次嵌套焦点操作的证据，不足以认定无限重入。离线调试使用匹配的输入法 x86 私有 PDB 补齐了原生调用链：

```text
某应用：跨进程焦点通知 → WPF Focus / PreviewGotKeyboardFocus
  → WPF TrySetFocus
  → win32u!NtUserSetFocus
  → user32!FocusSetIMCContext
  → 输入法 ImeSetActiveContext
  → ImeModuleRuntime.UpdateActiveContext
  → ImeHostBridge.UpdateInputTarget
  → Task.InternalWait / ManualResetEventSlim.Wait
  → CoWaitForMultipleHandles
```

[`ImeSetActiveContextManaged`](../src/XiaoXiIme.ImeModule/Exports/ImeExports.cs#L380) 通过 [`UpdateActiveContext`](../src/XiaoXiIme.ImeModule/Runtime/ImeModuleRuntime.cs#L212) 更新输入目标，随后在 [`UpdateInputTarget`](../src/XiaoXiIme.ImeModule/Runtime/ImeHostBridge.cs#L65) 中用 `GetAwaiter().GetResult()` 等待异步 IPC。`ConfigureAwait(false)` 不会让发起同步等待的 UI 线程立即返回。即使 IPC 有超时预算，也不能消除焦点回调对宿主应答的同步依赖。

在捕获转储时，课件 UI 线程不是处于操作系统挂起状态，而是在上述回调中等待。回调未返回时，外层 `SetFocus` 和当前 WPF Dispatcher 操作都未完成。STA 等待可能仍允许部分同步消息被处理，这与现场同步探测成功并不矛盾。

## 其他路径与证据边界

- 产品源码未发现主动挂起其他进程或线程的调用。代码中的 `IPC suspended` 指通信熔断，不是操作系统进程挂起。
- 输入法也有通过专用消息窗口投递按键结果的[异步回写路径](../src/XiaoXiIme.ImeModule/Runtime/AsyncImmInput.cs#L40)。采样时未在故障进程中找到这种窗口，也没有对应的回写失败日志；不能认定它是消息积压来源。
- 宿主的独立 IPC 就绪探测成功，只说明探测当时宿主可用，不能证明焦点更新请求已完成或推算它的延迟。
- 用户态转储不包含 Windows 内核中的完整消息队列。报告不对积压消息的类型、生产者或等待持续时间作无证据的推断。

## 修复方向与验收

1. 在调用线程上读取并保存 HWND/HIMC 等必要状态，完成本地更新后立即返回；把宿主通知交给受控的异步发送路径，不在 IMM32 焦点回调中等待应答。
2. 保留激活、失焦和销毁通知的顺序及会话生命周期语义，防止旧通知覆盖新焦点；避免快速切换时产生无界后台任务。
3. 验证宿主慢响应或不可用时焦点回调仍能及时返回；快速切换后，宿主的最终焦点目标应正确。
4. 在沙箱实际验证点击、翻页和中文输入，同时检查目标 UI 线程接受异步投递。不能用 IPC 就绪或者同步空消息成功替代交互验收。

## 证据保留范围

本文不附原始转储、截图、日志或其路径。原始材料可能包含可识别的应用信息、文档内容和会话数据，应只在受控环境中查看。
