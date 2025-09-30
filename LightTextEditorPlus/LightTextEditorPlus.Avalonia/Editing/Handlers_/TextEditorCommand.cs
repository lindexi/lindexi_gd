using System;
using System.Windows.Input;

namespace LightTextEditorPlus.Editing;

class TextEditorCommand : ICommand
{
    public TextEditorCommand(Action execute)
    {
        _execute = execute;
    }

    private readonly Action _execute;

    public bool CanExecute(object? parameter)
    {
        return true;
    }

    public void Execute(object? parameter)
    {
        _execute();
    }

    public event EventHandler? CanExecuteChanged;

    protected virtual void OnCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}