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

        var textEditorInfo = TextEditorInfo.GetTextEditorInfo(this);
        TextEditor textEditor = textEditorInfo.CurrentTextEditor;
        UpdateTextEditor(textEditor);

        textEditorInfo.CurrentTextEditorChanged += TextEditorInfo_OnCurrentTextEditorChanged;
    }

    private void TextEditorInfo_OnCurrentTextEditorChanged(object? sender, EventArgs e)
    {
        var textEditorInfo = TextEditorInfo.GetTextEditorInfo(this);
        TextEditor textEditor = textEditorInfo.CurrentTextEditor;
        UpdateTextEditor(textEditor);
    }

    private void UpdateTextEditor(TextEditor textEditor)
    {
        _currentTextEditor = textEditor;

        _currentTextEditor.CurrentSelectionChanged -= CurrentTextEditor_CurrentSelectionChanged;
        _currentTextEditor.CurrentSelectionChanged += CurrentTextEditor_CurrentSelectionChanged;

        _currentTextEditor.LayoutCompleted -=
            OnLayoutCompleted;
        _currentTextEditor.LayoutCompleted +=
            OnLayoutCompleted;

        SetCurrentCaretInfoText(textEditor.CurrentSelection);

        void OnLayoutCompleted(object? sender, LayoutCompletedEventArgs e)
        {
            SetCurrentCaretInfoText(textEditor.CurrentSelection);
        }
    }

    private void CurrentTextEditor_CurrentSelectionChanged(object? sender, TextEditorValueChangeEventArgs<Selection> e)
    {
        var selection = e.NewValue;
        SetCurrentCaretInfoText(in selection);
    }

    private void SetCurrentCaretInfoText(in Selection selection)
    {
        string caretInfo;

        if (selection.IsEmpty)
        {
            CaretOffset caretOffset = selection.StartOffset;
            if (caretOffset.IsAtLineStart)
            {
                caretInfo = $"光标位置: {caretOffset.Offset} 处于行首";
            }
            else
            {
                caretInfo = $"光标位置: {caretOffset.Offset}";
            }
        }
        else
        {
            caretInfo =
             $"选择范围: {selection.StartOffset.Offset} - {selection.EndOffset.Offset} 长度: {selection.Length}";
        }

        // 添加段落信息
        if (_currentTextEditor != null)
        {
            if (_currentTextEditor.TextEditorCore.TryGetRenderInfo(out var renderInfoProvider))
            {
                CaretOffset caretOffset = selection.StartOffset;
                var caretRenderInfo = renderInfoProvider.GetCaretRenderInfo(caretOffset);
                
                caretInfo += $" 第 {caretRenderInfo.ParagraphIndex.Index} 段 {caretRenderInfo.LineIndex} 行，行内第 {caretRenderInfo.HitLineCharOffset.Offset} 个字符";
            }
        }

        ViewModel.SetCurrentCaretInfoText(caretInfo);
    }

    private TextEditor? _currentTextEditor;

    private void DebugButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var textEditor = TextEditorInfo.GetTextEditorInfo(this).CurrentTextEditor;
        textEditor.SetInDebugMode();
        //textEditor.SkiaTextEditor.DebugConfiguration.DebugReRender();
    }
}