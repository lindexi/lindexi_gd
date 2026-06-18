using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ChatRoom.AvaloniaShell.ViewModels;

/// <summary>
/// 简单的同步 <see cref="ICommand"/> 实现。
/// </summary>
public sealed class SimpleCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    /// <summary>
    /// 使用指定的执行操作创建命令。
    /// </summary>
    /// <param name="execute">执行操作。</param>
    /// <param name="canExecute">是否可执行判断函数。</param>
    public SimpleCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <inheritdoc/>
    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    /// <inheritdoc/>
    public void Execute(object? parameter) => _execute();

    /// <inheritdoc/>
    event EventHandler? ICommand.CanExecuteChanged
    {
        add { }
        remove { }
    }
}

/// <summary>
/// 简单的异步 <see cref="ICommand"/> 实现。
/// </summary>
public sealed class SimpleAsyncCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;

    /// <summary>
    /// 使用指定的异步执行操作创建命令。
    /// </summary>
    public SimpleAsyncCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <inheritdoc/>
    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    /// <inheritdoc/>
    public void Execute(object? parameter) => _ = _execute();

    /// <inheritdoc/>
    event EventHandler? ICommand.CanExecuteChanged
    {
        add { }
        remove { }
    }
}

/// <summary>
/// 带参数的异步 <see cref="ICommand"/> 实现。
/// </summary>
public sealed class SimpleAsyncCommand<T> : ICommand
{
    private readonly Func<T?, Task> _execute;
    private readonly Func<T?, bool>? _canExecute;

    /// <summary>
    /// 使用指定的异步执行操作创建命令。
    /// </summary>
    public SimpleAsyncCommand(Func<T?, Task> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <inheritdoc/>
    public bool CanExecute(object? parameter) => parameter is T or null && (_canExecute?.Invoke((T?)parameter) ?? true);

    /// <inheritdoc/>
    public void Execute(object? parameter) => _ = _execute((T?)parameter);

    /// <inheritdoc/>
    event EventHandler? ICommand.CanExecuteChanged
    {
        add { }
        remove { }
    }
}
