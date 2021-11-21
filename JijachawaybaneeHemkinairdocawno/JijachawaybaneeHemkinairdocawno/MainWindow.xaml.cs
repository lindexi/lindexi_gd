using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace JijachawaybaneeHemkinairdocawno
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            HandleRef h = new HandleRef(null, new IntPtr(5));

            var dc = IntGetDC(h);
            if (dc == IntPtr.Zero)
            {
                var lastWin32Error = Marshal.GetLastWin32Error();

                var e = new Win32Exception();

                Console.WriteLine(e.NativeErrorCode == lastWin32Error);
            }
        }

        [DllImport("user32.dll", SetLastError = true, ExactSpelling = true, EntryPoint = "GetDC", CharSet = CharSet.Auto)]
        private static extern IntPtr IntGetDC(HandleRef hWnd);
    }
}
