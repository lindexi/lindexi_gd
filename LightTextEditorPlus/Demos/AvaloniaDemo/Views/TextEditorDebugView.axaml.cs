using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;

using LightTextEditorPlus.AvaloniaDemo.Business;
using LightTextEditorPlus.AvaloniaDemo.Business.RichTextCases;
using LightTextEditorPlus.Core.Utils;
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
        CreateAndReplaceTextEditor();

        TextContext.FontNameManager.UseDefaultFontFallbackRules();

        _richTextCaseProvider = new RichTextCaseProvider(() => TextEditor);
        // 调试代码
        _richTextCaseProvider.Debug();
    }

    [MemberNotNull(nameof(TextEditor), nameof(_textEditor))]
    private void CreateAndReplaceTextEditor()
    {
        TextEditor = new TextEditor()
        {
            Width = 500,
        };
        TextEditorGrid.Children.RemoveAll(TextEditorGrid.Children.OfType<TextEditor>().ToList());
        TextEditorGrid.Children.Insert(0, TextEditor);

        // 调试绑定
        if (_debugTextPropertyBinding)
        {
            TextEditor.ShouldRaiseTextPropertyChanged = true;
            TextBox textBox = new TextBox()
            {
                Margin = new Thickness(0, 100, 0, 0)
            };
            textBox.Bind(TextBox.TextProperty, TextEditor.GetObservable(TextEditor.TextProperty));
            TextEditorGrid.Children.Insert(1, textBox);
        }

        TextEditor.AvaloniaLayoutUpdated += TextEditor_LayoutUpdated;
    }

    private void TextEditor_LayoutUpdated(object? sender, EventArgs e)
    {
        UpdateTextEditorBorder();
    }

    private void UpdateTextEditorBorder()
    {
        Rect textEditorBounds = TextEditor.Bounds;
        TextEditorBorder.Width = textEditorBounds.Width;
        TextEditorBorder.Height = textEditorBounds.Height;
    }

    private bool _debugTextPropertyBinding = false;

    public TextEditor TextEditor
    {
        get => _textEditor;
        [MemberNotNull(nameof(_textEditor))]
        private set
        {
            _textEditor = value;
            TextEditorSettingsControl.TextEditor = TextEditor;
        }
    }

    private TextEditor _textEditor;

    private readonly RichTextCaseProvider _richTextCaseProvider;

    private void DebugButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _richTextCaseProvider.Debug();
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
            TextEditor.SkiaTextEditor.DebugConfiguration.HideHandwritingPaperDebugInfoWhenInDebugMode();
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
