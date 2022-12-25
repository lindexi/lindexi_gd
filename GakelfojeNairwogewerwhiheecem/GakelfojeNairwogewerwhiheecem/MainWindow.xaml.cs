using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GakelfojeNairwogewerwhiheecem;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        TextBox.AddHandler(UIElement.MouseDownEvent, new MouseButtonEventHandler(TextBox_OnMouseDown), true);
    }

    private void TextBox_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        ActivatePopup(Popup);
        TextBox.Focus();
    }

    [DllImport("USER32.DLL")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private static void ActivatePopup(Popup popup)
    {
        HwndSource source = (HwndSource) PresentationSource.FromVisual(popup.Child)!;
        IntPtr handle = source.Handle;

        SetForegroundWindow(handle);
    }
}
