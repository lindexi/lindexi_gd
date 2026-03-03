using Avalonia;

using LightTextEditorPlus;

using SimpleWrite.Views.Components;

using System;

namespace SimpleWrite.Views;

public class TextEditorInfo
{
    public TextEditorInfo(MainEditorView mainEditorView)
    {
        _mainEditorView = mainEditorView;
        _mainEditorView.CurrentTextEditorChanged += MainEditorView_CurrentTextEditorChanged;
    }

    private void MainEditorView_CurrentTextEditorChanged(object? sender, System.EventArgs e)
    {
        CurrentTextEditorChanged?.Invoke(sender, e);
    }

    public event EventHandler? CurrentTextEditorChanged;

    public TextEditor CurrentTextEditor => _mainEditorView.CurrentTextEditor;

    private readonly MainEditorView _mainEditorView;

    public static readonly AvaloniaProperty TextEditorInfoProperty = AvaloniaProperty.RegisterAttached<SimpleWriteMainView, TextEditorInfo>(
        "TextEditorInfo", typeof(TextEditorInfo), inherits: true);

    public static void SetTextEditorInfo(AvaloniaObject element, TextEditorInfo value)
    {
        element.SetValue(TextEditorInfoProperty, value);
    }

    public static TextEditorInfo GetTextEditorInfo(AvaloniaObject element)
    {
        return (TextEditorInfo) element.GetValue(TextEditorInfoProperty)!;
    }
}