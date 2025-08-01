using Avalonia.Controls;

namespace SimpleWrite.Views.Components;

public partial class MainEditorView : UserControl
{
    public MainEditorView()
    {
        InitializeComponent();
        var textEditor = new LightTextEditorPlus.TextEditor();
        TextEditorBorder.Child = textEditor;
    }
}