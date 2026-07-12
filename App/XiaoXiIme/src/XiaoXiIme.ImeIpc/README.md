# XiaoXiIme.ImeIpc

小希输入法 IPC 封装项目。

职责：基于 DotnetCampus.Ipc 封装 IME 模块、Host 和 UI 之间的通信，处理超时、重连、健康检查和测试替身。

## 数据流概览

当前主链路如下：

1. 用户在记事本、浏览器等 Win32 应用中按键后，Windows 的 IMM32/USER32 输入法基础设施在目标应用的线程中调用 IME 模块导出的 `ImeProcessKey`，询问当前按键是否应由输入法处理。
2. 如果按键需要由 IME 处理，Windows 随后调用 `ImeToAsciiEx`，传入虚拟键、扫描码、键盘状态、`HIMC` 和用于返回 `TRANSMSG` 的缓冲区。
3. `XiaoXiIme.ImeModule` 将 Win32 按键归一化为 `ImeKey`，通过本项目的 `ProcessKey` IPC 请求发送给 Host。
4. Host 中的 `ImeContext` 返回 `ImeProcessResult`：是否已处理、最新会话快照，以及可选的上屏文本。
5. IME 模块把会话快照写入 Win32 的 `COMPOSITIONSTRING`、`CANDIDATEINFO`、`GUIDELINE` 和私有数据；如果有上屏文本，则写入 `ResultStr`。
6. IME 模块同时返回 `WM_IME_STARTCOMPOSITION`、`WM_IME_COMPOSITION`、`WM_IME_ENDCOMPOSITION` 等 `TRANSMSG`，通知 Win32 应用读取相应的 IME 上下文数据。

因此，IPC 返回的不是直接投递给目标窗口的一条 Win32 消息，也不应只设计成一个字符串。IPC 返回的是与平台无关的结构化处理结果，IME 模块负责把它翻译成 Win32 IMM 数据结构和窗口消息。

## Win32 调用主体与同步时序

本文所说的“Win32 调用”主要是指 Windows 的 IMM32/USER32 输入法基础设施调用 IME DLL 导出函数，而不是记事本等应用的业务代码直接调用这些函数。宏观上可以说“记事本调用了输入法”，但更准确的过程是：用户在记事本编辑框中输入，Windows 代表当前窗口线程调用已经加载的 IME DLL，最终再由记事本窗口接收输入法消息并读取输入上下文。

调用关系可以简化为：

```text
用户按键
  -> 目标应用的窗口线程和消息循环
  -> Windows IMM32/USER32 输入法基础设施
  -> XiaoXiIme IME DLL
  -> XiaoXiIme Host（通过 IPC）
  -> IME DLL 更新 HIMC 并返回 TRANSMSG
  -> 目标应用读取组合文本或最终结果
```

### `ImeProcessKey` 与 `ImeToAsciiEx` 的顺序

从 Win32 导出函数的边界看，当前调用流程是同步的：

1. Windows 调用 `ImeProcessKey`。
2. `ImeProcessKey` 在 IME 模块内快速判断该键是否应由输入法处理，并同步返回 `0` 或 `1`。
3. 当前实现的 `ImeProcessKey` 不发送 IPC，也不会取得 `ImeProcessResult`。
4. 对于需要 IME 处理的按键，Windows 随后调用 `ImeToAsciiEx`。
5. `ImeToAsciiEx` 将按键转换为 `ImeKey`，通过 IPC 请求 Host 处理，然后等待 `ImeProcessResult`。
6. IME 模块取得结果后更新 `HIMC`，向 `transKey` 写入 `TRANSMSG`，最后同步返回消息数量。

因此，Windows 不会直接收到 `ImeProcessResult`。`ImeProcessResult` 是 IME 模块与 Host 之间的内部托管模型，最终需要转换为 `HIMC` 中的 IMM 数据以及 `WM_IME_*` 对应的 `TRANSMSG`。

### 异步 IPC 如何适配同步 Win32 API

IPC 客户端公开的是异步的 `ProcessKeyAsync`，但 `ImeToAsciiEx` 是同步 Win32 导出函数，不能返回 `Task`。当前 `ImeHostBridge.ProcessKey` 使用 `GetAwaiter().GetResult()` 同步等待异步 IPC 完成：

```text
Windows 调用 ImeToAsciiEx
  -> IME DLL 发起异步 IPC 请求
  -> 当前调用线程同步阻塞等待
  -> Host 返回 ImeProcessResult
  -> IME DLL 写入 HIMC 和 TRANSMSG
  -> ImeToAsciiEx 返回
```

不能在 `ImeToAsciiEx` 中先返回、等 IPC 完成后再写原来的 `transKey`。函数返回后，该缓冲区的生命周期和所有权不再由 IME 控制，而且 Windows 要求本次按键对应的数据和消息在函数返回前准备完成。

### IPC 延迟与目标应用卡顿

由于调用通常发生在目标应用的窗口/UI 线程上，当前同步等待会把 IPC 延迟直接加入按键处理路径。如果 Host 响应慢、连接建立慢或请求等待超时，调用 `ImeToAsciiEx` 的线程会在此期间被阻塞，表现为当前应用输入延迟或界面短暂卡顿。

这里通常不是整个 Windows 系统卡住，而是调用 IME 的目标线程被阻塞。例如在记事本中输入时，记事本的窗口线程可能暂时不能继续处理窗口消息；其他进程和 Windows 系统通常不受影响。

当前客户端的连接超时、请求超时和重试只能限制最长等待时间，不能消除等待期间的阻塞。IPC 最终失败或超时后，`ImeHostBridge` 会捕获异常并使用进程内的 fallback `ImeContext` 处理该按键，但 fallback 是在等待失败之后才执行，不能避免此前已经发生的卡顿。

因此，当前架构可以概括为：

```text
同步 Win32 调用
  -> 异步 IPC
  -> 同步阻塞等待异步结果
  -> 成功时转换并返回
  -> 失败或超时时改用本地 fallback 后返回
```

按键处理属于延迟敏感路径。后续若要降低卡顿风险，应优先考虑将组合状态机、退格、提交、候选选择等必须同步完成的逻辑放在 IME 进程内，并通过本地缓存或预取提供基础候选；大词典查询、云候选、UI 展示、遥测和非关键状态同步等工作再通过异步 IPC 完成。如果暂时保留同步 IPC，应使用较短的超时预算，并避免在单次按键路径中进行耗时的多次重试。

## 从 Win32 层能够得到什么

### 按键入口参数

`ImeProcessKey` 当前可得到：

- `HIMC inputContext`：当前输入上下文句柄。
- `virtualKey`：当前虚拟键码。这里代表本次进入 IME 的一个键，不是完整的历史按键序列。
- `keyData`：Win32 提供的按键附加数据。
- `keyState`：键盘状态数组指针。

`ImeToAsciiEx` 当前可得到：

- `virtualKey`：当前虚拟键码。
- `scanCode`：扫描码；当前实现把它作为 `modifiers` 参数向内部传递，但按键翻译尚未使用该值。
- `keyState`：键盘状态数组指针；当前实现尚未读取。
- `transKey`：用于写入 `TRANSMSGLIST` 的 Win32 缓冲区。
- `state`：Win32 传入的状态值；当前实现尚未使用。
- `HIMC inputContext`：当前输入上下文句柄。

当前 IPC 不发送原始虚拟键、扫描码或完整 `keyState`，而是先转换为 `ImeKey`。字母键转换为单个小写字符；退格、空格、回车、Esc、方向键、翻页键、Home/End 和数字候选选择会转换为对应的语义按键。

“已经打入的键”分成两层：

- 本次物理/虚拟按键：由当前 `virtualKey` 表示，每次调用只有一个。
- 当前累计输入串：由 Host 维护在 `ImeSessionSnapshot.Composition.Reading` 中，例如连续输入 `n`、`i` 后得到 `"ni"`。它不是从 Win32 的按键历史直接读取出来的。

### 接收输入的 HWND

锁定 `HIMC` 后，`InputContext` 中已有两个窗口句柄字段：

- `InputContext.Hwnd`：与该输入上下文关联的应用窗口，是当前最接近“接收输入的 HWND”的字段。
- `InputContext.HWndIme`：IME 窗口句柄，不等同于接收最终文本的应用窗口。

当前实现已经声明这些字段，但存在以下限制：

- 没有把 `Hwnd` 或 `HWndIme` 放入任何 IPC 请求。
- `ImeToAsciiEx` 构造 `TRANSMSG` 时没有读取 `InputContext.Hwnd`，消息中的 `Hwnd` 当前为默认值 `0`。

如果 Host 或独立 UI 需要区分多个应用窗口、输入框或输入会话，现有 `ImeProcessKeyRequest(ImeKey Key)` 不够，需要增加会话标识和目标窗口上下文。不要只依赖裸 `HWND` 作为跨进程长期身份，因为窗口句柄可能被复用。

### 候选窗口的位置

当前代码中没有从 Win32 读取候选窗口坐标，也没有通过 IPC 发送真实锚点位置。

现有 `ImeUiState` 虽然有 `AnchorX`、`AnchorY` 字段，但 `ImeUiState.FromSnapshot` 使用默认值 `0, 0`。这两个字段当前只是预留模型，不代表已经获得了候选窗口位置。

`InputContext` 当前定义的字段也不包含候选窗位置结构。若要让独立 UI 跟随光标，需要在 Win32/IME 模块侧补充位置采集，例如读取 IMM 候选窗/组合窗设置，或从目标窗口获得插入符、文本服务位置，然后把屏幕坐标、目标窗口和会话标识作为结构化上下文传给 Host/UI。

## IPC 消息设计

IPC 使用 JSON 直接路由，请求和响应均为强类型对象；JSON 属性使用 camelCase。

### IME 模块发往 Host 的请求

| 路由 | 请求 | 响应 | 作用 |
| --- | --- | --- | --- |
| `XiaoXiIme.ProcessKey` | `ImeProcessKeyRequest` | `ImeProcessKeyResponse` | 发送一个语义化 `ImeKey`，由 Host 更新输入法状态并返回 `ImeProcessResult`。这是 Win32 按键处理主链路。 |
| `XiaoXiIme.GetSnapshot` | `ImeSnapshotRequest` | `ImeSnapshotResponse` | 获取当前完整 `ImeSessionSnapshot`，用于状态恢复、查询或测试。 |
| `XiaoXiIme.GetUiState` | `ImeUiStateRequest` | `ImeUiStateResponse` | 获取适合候选 UI 展示的状态。默认由 Snapshot 转换而来。 |
| `XiaoXiIme.GetHostStatus` | `ImeHostStatusRequest` | `ImeHostStatusResponse` | 健康检查，返回 Host 是否运行及最近错误。 |

### Host/UI 方向的通知

| 路由 | 消息 | 作用 | 当前状态 |
| --- | --- | --- | --- |
| `XiaoXiIme.SnapshotChanged` | `ImeSnapshotChangedNotification` | 计划在输入状态变化时主动推送最新 Snapshot，避免 UI 轮询。 | 已定义路由和消息类型，但当前 Client/Server 尚未注册发送或订阅逻辑。 |

### `ImeKey` 的内容

`ImeProcessKeyRequest` 不是字符串请求，而是：

- `Kind`：按键语义，例如字符、退格、提交、移动候选、移动组合光标、选择候选。
- `Character`：仅字符键使用。
- `CandidateIndex`：仅候选选择使用，表示当前页内索引。

这种格式可以避免 Host 依赖 Win32 虚拟键常量，但当前会丢失原始按键上下文，例如 Shift/Ctrl/Alt、按下/抬起、重复次数、原始扫描码和目标输入窗口。

## IPC 应返回什么

`ProcessKey` 当前返回 `ImeProcessResult`，包含三部分：

### `Handled`

表示该键是否被输入法消费。

- `false`：IME 不处理，Win32/应用可继续按普通按键处理。
- `true`：IME 已处理，模块应更新输入上下文并生成相应 IME 消息。

### `Snapshot`

这是处理后的完整输入法状态，不是单一字符串：

- `Composition.DisplayText`：当前展示的组合文本。
- `Composition.Reading`：当前累计输入串/读音串。
- `Composition.CaretIndex`：组合串内光标位置。
- `Candidates`：候选项列表。
- `CandidateWindow`：当前选择项、页起点和页大小。
- `IsComposing`：是否处于组合输入状态。
- `Guideline`：提示、无候选或错误等辅助信息。

IME 模块将这些数据分别写入 Win32 的组合串、候选列表、提示信息和私有状态，而不是把整个 Snapshot 当成文本上屏。

### `CommitText`

`CommitText` 是可选的最终上屏字符串。选择候选时它是候选文本，按回车提交读音时它是当前 reading。没有提交动作时为 `null`。

它只描述“最终要提交的文本”，不能替代 `Snapshot` 和 `Handled`：输入过程中的组合串、候选列表、光标位置和按键是否被消费仍需要结构化字段表达。

## 最终 Win32 接收什么

Win32 应用最终接收的是 IMM 输入上下文数据与 `WM_IME_*` 消息的组合，而不是直接接收 IPC JSON：

### 仍在组合输入

- 将 `Snapshot.Composition.DisplayText`、`Reading`、属性、分句和光标位置写入 `COMPOSITIONSTRING`。
- 将候选文本、选择项和分页写入 `CANDIDATEINFO/CANDIDATELIST`。
- 将提示写入 `GUIDELINE`，并写入小希输入法私有状态。
- 返回 `WM_IME_STARTCOMPOSITION`。
- 返回 `WM_IME_COMPOSITION`，`lParam` 标记 `GCS_COMPSTR`、`GCS_COMPREADSTR`、`GCS_CURSORPOS`、候选、提示和私有数据可读取。

### 提交最终文本

- 将 `CommitText` 以 UTF-16 写入 `COMPOSITIONSTRING.ResultStr`。
- 返回 `WM_IME_COMPOSITION`，`lParam` 包含 `GCS_RESULTSTR`。
- 返回 `WM_IME_ENDCOMPOSITION`。

`WM_IME_COMPOSITION.wParam` 当前只设置为 `CommitText` 的第一个 UTF-16 字符；完整文本必须从 `ResultStr` 读取。因此，多字符上屏内容仍能够通过 `ResultStr` 表达，不能把 `wParam` 当作完整结果。

### 取消或结束组合

- 清空 Win32 composition/candidate/guideline/private data。
- 返回 `WM_IME_ENDCOMPOSITION`。

## 当前实现边界与后续建议

当前 IPC 足以支持单一逻辑输入会话的按键处理和状态返回，但尚未完整表达 Win32 输入上下文。若要支持真实桌面环境中的多窗口、多输入框和独立候选 UI，建议后续扩展：

1. 为请求增加稳定的输入会话标识，并携带当前进程、线程和目标 `HWND` 等诊断上下文。
2. 增加候选窗锚点结构，至少包含屏幕坐标、坐标系、目标窗口和有效性。
3. 明确传递修饰键、按键动作和重复信息，而不是把 `scanCode` 当作未使用的 `modifiers`。
4. 实现 `SnapshotChanged` 的发布与订阅，让 UI 使用推送状态。
5. 在构造 `TRANSMSG` 时确认并填入正确的目标 `Hwnd`，同时保留 `HIMC` 作为 Win32 上下文写回边界。
6. 如果未来存在并发输入会话，Host 不应继续只维护一个全局 `ImeContext`，而应按会话隔离 composition 和候选状态。
