using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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

namespace GabagearledeHembijenear
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            SourceInitialized += OnSourceInitialized;

        }

        private void OnSourceInitialized(object sender, EventArgs e)
        {
            var windowInteropHelper = new WindowInteropHelper(this);
            var hwnd = windowInteropHelper.Handle;

            HwndSource source = HwndSource.FromHwnd(hwnd);
            source.AddHook(Hook);
        }

        private unsafe IntPtr Hook(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            if (msg == SingleInstanceApp.SINGLEINSTANCE_APP)
            {
                var lParam = (My_lParam2*)lparam;
                var myLParam = Marshal.PtrToStructure<My_lParam>(lparam);
                //MessageBox.Show(myLParam.Text);
            }

            return IntPtr.Zero;
        }
    }
}
