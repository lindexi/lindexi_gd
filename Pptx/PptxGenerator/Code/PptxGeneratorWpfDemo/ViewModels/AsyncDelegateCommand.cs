using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PptxGenerator;

/// <summary>
/// Exposes an asynchronous delegate as an <see cref="ICommand" /> with an observable execution task.
/// </summary>
public sealed class AsyncDelegateCommand : ICommand
{
    private readonly Func<Task> _executeAsync;
    private readonly Func<bool>? _canExecute;
    private readonly Action<Exception> _onException;
    private bool _isExecuting;

    public AsyncDelegateCommand(
        Func<Task> executeAsync,
        Action<Exception> onException,
        Func<bool>? canExecute = null)
    {
        _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
        _onException = onException ?? throw new ArgumentNullException(nameof(onException));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool IsExecuting => _isExecuting;

    public Task ExecutionTask { get; private set; } = Task.CompletedTask;

    public bool CanExecute(object? parameter) => !IsExecuting && (_canExecute?.Invoke() ?? true);

    public void Execute(object? parameter)
    {
        ExecutionTask = ObserveExecutionAsync(ExecuteAsync());
    }

    public Task ExecuteAsync()
    {
        if (!(_canExecute?.Invoke() ?? true)
            || _isExecuting)
        {
            return Task.CompletedTask;
        }

        _isExecuting = true;
        ExecutionTask = ExecuteCoreAsync();
        return ExecutionTask;
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task ExecuteCoreAsync()
    {
        RaiseCanExecuteChanged();
        try
        {
            await _executeAsync();
        }
        finally
        {
            _isExecuting = false;
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
            _onException(ex);
        }
    }
}

/// <summary>
/// Exposes an asynchronous parameterized delegate as an <see cref="ICommand" /> with an observable execution task.
/// </summary>
public sealed class AsyncDelegateCommand<T> : ICommand
{
    private readonly Func<T?, Task> _executeAsync;
    private readonly Func<T?, bool>? _canExecute;
    private readonly Action<Exception> _onException;
    private bool _isExecuting;

    public AsyncDelegateCommand(
        Func<T?, Task> executeAsync,
        Action<Exception> onException,
        Func<T?, bool>? canExecute = null)
    {
        _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
        _onException = onException ?? throw new ArgumentNullException(nameof(onException));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool IsExecuting => _isExecuting;

    public Task ExecutionTask { get; private set; } = Task.CompletedTask;

    public bool CanExecute(object? parameter)
    {
        var value = parameter is T typedValue ? typedValue : default;
        return !IsExecuting && (_canExecute?.Invoke(value) ?? true);
    }

    public void Execute(object? parameter)
    {
        ExecutionTask = ObserveExecutionAsync(ExecuteAsync(parameter is T typedValue ? typedValue : default));
    }

    public Task ExecuteAsync(T? parameter = default)
    {
        if (!(_canExecute?.Invoke(parameter) ?? true)
            || _isExecuting)
        {
            return Task.CompletedTask;
        }

        _isExecuting = true;
        ExecutionTask = ExecuteCoreAsync(parameter);
        return ExecutionTask;
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task ExecuteCoreAsync(T? parameter)
    {
        RaiseCanExecuteChanged();
        try
        {
            await _executeAsync(parameter);
        }
        finally
        {
            _isExecuting = false;
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
            _onException(ex);
        }
    }
}
