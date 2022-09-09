using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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

namespace NarlurqaikiLeefallfabe;

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
        while (_contentLoaded)
        {
            try
            {
                var openClipboardWindow = GetOpenClipboardWindow();
                if (openClipboardWindow != IntPtr.Zero)
                {
                    TextBox.Text += "\r\n" + openClipboardWindow.ToInt64().ToString("X");

                    var stringBuilder = new StringBuilder(200);
                    GetWindowText(openClipboardWindow, stringBuilder, 200);

                    var windowTitle = stringBuilder.ToString();
                    if (!string.IsNullOrEmpty(windowTitle))
                    {
                        TextBox.Text += $" Title: {windowTitle}";
                    }

                    GetWindowThreadProcessId(openClipboardWindow, out var processId);

                    try
                    {
                        if (processId != 0)
                        {
                            var process = Process.GetProcessById((int) processId);
                            // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                            if (process != null)
                            {
                                TextBox.Text += $" Process: <{process.Id}> {process.ProcessName}";
                            }
                        }
                    }
                    catch
                    {
                        // 忽略
                    }
                }
            }
            catch
            {
                // 忽略
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern IntPtr GetOpenClipboardWindow();

    [DllImport("user32.dll", SetLastError = true)]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
}
