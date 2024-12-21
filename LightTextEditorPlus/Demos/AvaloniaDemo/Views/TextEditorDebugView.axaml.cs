using System;
using System.IO;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

using LightTextEditorPlus.AvaloniaDemo.Business;
using LightTextEditorPlus.AvaloniaDemo.Business.RichTextCases;
using LightTextEditorPlus.FontManagers;
using SkiaSharp;

namespace LightTextEditorPlus.AvaloniaDemo.Views;

public partial class TextEditorDebugView : UserControl
{
    public TextEditorDebugView()
    {
        InitializeComponent();

        //var fontFamily = (FontFamily) Application.Current!.Resources["TestMeatballFontFamily"]!;
        //TextEditorFontManager.RegisterFontNameToResource("仓耳小丸子", fontFamily);
        TextEditorFontResourceManager.TryRegisterFontNameToResource("仓耳小丸子", new FileInfo(Path.Join(AppContext.BaseDirectory, "Assets", "Fonts", "仓耳小丸子.ttf")));

        TextEditorSettingsControl.TextEditor = TextEditor;

        // 调试代码
        //TextEditor.AppendText("asd");
        _richTextCaseProvider.Debug(TextEditor);
    }

    private readonly RichTextCaseProvider _richTextCaseProvider = new RichTextCaseProvider();

    private void DebugButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _richTextCaseProvider.Debug(TextEditor);
    }

    private void ShowDocumentBoundsButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ShowDocumentBoundsButton.IsChecked is true)
        {
            TextEditor.SkiaTextEditor.DebugConfiguration.ShowAllDebugBoundsWhenInDebugMode();
        }
        else
        {
            TextEditor.SkiaTextEditor.DebugConfiguration.ClearAllDebugBounds();
        }
    }

    private void ShowHandwritingPaperDebugInfoButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ShowHandwritingPaperDebugInfoButton.IsChecked is true)
        {
            TextEditor.SkiaTextEditor.DebugConfiguration.ShowHandwritingPaperDebugInfoWhenInDebugMode();
        }
        else
        {
            TextEditor.SkiaTextEditor.DebugConfiguration.ShowHandwritingPaperDebugInfo = false;
        }
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
