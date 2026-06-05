using System;
using System.Windows.Input;
using Avalonia.Threading;

namespace PptxGenerator;

internal sealed class DelegateCommand : ICommand
{
    private readonly Func<bool>? _canExecute;
    private readonly Action _execute;

    public DelegateCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        return _canExecute?.Invoke() ?? true;
    }

    public void Execute(object? parameter)
    {
        _execute();
    }

    public void RaiseCanExecuteChanged()
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        });
    }
}

internal sealed class DelegateCommand<T> : ICommand
{
    private readonly Func<T?, bool>? _canExecute;
    private readonly Action<T?> _execute;

    public DelegateCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        return _canExecute?.Invoke(parameter is T t ? t : default) ?? true;
    }

    public void Execute(object? parameter)
    {
        _execute(parameter is T t ? t : default);
    }

    public void RaiseCanExecuteChanged()
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        });
    }
}
