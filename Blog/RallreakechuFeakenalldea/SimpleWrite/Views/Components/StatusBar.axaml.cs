using Avalonia.Controls;
using Avalonia.Interactivity;

using LightTextEditorPlus;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Events;

namespace SimpleWrite.Views.Components;

public partial class StatusBar : UserControl
{
    public StatusBar()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        var textEditor = TextEditorInfo.GetTextEditorInfo(this).CurrentTextEditor;
        _currentTextEditor = textEditor;

        _currentTextEditor.CurrentSelectionChanged += CurrentTextEditor_CurrentSelectionChanged;
    }

    private void CurrentTextEditor_CurrentSelectionChanged(object? sender, TextEditorValueChangeEventArgs<Selection> e)
    {
        if (e.NewValue.IsEmpty)
        {
            var caretOffset = e.NewValue.StartOffset;
            if (caretOffset.IsAtLineStart)
            {
                StatusBarTextBlock.Text = $"光标位置: {caretOffset.Offset} 处于行首";
            }
            else
            {
                StatusBarTextBlock.Text = $"光标位置: {caretOffset.Offset}";
            }
        }
        else
        {
            StatusBarTextBlock.Text = "选择范围: " +
                                      $"{e.NewValue.StartOffset.Offset} - {e.NewValue.EndOffset.Offset} " +
                                      $"长度: {e.NewValue.Length}";
        }
    }

    private TextEditor? _currentTextEditor;

    private void DebugButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var textEditor = TextEditorInfo.GetTextEditorInfo(this).CurrentTextEditor;
        textEditor.SetInDebugMode();
        textEditor.SkiaTextEditor.DebugConfiguration.DebugReRender();
    }
}