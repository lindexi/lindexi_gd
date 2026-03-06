using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

using LightTextEditorPlus;
using LightTextEditorPlus.Primitive;
using SimpleWrite.Business.ShortcutManagers;
using SimpleWrite.Models;
using SimpleWrite.ViewModels;

using SkiaSharp;

using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Interactivity;
using SimpleWrite.Business.FileHandlers;
using SimpleWrite.Business.TextEditors;

namespace SimpleWrite.Views.Components;

public partial class MainEditorView : UserControl
{
    public MainEditorView()
    {
        InitializeComponent();
    }

    public EditorViewModel ViewModel => DataContext as EditorViewModel
        ?? throw new InvalidOperationException("DataContext must be of type EditorViewModel");

    protected override void OnDataContextChanged(EventArgs e)
    {
        if (DataContext is EditorViewModel editorViewModel)
        {
            editorViewModel.EditorModelChanged += EditorViewModel_EditorModelChanged;
            UpdateCurrentEditorMode(editorViewModel.CurrentEditorModel);
        }

        base.OnDataContextChanged(e);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        // 提供 View 层的功能给 ViewModel 使用
        if (TopLevel.GetTopLevel(this) is { } topLevel)
        {
            ViewModel.FilePickerHandler ??= new FilePickerHandler(topLevel);
        }

        base.OnLoaded(e);
    }

    private void EditorViewModel_EditorModelChanged(object? sender, EventArgs e)
    {
        UpdateCurrentEditorMode(ViewModel.CurrentEditorModel);
    }

    private void UpdateCurrentEditorMode(EditorModel editorModel)
    {
        var textEditor = editorModel.TextEditor;
        if (textEditor is null)
        {
            //TextEditor textEditor = CreateTextEditor(editorModel);
            //editorModel.TextEditor = textEditor;

            //// 如果此时已经有文件了，就加载文件内容
            //if (editorModel.FileInfo is { } fileInfo)
            //{
            //    _ = ViewModel.LoadFileToTextEditorAsync(editorModel, textEditor, fileInfo);
            //}
            textEditor = ViewModel.EnsureTextEditor(editorModel);
        }

        CurrentTextEditor = textEditor;
    }

    

    private TextEditor _currentTextEditor = null!;

    public TextEditor CurrentTextEditor
    {
        get => _currentTextEditor;
        private set
        {
            _currentTextEditor = value;
            TextEditorScrollViewer.Content = value;

            CurrentTextEditorChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? CurrentTextEditorChanged;
}