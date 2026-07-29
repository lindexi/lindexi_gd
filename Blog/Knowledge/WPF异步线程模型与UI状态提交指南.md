# WPF 异步线程模型与 UI 状态提交指南

本文给出 WPF 应用中 `async`/`await`、Dispatcher、绑定状态、命令、事件、后台任务和并发状态的统一设计方法。核心目标是：先确认调用链原本是否由 UI Dispatcher 进入；能够自然保留 UI 上下文时直接更新 UI 状态，不主动丢失上下文再补调度；只有真正从非 UI 线程提交线程亲和对象或 UI 可观察状态时，才调度最小同步变更，并确保任何可等待任务覆盖完整操作生命周期。

## 一、先区分四类问题

线程问题不能统一用 Dispatcher 解决。分析任何代码前，先判断它属于哪一类：

| 问题 | 典型对象或现象 | 正确工具 |
| --- | --- | --- |
| WPF 线程亲和性 | 控件、`DispatcherObject`、未冻结的 `Freezable`、WPF 渲染对象 | 优先遵守所属 `Dispatcher`；确需跨线程共享且适合长期只读复用时再评估 `Freeze()` |
| UI 可观察状态 | 绑定属性、`INotifyPropertyChanged`、`ObservableCollection`、`CanExecuteChanged` | 明确的 UI 状态提交边界 |
| 普通数据竞争 | 活动实例、会话代次、取消源、后台初始化状态、多个线程读写的字段 | `Interlocked`、`Volatile`、锁、不可变快照或串行队列 |
| 异步生命周期不完整 | 未观察任务、迟到通知、异常丢失、关闭后仍写状态 | 任务所有者、取消、异常观察、关闭等待 |

Dispatcher 只解决前两类问题，不自动解决数据竞争和任务所有权。

## 二、`await` 与线程的准确语义

### 2.1 `await` 不是线程切换 API

`await` 表示异步等待。等待完成后的续体可能：

- 恢复到捕获的 `SynchronizationContext`；
- 由当前 `TaskScheduler` 安排；
- 在线程池线程继续；
- 因 awaiter 已完成而同步继续。

因此，不能把“`await` 之前在哪个线程”当作“`await` 之后仍在哪个线程”的业务保证。

### 2.2 `ConfigureAwait(false)` 只表示不要求恢复上下文

`ConfigureAwait(false)`：

- 不保证切到后台线程；
- 不启动新线程；
- 不解除 WPF 对象的线程亲和性；
- 不让后续 UI 状态写入自动变得安全；
- 不应被当作通用性能开关。

类库可以不承诺恢复调用方上下文。WPF 命令或 UI 事件处理链若明确从 UI Dispatcher 进入，默认应使用普通 `await` 保留 UI 同步上下文，随后直接更新自己拥有的 UI 状态；不要先使用 `ConfigureAwait(false)` 主动丢失上下文，再用 Dispatcher 补回。只有方法契约允许任意线程调用、或状态确实由后台回调产生时，才显式提交到 UI Dispatcher。无论选择哪种策略，成功、取消、异常和 `finally` 必须一致。

### 2.3 `Task.Run` 只适合明确的 CPU 密集型工作

不要用 `Task.Run` 包装本来已经异步的网络、文件、数据库或模型调用。异步 I/O 不需要额外占用线程。只有确定为 CPU 密集型、可在线程池安全执行且测量表明会阻塞 UI 时，才考虑 `Task.Run`。

## 三、对象所有权决定提交边界

每份可观察状态只能有一个明确提交所有者。

假设：

- 渲染服务拥有 `LatestPreview`；
- ViewModel 拥有 `IsBusy`、`StatusText` 和命令状态。

那么渲染服务负责保证 `LatestPreview` 在自己的提交任务完成前已经更新；ViewModel 仍须单独提交 `IsBusy` 和 `StatusText`。下层对象完成 Dispatcher 提交，不会把调用方的后续续体“传送”到 UI 线程，也不会替调用方提交其他对象的状态。

### 3.1 状态所有权规则

1. 谁声明并维护状态，谁定义它的线程与完成契约。
2. 上层不重复调度下层已经拥有的状态提交。
3. 下层不越层修改上层 ViewModel 状态。
4. 多个需要被 UI 原子观察的相关值，在同一次同步提交中更新。
5. 跨线程读取的多个相关字段优先合并为单个不可变快照发布。

### 3.2 设计问题的固定顺序

遇到线程异常或通知错乱时，按以下顺序判断：

1. 被访问的对象是否具有 WPF 线程亲和性？
2. 若没有，它是否是 UI 正在观察的状态或集合？
3. 谁拥有这份状态？
4. 写入是否必须与其他状态形成一个不可分割的提交？
5. 当前 `Task` 是否在提交完成前就结束？
6. 是否存在多个线程并发读写普通字段？
7. 取消、释放或新会话开始后，旧回调是否仍可能提交？

只有前两问命中且当前执行上下文不属于目标 UI Dispatcher 时，才需要额外调度。若调用链本来就从目标 UI Dispatcher 进入并通过普通 `await` 保持上下文，应直接提交状态，不要重复调用 Dispatcher。第 5 至第 7 问分别需要完整任务生命周期、并发原语和迟到提交防护。

## 四、Dispatcher 只执行最小同步提交

### 4.1 合法的 Dispatcher 区域

当代码确实位于非 UI 线程时，Dispatcher 委托应短小、同步、不可被异步打断，只包含：

- 字段或绑定属性赋值；
- 必须跨线程提交的 `ObservableCollection` 最小增删改；
- 同步触发 `PropertyChanged` 或 `CanExecuteChanged`；
- 创建、访问或渲染必须位于所属 Dispatcher 的 WPF 对象；
- 将一组相关 UI 状态作为一个提交一起更新。

### 4.2 不应进入 Dispatcher 的工作

以下工作应在 Dispatcher 外等待：

- 网络、数据库、模型和 MCP 调用；
- 文件 I/O、图片编码和大对象序列化；
- XML/JSON 解析、业务校验和普通布局计算；
- 可在线程中立环境运行的渲染前处理；
- 等待外部异步操作。

错误示例：

```csharp
await dispatcher.InvokeAsync(async () =>
{
    var response = await client.SendAsync(request);
    var bytes = await File.ReadAllBytesAsync(path);
    StatusText = "完成";
});
```

这会扩大 UI 区域，并错误暗示整个异步委托始终属于 UI 线程。WPF 的泛型重载还可能返回 `DispatcherOperation<Task>`：第一次 `await` 只等待 Dispatcher 调用委托并取得内部 `Task`，不一定等待内部异步工作结束。若忽略内部任务，就会出现方法提前完成、迟到状态提交和异常无人观察。即使显式取得并再次等待内部任务，也不应把 I/O 放入 UI 区域；正确结构仍是先在 Dispatcher 外完成工作，再用一次同步提交更新 UI。

### 4.3 UI 调度抽象优先只接收 `Action`

面向 ViewModel 的调度接口应限制为同步提交：

```csharp
internal interface IUiDispatcher
{
    bool CheckAccess();

    Task InvokeAsync(Action action, CancellationToken cancellationToken = default);
}
```

不暴露 `Func<Task>`，避免调用方把完整业务流程放入 Dispatcher。如果底层框架 API 只能接受异步委托，适配层传入的委托应同步执行 `Action` 并返回 `Task.CompletedTask`；但适配层返回给调用方的任务仍必须等待该委托真正在 Dispatcher 上执行完成，不能在委托尚未执行时提前返回。

### 4.4 Dispatcher 初始化也必须异步可观察

没有 WPF `Application` 时，可以创建专用 STA 线程和 Dispatcher，但必须满足：

- 初始化通过 `TaskCompletionSource<Dispatcher>` 异步发布；
- 调用方在 `InvokeAsync` 中等待 Dispatcher 准备完成；
- `CheckAccess` 在尚未准备时直接返回 `false`，不能为了查询而阻塞；
- 初始化异常进入准备任务；
- 关闭过程可取消、可等待，不依赖 `.Result`、`.Wait()` 或 `GetAwaiter().GetResult()`。

## 五、`Task` 必须表示完整操作生命周期

一个可等待方法返回的 `Task`，应在以下阶段全部结束后才完成：

1. 业务工作结束；
2. 结果状态提交完成；
3. 失败或取消状态提交完成；
4. 必要清理完成；
5. 与本次操作相关的命令通知完成。

如果 `await` 返回后仍可能出现迟到的 `PropertyChanged`、集合写入、命令状态变化或异常，该方法的完成契约就是不完整的。

### 5.1 ViewModel 长操作优先保留 UI 上下文

WPF 命令或 UI 事件触发的方法通常已经位于 UI Dispatcher。此时普通 `await` 会恢复到捕获的 UI 同步上下文，方法可以直接完成状态更新，不需要额外 Dispatcher：

```csharp
private async Task RunOperationAsync(CancellationToken cancellationToken)
{
    OperationInput input = CaptureInputAndBegin();
    OperationOutcome outcome = OperationOutcome.Faulted();

    try
    {
        OperationResult result = await _service.RunAsync(
            input,
            cancellationToken);
        outcome = OperationOutcome.Succeeded(result);
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
        outcome = OperationOutcome.Canceled();
    }
    catch (ExpectedOperationException exception)
    {
        outcome = OperationOutcome.Failed(exception.Message);
    }
    finally
    {
        ApplyOutcomeAndComplete(outcome);
    }
}
```

阶段含义：

1. **UI 开始阶段**：检查条件、快照输入、设置忙碌状态、触发命令通知。
2. **异步等待阶段**：直接等待异步 I/O，不使用 `Task.Run`，也不主动丢失 UI 上下文。
3. **形成结果**：只写局部变量，统一表达成功、取消和失败。
4. **UI 结束阶段**：续体恢复到原 UI 上下文后，直接完成最终状态、忙碌清理和命令通知。

该模板的前置条件是：方法由目标 WPF UI Dispatcher 调入，调用链没有主动使用 `ConfigureAwait(false)` 丢失上下文。若方法明确允许从任意线程调用，则应把线程中立工作与 UI 状态提交分成不同职责，并只在真正跨线程的提交点使用 Dispatcher。跨线程的最终提交通常不应使用已经取消的业务令牌，否则取消可能阻止 UI 清理，导致 `IsBusy` 永远为 `true`。

### 5.2 不要把可观察状态散落在 `catch` 和 `finally`

错误示例：

```csharp
try
{
    await service.RunAsync().ConfigureAwait(false);
    await dispatcher.InvokeAsync(() => StatusText = "完成");
}
catch (OperationCanceledException)
{
    StatusText = "已取消";
}
finally
{
    IsBusy = false;
}
```

这段代码先用 `ConfigureAwait(false)` 主动丢失 UI 上下文，再只为成功路径补 Dispatcher；取消和清理路径则依赖偶然线程。若方法本来由 UI Dispatcher 调入，首选修复是移除 `ConfigureAwait(false)` 和多余的 Dispatcher，使用普通 `await`，再统一直接设置 `StatusText` 与 `IsBusy`。只有方法确实允许从任意线程调用时，才应保留显式 Dispatcher，并让成功、取消和清理共享同一个提交边界。

## 六、WPF 异步命令契约

`ICommand.Execute` 是同步 `void` 接口。WPF 通常从 UI 线程调用它，因此异步命令适配器应保持同步入口，只负责启动并观察一个可等待任务。

### 6.1 正确结构

```csharp
internal sealed class AsyncCommand : ICommand
{
    private readonly Func<CancellationToken, Task> _executeAsync;
    private readonly Action<Exception> _onException;
    private int _executionCount;

    public AsyncCommand(
        Func<CancellationToken, Task> executeAsync,
        Action<Exception> onException)
    {
        ArgumentNullException.ThrowIfNull(executeAsync);
        ArgumentNullException.ThrowIfNull(onException);
        _executeAsync = executeAsync;
        _onException = onException;
    }

    public event EventHandler? CanExecuteChanged;

    public Task? ExecutionTask { get; private set; }

    public bool CanExecute(object? parameter) => _executionCount == 0;

    public void Execute(object? parameter)
    {
        Task executionTask = ExecuteAsync(CancellationToken.None);
        Task observationTask = ObserveExecutionAsync(executionTask);
        ExecutionTask = observationTask;
        _ = observationTask;
    }

    internal async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _executionCount++;

        try
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            await _executeAsync(cancellationToken);
        }
        finally
        {
            _executionCount--;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private async Task ObserveExecutionAsync(Task executionTask)
    {
        await Task.WhenAny(executionTask);

        if (executionTask.Exception is { } exception)
        {
            _onException(exception.GetBaseException());
        }
    }
}
```

关键点：

- `Execute` 不是 `async void`；
- `ExecutionTask` 指向观察任务，覆盖命令执行、命令状态恢复和同步错误回调，便于测试和显式等待；
- 观察任务读取故障任务的 `Exception`，消费异常且不把取消当作失败；
- 命令内部普通 `await` 保留 WPF 命令入口的同步上下文；
- 命令不注入 Dispatcher，也不先 `ConfigureAwait(false)` 再调度通知；
- `ExecuteAsync` 不承诺把任意工作线程调用自动切回 UI。测试线程所有权时，应从真实 WPF Dispatcher 调用命令。

`_onException` 是 UI 入口的最终错误边界，必须同步完成且不能再次抛出异常；`CanExecuteChanged` 订阅者同样不应抛出异常。受控的 `_ = observationTask` 与裸 `_ = SomeAsync()` 不同：前者已保存为 `ExecutionTask`，其内部负责消费命令任务异常；后者通常没有所有者、取消、异常和关闭契约。

该示例依赖一个明确前置条件：命令在绑定它的同一个 WPF UI Dispatcher 上创建和调用，`ExecuteAsync` 只用于该 Dispatcher 上的测试或显式等待。示例中的 `_executionCount`、`ExecutionTask` 和通知不是为跨线程并发调用设计的。若业务入口允许从任意线程调用，应让 ViewModel 方法显式提交自己的 UI 状态，而不是让命令对象负责把任意调用切回 UI。

## 七、事件默认线程中立

核心业务事件应默认：

- 在产生事件的操作线程同步触发；
- 保持与当前操作的顺序关系；
- 不承诺 UI 线程；
- 不依赖 `Application.Current.Dispatcher`。

UI 订阅者若要修改绑定状态，应在靠近 UI 的适配层提交最小变更。核心发布者不应为了某个 WPF 订阅者把事件统一调到 UI，否则会污染 CLI、测试、服务端或其他 UI 框架调用方。

### 7.1 `void` 事件不能表达异步完成

以下写法无法让发布方等待 UI 更新：

```csharp
private void OnCompleted(object? sender, EventArgs e)
{
    _ = _uiDispatcher.InvokeAsync(UpdateUi);
}
```

问题包括：

- 发布操作结束时，UI 更新可能尚未完成；
- Dispatcher 异常无人观察；
- 多次事件可能乱序；
- 对象释放后可能发生迟到提交。

如果订阅者处理是操作完成的一部分，应使用可等待回调，例如 `Func<T, CancellationToken, Task>`，或由更高层显式等待状态提交。若必须使用同步事件，可将事件快速写入一个有序队列，并提供排空、取消和异常观察能力。

## 八、进度报告需要顺序与排空契约

`IProgress<T>` 在存在同步上下文时通常异步投递，因此 `Report` 返回不等于 UI 已完成更新。高频或有顺序要求的进度不能仅靠 fire-and-forget Dispatcher。

可选方案：

1. 服务接受可等待回调 `Func<TProgress, CancellationToken, Task>`；
2. 使用 `Channel<T>` 建立单消费者有序队列；
3. 合并高频进度，只提交最新值，并提供完成时排空；
4. 为每次操作绑定会话代次或取消令牌，拒绝旧操作的迟到进度。

任何方案都应回答四个问题：谁消费、谁观察异常、何时排空、对象关闭时如何停止。

## 九、后台任务必须有所有者

某个阶段可以不阻塞当前 API，但不能成为无所有者任务。合法后台任务至少具有：

- 保存任务引用的所有者；
- 可查询或可等待的完成入口；
- 明确的取消令牌；
- 异常观察；
- 新任务替换旧任务时的规则；
- 对象关闭时的取消与等待策略。

例如，生成完成后自动评估可以不延长“发送”方法。以下示例选择“同一时刻最多一个自动评估”的策略：启动、任务发布和关闭快照受同一把锁保护，新请求在旧评估未结束或对象开始关闭后不再启动。

```csharp
private readonly object _automaticEvaluationGate = new();
private readonly CancellationTokenSource _lifetimeCancellationSource = new();
private Task _automaticEvaluationTask = Task.CompletedTask;
private bool _isClosing;

internal Task AutomaticEvaluationTask
{
    get
    {
        lock (_automaticEvaluationGate)
        {
            return _automaticEvaluationTask;
        }
    }
}

private void StartAutomaticEvaluation(EvaluationContext context)
{
    lock (_automaticEvaluationGate)
    {
        if (_isClosing || !_automaticEvaluationTask.IsCompleted)
        {
            return;
        }

        _automaticEvaluationTask =
            ObserveAutomaticEvaluationAsync(context);
    }
}

private async Task ObserveAutomaticEvaluationAsync(
    EvaluationContext context)
{
    Task evaluationTask = EvaluateAsync(
        context,
        _lifetimeCancellationSource.Token);

    await Task.WhenAny(evaluationTask).ConfigureAwait(false);

    if (evaluationTask.Exception is { } exception)
    {
        _logger.LogWarning(
            exception.GetBaseException(),
            "Automatic evaluation failed.");
    }
}

private async Task CloseAutomaticEvaluationAsync()
{
    Task automaticEvaluationTask;

    lock (_automaticEvaluationGate)
    {
        _isClosing = true;
        automaticEvaluationTask = _automaticEvaluationTask;
    }

    await _lifetimeCancellationSource
        .CancelAsync()
        .ConfigureAwait(false);
    await automaticEvaluationTask.ConfigureAwait(false);
    _lifetimeCancellationSource.Dispose();
}
```

锁内调用的观察方法必须尽快返回任务，不能在首次异步等待前执行耗时同步工作。这里“不阻塞发送”与“允许丢弃任务”是两回事。后台阶段仍需拥有生命周期和异常契约。若业务允许多个评估重叠，单个任务字段不足以描述全部生命周期；应使用任务登记集合，并在关闭时禁止新任务、取消和等待全部已登记任务。

## 十、普通字段的并发可见性

UI Dispatcher 上连续写入多个字段，只能保证 UI 线程内顺序，不能自动保证后台线程看到一致组合。

### 10.1 单引用状态优先发布不可变快照

```csharp
internal sealed record PipelineSnapshot(
    IRenderPipeline ActivePipeline,
    long Generation);

private PipelineSnapshot _snapshot;

internal PipelineOwner(IRenderPipeline initialPipeline)
{
    ArgumentNullException.ThrowIfNull(initialPipeline);
    _snapshot = new PipelineSnapshot(initialPipeline, 0);
}

internal PipelineSnapshot Snapshot => Volatile.Read(ref _snapshot);

private void Publish(IRenderPipeline pipeline)
{
    PipelineSnapshot current = Volatile.Read(ref _snapshot);
    var next = new PipelineSnapshot(pipeline, current.Generation + 1);
    Interlocked.Exchange(ref _snapshot, next);
}
```

不可变快照避免读者观察到“管道已切换，但配套代次仍是旧值”的撕裂状态。

### 10.2 原语选择

| 场景 | 优先方案 |
| --- | --- |
| 单个引用或整数的发布/交换 | `Volatile.Read`、`Interlocked.Exchange` |
| 多字段必须一致读取 | 不可变快照或锁 |
| 严格串行处理事件或进度 | 单消费者队列、`Channel<T>` |
| 保护短小临界区 | `lock` |
| 取消源替换与释放 | `Interlocked.Exchange` 后由获得所有权的一方处理 |
| 只依赖 UI 线程写、后台线程永不读 | UI Dispatcher 串行化即可 |

不要用 Dispatcher 代替内存可见性设计；也不要仅因字段会跨线程读取就把所有业务操作调到 UI。

## 十一、取消、释放与迟到提交

异步操作被取消，不代表已经排队的回调会自动消失。对象释放或新会话开始后，旧操作可能仍然完成并覆盖新状态。

常见防护：

- 每次操作分配递增代次；提交前比较当前代次；
- 为会话建立独立 `CancellationTokenSource`；
- `DisposeAsync` 先阻止新任务，再取消，最后等待已登记任务；
- UI 提交前同时检查对象存活状态、会话代次和结果所有权；
- 令牌清理与 UI 通知分离：纯线程安全资源清理可在任意线程执行，绑定状态清理必须在 UI 提交中完成。

示例：

```csharp
private long _sessionGeneration;

private long BeginSession() => Interlocked.Increment(ref _sessionGeneration);

private bool IsCurrentSession(long generation) =>
    Volatile.Read(ref _sessionGeneration) == generation;

private async Task CommitIfCurrentAsync(
    long generation,
    Action commit)
{
    await _uiDispatcher.InvokeAsync(() =>
    {
        if (IsCurrentSession(generation))
        {
            commit();
        }
    }).ConfigureAwait(false);
}
```

检查应尽量在真正提交的 Dispatcher 委托内再次执行，避免“检查通过后、提交执行前”会话已经变化。

### 11.1 UI 所有的会话操作优先保持同一 Dispatcher

如果会话由 ViewModel 拥有，而且开始、取消、切换和关闭都由同一个 UI Dispatcher 调入，就应利用这条串行化契约，而不是把方法改造成线程中立后再到处补 Dispatcher：

1. 在 UI Dispatcher 上拒绝关闭后的新操作、递增会话代次并快照输入；
2. 使用普通 `await` 等待异步业务，不主动丢失 UI 上下文；
3. 成功、取消和失败只形成局部结果；
4. `finally` 仍位于原 UI Dispatcher，可直接检查关闭状态与代次并提交结果；
5. 关闭时在同一 UI Dispatcher 上禁止新操作、取消当前操作并等待其任务。

```csharp
private long _sessionGeneration;
private bool _isClosing;

private async Task RunSessionOperationAsync(
    SessionInput input,
    CancellationToken cancellationToken)
{
    if (_isClosing)
    {
        throw new InvalidOperationException("The owner is closing.");
    }

    long generation = ++_sessionGeneration;
    OperationOutcome outcome = OperationOutcome.Faulted();

    try
    {
        OperationResult result = await _service.RunAsync(
            input,
            cancellationToken);
        outcome = OperationOutcome.Succeeded(result);
    }
    catch (OperationCanceledException)
        when (cancellationToken.IsCancellationRequested)
    {
        outcome = OperationOutcome.Canceled();
    }
    catch (ExpectedOperationException exception)
    {
        outcome = OperationOutcome.Failed(exception.Message);
    }
    finally
    {
        if (!_isClosing && generation == _sessionGeneration)
        {
            ApplyOutcomeAndComplete(outcome);
        }
    }
}
```

这段代码不需要 Dispatcher，因为它明确依赖同一个 WPF UI Dispatcher 调入，并且所有 `await` 都保留该上下文。若操作必须从任意线程启动，或者生命周期状态确实由多个线程并发访问，就不能套用此模板；那时应重新定义线程安全的登记与关闭协议，并仅在最终 UI 提交点调度。不要为了追求“线程中立”主动破坏原本简单可靠的 UI 串行化契约。

## 十二、WPF 专用线程规则

### 12.1 `DispatcherObject`

`DispatcherObject` 只能由创建它的 Dispatcher 访问。使用 `CheckAccess()` 或 `VerifyAccess()` 判断所有权；不要把“当前是 STA 线程”误认为“当前就是对象所属 Dispatcher”。STA 是线程单元模型，Dispatcher 所有权是具体线程身份。

### 12.2 `Freezable`

未冻结的 `Freezable` 具有线程亲和性，但 `Freeze()` 只是跨线程只读共享的一种可选技术，不是普遍指导思想。设计时应先判断对象是否本来就可以留在所属 Dispatcher 上复用；如果无需跨线程，就没有必要为了“线程安全”反复创建、复制和冻结对象。

只有对象确实需要跨线程长期只读共享，且冻结收益大于创建与复制成本时，才考虑：

1. 在所属线程完成构造和修改；
2. 检查 `CanFreeze`；
3. 调用 `Freeze()`；
4. 冻结后只读跨线程使用。

冻结不会让对象重新可写。任何修改都需要新的可写实例；若高频路径因此不断创建、克隆和冻结大对象，会增加分配、GC 和峰值内存压力。应结合对象大小、创建频率、复用周期和实际性能测量，在“留在所属 Dispatcher 复用”“冻结后长期共享”“转换为非 WPF 不可变数据”之间选择。

### 12.3 `ObservableCollection`

绑定集合的首要工作是梳理调用链，而不是机械添加 Dispatcher：

- 若命令或事件明确由绑定 UI Dispatcher 调入，且调用链使用普通 `await` 保持上下文，应直接修改 `ObservableCollection<T>`；再次 `InvokeAsync` 只会增加排队、重入和复杂度。
- 若后台线程只负责计算或加载数据，应先在线程外形成普通列表或不可变快照，再在 UI 上一次性应用最小集合变更；不要为循环中的每一项分别投递 Dispatcher。
- 只有集合修改确实从非 UI 线程到达时，才调度集合变更；高频更新应考虑批处理、合并通知或替换快照，避免淹没 UI 消息队列。
- `BindingOperations.EnableCollectionSynchronization` 不是默认替代方案：它引入锁协议和 UI 枚举协作，只适合确有后台生产需求且已经设计锁顺序的场景。

### 12.4 图片保存不能按接口名一概调度

是否需要 UI Dispatcher 取决于实际对象契约：

- 已冻结的 WPF 位图可以跨线程只读编码；
- 普通文件图片是文件 I/O；
- 其他 UI 框架图片遵循各自线程规则；
- 若接口无法表达线程要求，应让实现返回不可变、可跨线程的快照，而不是让上层把所有 `Save` 操作统一调到“主线程”。

## 十三、常见错误模式

### 13.1 给完整业务流程补 Dispatcher

症状：异常发生后把网络、解析、渲染和状态更新整体包进 Dispatcher。

错误原因：扩大 UI 占用，掩盖真正的对象所有权，可能引入卡顿和死锁。

正确做法：定位具体线程亲和对象或 UI 可观察写入，只调度最小同步区域。

### 13.2 把 `ConfigureAwait(false)` 当作后台化

症状：认为调用后“已经切到后台”，随后到处补 Dispatcher。

错误原因：它只是不要求恢复上下文，续体甚至可能同步执行。

正确做法：先确认方法是否明确由 UI Dispatcher 调入。若是，使用普通 `await` 保持上下文并直接提交；若不是，再按状态所有权显式设计 Dispatcher 边界，不依据当前线程猜测。

### 13.3 下层提交完成后直接写上层状态

```csharp
await renderTool.ApplyResultAsync(result).ConfigureAwait(false);
StatusText = "渲染完成";
IsBusy = false;
```

`ApplyResultAsync` 只能保证渲染工具自己的状态已经提交，不能单独证明调用方续体位于 UI 线程。若 ViewModel 调用链原本从 UI Dispatcher 进入且此处使用普通 `await`，可直接更新自己的状态；若调用方主动使用了 `ConfigureAwait(false)` 或方法允许任意线程调用，才需要为 ViewModel 状态建立显式提交边界。

### 13.4 裸 fire-and-forget

```csharp
_ = dispatcher.InvokeAsync(UpdateUi);
_ = InitializeOptionalServiceAsync();
_ = EvaluateAsync(context);
```

除非任务由明确所有者登记、观察、取消并在关闭时处理，否则会丢失异常、顺序和生命周期。

### 13.5 核心事件承诺 UI 线程

```csharp
await dispatcher.InvokeAsync(() => Rendered?.Invoke(result));
```

这会让核心流水线依赖某个 UI 框架，并迫使非 UI 调用方等待无关 Dispatcher。核心事件应线程中立，UI 适配层负责提交。

### 13.6 使用异步转同步初始化 Dispatcher

```csharp
dispatcherReady.Task.GetAwaiter().GetResult();
```

这会阻塞调用线程，并可能形成死锁或不可控启动等待。应保存准备任务并在异步 API 中等待；同步查询不能触发阻塞初始化。

## 十四、线程测试必须使用真实所有权

始终立即执行委托的 fake Dispatcher 无法发现跨线程问题。关键线程契约应使用专用 STA 线程和真实 WPF Dispatcher。

### 14.1 必测场景

- 从真实 WPF Dispatcher 启动命令，断言续体、`CanExecuteChanged` 和异常回调线程；
- 从线程池触发业务完成，断言绑定属性和集合事件最终发生在 UI Dispatcher；
- 核心业务事件不调用 UI Dispatcher，并与业务操作保持同步顺序；
- Dispatcher 初始化可异步等待，`CheckAccess` 不阻塞；
- 若设计选择冻结后共享 WPF 图片，验证其可在线程池只读编码，并检查分配与内存压力；
- 正常、取消、预期失败路径都在命令任务完成前提交最终状态；
- 对象释放或新会话开始后，旧回调不能覆盖当前状态；
- 后台任务异常被观察，关闭流程能够取消并等待；
- 活动实例切换通过原子快照对并发读者可见。

### 14.2 测试同步原则

- 使用 `TaskCompletionSource`、事件、队列排空或明确任务作为同步点；
- 不用 `Thread.Sleep` 猜测时序；
- 所有可能挂起的测试设置硬超时；
- 测试不仅断言最终值，还断言通知线程、通知次数和任务完成顺序；
- 不从线程池调用异步命令测试入口，再要求命令自行切回 UI；生产契约若规定从 WPF `ICommand.Execute` 进入，测试也应从 UI Dispatcher 进入。

## 十五、设计与审查清单

### 15.1 异步方法

- 返回的 `Task` 是否覆盖业务、结果提交、失败/取消提交和清理？
- 是否存在 `.Result`、`.Wait()`、`GetAwaiter().GetResult()`？
- 是否错误使用 `Task.Run` 包装异步 I/O？
- 是否把 `ConfigureAwait(false)` 当作线程切换？
- 成功、取消、失败是否使用同一状态提交策略？

### 15.2 UI 状态

- 每份绑定状态是否只有一个提交所有者？
- Dispatcher 委托是否只做短同步提交？
- 相关状态是否在一次提交中形成一致组合？
- 是否先确认调用链已在绑定 UI Dispatcher，而不是机械为集合和通知增加调度？
- 真正跨线程的集合更新是否批量形成数据后再提交最小变更？
- 下层提交是否被错误当成上层线程保证？

### 15.3 命令、事件与进度

- `ICommand.Execute` 是否保持同步 `void` 入口？
- 异步命令是否暴露执行任务并观察异常？
- 核心事件是否保持线程中立？
- 需要异步完成的处理是否仍错误使用 `void` 事件？
- 进度是否有顺序、排空、取消和迟到更新防护？

### 15.4 后台任务与并发

- 每个不被当前 API 等待的任务是否有所有者？
- 是否保存任务、观察异常、支持取消并在关闭时处理？
- 普通字段的数据竞争是否使用并发原语，而不是 Dispatcher？
- 多字段一致性是否通过不可变快照或锁保证？
- 新会话、取消或释放后是否拒绝旧结果提交？

### 15.5 WPF 对象

- 是否确认了具体对象所属 Dispatcher，而不只是 STA？
- 是否确实需要跨线程共享 `Freezable`，并评估了冻结、复用和对象创建压力？
- 图片编码是否依据具体实现契约，而不是接口名称统一调度？
- 无 `Application` 场景的专用 Dispatcher 是否异步初始化、可观察并可关闭？

## 十六、结论

可靠的 WPF 异步线程模型可以压缩为五条规则：

1. 先识别对象类型和状态所有者，再决定是否需要 Dispatcher。
2. 已位于目标 UI Dispatcher 时直接提交；只有真正跨线程时，Dispatcher 才执行最小、同步、不可异步打断的 UI 提交。
3. `Task` 必须覆盖完整操作生命周期，不能在状态仍会迟到变化时提前结束。
4. 命令、事件、进度和后台任务必须明确线程、顺序、异常、取消与关闭契约。
5. Dispatcher 不替代并发原语；WPF 线程亲和性、UI 通知、数据竞争和后台任务所有权必须分别处理。

## 十七、不要把“最小 UI 提交”误读为默认注入 Dispatcher

本文提到“核心事件保持线程中立，UI 适配层负责最小状态提交”，不代表遇到跨线程异常时，应立即向 ViewModel、命令或其他 UI 对象注入 Dispatcher。最小 UI 提交是完成调用链分析之后，确认状态确实合理地从非 UI 线程产生时使用的边界，不是跳过线程模型梳理的通用修复模板。

如果某个命令状态通知、绑定属性或集合更新从后台线程到达，首先应追踪它的完整来源：操作是否原本由 UI Dispatcher 上的命令或 UI 输入事件启动，中间是否使用了 `ConfigureAwait(false)` 主动丢失上下文，是否存在未等待任务、后台回调或提前完成的任务，以及状态所有者是否错误地把本应属于同一 UI 操作生命周期的提交拆到了其他线程。只要调用链本来属于 UI，就应优先恢复并保留这条串行化契约，而不是通过注入 Dispatcher 掩盖上下文丢失的位置。

诊断顺序应固定为：

1. 找到实际写入状态或触发通知的位置，而不是只处理最终抛出线程异常的位置。
2. 沿调用链向上确认操作入口是否位于目标 UI Dispatcher。
3. 检查每个 `await`、回调和任务边界，确认是否主动丢失上下文、任务提前完成或出现无所有者后台任务。
4. 确认状态所有者及其完成契约，判断该状态是否本应在原 UI 调用链内直接提交。
5. 优先修复错误的调用链、任务生命周期或状态所有权。
6. 只有确认通知确实来自合法的线程中立事件、后台生产者或明确允许任意线程调用的 API，而且无法也不应该恢复为 UI 所有的调用链时，才在靠近 UI 的位置调度最小同步提交。

以下处理顺序是错误的：看到控件在线程检查中抛出异常，就给 ViewModel 注入 Dispatcher，再把所有 `PropertyChanged`、`CollectionChanged` 和 `CanExecuteChanged` 统一转发到 UI。这样虽然可能暂时消除异常，却会隐藏真正的上下文丢失、未等待任务或错误所有权，还会引入额外排队、迟到提交、释放后回调和异常观察问题。

命令对象同样不应成为通用线程切换器。`CanExecuteChanged` 必须在正确的 UI 操作上下文中触发，但首选保证产生通知的调用链原本就在 UI Dispatcher，而不是让命令在每次通知时自行判断并切换线程。否则命令会掩盖调用方的线程契约，并让测试难以发现真正的跨线程来源。

因此，“需要 Dispatcher”必须来自明确的线程契约或排查结论，而不能是看到异常后的默认起点。代码审查时可以增加两个反向问题：

- 这个 Dispatcher 是因为业务确实允许后台线程产生 UI 状态，还是只为掩盖某处丢失的 UI 上下文？
- 如果该操作原本属于 UI 调用链，移除这个 Dispatcher 后，最早在哪个任务、回调或事件边界丢失了 UI 上下文？如果它本来就来自非 UI 线程，其任意线程调用契约在哪里定义？

只有能够明确回答这两个问题，额外调度才具有可验证的设计依据。
