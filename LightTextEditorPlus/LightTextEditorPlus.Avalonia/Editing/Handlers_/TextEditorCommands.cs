using System;
using System.Windows.Input;

namespace LightTextEditorPlus.Editing;

//public static class TextEditorCommands
//{
//    public static ICommand Copy { get; } = new RoutedCommand();
//}

//file
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
}