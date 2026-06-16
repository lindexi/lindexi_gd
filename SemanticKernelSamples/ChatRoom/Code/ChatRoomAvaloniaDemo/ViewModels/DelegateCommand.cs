using System;
using System.Windows.Input;

namespace ChatRoomAvaloniaDemo.ViewModels;

/// <summary>
/// 通用的委托命令实现，支持同步和异步执行。
/// </summary>
public sealed class DelegateCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    /// <summary>
    /// 创建同步委托命令。
    /// </summary>
    /// <param name="execute">执行操作。</param>
    /// <param name="canExecute">可选的可执行性判断。</param>
    public DelegateCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>
    /// 创建无参数的同步委托命令。
    /// </summary>
    /// <param name="execute">执行操作。</param>
    /// <param name="canExecute">可选的可执行性判断。</param>
    public DelegateCommand(Action execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute is not null ? _ => canExecute() : null)
    {
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <inheritdoc />
    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    /// <inheritdoc />
    public void Execute(object? parameter) => _execute(parameter);

    /// <summary>
    /// 引发 <see cref="CanExecuteChanged"/> 事件，通知 UI 重新查询可执行性。
    /// </summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>
/// 支持泛型参数的委托命令。
/// </summary>
/// <typeparam name="T">参数类型。</typeparam>
public sealed class DelegateCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Predicate<T?>? _canExecute;

    /// <summary>
    /// 创建泛型委托命令。
    /// </summary>
    /// <param name="execute">执行操作。</param>
    /// <param name="canExecute">可选的可执行性判断。</param>
    public DelegateCommand(Action<T?> execute, Predicate<T?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <inheritdoc />
    public bool CanExecute(object? parameter)
    {
        if (_canExecute is null)
        {
            return true;
        }

        if (parameter is T t)
        {
            return _canExecute(t);
        }

        return _canExecute(default);
    }

    /// <inheritdoc />
    public void Execute(object? parameter)
    {
        if (parameter is T t)
        {
            _execute(t);
        }
        else
        {
            _execute(default);
        }
    }

    /// <summary>
    /// 引发 <see cref="CanExecuteChanged"/> 事件。
    /// </summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}