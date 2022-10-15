using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace HineakemnerFeceqerhai;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Dispatcher.ShutdownFinished += Dispatcher_ShutdownFinished;

        Application.Current.Exit += Application_OnExit;
    }

    private void Dispatcher_ShutdownFinished(object? sender, EventArgs e)
    {
        Debugger.Break();
    }

    private void Application_OnExit(object sender, ExitEventArgs e)
    {
        Debugger.Break();
    }

    private void DispatcherInvokeShutdownButton_OnClick(object sender, RoutedEventArgs e)
    {
        Dispatcher.InvokeShutdown();
    }

    private void ApplicationCurrentShutdownButton_OnClick(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}
