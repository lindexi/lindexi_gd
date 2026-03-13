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
using Avalonia.Threading;
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

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (oldTextEditor != null)
            {
                oldTextEditor.CurrentSelectionChanged -= TextEditor_OnCurrentSelectionChanged;
            }

            _currentTextEditor.CurrentSelectionChanged -= TextEditor_OnCurrentSelectionChanged;
            _currentTextEditor.CurrentSelectionChanged += TextEditor_OnCurrentSelectionChanged;

            CurrentTextEditorChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? CurrentTextEditorChanged;

    private void TextEditor_OnCurrentSelectionChanged(object? sender, TextEditorValueChangeEventArgs<Selection> e)
    {
        var currentTextEditor = _currentTextEditor;

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

    /// <summary>
    /// 根据当前的光标坐标更新滚动条，让光标在滚动条内可见
    /// </summary>
    /// <param name="currentCaretOffset"></param>
    /// <param name="renderInfoProvider"></param>
    /// 滚动条跟随光标
    private void UpdateTextEditorScrollViewer(CaretOffset currentCaretOffset, RenderInfoProvider renderInfoProvider)
    {
        var caretBounds = renderInfoProvider.GetCaretRenderInfo(currentCaretOffset)
            .GetCaretBounds(CurrentTextEditor.CaretConfiguration.CaretThickness);

        var viewport = TextEditorScrollViewer.Viewport;
        if (viewport.Width <= 0 || viewport.Height <= 0)
        {
            return;
        }

        var currentOffset = TextEditorScrollViewer.Offset;
        var targetOffsetX = currentOffset.X;
        var targetOffsetY = currentOffset.Y;

        var viewportTop = currentOffset.Y;
        var viewportBottom = currentOffset.Y + viewport.Height;
        if (caretBounds.Top < viewportTop)
        {
            targetOffsetY = caretBounds.Top;
        }
        else if (caretBounds.Bottom > viewportBottom)
        {
            targetOffsetY = caretBounds.Bottom - viewport.Height;
        }

        var viewportLeft = currentOffset.X;
        var viewportRight = currentOffset.X + viewport.Width;
        if (caretBounds.Left < viewportLeft)
        {
            targetOffsetX = caretBounds.Left;
        }
        else if (caretBounds.Right > viewportRight)
        {
            targetOffsetX = caretBounds.Right - viewport.Width;
        }

        var extent = TextEditorScrollViewer.Extent;
        var maxOffsetX = Math.Max(0, extent.Width - viewport.Width);
        var maxOffsetY = Math.Max(0, extent.Height - viewport.Height);

        targetOffsetX = Math.Clamp(targetOffsetX, 0, maxOffsetX);
        targetOffsetY = Math.Clamp(targetOffsetY, 0, maxOffsetY);

        if (Math.Abs(targetOffsetX - currentOffset.X) < double.Epsilon
            && Math.Abs(targetOffsetY - currentOffset.Y) < double.Epsilon)
        {
            return;
        }

        // 如果发生在渲染过程中，调用强制布局，导致进入此方法的，那将会由于 `ScrollViewer.Offset` 请求刷新界面而抛异常
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            TextEditorScrollViewer.Offset = new Avalonia.Vector(targetOffsetX, targetOffsetY);
        });
    }
}