using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using CoursewarePptxGeneratorWpfDemo.ViewModels;

namespace CoursewarePptxGeneratorWpfDemo;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyPropertyChanged oldViewModel)
        {
            oldViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        if (e.NewValue is INotifyPropertyChanged newViewModel)
        {
            newViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        MoveFocusToCurrentPage();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CoursewareWorkspaceViewModel.CurrentPage))
        {
            MoveFocusToCurrentPage();
        }
    }

    private void MoveFocusToCurrentPage()
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (DataContext is not CoursewareWorkspaceViewModel viewModel)
            {
                return;
            }

            if (viewModel.IsAnalysisPage)
            {
                AnalysisView.FocusPrimaryElement();
            }
            else
            {
                WorkspaceView.FocusPrimaryElement();
            }
        }, DispatcherPriority.Loaded);
    }
}