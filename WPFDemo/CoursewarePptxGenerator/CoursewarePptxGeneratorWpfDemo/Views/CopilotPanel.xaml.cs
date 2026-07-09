using System.Windows.Controls;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using CoursewarePptxGeneratorWpfDemo.ViewModels;
using Microsoft.Win32;

namespace CoursewarePptxGeneratorWpfDemo.Views;

/// <summary>
/// Interaction logic for CopilotPanel.xaml.
/// </summary>
public partial class CopilotPanel : UserControl
{
    private ScrollViewer? _chatScrollViewer;
    private INotifyCollectionChanged? _subscribedChatMessages;
    private bool _isUserAtBottom = true;

    public CopilotPanel()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        ChatListBox.Loaded += OnChatListBoxLoaded;
        ChatListBox.Unloaded += OnChatListBoxUnloaded;
    }

    private MainWindowViewModel ViewModel => DataContext as MainWindowViewModel ?? throw new InvalidOperationException("DataContext must be MainWindowViewModel.");

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MainWindowViewModel oldViewModel)
        {
            oldViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            UnsubscribeChatMessages();
        }

        if (e.NewValue is MainWindowViewModel newViewModel)
        {
            newViewModel.PropertyChanged += OnViewModelPropertyChanged;
            SubscribeChatMessages(newViewModel);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.CopilotChatManager) && sender is MainWindowViewModel viewModel)
        {
            SubscribeChatMessages(viewModel);
            ScrollToEndAndKeep();
        }
    }

    private void SubscribeChatMessages(MainWindowViewModel viewModel)
    {
        UnsubscribeChatMessages();
        _subscribedChatMessages = viewModel.CopilotChatManager.ChatMessages;
        _subscribedChatMessages.CollectionChanged += OnChatMessagesCollectionChanged;
    }

    private void UnsubscribeChatMessages()
    {
        if (_subscribedChatMessages is not null)
        {
            _subscribedChatMessages.CollectionChanged -= OnChatMessagesCollectionChanged;
            _subscribedChatMessages = null;
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
        if (e.Action == NotifyCollectionChangedAction.Add && _isUserAtBottom)
        {
            ScrollToEndAndKeep();
        }
    }

    private void OnChatScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (_chatScrollViewer is null)
        {
            return;
        }

        const double tolerance = 2.0;
        bool wasAtBottom = _isUserAtBottom;
        _isUserAtBottom = _chatScrollViewer.VerticalOffset >= _chatScrollViewer.ScrollableHeight - tolerance;
        if (wasAtBottom != _isUserAtBottom)
        {
            ScrollToBottomButton.Visibility = _isUserAtBottom ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    private void ScrollToBottomButton_OnClick(object sender, RoutedEventArgs e)
    {
        ScrollToEndAndKeep();
    }

    private void ScrollToEndAndKeep()
    {
        if (_chatScrollViewer is null)
        {
            return;
        }

        _isUserAtBottom = true;
        ScrollToBottomButton.Visibility = Visibility.Collapsed;
        _chatScrollViewer.ScrollToEnd();
        Dispatcher.InvokeAsync(() => _chatScrollViewer?.ScrollToEnd(), DispatcherPriority.Loaded);
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
}
