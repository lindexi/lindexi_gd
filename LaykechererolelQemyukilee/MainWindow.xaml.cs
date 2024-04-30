using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
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

namespace LaykechererolelQemyukilee;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        KeyDown += MainWindow_KeyDown;
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key;
        var virtualKey = KeyInterop.VirtualKeyFromKey(key);

        // MAPVK_VK_TO_VSC 0
        var scanCode = MapVirtualKeyW((uint) virtualKey, 0);

        var scanCodeFromWpf = typeof(KeyEventArgs).GetProperty("ScanCode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(e);

        Debug.Assert(scanCode == (int) scanCodeFromWpf!);
    }

    [DllImport("User32.dll")]
    private static extern uint MapVirtualKeyW(uint code, uint mapType);
}