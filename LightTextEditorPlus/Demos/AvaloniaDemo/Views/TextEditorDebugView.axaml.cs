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

        // 调试代码
        TextEditor.AppendText("asd");
        _richTextCaseProvider.Debug(TextEditor);
    }

    private readonly RichTextCaseProvider _richTextCaseProvider = new RichTextCaseProvider();

    private void DebugButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _richTextCaseProvider.Debug(TextEditor);
    }

    private void ShowDocumentBoundsButton_OnClick(object? sender, RoutedEventArgs e)
    {
    }

    private void ReadOnlyModeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (TextEditor.IsEditable)
        {
            ReadOnlyModeButton.Content = $"进入编辑模式";
            TextEditor.IsEditable = false;
        }
        else
        {
            ReadOnlyModeButton.Content = $"进入只读模式";
            TextEditor.IsEditable = true;
            TextEditor.Focus();
        }
    }
}