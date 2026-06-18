using System;
using System.Windows.Input;

namespace PptxGenerator;

/// <summary>
/// 基于委托的 <see cref="ICommand"/> 实现，使用 <see cref="IDispatcher"/> 抽象 UI 线程调度。
/// </summary>
public sealed class DelegateCommand : ICommand
{
    private readonly Func<bool>? _canExecute;
    private readonly Action _execute;
    private readonly IDispatcher _dispatcher;

    /// <summary>
    /// 初始化 <see cref="DelegateCommand"/> 的新实例。
    /// </summary>
    /// <param name="execute">执行操作。</param>
    /// <param name="dispatcher">UI 线程调度器。</param>
    /// <param name="canExecute">可执行条件。</param>
    public DelegateCommand(Action execute, IDispatcher dispatcher, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _canExecute = canExecute;
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <inheritdoc />
    public bool CanExecute(object? parameter)
    {
        return _canExecute?.Invoke() ?? true;
    }

    /// <inheritdoc />
    public void Execute(object? parameter)
    {
        _execute();
    }

    /// <summary>
    /// 引发 <see cref="CanExecuteChanged"/> 事件，通知 UI 重新查询可执行状态。
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        _dispatcher.InvokeAsync(() =>
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        });
    }
}

/// <summary>
/// 基于委托的泛型 <see cref="ICommand"/> 实现，使用 <see cref="IDispatcher"/> 抽象 UI 线程调度。
/// </summary>
public sealed class DelegateCommand<T> : ICommand
{
    private readonly Func<T?, bool>? _canExecute;
    private readonly Action<T?> _execute;
    private readonly IDispatcher _dispatcher;

    /// <summary>
    /// 初始化 <see cref="DelegateCommand{T}"/> 的新实例。
    /// </summary>
    /// <param name="execute">执行操作。</param>
    /// <param name="dispatcher">UI 线程调度器。</param>
    /// <param name="canExecute">可执行条件。</param>
    public DelegateCommand(Action<T?> execute, IDispatcher dispatcher, Func<T?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _canExecute = canExecute;
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <inheritdoc />
    public bool CanExecute(object? parameter)
    {
        return _canExecute?.Invoke(parameter is T t ? t : default) ?? true;
    }

    /// <inheritdoc />
    public void Execute(object? parameter)
    {
        _execute(parameter is T t ? t : default);
    }

    /// <summary>
    /// 引发 <see cref="CanExecuteChanged"/> 事件，通知 UI 重新查询可执行状态。
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        _dispatcher.InvokeAsync(() =>
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        });
    }
}