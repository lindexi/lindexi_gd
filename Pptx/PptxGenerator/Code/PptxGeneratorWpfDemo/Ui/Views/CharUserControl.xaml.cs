using PptxGenerator;

using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using AgentLib.Model;

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
            oldVm.ChatMessages.CollectionChanged -= OnChatMessagesCollectionChanged;
        }

        // 订阅新的
        if (e.NewValue is MainWindowViewModel newVm)
        {
            newVm.ChatMessages.CollectionChanged += OnChatMessagesCollectionChanged;
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
        _isUserAtBottom = _chatScrollViewer.VerticalOffset >= _chatScrollViewer.ScrollableHeight - tolerance;
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
}
