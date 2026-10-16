using System.Windows;

namespace ProcesssManager;

public partial class MainWindow : Window
{
    /// <summary>Displays a snapshot of accessible processes and their memory usage.</summary>
    public MainWindow()
    {
        InitializeComponent();
        var processes = ProcessSnapshot.Capture();
        Content = new System.Windows.Controls.DataGrid { ItemsSource = processes, IsReadOnly = true, AutoGenerateColumns = true };
        Title = $"Processes: {processes.Count}, Working set: {processes.Sum(p => p.WorkingSetSize) / 1024d / 1024:N2} MiB";
    }
}
