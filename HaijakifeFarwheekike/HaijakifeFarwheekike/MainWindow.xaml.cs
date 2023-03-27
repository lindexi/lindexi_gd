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
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HaijakifeFarwheekike
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            TextBlock = new TextBlock()
            {
                Margin = new Thickness(10, 10, 10, 10),
                VerticalAlignment = VerticalAlignment.Bottom,
                TextWrapping = TextWrapping.Wrap
            };

            ((Grid) Content).Children.Add(TextBlock);

            SourceInitialized += OnSourceInitialized;
        }

        public static TextBlock TextBlock { private set; get; } = null!;

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            var windowInteropHelper = new WindowInteropHelper(this);
            var hwnd = windowInteropHelper.Handle;

            HwndSource source = HwndSource.FromHwnd(hwnd)!;
            source.AddHook(Hook);
        }

        private IntPtr Hook(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            const int WM_POINTERDOWN = 0x0246;
            const int WM_POINTERUPDATE = 0x0245;

            if (msg == WM_POINTERUPDATE)
            {
                TextBlock.Text += $"Tick={_stopwatch.ElapsedTicks} ms={_stopwatch.ElapsedMilliseconds}\r\n";

                _stopwatch.Restart();
            }

            return IntPtr.Zero;
        }

        private readonly Stopwatch _stopwatch = new Stopwatch();
    }
}
