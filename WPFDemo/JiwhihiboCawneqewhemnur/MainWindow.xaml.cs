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

using Color = System.Drawing.Color;
using Panel = System.Windows.Forms.Panel;
using Rectangle = System.Drawing.Rectangle;

namespace JiwhihiboCawneqewhemnur;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var panel = new Panel()
        {
            Dock = DockStyle.Fill,
        };
        panel.Paint += Panel_Paint;
        WindowsFormsHost.Child = panel;
    }

    private void Panel_Paint(object? sender, PaintEventArgs e)
    {
        Graphics graphics = e.Graphics;
        Rectangle rect = new Rectangle(10, 10, 200, 50);
        Font font = new Font("Arial", 12);

        SolidBrush brush = new SolidBrush(Color.Black);

        StringFormat format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        IntPtr hTheme = NativeMethods.OpenThemeData(IntPtr.Zero, "Button");

        var hr = NativeMethods.DrawThemeTextEx(hTheme, graphics.GetHdc(), 0, 0, "Hello, WPF!", -1, (uint) format.FormatFlags, ref rect, ref format);
        Marshal.ThrowExceptionForHR(hr);
        graphics.ReleaseHdc();
        NativeMethods.CloseThemeData(hTheme);
    }
}

static class NativeMethods
{
    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr OpenThemeData(IntPtr hwnd, string pszClassList);

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    public static extern int DrawThemeTextEx(IntPtr hTheme, IntPtr hdc, int iPartId, int iStateId, string pszText, int iCharCount, uint dwFlags, ref Rectangle pRect, ref StringFormat pOptions);

    [DllImport("uxtheme.dll")]
    public static extern int CloseThemeData(IntPtr hTheme);
}