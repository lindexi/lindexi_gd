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
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using Windows.UI.StartScreen;
using WinRT;

namespace WelhearyalluneaceKujalwhekiraqi
{
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
            var hwnd = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle; //WinRT.Interop.WindowNative.GetWindowHandle(this);
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.FileTypeFilter.Add("*");
            //WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
            //IInitializeWithCoreWindow initializeWithCoreWindow;// 这个不能使用
            var initializeWithCoreWindow = folderPicker.As<IInitializeWithWindow>();
            initializeWithCoreWindow.Initialize(hwnd);

            // Now you can call methods on folderPicker
            var folder = await folderPicker.PickSingleFolderAsync();
            Debug.WriteLine(folder.Path);
        }

        [ComImport]
        [Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IInitializeWithWindow
        {
            void Initialize(IntPtr hwnd);
        }
    }
}
