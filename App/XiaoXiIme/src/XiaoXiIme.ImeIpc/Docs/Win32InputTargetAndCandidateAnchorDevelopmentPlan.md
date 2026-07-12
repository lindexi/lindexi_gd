# Win32 输入目标与候选窗口锚点开发计划

## 1. 目标

完成以下两个当前未实现的能力：

1. 从 `HIMC` 对应的 Win32 输入上下文中取得正确的目标窗口 `HWND`，用于：
   - 构造 `ImeToAsciiEx` 返回的 `TRANSMSG.Hwnd`；
   - 标识本次输入所属的窗口和输入会话；
   - 通过 IPC 向 Host/UI 提供必要的目标窗口上下文。
2. 从 Win32 输入上下文中取得候选窗口锚点，将有效的屏幕坐标写入 `ImeUiState.AnchorX/AnchorY`，使独立候选窗口能够跟随应用插入符或应用指定的候选位置。

本计划只描述开发方案，不包含代码修改。

## 2. 当前实现基线

### 2.1 输入目标 HWND

当前 `ImeToAsciiExManaged` 已接收 `HIMC inputContext`，但处理流程为：

1. 将 `virtualKey` 转换为 `ImeKey`。
2. 通过 IPC 获取 `ImeProcessResult`。
3. 把结果写回 `HIMC`。
4. 调用 `ImeTransMsgBuilder.BuildMessages(result)`。

`BuildMessages` 的 `hwnd` 参数有默认值，调用处未传入窗口句柄，因此所有生成的 `TRANSMSG.Hwnd` 当前均为 `0`。

项目中的 `InputContext` 已声明 `Hwnd` 和 `HWndIme`，但其结构布局尚未覆盖 Windows 原生 `INPUTCONTEXT` 的全部字段。在依赖该结构读取窗口句柄或候选位置前，必须先校验并补全原生布局，避免字段偏移错误。

### 2.2 候选窗口锚点

当前 `ImeUiState` 已包含：

- `AnchorX`
- `AnchorY`

但 `ImeUiState.FromSnapshot` 不接收锚点，默认返回 `(0, 0)`。Host 当前只维护一个全局 `ImeContext`，也没有保存 Win32 输入目标或候选位置。

Avalonia UI 已将 `ImeUiState.AnchorX/AnchorY` 映射到 `CandidateWindowViewState`，因此 UI 状态模型已经具备最小消费入口，但尚未定义：

- 坐标是否有效；
- 坐标系是客户区还是屏幕；
- 锚点代表插入符左上、左下还是排除矩形；
- 目标显示器和 DPI；
- 锚点不可用时的降级行为。

## 3. 设计原则

### 3.1 Win32 边界留在 ImeModule

`HWND`、`HIMC`、`CANDIDATEFORM`、`COMPOSITIONFORM`、客户区坐标和屏幕坐标都属于 Win32 平台细节，应由 `XiaoXiIme.ImeModule`/`XiaoXiIme.ImeInterop` 读取和归一化。

IPC 不应直接传输可供远端解引用的指针，也不应要求 Host 锁定 `HIMC`。Host/UI 只接收稳定的值类型快照。

### 3.2 IPC 使用结构化上下文

不要只给 `ImeProcessKeyRequest` 增加一个 `long Hwnd`。应建立结构化输入上下文，至少区分：

- 输入会话身份；
- 目标窗口句柄；
- 目标进程/线程；
- 候选锚点；
- 坐标有效性和来源。

这样可以为后续多窗口、多输入框、DPI 和会话隔离留出扩展空间。

### 3.3 IPC 统一使用屏幕物理像素坐标

建议跨进程传输时统一使用 Win32 屏幕坐标，并明确单位为物理像素：

- ImeModule 负责把客户区坐标转换为屏幕坐标；
- Host 不进行窗口坐标转换；
- UI 在最终定位窗口时，根据 Avalonia 当前使用的坐标单位和显示器缩放完成物理像素到 UI 坐标的转换。

不要仅传 `AnchorX/AnchorY` 而不标明坐标系，否则在非零窗口原点和多显示器环境中会产生位置错误。

### 3.4 HWND 只作为瞬时上下文

`HWND` 可以用于当前调用和诊断，但窗口句柄可能被系统复用。长期会话身份应由 ImeModule 生成的 `SessionId` 或由组合生命周期管理的标识承担。

## 4. 建议的数据模型

具体命名可在实现时按项目风格调整，但模型职责应保持稳定。

### 4.1 输入会话标识

新增不可变的 `ImeInputSessionId` 或等价值对象。

建议内容：

- `long Value` 或 `Guid Value`；
- JSON 可稳定序列化；
- 不包含进程内指针；
- 同一活动组合会话保持不变，组合结束后可释放；
- 目标窗口、线程或 `HIMC` 发生变化时创建新会话。

如果第一阶段不实现完整多会话 Host，可以先把会话标识加入契约并由 Host 原样关联，避免后续再次破坏 IPC 协议。

### 4.2 Win32 输入目标快照

新增 `ImeInputTarget` 或等价模型，建议包含：

- `ImeInputSessionId SessionId`
- `long WindowHandle`
- `int ProcessId`
- `int ThreadId`
- `bool IsWindowValid`

说明：

- `WindowHandle` 使用固定宽度整数进行 JSON 传输，不在 Host/UI 中转换为可操作指针，除非明确调用 Win32 API。
- `IsWindowValid` 表示采集时 `HWND != 0` 且通过 `IsWindow` 校验；它不是永久有效承诺。
- `ProcessId`、`ThreadId` 通过 `GetWindowThreadProcessId` 获取，用于诊断、会话区分和句柄复用防护。

### 4.3 候选锚点快照

新增 `ImeCandidateAnchor` 或等价模型，建议包含：

- `bool IsValid`
- `int X`
- `int Y`
- `ImeCoordinateSpace CoordinateSpace`
- `ImeCandidateAnchorKind Kind`
- `ImeCandidateAnchorSource Source`
- 可选 `ImeScreenRectangle ExclusionArea`

建议枚举：

- `ImeCoordinateSpace.ScreenPhysicalPixels`
- `ImeCandidateAnchorKind.CaretBottomLeft`
- `ImeCandidateAnchorKind.CandidatePosition`
- `ImeCandidateAnchorKind.ExclusionAreaBottomLeft`
- `ImeCandidateAnchorSource.CandidateForm`
- `ImeCandidateAnchorSource.CompositionForm`
- `ImeCandidateAnchorSource.Caret`
- `ImeCandidateAnchorSource.Fallback`

如果读取到 `CFS_EXCLUDE`，仅传一个点会损失信息，因此建议同时传排除矩形。UI 可优先显示在排除矩形下方，若屏幕空间不足则翻转到上方。

### 4.4 IPC 请求上下文

将 `ImeProcessKeyRequest` 从只有 `ImeKey` 扩展为至少包含：

- `ImeKey Key`
- `ImeInputTarget Target`
- `ImeCandidateAnchor CandidateAnchor`

为控制协议演进风险，可选择以下方式之一：

1. 直接扩展现有 record 构造参数，并同步更新所有调用和测试；
2. 新增 `ImeInputContextSnapshot Context`，把 Target、Anchor 和后续修饰键状态集中放入；
3. 新增 V2 路由，保留旧路由一段兼容期。

当前项目尚未发布稳定协议时，推荐第二种方式，避免 `ImeProcessKeyRequest` 后续持续膨胀。

### 4.5 UI 状态

将 `ImeUiState.AnchorX/AnchorY` 替换或补充为结构化 `CandidateAnchor`。

过渡方案：

- 保留 `AnchorX/AnchorY`，由 `CandidateAnchor` 派生；
- 新增 `AnchorIsValid`、`AnchorKind`、`CoordinateSpace`；
- UI 完成迁移后再考虑删除裸坐标字段。

若不需要兼容旧序列化数据，可以直接用 `ImeCandidateAnchor CandidateAnchor` 替代两个整数。

## 5. Win32 Interop 开发细节

### 5.1 校验并补全原生结构布局

在读取 `InputContext.Hwnd`、候选窗位置前，先对照当前目标 Windows SDK 的 `INPUTCONTEXT`、`CANDIDATEFORM`、`COMPOSITIONFORM`、`POINT`、`RECT`、`LOGFONTW` 定义。

需要完成：

1. 为所有结构显式使用 `StructLayout(LayoutKind.Sequential)`。
2. 使用与原生字段一致的类型和顺序。
3. 正确表达固定长度候选窗数组。
4. 检查 x64 下 `BOOL`、`DWORD`、句柄和指针字段的宽度及对齐。
5. 增加 `Unsafe.SizeOf<T>()`/`Marshal.OffsetOf<T>()` 测试，对关键字段偏移进行断言。
6. 确认 Native AOT 下结构布局与普通 JIT 一致。

特别注意：当前 `InputContext` 只声明了部分字段，直接把 `ImmLockIMC` 返回值解释为该结构存在偏移风险。此项应作为后续功能开发的前置条件，而不是在现有结构上直接追加读取逻辑。

### 5.2 扩展 Win32 API 声明

按最终采集方案增加必要的 P/Invoke：

- `IsWindow`
- `GetWindowThreadProcessId`
- `ClientToScreen`
- `GetGUIThreadInfo`
- 可选 `GetCaretPos`
- 可选 `ImmGetCandidateWindow`
- 可选 `ImmGetCompositionWindow`

优先从已锁定的原生 `INPUTCONTEXT` 读取 IME 内部数据；公共 IMM API 可作为校验或降级路径。所有 P/Invoke 应：

- 使用精确入口名；
- 指定返回值布尔封送；
- 不在 Host/UI 层重复声明；
- 用可替换的访问器接口封装，便于单元测试。

### 5.3 扩展上下文访问器

避免让 `ImeExports` 直接散落 Win32 调用。建议新增：

- `IImeInputContextReader`
- `ImeInputContextReader`
- `IWin32WindowContextAccessor`

职责划分：

- `ImeInputContextReader`：锁定 `HIMC`，读取目标 `HWND`、候选窗/组合窗信息。
- `IWin32WindowContextAccessor`：校验窗口、查询 PID/TID、执行坐标转换和读取 caret。
- 现有 `IImmContextAccessor`：继续负责 IMM 锁定、解锁、内存调整和消息生成。

读取失败必须返回无效快照，而不是抛出异常穿过非托管导出边界。

## 6. 目标 HWND 实现方案

### 6.1 读取流程

在 `ImeToAsciiExManaged` 中，处理按键前读取一次输入上下文快照：

1. 校验 `inputContext != 0`。
2. 调用 `ImmLockIMC`。
3. 从正确布局的 `INPUTCONTEXT.hWnd` 读取应用目标窗口。
4. 在解锁前复制所有需要的数据，禁止保存原生结构指针。
5. 调用 `IsWindow` 验证句柄。
6. 调用 `GetWindowThreadProcessId` 获取线程和进程标识。
7. 生成或解析当前 `SessionId`。
8. 在 `finally` 中调用 `ImmUnlockIMC`。

`hWnd` 为 `0`、窗口已失效或读取失败时：

- IPC 仍可发送按键；
- Target 标记为无效；
- `TRANSMSG.Hwnd` 保持 `0`；
- 不因位置采集失败丢失输入功能。

### 6.2 写入 TRANSMSG

调整 `ImeToAsciiExManaged` 的流程，使 `ImeTransMsgBuilder.BuildMessages` 接收读取到的目标 `HWnd`。

所有同一次 `ImeToAsciiEx` 返回的消息应使用相同目标：

- `WM_IME_STARTCOMPOSITION`
- `WM_IME_COMPOSITION`
- `WM_IME_ENDCOMPOSITION`
- 后续新增的 `WM_IME_NOTIFY`

测试应覆盖提交、组合中、结束组合三条分支，确保每条消息的 `Hwnd` 一致。

### 6.3 与 HIMC 写回的一致性

读取 Target 和写入 composition context 应针对同一个 `HIMC`。不要仅依赖 IPC 响应中返回的窗口句柄覆盖当前 Win32 调用的目标，因为 IPC 响应可能延迟，且窗口可能在请求期间切换。

建议规则：

- Win32 写回和 `TRANSMSG.Hwnd` 使用本次调用现场读取的 Target；
- IPC 返回只负责输入法语义结果；
- Host 不允许修改 Win32 目标窗口。

## 7. 候选窗口锚点实现方案

### 7.1 位置来源优先级

建议按以下优先级采集：

1. 应用在输入上下文中指定的候选窗位置 `CANDIDATEFORM`。
2. 应用指定的组合窗位置 `COMPOSITIONFORM`。
3. 目标 GUI 线程当前 caret 矩形。
4. 目标窗口客户区中的安全默认点。
5. 无效锚点。

必须记录 `Source`，便于 UI 选择定位策略和排查应用兼容问题。

### 7.2 CANDIDATEFORM 处理

读取候选窗索引 `0` 作为当前第一候选列表的位置。根据样式处理：

- `CFS_CANDIDATEPOS`：使用 `ptCurrentPos`。
- `CFS_EXCLUDE`：优先使用 `rcArea` 作为排除区域，锚点取排除区域左下角。
- 其他不支持或未初始化样式：进入下一降级来源。

候选窗坐标通常需要结合目标窗口客户区解释。实现时必须通过 Windows SDK 文档和实际测试确认每种样式的坐标空间，再统一调用 `ClientToScreen` 转为屏幕物理像素。

不要把尚未确认坐标系的原始值直接写入 `AnchorX/AnchorY`。

### 7.3 COMPOSITIONFORM 处理

若候选窗信息无效，读取组合窗位置：

- `CFS_POINT`/`CFS_FORCE_POSITION`：使用当前点；
- `CFS_RECT`：使用矩形左下角或按排除区域策略处理；
- 未初始化或不支持样式：继续降级。

组合窗位置用于候选窗口跟随时，应将 `Kind` 标记为组合位置来源，避免 UI 将其误认为精确 caret 矩形。

### 7.4 Caret 降级

当输入上下文没有有效候选/组合位置时：

1. 使用 Target 的线程 ID 调用 `GetGUIThreadInfo`。
2. 读取当前 caret 所属 `hwndCaret` 和 `rcCaret`。
3. 若 `hwndCaret` 有效，将 caret 客户区矩形转换到屏幕坐标。
4. 锚点使用 caret 左下角，使候选窗出现在文本插入位置下方。
5. 如果 caret HWND 与 Target 不同，保留实际 caret HWND 作为诊断信息，或至少记录来源。

不建议跨线程直接依赖全局 `GetCaretPos` 作为唯一来源，因为它对调用线程和 GUI 线程状态有约束。

### 7.5 坐标转换与 DPI

ImeModule 输出屏幕物理像素后，UI 定位时需要：

1. 根据锚点所在显示器选择 Avalonia `Screen`。
2. 将物理像素转换为 Avalonia 使用的坐标单位。
3. 对候选窗尺寸执行工作区边界约束。
4. 下方空间不足时显示在排除矩形或 caret 上方。
5. 多显示器存在负坐标时不得把坐标钳制到 `0`。
6. 跨显示器或 DPI 变化后重新计算，不缓存永久转换结果。

如果 Avalonia 窗口定位 API 本身接受物理像素，应在实现前通过一个最小验证测试确认，避免重复缩放。

## 8. IPC 与 Host 开发细节

### 8.1 扩展序列化上下文

所有新增 DTO 和枚举必须加入 `XiaoXiImeIpcJsonSerializerContext`，确保 Native AOT 下可序列化。

新增测试应覆盖：

- 有效 HWND 和 PID/TID；
- 无效 Target；
- 有效候选点；
- `CFS_EXCLUDE` 对应的排除矩形；
- 负屏幕坐标；
- `SessionId` 往返一致。

### 8.2 Host 状态管理

第一阶段最小实现可以让 Host 在处理 `ProcessKey` 时保存最近一次上下文，并在 `GetUiState` 时把锚点合并进 `ImeUiState`。

但为了避免窗口 A 的候选状态被窗口 B 使用，推荐直接按 `SessionId` 隔离：

- `Dictionary<ImeInputSessionId, ImeContext>` 保存每个会话的输入状态；
- 同时保存每个会话最近一次 Target 和 Anchor；
- `ProcessKey` 按 SessionId 选择上下文；
- `GetSnapshot`/`GetUiState` 明确请求哪个 SessionId；
- 会话结束、窗口失效或超时后清理。

如果本次范围只完成两个现状而暂不引入多会话，应在代码中建立单独的 `ImeInputContextSnapshot` 保存点，并添加明确的后续任务，避免将 HWND/Anchor 塞入 `ImeContext` 的词典转换逻辑。

### 8.3 UI 状态生成

`ImeUiState.FromSnapshot` 当前只接收 Snapshot。建议新增重载或由 Host 专门组装：

- Snapshot 决定 composition、candidate、guideline 和可见性；
- InputContextSnapshot 决定 Target 和 CandidateAnchor；
- 锚点无效时 `CandidateWindowVisible` 是否仍为 true，应与 UI 降级策略一致。

建议候选内容可见但锚点无效时：

- 状态仍标记候选内容存在；
- UI 不在 `(0,0)` 显示；
- UI 保持上一次同 SessionId 的有效位置，或暂时隐藏并等待下一次有效位置。

## 9. Avalonia UI 开发细节

### 9.1 Mapper

更新 `CandidateWindowStateMapper`：

- 映射结构化 Anchor；
- 保留 `IsAnchorValid`；
- 不把无效锚点转换成 `(0,0)`；
- 保留排除矩形和 AnchorKind，供窗口摆放算法使用。

### 9.2 Controller

`CandidateWindowController` 应按 SessionId 维护最近有效锚点，至少满足：

- 同一会话锚点短暂读取失败时可继续使用最近值；
- 会话改变时不得沿用旧窗口位置；
- composition 结束时清除缓存；
- Target 失效时隐藏或等待状态恢复。

### 9.3 窗口定位

在实际 Avalonia Window 接入时实现：

1. 先测量候选窗期望尺寸。
2. 选取锚点所在显示器的工作区。
3. 默认放在锚点或排除矩形下方。
4. 越过底部时翻转到上方。
5. 越过左右边界时仅做必要平移。
6. 保证窗口不激活、不抢占目标应用焦点。
7. Anchor 或候选内容变化时在 UI 线程更新位置。

当前项目中的 `CandidateWindowController` 只维护 ViewState，尚未包含实际窗口对象；如果窗口宿主位于其他项目，应在实施前先定位真实窗口创建和显示入口，再把定位职责放在最接近窗口平台 API 的层。

## 10. 消息与生命周期

### 10.1 组合生命周期

建议定义：

- 首次进入 `IsComposing=true`：创建或激活 SessionId。
- 组合中：持续更新同一 SessionId 的 Target 和 Anchor。
- `CommitText != null`：提交后结束会话。
- Escape/清空组合：结束会话。
- `ImeSelect(..., select=0)`、目标窗口销毁或 Host 超时：清理会话。

### 10.2 SnapshotChanged

完成锚点传播后，建议同步实现现有 `XiaoXiIme.SnapshotChanged` 通知，否则 UI 只能轮询 `GetUiState`。

通知内容应包含：

- SessionId；
- Snapshot；
- Target；
- CandidateAnchor；
- 单调递增版本号或时间戳。

UI 应丢弃旧版本通知，避免 IPC 时序变化导致候选窗回跳。

如果本次严格限定为两个现状，可把通知实现拆成后续里程碑，但 IPC DTO 应避免阻碍主动推送。

## 11. 错误处理与降级

所有 Win32 读取都必须是“增强信息”，不能阻断按键输入主链路。

### 11.1 读取 HWND 失败

- Target 标记无效；
- `TRANSMSG.Hwnd = 0`；
- 继续写回当前 `HIMC`；
- 记录可诊断但限频的日志。

### 11.2 坐标读取失败

- Anchor 标记无效；
- 不发送伪造的 `(0,0)` 有效坐标；
- UI 使用同 SessionId 最近有效坐标或隐藏；
- 下一个按键重新尝试采集。

### 11.3 IPC 失败

现有 fallback `ImeContext` 仍应工作。ImeModule 应使用本地采集到的 Target 构造 `TRANSMSG`，因为 Target 不依赖 Host 返回。

### 11.4 窗口在请求期间销毁

- 写 `TRANSMSG` 前可选择再次调用 `IsWindow`；
- 即使句柄失效也不得向其他新窗口主动发送消息；
- 当前实现只是填充 Win32 返回缓冲区，不应额外调用 `SendMessage`/`PostMessage`；
- SessionId 与 PID/TID 一起用于降低 HWND 复用导致的状态串用风险。

## 12. 测试计划

### 12.1 ImeInterop 布局测试

新增测试验证：

- `INPUTCONTEXT` 总大小；
- `hWnd`、候选窗数组、组合窗、IME 窗口句柄等关键字段偏移；
- `CANDIDATEFORM`、`COMPOSITIONFORM`、`POINT`、`RECT` 大小；
- x64 目标下布局与 Windows SDK 定义一致。

布局数值应来源于对应 SDK/本机原生探针，不应凭手工猜测。

### 12.2 ImeModule 单元测试

使用假的上下文访问器覆盖：

1. 从有效 `INPUTCONTEXT` 读取 Target HWND。
2. HWND 无效时返回无效 Target。
3. PID/TID 正确写入快照。
4. `ImeToAsciiExManaged` 为所有 `TRANSMSG` 写入目标 HWND。
5. 提交文本、组合中、结束组合三条消息分支。
6. `CFS_CANDIDATEPOS` 转换为屏幕坐标。
7. `CFS_EXCLUDE` 保留矩形并使用左下锚点。
8. CandidateForm 无效时降级到 CompositionForm。
9. Candidate/Composition 均无效时降级到 caret。
10. `ClientToScreen` 失败时 Anchor 无效。
11. 负坐标保持不变。
12. 任一 Win32 API 抛出或失败时，非托管导出不抛异常。

### 12.3 IPC 测试

扩展 `XiaoXiImeIpcTests`：

- 新 DTO 的 AOT JSON 往返；
- `ImeProcessKeyRequest` 包含完整 Context；
- `ImeUiStateResponse` 保留 Anchor 有效性、来源、矩形和负坐标；
- 新旧默认值不会把缺失锚点误判为有效 `(0,0)`；
- 路由名称保持稳定，或为 V2 路由增加稳定性断言。

### 12.4 Host 测试

覆盖：

- ProcessKey 保存最新 Target/Anchor；
- GetUiState 返回同一会话的 Anchor；
- 两个 SessionId 不串用状态；
- 旧版本位置更新不覆盖新版本；
- composition 结束后清理状态；
- 无效 Anchor 不覆盖同会话最近有效 Anchor，若采用该策略。

### 12.5 UI 测试

覆盖：

- 有效锚点映射到 ViewState；
- 无效锚点不会显示在屏幕原点；
- 多显示器负坐标保留；
- 窗口越过底部时翻转；
- 左右边界约束；
- SessionId 改变后不复用旧锚点；
- 候选隐藏后清理位置缓存。

### 12.6 Windows 集成验证

单元测试不能替代真实 IMM 行为。增加手工或自动化集成验证矩阵：

- 记事本/标准 Edit 控件；
- RichEdit；
- WPF TextBox；
- WinForms TextBox；
- Avalonia TextBox；
- 浏览器输入框；
- 多显示器，包含副屏位于主屏左侧的负坐标；
- 100%、125%、150%、200% DPI；
- 窗口移动到不同 DPI 显示器；
- 中英文切换、提交、取消、候选翻页；
- 快速切换两个窗口输入；
- 目标窗口在 IPC 请求期间关闭。

验证时记录 Target HWND、PID/TID、原始 Candidate/CompositionForm、转换后 Anchor 和最终 UI 位置，便于判断偏差发生在哪一层。

## 13. 实施顺序

### 阶段一：原生结构与读取基础

1. 对照 Windows SDK 补全并验证原生结构布局。
2. 增加窗口、坐标和 caret 所需 P/Invoke。
3. 建立可替换的输入上下文读取器。
4. 完成 Target 和 Anchor 的纯读取单元测试。

交付条件：无需 IPC 和 UI，也能从测试输入上下文稳定生成结构化 Target/Anchor。

### 阶段二：TRANSMSG 目标窗口

1. 在 `ImeToAsciiExManaged` 读取本次 Target。
2. 将 Target HWND 传给 `ImeTransMsgBuilder`。
3. 验证所有消息分支的 `TransMsg.Hwnd`。
4. 保证读取失败时保持现有输入能力。

交付条件：真实或模拟 `HIMC` 中存在有效 HWND 时，所有返回消息携带同一目标 HWND。

### 阶段三：IPC 上下文传播

1. 新增 Session、Target、Anchor DTO。
2. 扩展 `ImeProcessKeyRequest`。
3. 更新 AOT JSON 上下文。
4. 更新 Client、Server、Bridge 和 Host handler。
5. 增加 IPC 往返测试。

交付条件：Host 可获得与本次按键对应的 Target 和屏幕坐标 Anchor。

### 阶段四：Host 会话与 UI 状态

1. 保存每个会话最近的 Target/Anchor。
2. 将 Anchor 合并进 `ImeUiState`。
3. 明确无效位置和最近有效位置策略。
4. 如纳入本次范围，实现按 SessionId 隔离 `ImeContext`。

交付条件：`GetUiState` 返回非默认、可验证来源的候选窗锚点。

### 阶段五：Avalonia 候选窗定位

1. 扩展 ViewState 和 Mapper。
2. 在 Controller 中处理会话和最近有效锚点。
3. 接入实际窗口定位入口。
4. 完成 DPI、多显示器和工作区边界处理。

交付条件：候选窗口稳定显示在目标插入点附近，不出现在 `(0,0)`，不抢焦点。

### 阶段六：集成验证与文档

1. 运行 ImeModule、ImeIpc、Host 和 UI 测试。
2. 进行 Windows 应用兼容矩阵验证。
3. 更新 ImeIpc、ImeModule、ImeInterop 和 UI README。
4. 记录坐标单位、来源优先级、降级策略和协议版本。

交付条件：自动测试通过，真实应用验证结果可复现，文档与实现一致。

## 14. 预计涉及的文件

以下为预计范围，实施前应再次确认实际依赖关系。

### XiaoXiIme.ImeInterop

- `Win32Types.cs`：补全原生结构，增加 Point/Rect/CandidateForm/CompositionForm。
- `ImeConstants.cs`：增加 Candidate/Composition Form 样式常量。
- 可新增 Win32 API 文件：窗口验证、线程信息、坐标转换和 caret API。

### XiaoXiIme.Foundation

- 新增 Session、Target、Anchor、坐标空间和矩形模型。
- `ImeUiState.cs`：接入结构化锚点。

### XiaoXiIme.ImeModule

- `ImeExports.cs`：读取输入上下文并向消息构造器传 HWND。
- `ImeTransMsgBuilder.cs`：强制或显式接收目标 HWND。
- `ImmContextAccessor.cs`：按职责扩展或配合新的 reader/accessor。
- 可新增 `ImeInputContextReader.cs`：封装 Target/Anchor 采集。
- `ImeHostBridge.cs`：传递扩展后的 ProcessKey 请求上下文。

### XiaoXiIme.ImeIpc

- `ImeProcessKeyRequest.cs`：增加输入上下文快照。
- `XiaoXiImeIpcClient.cs`、`XiaoXiImeIpcServer.cs`：更新 handler 签名。
- `XiaoXiImeIpcJsonSerializerContext.cs`：注册新增 DTO。
- 如选择版本化协议，更新 `XiaoXiImeIpcRoutes.cs`。

### XiaoXiIme.ImeHost

- `ImeHostService.cs`：保存并返回会话 Target/Anchor；必要时按 Session 隔离 ImeContext。

### XiaoXiIme.ImeUi.Avalonia

- `CandidateWindowViewState.cs`：表达锚点有效性和排除矩形。
- `CandidateWindowStateMapper.cs`：映射结构化 Anchor。
- `CandidateWindowController.cs`：管理会话位置缓存。
- 实际候选 Window 文件：完成屏幕定位、翻转和 DPI 处理；当前项目文件列表中尚未发现该窗口宿主，需要实施前定位。

### Tests

- `XiaoXiIme.ImeModule.Tests/ImeExportsTests.cs`
- 可拆分新的 `ImeInputContextReaderTests.cs`
- `XiaoXiIme.ImeIpc.Tests/XiaoXiImeIpcTests.cs`
- Host/Core/UI 对应测试项目；若目前缺少 UI 测试项目，需要创建或在现有合适项目中补充。

## 15. 风险与待确认事项

1. **原生结构布局风险**：当前 `InputContext` 不完整，这是最高优先级风险。布局未验证前不能安全读取后续字段。
2. **坐标语义风险**：CandidateForm/CompositionForm 不同样式的坐标解释必须以 Windows SDK 和真实应用验证为准。
3. **DPI 风险**：Win32 物理像素与 Avalonia 坐标单位之间可能存在缩放差异，需要最小实验确认。
4. **多会话风险**：Host 当前只有一个 `ImeContext`，完成 HWND/Anchor 后会更容易暴露跨窗口状态串用问题。
5. **IPC 同步延迟**：`ImeToAsciiEx` 当前同步等待 IPC；位置数据必须视为本次按键采集快照，不能假设响应返回时窗口仍然有效。
6. **实际 UI 宿主缺失**：当前 Avalonia 项目只发现状态映射和控制器，未发现真实窗口显示代码。实施 UI 定位前需确认窗口宿主是否尚未开发或位于其他项目。
7. **兼容协议选择**：需要确认当前 IPC 是否已有外部消费者。如果已有，应使用可选字段或 V2 路由；如果没有，可直接演进现有 DTO。

## 16. 验收标准

### HWND

- 有效 `HIMC` 关联目标窗口时，所有本次返回的 `TRANSMSG.Hwnd` 等于该目标 `HWND`。
- 无效或缺失 HWND 时不抛异常、不破坏 composition/result 写回，消息 HWND 为 `0`。
- IPC 中可观察到 Target HWND、PID、TID 和有效性。
- 快速切换窗口时不会把窗口 A 的状态或候选位置发送给窗口 B。

### 候选锚点

- 标准 Win32、WPF、WinForms、Avalonia 和浏览器输入框中，UI 能获得非默认且来源明确的屏幕坐标。
- 无有效坐标时不会把候选窗显示在 `(0,0)`。
- 多显示器负坐标、不同 DPI 和窗口跨屏移动时位置正确。
- `CFS_EXCLUDE` 场景下候选窗不覆盖应用指定的排除区域。
- 组合结束或会话切换后不复用错误的旧位置。

### 工程质量

- 新增 DTO 全部通过 AOT JSON 往返测试。
- 原生结构大小和关键字段偏移有自动测试保护。
- ImeModule、ImeIpc、Host 和 UI 相关测试全部通过。
- 非托管导出边界不传播托管异常。
- README 明确记录坐标系、单位、来源优先级、降级策略和协议版本。

## 17. 参考资料

- [Input Context](https://learn.microsoft.com/windows/win32/intl/input-context)
- [ImmGetCandidateWindow](https://learn.microsoft.com/windows/win32/api/imm/nf-imm-immgetcandidatewindow)
- [IME Messages](https://learn.microsoft.com/windows/win32/intl/ime-messages)
- [Status, Composition, and Candidates Windows](https://learn.microsoft.com/windows/win32/intl/status--composition--and-candidates-windows)
