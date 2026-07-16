using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace CoursewarePptxGeneratorWpfDemo.Views;

/// <summary>
/// Displays a reusable Copilot message stream with bottom-follow behavior.
/// </summary>
public partial class CopilotMessageStream : UserControl
{
    /// <summary>Identifies the <see cref="ItemsSource" /> dependency property.</summary>
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource),
        typeof(IEnumerable),
        typeof(CopilotMessageStream),
        new PropertyMetadata(null, OnItemsSourceChanged));

    /// <summary>Identifies the <see cref="ListBackground" /> dependency property.</summary>
    public static readonly DependencyProperty ListBackgroundProperty = DependencyProperty.Register(
        nameof(ListBackground),
        typeof(Brush),
        typeof(CopilotMessageStream),
        new PropertyMetadata(Brushes.White));

    private ScrollViewer? _scrollViewer;
    private INotifyCollectionChanged? _subscribedCollection;
    private readonly HashSet<INotifyPropertyChanged> _subscribedItems = [];
    private bool _isUserAtBottom = true;

    /// <summary>Initializes a new instance of the <see cref="CopilotMessageStream" /> class.</summary>
    public CopilotMessageStream()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    /// <summary>Gets or sets the messages displayed by the stream.</summary>
    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?) GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>Gets or sets the message-list background.</summary>
    public Brush ListBackground
    {
        get => (Brush) GetValue(ListBackgroundProperty);
        set => SetValue(ListBackgroundProperty, value);
    }

    private static void OnItemsSourceChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        var stream = (CopilotMessageStream) dependencyObject;
        stream.SubscribeItemsSource(e.NewValue as IEnumerable);
        stream.ScrollToEndAndKeep();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _scrollViewer = FindVisualChild<ScrollViewer>(MessageListBox);
        if (_scrollViewer is not null)
        {
            _scrollViewer.ScrollChanged += OnScrollChanged;
        }

        SubscribeItemsSource(ItemsSource);
        ScrollToEndAndKeep();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_scrollViewer is not null)
        {
            _scrollViewer.ScrollChanged -= OnScrollChanged;
            _scrollViewer = null;
        }

        UnsubscribeItemsSource();
    }

    private void SubscribeItemsSource(IEnumerable? itemsSource)
    {
        UnsubscribeItemsSource();
        _subscribedCollection = itemsSource as INotifyCollectionChanged;
        if (_subscribedCollection is not null)
        {
            _subscribedCollection.CollectionChanged += OnCollectionChanged;
        }

        if (itemsSource is not null)
        {
            foreach (var item in itemsSource.OfType<INotifyPropertyChanged>())
            {
                SubscribeItem(item);
            }
        }
    }

    private void UnsubscribeItemsSource()
    {
        if (_subscribedCollection is not null)
        {
            _subscribedCollection.CollectionChanged -= OnCollectionChanged;
            _subscribedCollection = null;
        }

        foreach (var item in _subscribedItems)
        {
            item.PropertyChanged -= OnItemPropertyChanged;
        }

        _subscribedItems.Clear();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (var item in e.OldItems.OfType<INotifyPropertyChanged>())
            {
                UnsubscribeItem(item);
            }
        }

        if (e.NewItems is not null)
        {
            foreach (var item in e.NewItems.OfType<INotifyPropertyChanged>())
            {
                SubscribeItem(item);
            }
        }

        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            SubscribeItemsSource(ItemsSource);
        }

        ScrollToEndIfFollowing();
    }

    private void SubscribeItem(INotifyPropertyChanged item)
    {
        if (_subscribedItems.Add(item))
        {
            item.PropertyChanged += OnItemPropertyChanged;
        }
    }

    private void UnsubscribeItem(INotifyPropertyChanged item)
    {
        if (_subscribedItems.Remove(item))
        {
            item.PropertyChanged -= OnItemPropertyChanged;
        }
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        ScrollToEndIfFollowing();
    }

    private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (_scrollViewer is null)
        {
            return;
        }

        const double tolerance = 2;
        _isUserAtBottom = _scrollViewer.VerticalOffset >= _scrollViewer.ScrollableHeight - tolerance;
        ScrollToBottomButton.Visibility = _isUserAtBottom ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ScrollToBottomButton_OnClick(object sender, RoutedEventArgs e)
    {
        ScrollToEndAndKeep();
    }

    private void ScrollToEndIfFollowing()
    {
        if (_isUserAtBottom)
        {
            ScrollToEndAndKeep();
        }
    }

    private void ScrollToEndAndKeep()
    {
        if (_scrollViewer is null)
        {
            return;
        }

        _isUserAtBottom = true;
        ScrollToBottomButton.Visibility = Visibility.Collapsed;
        _scrollViewer.ScrollToEnd();
        Dispatcher.InvokeAsync(() => _scrollViewer?.ScrollToEnd(), DispatcherPriority.Loaded);
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
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
