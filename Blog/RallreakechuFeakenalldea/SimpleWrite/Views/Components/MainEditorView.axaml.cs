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
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Events;
using LightTextEditorPlus.Core.Rendering;
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
            var oldTextEditor = _currentTextEditor;
            _currentTextEditor = value;
            TextEditorScrollViewer.Content = value;

            oldTextEditor.CurrentSelectionChanged -= TextEditor_OnCurrentSelectionChanged;
            _currentTextEditor.CurrentSelectionChanged -= TextEditor_OnCurrentSelectionChanged;
            _currentTextEditor.CurrentSelectionChanged += TextEditor_OnCurrentSelectionChanged;

            CurrentTextEditorChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? CurrentTextEditorChanged;

    private void TextEditor_OnCurrentSelectionChanged(object? sender, TextEditorValueChangeEventArgs<Selection> e)
    {
        var currentTextEditor = _currentTextEditor;
        if (!ReferenceEquals(sender, currentTextEditor))
        {
            return;
        }

        if (currentTextEditor.TextEditorCore.TryGetRenderInfo(out var renderInfo, autoLayoutEmptyTextEditor: false))
        {
            UpdateTextEditorScrollViewer(currentTextEditor.CurrentCaretOffset, renderInfo);
        }
        else
        {
            currentTextEditor.LayoutCompleted -= OnLayoutCompleted;
            currentTextEditor.LayoutCompleted += OnLayoutCompleted;
        }

        void OnLayoutCompleted(object? s,
            LayoutCompletedEventArgs layoutCompletedEventArgs)
        {
            if (currentTextEditor.TextEditorCore.TryGetRenderInfo(out var renderInfo2, autoLayoutEmptyTextEditor: false))
            {
                // 能获取到，一次性即可
                currentTextEditor.LayoutCompleted -= OnLayoutCompleted;

                UpdateTextEditorScrollViewer(currentTextEditor.CurrentCaretOffset, renderInfo2);
            }
            else
            {
                // 等待下一次 LayoutCompleted 事件
                // 等待的方法就是啥都不干，等下一次事件进入
            }
        }
    }

    private void UpdateTextEditorScrollViewer(CaretOffset currentCaretOffset, RenderInfoProvider renderInfoProvider)
    {
        // 根据当前的光标坐标更新滚动条，让光标在滚动条内可见
    }
}