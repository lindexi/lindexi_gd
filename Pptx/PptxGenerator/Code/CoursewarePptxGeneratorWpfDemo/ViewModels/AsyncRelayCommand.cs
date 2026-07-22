using System.Windows.Input;

namespace CoursewarePptxGeneratorWpfDemo.ViewModels;

/// <summary>
/// Exposes an asynchronous delegate as an <see cref="ICommand" /> and makes the active execution observable.
/// </summary>
public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<object?, Task> _executeAsync;
    private readonly Predicate<object?>? _canExecute;
    private readonly Action<Exception>? _onException;
    private readonly Action? _cancel;
    private readonly bool _allowsConcurrentExecutions;
    private int _executionCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncRelayCommand" /> class.
    /// </summary>
    /// <param name="executeAsync">The asynchronous delegate to execute.</param>
    /// <param name="canExecute">The optional executability predicate.</param>
    /// <param name="onException">The optional exception observer used by fire-and-forget ICommand execution.</param>
    /// <param name="cancel">The optional cancellation callback for the active operation.</param>
    /// <param name="allowsConcurrentExecutions">Whether separate command targets may execute concurrently.</param>
    public AsyncRelayCommand(
        Func<object?, Task> executeAsync,
        Predicate<object?>? canExecute = null,
        Action<Exception>? onException = null,
        Action? cancel = null,
        bool allowsConcurrentExecutions = false)
    {
        ArgumentNullException.ThrowIfNull(executeAsync);
        _executeAsync = executeAsync;
        _canExecute = canExecute;
        _onException = onException;
        _cancel = cancel;
        _allowsConcurrentExecutions = allowsConcurrentExecutions;
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// Gets a value indicating whether the command is currently executing.
    /// </summary>
    public bool IsExecuting => Volatile.Read(ref _executionCount) > 0;

    /// <summary>
    /// Gets the current execution task, or a completed task when the command is idle.
    /// </summary>
    public Task ExecutionTask { get; private set; } = Task.CompletedTask;

    /// <inheritdoc />
    public bool CanExecute(object? parameter) =>
        (_allowsConcurrentExecutions || !IsExecuting)
        && (_canExecute?.Invoke(parameter) ?? true);

    /// <inheritdoc />
    public void Execute(object? parameter)
    {
        _ = ObserveExecutionAsync(ExecuteAsync(parameter));
    }

    /// <summary>
    /// Executes the command and returns a task that callers and tests can await.
    /// </summary>
    /// <param name="parameter">The command parameter.</param>
    /// <returns>A task that represents the command execution.</returns>
    public Task ExecuteAsync(object? parameter = null)
    {
        if (!(_canExecute?.Invoke(parameter) ?? true))
        {
            return Task.CompletedTask;
        }

        if (_allowsConcurrentExecutions)
        {
            Interlocked.Increment(ref _executionCount);
        }
        else if (Interlocked.CompareExchange(ref _executionCount, 1, 0) != 0)
        {
            return Task.CompletedTask;
        }

        ExecutionTask = ExecuteCoreAsync(parameter);
        return ExecutionTask;
    }

    /// <summary>
    /// Notifies WPF that command executability may have changed.
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Requests cancellation of the current execution when a cancellation callback was supplied.
    /// </summary>
    public void Cancel()
    {
        if (IsExecuting)
        {
            _cancel?.Invoke();
        }
    }

    private async Task ExecuteCoreAsync(object? parameter)
    {
        RaiseCanExecuteChanged();
        try
        {
            await _executeAsync(parameter);
        }
        finally
        {
            Interlocked.Decrement(ref _executionCount);
            RaiseCanExecuteChanged();
        }
    }

    private async Task ObserveExecutionAsync(Task executionTask)
    {
        try
        {
            await executionTask;
        }
        catch (Exception ex)
        {
            _onException?.Invoke(ex);
        }
    }
}
