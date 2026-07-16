using System.Windows;

namespace CakawuhealehaneJairijelanefer;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}