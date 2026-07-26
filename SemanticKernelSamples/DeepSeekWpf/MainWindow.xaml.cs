using System.Windows;
using System.ComponentModel;
using System.Windows.Media;
using DeepSeekWpf.ViewModels;
using DeepSeekWpf.Views;

namespace DeepSeekWpf;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private bool _isShutdownComplete;

    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.FocusSearchRequested += OnFocusSearchRequested;
        Closing += OnClosing;
    }

    private void OnFocusSearchRequested(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            FindVisualChild<ChatView>(PageContent)?.FocusSessionSearch();
        });
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match)
            {
                return match;
            }

            var descendant = FindVisualChild<T>(child);
            if (descendant is not null)
            {
                return descendant;
            }
        }

        return null;
    }

    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_isShutdownComplete)
        {
            return;
        }

        e.Cancel = true;
        IsEnabled = false;

        await ((App)Application.Current).RequestShutdownAsync();

        _isShutdownComplete = true;
        Close();
    }
}