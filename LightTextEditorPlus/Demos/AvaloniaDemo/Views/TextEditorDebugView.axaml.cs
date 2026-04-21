using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;

using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Utils;
using LightTextEditorPlus.Demo.Business.RichTextCases;
using LightTextEditorPlus.FontManagers;

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace LightTextEditorPlus.AvaloniaDemo.Views;

public partial class TextEditorDebugView : UserControl
{
    public TextEditorDebugView() : this(runDebug: true)
    {
        // 不使用构造函数默认参数是因为试图解决 Avalonia warning AVLN3001: XAML resource "avares://LightTextEditorPlus.AvaloniaDemo/Views/TextEditorDebugView.axaml" won't be reachable via runtime loader, as no public constructor was found 
    }

    public TextEditorDebugView(bool runDebug)
    {
        InitializeComponent();

        CreateAndReplaceTextEditor();
        RichTextCaseProvider = new RichTextCaseProvider(() => TextEditor);

        if (runDebug)
        {
            //var fontFamily = (FontFamily) Application.Current!.Resources["TestMeatballFontFamily"]!;
            //TextEditorFontManager.RegisterFontNameToResource("仓耳小丸子", fontFamily);
            FileInfo fontFile = new FileInfo(Path.Join(AppContext.BaseDirectory, "Assets", "Fonts", "仓耳小丸子.ttf"));
            if (fontFile.Exists)
            {
                TextEditorFontResourceManager.TryRegisterFontNameToResource("仓耳小丸子",
                    fontFile);
            }

            TextContext.GlobalFontNameManager.UseDefaultFontFallbackRules();

            // 调试代码
            RichTextCaseProvider.Debug();
        }

        TextEditorGrid.PointerWheelChanged += TextEditorGrid_OnPointerWheelChanged;

        void TextEditorGrid_OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if ((e.KeyModifiers & KeyModifiers.Control) == 0)
            {
                return;
            }

            if (TextEditorGrid.RenderTransform is not ScaleTransform scaleTransform)
            {
                scaleTransform = new ScaleTransform();
                TextEditorGrid.RenderTransform = scaleTransform;
                TextEditorGrid.RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Relative);
            }

            var delta = e.Delta.Y / 10;

            scaleTransform.ScaleX += delta;
            scaleTransform.ScaleY += delta;

            scaleTransform.ScaleX = Math.Max(0.1, scaleTransform.ScaleX);
            scaleTransform.ScaleY = Math.Max(0.1, scaleTransform.ScaleY);
        }
    }

    [MemberNotNull(nameof(TextEditor), nameof(_textEditor))]
    private void CreateAndReplaceTextEditor()
    {
        TextEditor = new TextEditor()
        {
            Width = 500,
            Logger = new TextConsoleLogger(),
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
            TextEditorDebugBoundsSettingsControl.TextEditor = TextEditor;
        }
    }

    private TextEditor _textEditor;

    public RichTextCaseProvider RichTextCaseProvider { get; }

    private void DebugButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _ = RichTextCaseProvider;
        TextEditor.TextEditorCore.DebugRequireReUpdateAllDocumentLayout();

        //_richTextCaseProvider.Debug();
    }

    private void ShowDocumentBoundsButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (ShowDocumentBoundsButton.IsChecked is true)
        {
            TextEditorDebugBoundsSettingsControl.Update();
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

public class TextConsoleLogger : ITextLogger
{
    public void LogDebug(string message)
    {
        RecordMessage($"[Debug] {message}");
    }

    public void LogException(Exception exception, string? message)
    {
        RecordMessage($"[Warn] {message} 异常:{exception}");
    }

    public void LogInfo(string message)
    {
        RecordMessage($"[Info] {message}");
    }

    public void LogWarning(string message)
    {
        RecordMessage($"[Warn] {message}");
    }

    public void Log<T>(T info) where T : notnull
    {
        RecordMessage($"[{info.GetType().Name}] {info.ToString()}");
    }

    private void RecordMessage(string message, bool outputToDebug = true)
    {
        if (outputToDebug)
        {
            Debug.WriteLine(message);
        }

        Console.WriteLine(message);
    }
}