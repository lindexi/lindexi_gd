using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using LightTextEditorPlus.AvaloniaDemo.Business;
using LightTextEditorPlus.AvaloniaDemo.Business.RichTextCases;

namespace LightTextEditorPlus.AvaloniaDemo.Views;

public partial class TextEditorDebugView : UserControl
{
    public TextEditorDebugView()
    {
        InitializeComponent();

        TextEditorSettingsControl.TextEditor = TextEditor;

        // µ÷ÊÔ´úÂë
        TextEditor.AppendText("asd");
    }

    private readonly RichTextCaseProvider _richTextCaseProvider = new RichTextCaseProvider();

    private void DebugButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _richTextCaseProvider.Debug(TextEditor);
    }

    private void ShowDocumentBoundsButton_OnClick(object? sender, RoutedEventArgs e)
    {
    }
}