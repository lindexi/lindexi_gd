using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
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
        TextEditorScrollViewer.ScrollChanged += TextEditorScrollViewer_OnScrollChanged;
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
        else if (DataContext is SimpleWriteMainViewModel mainViewModel)
        {
            DataContext = mainViewModel.EditorViewModel;
        }

        base.OnDataContextChanged(e);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        // 提供 View 层的功能给 ViewModel 使用
        if (TopLevel.GetTopLevel(this) is { } topLevel)
        {
            var filePickerHandler = ViewModel.FilePickerHandler ?? new FilePickerHandler(topLevel);
            ViewModel.FilePickerHandler = filePickerHandler;
            ViewModel.MainViewModel.FolderExplorerViewModel.FilePickerHandler ??= filePickerHandler;
        }

        AddHandler(PointerPressedEvent, OnGlobalPointerPressed, RoutingStrategies.Tunnel);

        base.OnLoaded(e);
    }

    private void EditorViewModel_EditorModelChanged(object? sender, EventArgs e)
    {
        UpdateCurrentEditorMode(ViewModel.CurrentEditorModel);
    }

    private void UpdateCurrentEditorMode(EditorModel editorModel)
    {
        if (_currentEditorModel is { } currentEditorModel)
        {
            CaptureRuntimeState(currentEditorModel);
        }

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

        _currentEditorModel = editorModel;
        CurrentTextEditor = textEditor;
        RestoreRuntimeState(editorModel, textEditor);
    }

    private EditorModel? _currentEditorModel;

    private bool _isRestoringEditorRuntimeState;

    private TextEditor _currentTextEditor = null!;

    public TextEditor CurrentTextEditor
    {
        get => _currentTextEditor;
        private set
        {
            var oldTextEditor = _currentTextEditor;
            _currentTextEditor = value;

            // 切换编辑器时先 Detach 旧编辑器的命令面板事件
            if (oldTextEditor is SimpleWriteTextEditor oldSimpleEditor)
            {
                CommandPalette.Detach();
            }

            // 将 TextEditor 放入 Grid 的 Column 1
            Grid.SetColumn(value, 1);
            if (EditorGrid.Children.Contains(value))
            {
                // 已经在 Grid 中，无需重复添加
            }
            else
            {
                EditorGrid.Children.Add(value);
            }

            // 切换编辑器时先 Detach 旧的，再 Attach 新的
            LineNumberArea.Detach();
            LineNumberArea.Attach(value);

            // 显式设置 ScrollViewer，避免控件内部向上查找
            // LineNumberControl 的 ScrollViewer 通过 XAML 绑定注入，无需代码设置
            if (value is SimpleWriteTextEditor simpleWriteTextEditor)
            {
                simpleWriteTextEditor.SetScrollViewer(TextEditorScrollViewer);
            }

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (oldTextEditor != null)
            {
                oldTextEditor.CurrentSelectionChanged -= TextEditor_OnCurrentSelectionChanged;
                // 从 Grid 中移除旧的编辑器
                if (!ReferenceEquals(oldTextEditor, value))
                {
                    EditorGrid.Children.Remove(oldTextEditor);
                }
            }

            _currentTextEditor.CurrentSelectionChanged -= TextEditor_OnCurrentSelectionChanged;
            _currentTextEditor.CurrentSelectionChanged += TextEditor_OnCurrentSelectionChanged;

            // Attach 新编辑器的命令面板事件
            if (value is SimpleWriteTextEditor newSimpleEditor)
            {
                CommandPalette.Attach(newSimpleEditor);
            }

            CurrentTextEditorChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? CurrentTextEditorChanged;

    private void TextEditor_OnCurrentSelectionChanged(object? sender, TextEditorValueChangeEventArgs<Selection> e)
    {
        var currentTextEditor = _currentTextEditor;

        if (!_isRestoringEditorRuntimeState
            && ReferenceEquals(sender, currentTextEditor)
            && _currentEditorModel is not null)
        {
            _currentEditorModel.RuntimeSelection = e.NewValue;
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

    private void TextEditorScrollViewer_OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (_isRestoringEditorRuntimeState || _currentEditorModel is null)
        {
            return;
        }

        var offset = TextEditorScrollViewer.Offset;
        _currentEditorModel.RuntimeScrollOffsetX = offset.X;
        _currentEditorModel.RuntimeScrollOffsetY = offset.Y;

        // GetViewport() 返回 ScrollViewer 的 Offset 和 Viewport，
        // 滚动时视口变化，需要通知 TextEditor 重新渲染以更新可见范围内容
        _currentTextEditor.InvalidateVisual();
    }

    private void CaptureRuntimeState(EditorModel editorModel)
    {
        if (editorModel.TextEditor is { } textEditor)
        {
            editorModel.RuntimeSelection = textEditor.CurrentSelection;
        }

        if (ReferenceEquals(editorModel, _currentEditorModel))
        {
            var offset = TextEditorScrollViewer.Offset;
            editorModel.RuntimeScrollOffsetX = offset.X;
            editorModel.RuntimeScrollOffsetY = offset.Y;
        }
    }

    private void RestoreRuntimeState(EditorModel editorModel, TextEditor textEditor)
    {
        _isRestoringEditorRuntimeState = true;

        Dispatcher.UIThread.Post(() =>
        {
            textEditor.CurrentSelection = editorModel.RuntimeSelection;
            RestoreScrollOffset(editorModel, textEditor);
            _isRestoringEditorRuntimeState = false;

        }, DispatcherPriority.Background);
    }

    private void RestoreScrollOffset(EditorModel editorModel, TextEditor textEditor)
    {
        if (!ReferenceEquals(editorModel, _currentEditorModel) || !ReferenceEquals(textEditor, _currentTextEditor))
        {
            return;
        }

        if (!TryApplyScrollOffset(editorModel))
        {
            textEditor.LayoutCompleted += OnLayoutCompleted;
        }

        void OnLayoutCompleted(object? sender, LayoutCompletedEventArgs e)
        {
            if (!ReferenceEquals(editorModel, _currentEditorModel) || !ReferenceEquals(textEditor, _currentTextEditor))
            {
                textEditor.LayoutCompleted -= OnLayoutCompleted;
                return;
            }

            if (TryApplyScrollOffset(editorModel))
            {
                textEditor.LayoutCompleted -= OnLayoutCompleted;
            }
        }
    }

    private bool TryApplyScrollOffset(EditorModel editorModel)
    {
        var viewport = TextEditorScrollViewer.Viewport;
        if (viewport.Width <= 0 || viewport.Height <= 0)
        {
            return false;
        }

        var extent = TextEditorScrollViewer.Extent;
        var maxOffsetX = Math.Max(0, extent.Width - viewport.Width);
        var maxOffsetY = Math.Max(0, extent.Height - viewport.Height);
        var targetOffset = new Avalonia.Vector(
            Math.Clamp(editorModel.RuntimeScrollOffsetX, 0, maxOffsetX),
            Math.Clamp(editorModel.RuntimeScrollOffsetY, 0, maxOffsetY));

        if (TextEditorScrollViewer.Offset == targetOffset)
        {
            return true;
        }

        _isRestoringEditorRuntimeState = true;
        try
        {
            TextEditorScrollViewer.Offset = targetOffset;
        }
        finally
        {
            _isRestoringEditorRuntimeState = false;
        }

        return true;
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

    private void OnGlobalPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not EditorViewModel viewModel) return;
        if (!viewModel.CommandPaletteViewModel.IsVisible) return;

        // 判断点击是否在面板内
        var position = e.GetPosition(CommandPalette);
        var bounds = CommandPalette.Bounds;
        if (position.X >= 0 && position.X <= bounds.Width
            && position.Y >= 0 && position.Y <= bounds.Height)
        {
            return; // 点击在面板内，不关闭
        }

        viewModel.CommandPaletteViewModel.Close();
    }
}