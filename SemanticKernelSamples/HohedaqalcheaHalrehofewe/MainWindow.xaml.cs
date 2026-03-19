using System.Collections.Specialized;
using System.Windows;
using System.Windows.Threading;

using HohedaqalcheaHalrehofewe.ViewModels;

namespace HohedaqalcheaHalrehofewe;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainWindowViewModel();
        DataContext = _viewModel;
        _viewModel.Timeline.CollectionChanged += OnTimelineCollectionChanged;
    }

    private void OnTimelineCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_viewModel.Timeline.Count == 0)
        {
            return;
        }

        Dispatcher.BeginInvoke(() => TimelineListBox.ScrollIntoView(_viewModel.Timeline[^1]), DispatcherPriority.Background);
    }
}