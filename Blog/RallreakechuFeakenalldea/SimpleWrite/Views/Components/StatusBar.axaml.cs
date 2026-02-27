using System;

using Avalonia.Controls;
using Avalonia.Interactivity;

using LightTextEditorPlus;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Events;

using SimpleWrite.ViewModels;

namespace SimpleWrite.Views.Components;

public partial class StatusBar : UserControl
{
    public StatusBar()
    {
        InitializeComponent();
        this.DataContextChanged += StatusBar_DataContextChanged;
    }

    public StatusViewModel ViewModel => (DataContext as StatusViewModel)
        ?? throw new InvalidOperationException("StatusBar's DataContext must be of type StatusViewModel");

    private void StatusBar_DataContextChanged(object? sender, EventArgs e)
    {
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        if (Design.IsDesignMode)
        {
            return;
        }

        _ = ViewModel;
        base.OnLoaded(e);

        var textEditor = TextEditorInfo.GetTextEditorInfo(this).CurrentTextEditor;
        _currentTextEditor = textEditor;

        _currentTextEditor.CurrentSelectionChanged += CurrentTextEditor_CurrentSelectionChanged;
        SetCurrentCaretInfoText(textEditor.CurrentSelection);
    }

    private void CurrentTextEditor_CurrentSelectionChanged(object? sender, TextEditorValueChangeEventArgs<Selection> e)
    {
        var selection = e.NewValue;
        SetCurrentCaretInfoText(in selection);
    }

    private void SetCurrentCaretInfoText(in Selection selection)
    {
        if (selection.IsEmpty)
        {
            CaretOffset caretOffset = selection.StartOffset;
            if (caretOffset.IsAtLineStart)
            {
                ViewModel.SetCurrentCaretInfoText($"光标位置: {caretOffset.Offset} 处于行首");
            }
            else
            {
                ViewModel.SetCurrentCaretInfoText($"光标位置: {caretOffset.Offset}");
            }
        }
        else
        {
            ViewModel.SetCurrentCaretInfoText
            (
                $"选择范围: {selection.StartOffset.Offset} - {selection.EndOffset.Offset} 长度: {selection.Length}"
            );
        }
    }

    private TextEditor? _currentTextEditor;

    private void DebugButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var textEditor = TextEditorInfo.GetTextEditorInfo(this).CurrentTextEditor;
        textEditor.SetInDebugMode();
        //textEditor.SkiaTextEditor.DebugConfiguration.DebugReRender();
    }
}