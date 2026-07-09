using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace NucerenihaHalqahehi.Views;

/// <summary>
/// Provides an interactive experiment for keeping growing Avalonia lists scrolled to the bottom.
/// </summary>
public partial class KeepScrollToBottomView : UserControl
{
    private const double BottomTolerance = 8;
    private const int MaxLogCount = 80;
    private readonly ObservableCollection<string> _normalMessages = [];
    private readonly ObservableCollection<string> _listBoxMessages = [];
    private readonly ObservableCollection<string> _logs = [];
    private int _normalMessageIndex;
    private int _listBoxMessageIndex;
    private ScrollViewer? _listBoxScrollViewer;
    private bool _isListBoxScrollChangedSubscribed;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeepScrollToBottomView"/> class.
    /// </summary>
    public KeepScrollToBottomView()
    {
        InitializeComponent();
        NormalItemsControl.ItemsSource = _normalMessages;
        MessagesListBox.ItemsSource = _listBoxMessages;
        LogListBox.ItemsSource = _logs;
        AddNormalMessageButton.Click += OnAddNormalMessageButtonClick;
        GrowNormalLastButton.Click += OnGrowNormalLastButtonClick;
        ClearNormalButton.Click += OnClearNormalButtonClick;
        AddListBoxMessageButton.Click += OnAddListBoxMessageButtonClick;
        GrowListBoxLastButton.Click += OnGrowListBoxLastButtonClick;
        ClearListBoxButton.Click += OnClearListBoxButtonClick;
        NormalScrollViewer.ScrollChanged += OnNormalScrollChanged;
        MessagesListBox.Loaded += OnMessagesListBoxLoaded;
        SeedMessages();
        UpdateScrollStateTexts();
        AddLog("初始化完成");
    }

    private bool KeepToBottom => KeepToBottomCheckBox.IsChecked == true;

    private void OnAddNormalMessageButtonClick(object? sender, RoutedEventArgs e)
    {
        var shouldKeepBottom = ShouldKeepNormalBottom();
        _normalMessages.Add(CreateMessage("普通", ++_normalMessageIndex));
        AddLog($"普通列表追加，原本接近底部={shouldKeepBottom}");
        KeepNormalBottomIfNeeded(shouldKeepBottom);
    }

    private void OnGrowNormalLastButtonClick(object? sender, RoutedEventArgs e)
    {
        var shouldKeepBottom = ShouldKeepNormalBottom();
        GrowLastMessage(_normalMessages, "普通");
        AddLog($"普通列表增长最后一条，原本接近底部={shouldKeepBottom}");
        KeepNormalBottomIfNeeded(shouldKeepBottom);
    }

    private void OnClearNormalButtonClick(object? sender, RoutedEventArgs e)
    {
        _normalMessages.Clear();
        _normalMessageIndex = 0;
        AddLog("普通列表已清空");
        QueueStateUpdate();
    }

    private void OnAddListBoxMessageButtonClick(object? sender, RoutedEventArgs e)
    {
        var shouldKeepBottom = ShouldKeepListBoxBottom();
        _listBoxMessages.Add(CreateMessage("ListBox", ++_listBoxMessageIndex));
        AddLog($"ListBox 追加，原本接近底部={shouldKeepBottom}");
        KeepListBoxBottomIfNeeded(shouldKeepBottom);
    }

    private void OnGrowListBoxLastButtonClick(object? sender, RoutedEventArgs e)
    {
        var shouldKeepBottom = ShouldKeepListBoxBottom();
        GrowLastMessage(_listBoxMessages, "ListBox");
        AddLog($"ListBox 增长最后一条，原本接近底部={shouldKeepBottom}");
        KeepListBoxBottomIfNeeded(shouldKeepBottom);
    }

    private void OnClearListBoxButtonClick(object? sender, RoutedEventArgs e)
    {
        _listBoxMessages.Clear();
        _listBoxMessageIndex = 0;
        AddLog("ListBox 已清空");
        QueueStateUpdate();
    }

    private void OnNormalScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        UpdateScrollStateTexts();
        AddLog($"普通 ScrollChanged Offset={Format(NormalScrollViewer.Offset.Y)} Extent={Format(NormalScrollViewer.Extent.Height)} Viewport={Format(NormalScrollViewer.Viewport.Height)}");
    }

    private void OnListBoxScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        UpdateScrollStateTexts();
        if (_listBoxScrollViewer is not null)
        {
            AddLog($"ListBox ScrollChanged Offset={Format(_listBoxScrollViewer.Offset.Y)} Extent={Format(_listBoxScrollViewer.Extent.Height)} Viewport={Format(_listBoxScrollViewer.Viewport.Height)}");
        }
    }

    private void OnMessagesListBoxLoaded(object? sender, RoutedEventArgs e)
    {
        var scrollViewer = MessagesListBox.FindDescendantOfType<ScrollViewer>();
        if (!ReferenceEquals(_listBoxScrollViewer, scrollViewer))
        {
            UnsubscribeListBoxScrollChanged();
            _listBoxScrollViewer = scrollViewer;
        }

        if (_listBoxScrollViewer is not null)
        {
            if (!_isListBoxScrollChangedSubscribed)
            {
                _listBoxScrollViewer.ScrollChanged += OnListBoxScrollChanged;
                _isListBoxScrollChangedSubscribed = true;
            }

            AddLog("已获取 ListBox 内部 ScrollViewer");
        }
        else
        {
            AddLog("未获取到 ListBox 内部 ScrollViewer");
        }
        UpdateScrollStateTexts();
    }

    private void UnsubscribeListBoxScrollChanged()
    {
        if (_listBoxScrollViewer is null || !_isListBoxScrollChangedSubscribed)
        {
            return;
        }

        _listBoxScrollViewer.ScrollChanged -= OnListBoxScrollChanged;
        _isListBoxScrollChangedSubscribed = false;
    }

    private void SeedMessages()
    {
        for (var i = 0; i < 30; i++)
        {
            _normalMessages.Add(CreateMessage("普通", ++_normalMessageIndex));
            _listBoxMessages.Add(CreateMessage("ListBox", ++_listBoxMessageIndex));
        }
    }

    private static string CreateMessage(string areaName, int index)
    {
        return $"{areaName} 消息 {index:00} - 用于观察滚动偏移、内容高度和视口高度。";
    }

    private static void GrowLastMessage(ObservableCollection<string> messages, string areaName)
    {
        if (messages.Count == 0)
        {
            messages.Add(CreateMessage(areaName, 1));
            return;
        }
        var lastIndex = messages.Count - 1;
        messages[lastIndex] = $"{messages[lastIndex]}{Environment.NewLine}增长内容：{DateTime.Now:HH:mm:ss.fff}。这会增加最后一条消息的高度，用来观察 Extent 增长后的滚动保持效果。";
    }

    private bool ShouldKeepNormalBottom()
    {
        return KeepToBottom && IsNearBottom(NormalScrollViewer);
    }

    private bool ShouldKeepListBoxBottom()
    {
        return KeepToBottom && _listBoxScrollViewer is not null && IsNearBottom(_listBoxScrollViewer);
    }

    private static bool IsNearBottom(ScrollViewer scrollViewer)
    {
        var bottomOffset = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);
        return bottomOffset - scrollViewer.Offset.Y <= BottomTolerance;
    }

    private void KeepNormalBottomIfNeeded(bool shouldKeepBottom)
    {
        if (!shouldKeepBottom)
        {
            QueueStateUpdate();
            return;
        }
        NormalScrollViewer.ScrollToEnd();
        Dispatcher.UIThread.Post(() =>
        {
            NormalScrollViewer.ScrollToEnd();
            UpdateScrollStateTexts();
            AddLog("普通列表已执行 Dispatcher 延迟滚动到底部");
        }, DispatcherPriority.Loaded);
    }

    private void KeepListBoxBottomIfNeeded(bool shouldKeepBottom)
    {
        if (!shouldKeepBottom)
        {
            QueueStateUpdate();
            return;
        }
        ScrollListBoxToEnd();
        Dispatcher.UIThread.Post(() =>
        {
            ScrollListBoxToEnd();
            UpdateScrollStateTexts();
            AddLog("ListBox 已执行 Dispatcher 延迟滚动到底部");
        }, DispatcherPriority.Loaded);
    }

    private void ScrollListBoxToEnd()
    {
        if (_listBoxMessages.Count == 0)
        {
            return;
        }
        var lastItem = _listBoxMessages[^1];
        MessagesListBox.ScrollIntoView(lastItem);
        _listBoxScrollViewer?.ScrollToEnd();
    }

    private void QueueStateUpdate()
    {
        Dispatcher.UIThread.Post(UpdateScrollStateTexts, DispatcherPriority.Loaded);
    }

    private void UpdateScrollStateTexts()
    {
        NormalScrollStateTextBlock.Text = CreateStateText(NormalScrollViewer);
        ListBoxScrollStateTextBlock.Text = _listBoxScrollViewer is null ? "内部 ScrollViewer 尚未加载" : CreateStateText(_listBoxScrollViewer);
    }

    private static string CreateStateText(ScrollViewer scrollViewer)
    {
        return $"Offset={Format(scrollViewer.Offset.Y)}  Extent={Format(scrollViewer.Extent.Height)}  Viewport={Format(scrollViewer.Viewport.Height)}  NearBottom={IsNearBottom(scrollViewer)}";
    }

    private void AddLog(string message)
    {
        _logs.Insert(0, $"{DateTime.Now:HH:mm:ss.fff} {message}");
        if (_logs.Count > MaxLogCount)
        {
            _logs.RemoveAt(_logs.Count - 1);
        }
    }

    private static string Format(double value)
    {
        return value.ToString("0.##");
    }
}
