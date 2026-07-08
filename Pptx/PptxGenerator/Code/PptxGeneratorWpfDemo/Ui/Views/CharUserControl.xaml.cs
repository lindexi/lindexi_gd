using PptxGenerator;

using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

using Microsoft.Win32;

namespace PptxGeneratorWpfDemo;

public partial class CharUserControl : UserControl
{
    private ScrollViewer? _chatScrollViewer;
    private bool _isUserAtBottom = true;

    public CharUserControl()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        ChatListBox.Loaded += OnChatListBoxLoaded;
        ChatListBox.Unloaded += OnChatListBoxUnloaded;
    }

    public MainWindowViewModel ViewModel => DataContext as MainWindowViewModel ?? throw new InvalidCastException("DataContext must be of type MainWindowViewModel.");

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        // 取消旧的订阅
        if (e.OldValue is MainWindowViewModel oldVm)
        {
            oldVm.CopilotChatManager.ChatMessages.CollectionChanged -= OnChatMessagesCollectionChanged;
        }

        // 订阅新的
        if (e.NewValue is MainWindowViewModel newVm)
        {
            newVm.CopilotChatManager.ChatMessages.CollectionChanged += OnChatMessagesCollectionChanged;
        }
    }

    private void OnChatListBoxLoaded(object sender, RoutedEventArgs e)
    {
        _chatScrollViewer = FindVisualChild<ScrollViewer>(ChatListBox);
        if (_chatScrollViewer is not null)
        {
            _chatScrollViewer.ScrollChanged += OnChatScrollViewerScrollChanged;
        }
    }

    private void OnChatListBoxUnloaded(object sender, RoutedEventArgs e)
    {
        if (_chatScrollViewer is not null)
        {
            _chatScrollViewer.ScrollChanged -= OnChatScrollViewerScrollChanged;
            _chatScrollViewer = null;
        }
    }

    private void OnChatMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // 仅在新添加消息且用户在底部时才自动滚动
        if (e.Action == NotifyCollectionChangedAction.Add && _isUserAtBottom)
        {
            if (_chatScrollViewer is not null && _chatScrollViewer.IsLoaded)
            {
                _chatScrollViewer.ScrollToEnd();
            }
        }
    }

    private void OnChatScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (_chatScrollViewer is null) return;

        // 判断用户是否处于底部
        // 使用 2 像素容差避免浮点精度问题
        const double tolerance = 2.0;
        bool wasAtBottom = _isUserAtBottom;
        _isUserAtBottom = _chatScrollViewer.VerticalOffset >= _chatScrollViewer.ScrollableHeight - tolerance;

        // 用户离开底部时显示按钮，回到底部时隐藏
        if (wasAtBottom != _isUserAtBottom)
        {
            ScrollToBottomButton.Visibility = _isUserAtBottom
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
    }

    private void ScrollToBottomButton_OnClick(object sender, RoutedEventArgs e)
    {
        ScrollToEndAndKeep();
    }

    /// <summary>
    /// 滚动到底部并恢复自动跟随模式。
    /// 由于 ListBox 使用物理滚动（CanContentScroll=False），ScrollToEnd 一次即可到位；
    /// 再通过 Dispatcher 延迟一次以确保布局完成后仍处于底部。
    /// </summary>
    private void ScrollToEndAndKeep()
    {
        if (_chatScrollViewer is null) return;

        _isUserAtBottom = true;
        ScrollToBottomButton.Visibility = Visibility.Collapsed;
        _chatScrollViewer.ScrollToEnd();

        // 等待布局完成后再滚一次，确保新内容被纳入后仍在底部
        Dispatcher.InvokeAsync(
            () => _chatScrollViewer?.ScrollToEnd(),
            DispatcherPriority.Loaded);
    }

    /// <summary>
    /// 在可视树中查找指定类型的子元素。
    /// </summary>
    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T result)
            {
                return result;
            }

            var descendant = FindVisualChild<T>(child);
            if (descendant is not null)
            {
                return descendant;
            }
        }
        return null;
    }

    private async void ReduceButton_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.StatusText = "正在总结对话...";
        try
        {
            await ViewModel.SlideChatManager.Pipeline.ChatManager.ReduceSessionAsync();
            ViewModel.StatusText = "总结完成";
        }
        catch (Exception)
        {
            ViewModel.StatusText = "总结失败";
        }
    }


    private void AttachImageButton_OnClick(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "选择图片文件",
            Multiselect = true,
            Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp",
        };

        if (openFileDialog.ShowDialog() == true)
        {
            ViewModel.AddAttachedImageFiles(openFileDialog.FileNames);
        }
    }

    private void CloseAttachedImageButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: FileInfo fileInfo })
        {
            ViewModel.AttachedImageFiles.Remove(fileInfo);
        }
    }

    private void AttachedImage_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || sender is not FrameworkElement { DataContext: FileInfo fileInfo })
        {
            return;
        }

        if (!fileInfo.Exists)
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"/select,\"{fileInfo.FullName}\"",
            UseShellExecute = true,
        });

        e.Handled = true;
    }

}
