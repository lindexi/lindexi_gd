using Avalonia;
using LightTextEditorPlus;
using SimpleWrite.Views.Components;

namespace SimpleWrite.Views;

public class TextEditorInfo
{
    public TextEditorInfo(MainEditorView mainEditorView)
    {
        _mainEditorView = mainEditorView;
    }

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