using System.Diagnostics;
using System.Management;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace YanerehaylemJeekalhebel;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        AppDomain.CurrentDomain.FirstChanceException += (sender, args) =>
        {
            Debug.WriteLine(args.Exception);
        };

        WqlEventQuery insertQuery =
            new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'");
        ManagementEventWatcher insertWatcher = new ManagementEventWatcher(insertQuery);
        insertWatcher.Start(); // When the line `insertWatcher.Start();` is commented out, touching the window hits the breakpoint in the `MainWindow_TouchDown` method. However, after executing `insertWatcher.Start();`, touching the window does not hit the `MainWindow_TouchDown` method.

        TouchDown += MainWindow_TouchDown;
    }

    private void MainWindow_TouchDown(object? sender, TouchEventArgs e)
    {
        Debugger.Break(); // Never hit
    }
}