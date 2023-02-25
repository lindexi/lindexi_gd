using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Windows.Win32;
using Windows.Win32.Foundation;

namespace LaiwurhiroJaqadihawaho;
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

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var windowMessage = PInvoke.RegisterWindowMessage("HwndWrapper.GetGCMemMessage");

        var hwndSource = (HwndSource) PresentationSource.FromVisual(this);
        //var fieldInfo = typeof(HwndSource).GetField("_hwndWrapper",BindingFlags.NonPublic|BindingFlags.Instance);
        //var hwndWrapper = fieldInfo.GetValue(hwndSource);

        var windowInteropHelper = new WindowInteropHelper(this);
        PInvoke.SendMessage(new HWND(windowInteropHelper.Handle), windowMessage, new WPARAM(0), new LPARAM(0));

        var totalMemory = GC.GetTotalMemory(false);
         totalMemory = GC.GetTotalMemory(true);
    }
}
