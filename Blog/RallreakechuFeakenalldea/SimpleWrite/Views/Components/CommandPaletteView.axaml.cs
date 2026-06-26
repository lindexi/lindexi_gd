using System;
using System.ComponentModel;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

using LightTextEditorPlus;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Rendering;

using SimpleWrite.Business.TextEditors;
using SimpleWrite.ViewModels;

namespace SimpleWrite.Views.Components;

/// <summary>
/// 浮动命令面板视图，提供命令搜索、键盘导航与拖拽移动能力。
/// </summary>
public partial class CommandPaletteView : UserControl
{
    private bool _isDragging;
    private Point _dragStartPoint;
    private Vector _dragStartOffset;
    private SimpleWriteTextEditor? _editor;

    /// <summary>
    /// 初始化 <see cref="CommandPaletteView"/> 的新实例。
    /// </summary>
    public CommandPaletteView()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// 获取当前绑定的 <see cref="CommandPaletteViewModel"/>。
    /// 从父级 <see cref="EditorViewModel"/> 的 DataContext 中获取。
    /// </summary>
    public CommandPaletteViewModel? ViewModel => (DataContext as EditorViewModel)?.CommandPaletteViewModel;

    /// <summary>
    /// 绑定到指定编辑器，订阅其命令面板请求事件。
    /// </summary>
    internal void Attach(SimpleWriteTextEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);
        Detach();

        _editor = editor;
        if (editor.TextEditorHandler is SimpleWriteTextEditorHandler handler)
        {
            handler.CommandPaletteRequested += OnCommandPaletteRequested;
        }
    }

    /// <summary>
    /// 解除与当前编辑器的绑定。
    /// </summary>
    internal void Detach()
    {
        if (_editor?.TextEditorHandler is SimpleWriteTextEditorHandler handler)
        {
            handler.CommandPaletteRequested -= OnCommandPaletteRequested;
        }

        _editor = null;
    }

    private void OnCommandPaletteRequested(object? sender, Point? rightClickPosition)
    {
        if (_editor is not { } editor || ViewModel is not { } viewModel)
        {
            return;
        }

        // 收集上下文文本
        // 有选区时取选区文本，无选区时取当前段落文本（单行模式）
        var selection = editor.CurrentSelection;
        string selectedText;
        bool isSingleLine;

        if (!selection.IsEmpty)
        {
            selectedText = editor.GetText(in selection);
            isSingleLine = false;
        }
        else
        {
            isSingleLine = true;

            // 右键时用命中测试获取点击位置的段落，快捷键时用光标所在段落
            if (rightClickPosition is { } hitPoint)
            {
                var textPoint = new TextPoint(hitPoint.X, hitPoint.Y);
                if (editor.TextEditorCore.TryHitTest(in textPoint, out var hitResult))
                {
                    var hitParagraph = hitResult.HitParagraphData;
                    selectedText = hitParagraph.IsEmptyParagraph ? string.Empty : hitParagraph.GetText();
                }
                else
                {
                    selectedText = string.Empty;
                }
            }
            else
            {
                selectedText = editor.GetCurrentCaretOffsetParagraph().GetText();
            }
        }

        if (string.IsNullOrEmpty(selectedText))
        {
            return;
        }

        // 计算锚点：右键用鼠标位置，快捷键用光标位置
        Point textEditorPoint;
        if (rightClickPosition is { } clickPosition)
        {
            textEditorPoint = clickPosition;
        }
        else if (editor.TextEditorCore.TryGetRenderInfo(out var renderInfo, autoLayoutEmptyTextEditor: false))
        {
            var caretInfo = renderInfo.GetCaretRenderInfo(editor.CurrentCaretOffset);
            var caretBounds = caretInfo.GetCaretBounds(editor.CaretConfiguration.CaretThickness);
            textEditorPoint = new Point(caretBounds.X, caretBounds.Bottom);
        }
        else
        {
            textEditorPoint = default;
        }

        // 定位面板
        PositionAt(textEditorPoint, editor);

        // 匹配命令并打开
        _ = viewModel.OpenAsync(selectedText, editor.CommandPatternManager, isSingleLine);
    }

    /// <summary>
    /// 将面板定位到 TextEditor 控件坐标系中的指定锚点，处理坐标转换和边界翻转。
    /// </summary>
    /// <param name="textEditorPoint">锚点坐标，相对于 <paramref name="editor"/> 控件左上角</param>
    /// <param name="editor">提供坐标转换来源的文本编辑器</param>
    private void PositionAt(Point textEditorPoint, TextEditor editor)
    {
        var host = this.GetVisualParent() as Visual;
        if (host is null)
        {
            return;
        }

        if (editor.TranslatePoint(textEditorPoint, host) is not { } hostPoint)
        {
            return;
        }

        Size hostSize = host.Bounds.Size;
        double panelWidth = Bounds.Width > 0 ? Bounds.Width : Width;
        double panelHeight = Bounds.Height > 0 ? Bounds.Height : MaxHeight;
        const double margin = 4;

        double x = hostPoint.X;
        double y = hostPoint.Y + margin;

        if (panelWidth > 0 && x + panelWidth > hostSize.Width)
        {
            x = Math.Max(0, hostSize.Width - panelWidth - margin);
        }

        double spaceBelow = hostSize.Height - y;
        if (panelHeight > 0 && spaceBelow < panelHeight)
        {
            y = hostPoint.Y - panelHeight - margin;
            if (y < 0)
            {
                y = margin;
            }
        }

        RenderTransform = new TranslateTransform(x, y);
    }

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (ViewModel is { } viewModel)
        {
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CommandPaletteViewModel.IsVisible))
        {
            if (ViewModel?.IsVisible == true)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    SearchInput.Focus();
                    SearchInput.SelectAll();
                    CommandListBox.SelectedIndex = 0;
                });
            }
        }
    }

    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                ViewModel?.Close();
                e.Handled = true;
                break;
            case Key.Enter:
                if (_editor is { } editor && ViewModel is { } viewModel
                    && CommandListBox.SelectedIndex is >= 0
                    && CommandListBox.ItemCount > CommandListBox.SelectedIndex)
                {
                    var item = (CommandPaletteItem) CommandListBox.Items[CommandListBox.SelectedIndex]!;
                    _ = viewModel.ExecuteAsync(item, editor);
                }

                e.Handled = true;
                break;
            case Key.Down:
                MoveSelection(1);
                e.Handled = true;
                break;
            case Key.Up:
                MoveSelection(-1);
                e.Handled = true;
                break;
        }
    }

    /// <summary>
    /// 在过滤命令列表中循环移动选中项。
    /// </summary>
    /// <param name="direction">移动方向，正值向后，负值向前。</param>
    private void MoveSelection(int direction)
    {
        int count = CommandListBox.ItemCount;
        if (count == 0)
        {
            return;
        }

        int currentIndex = CommandListBox.SelectedIndex;
        int newIndex = (currentIndex + direction + count) % count;
        CommandListBox.SelectedIndex = newIndex;
        CommandListBox.ScrollIntoView(newIndex);
    }

    private void CommandListBox_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_editor is not { } editor || ViewModel is not { } viewModel)
        {
            return;
        }

        // 通过命中测试获取被点击的 ListBoxItem，取其 DataContext 作为命令项
        if (e.Source is not Visual source)
        {
            return;
        }

        var item = source.FindAncestorOfType<ListBoxItem>(true)?.DataContext as CommandPaletteItem;
        if (item is null)
        {
            return;
        }

        _ = viewModel.ExecuteAsync(item, editor);
        e.Handled = true;
    }

    private void DragHandle_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Pointer.Captured == null)
        {
            _isDragging = true;
            _dragStartPoint = e.GetPosition(TopLevel.GetTopLevel(this) as Visual);
            _dragStartOffset = RenderTransform is TranslateTransform translate
                ? new Vector(translate.X, translate.Y)
                : Vector.Zero;
            e.Pointer.Capture(DragHandle);
            e.Handled = true;
        }
    }

    private void DragHandle_OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragging)
        {
            return;
        }

        Point currentPoint = e.GetPosition(TopLevel.GetTopLevel(this) as Visual);
        Vector delta = currentPoint - _dragStartPoint;
        RenderTransform = new TranslateTransform(_dragStartOffset.X + delta.X, _dragStartOffset.Y + delta.Y);
    }

    private void DragHandle_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }
}
