using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CodingChatRoom.AvaloniaShell.ViewModels;

/// <summary>
/// 提供同步命令实现。
/// </summary>
public sealed class SimpleCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    /// <summary>
    /// 使用指定执行委托创建命令。
    /// </summary>
    public SimpleCommand(Action execute, Func<bool>? canExecute = null)
    {
        ArgumentNullException.ThrowIfNull(execute);
        _execute = execute;
        _canExecute = canExecute;
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <inheritdoc />
    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    /// <inheritdoc />
    public void Execute(object? parameter) => _execute();

    /// <summary>
    /// 通知绑定目标重新计算命令状态。
    /// </summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>
/// 提供带参数的同步命令实现。
/// </summary>
public sealed class SimpleCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    /// <summary>
    /// 使用指定执行委托创建命令。
    /// </summary>
    public SimpleCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        ArgumentNullException.ThrowIfNull(execute);
        _execute = execute;
        _canExecute = canExecute;
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <inheritdoc />
    public bool CanExecute(object? parameter)
        => parameter is T or null && (_canExecute?.Invoke((T?) parameter) ?? true);

    /// <inheritdoc />
    public void Execute(object? parameter)
    {
        if (CanExecute(parameter))
        {
            _execute((T?) parameter);
        }
    }

    /// <summary>
    /// 通知绑定目标重新计算命令状态。
    /// </summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>
/// 提供防止重复执行的异步命令实现。
/// </summary>
public sealed class SimpleAsyncCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    private readonly bool _allowConcurrentExecutions;
    private readonly Action<Exception> _exceptionHandler;
    private bool _isExecuting;

    /// <summary>
    /// 使用指定异步执行委托创建命令。
    /// </summary>
    public SimpleAsyncCommand(
        Func<Task> execute,
        Func<bool>? canExecute = null,
        bool allowConcurrentExecutions = false,
        Action<Exception>? exceptionHandler = null)
    {
        ArgumentNullException.ThrowIfNull(execute);
        _execute = execute;
        _canExecute = canExecute;
        _allowConcurrentExecutions = allowConcurrentExecutions;
        _exceptionHandler = exceptionHandler ?? TraceUnhandledException;
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <inheritdoc />
    public bool CanExecute(object? parameter)
        => (_allowConcurrentExecutions || !_isExecuting) && (_canExecute?.Invoke() ?? true);

    /// <inheritdoc />
    public async void Execute(object? parameter) => await ExecuteAsync(parameter).ConfigureAwait(true);

    internal async Task ExecuteAsync(object? parameter = null)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        _isExecuting = true;
        RaiseCanExecuteChanged();
        try
        {
            await _execute().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            ReportException(exception);
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// 通知绑定目标重新计算命令状态。
    /// </summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    private void ReportException(Exception exception)
    {
        try
        {
            _exceptionHandler(exception);
        }
        catch (Exception handlerException)
        {
            Trace.TraceError($"异步命令错误处理失败：{handlerException}");
            TraceUnhandledException(exception);
        }
    }

    private static void TraceUnhandledException(Exception exception)
        => Trace.TraceError($"异步命令执行失败：{exception}");
}

/// <summary>
/// 提供带参数且防止重复执行的异步命令实现。
/// </summary>
public sealed class SimpleAsyncCommand<T> : ICommand
{
    private readonly Func<T?, Task> _execute;
    private readonly Func<T?, bool>? _canExecute;
    private readonly Action<Exception> _exceptionHandler;
    private bool _isExecuting;

    /// <summary>
    /// 使用指定异步执行委托创建命令。
    /// </summary>
    public SimpleAsyncCommand(
        Func<T?, Task> execute,
        Func<T?, bool>? canExecute = null,
        Action<Exception>? exceptionHandler = null)
    {
        ArgumentNullException.ThrowIfNull(execute);
        _execute = execute;
        _canExecute = canExecute;
        _exceptionHandler = exceptionHandler ?? TraceUnhandledException;
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <inheritdoc />
    public bool CanExecute(object? parameter)
        => parameter is T or null && !_isExecuting && (_canExecute?.Invoke((T?) parameter) ?? true);

    /// <inheritdoc />
    public async void Execute(object? parameter) => await ExecuteAsync(parameter).ConfigureAwait(true);

    internal async Task ExecuteAsync(object? parameter = null)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        _isExecuting = true;
        RaiseCanExecuteChanged();
        try
        {
            await _execute((T?) parameter).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            ReportException(exception);
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// 通知绑定目标重新计算命令状态。
    /// </summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    private void ReportException(Exception exception)
    {
        try
        {
            _exceptionHandler(exception);
        }
        catch (Exception handlerException)
        {
            Trace.TraceError($"异步命令错误处理失败：{handlerException}");
            TraceUnhandledException(exception);
        }
    }

    private static void TraceUnhandledException(Exception exception)
        => Trace.TraceError($"异步命令执行失败：{exception}");
}
