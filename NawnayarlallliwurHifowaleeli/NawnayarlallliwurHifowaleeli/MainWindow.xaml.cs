using PInvoke;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace NawnayarlallliwurHifowaleeli;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var process = Process.GetProcessesByName("WpfApp1").First();

        if (process.MainWindowTitle == "MainWindow")
        {
            while (_contentLoaded)
            {
                User32.SetWindowPos
                (
                    hWnd: process.MainWindowHandle,
                    hWndInsertAfter: IntPtr.Zero,
                    X: Random.Shared.Next(5),
                    Y: Random.Shared.Next(5),
                    cx: 0,
                    cy: 0,
                    uFlags: User32.SetWindowPosFlags.SWP_NOSIZE
                    | User32.SetWindowPosFlags.SWP_NOZORDER
                    | User32.SetWindowPosFlags.SWP_NOACTIVATE
                );
                await Task.Delay(TimeSpan.FromMilliseconds(500));
            }
        }
    }
}
